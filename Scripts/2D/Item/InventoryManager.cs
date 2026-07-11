namespace LAB2D
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// 仓库管理
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        private readonly Dictionary<int, Dictionary<Vector3Int, ResourceInfo>> id2Resource; // 同一个id对应的所有位置
        private readonly Dictionary<Vector3Int, ResourceInfo> posToResource; // 根据pos查资源
        private readonly Dictionary<AWorker, Dictionary<Vector3Int, ResourceInfo>> preTakeResource; // 预申请资源
        private readonly Dictionary<AWorker, Dictionary<Vector3Int, ResourceInfo>> prePlaceResource; // 预放置资源
        private readonly int capacity = 1000; // 单个cell的容量
        private readonly InventoryStackingService stackingService;
        private readonly InventoryFoodReservationService foodReservationService;
        private readonly InventoryTakeReservationService takeReservationService;

        public InventoryManager()
        {
            this.posToResource = new Dictionary<Vector3Int, ResourceInfo>();
            this.id2Resource = new Dictionary<int, Dictionary<Vector3Int, ResourceInfo>>();
            this.preTakeResource = new Dictionary<AWorker, Dictionary<Vector3Int, ResourceInfo>>();
            this.prePlaceResource = new Dictionary<AWorker, Dictionary<Vector3Int, ResourceInfo>>();
            this.stackingService = new InventoryStackingService();
            this.foodReservationService = new InventoryFoodReservationService();
            this.takeReservationService = new InventoryTakeReservationService();
            this.TypeToResource = new Dictionary<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>>();
        }

        /// <summary>
        /// 同一个类型对应的所有位置
        /// </summary>
        public Dictionary<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> TypeToResource { get; set; }

        /// <summary>
        /// 新建仓库时，插入cell
        /// posToResource,idToResource,typeToResource中的ResourceInfo公用
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="width">宽度</param>
        /// <param name="length">高度</param>
        public void AddCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            // 外层字典需要拷贝,由于idTo中仅包含相同id的信息
            if (!this.id2Resource.ContainsKey(-1))
            {
                this.id2Resource.Add(-1, new Dictionary<Vector3Int, ResourceInfo>());
            }

            Dictionary<Vector3Int, ResourceInfo> resources = this.id2Resource[-1];
            if (!this.TypeToResource.ContainsKey(AItem.ItemTypeEnum.Null))
            {
                this.TypeToResource.Add(AItem.ItemTypeEnum.Null, new Dictionary<Vector3Int, ResourceInfo>());
            }

            Dictionary<Vector3Int, ResourceInfo> typeTo = this.TypeToResource[AItem.ItemTypeEnum.Null];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = VectorTool.Add(startPos, i, j);
                    ResourceInfo resourceInfo = new (-1, 0);
                    this.posToResource.Add(pos, resourceInfo);
                    resources.Add(pos, resourceInfo);
                    typeTo.Add(pos, resourceInfo);
                }
            }
        }

        /// <summary>
        /// 得到一个预放置的位置
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>位置</returns>
        public Vector3Int GetPosByPrePlace(AWorker worker)
        {
            if (this.prePlaceResource.ContainsKey(worker))
            {
                return this.prePlaceResource[worker].First().Key;
            }

            LogManager.Instance.Log("没有预放置资源", LogManager.LogLevelEnum.Error);
            return default;
        }

        /// <summary>
        /// 如果足够放置，那么预放置资源
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="resourceInfo">资源信息</param>
        /// <param name="isPre">是否需要预放置</param>
        /// <returns>是否足够</returns>
        public bool IsEnoughAndPrePlace(AWorker worker, ResourceInfo resourceInfo, bool isPre = false)
        {
            // 对于不可堆叠的资源
            if (!ItemDataManager.Instance.GetById(resourceInfo.Id).IsStackable)
            {
                if (this.id2Resource.ContainsKey(-1))
                {
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in this.id2Resource[-1])
                    {
                        // 该位置没有被预放置
                        if (this.IsAreadyPrePlace(cell.Key, resourceInfo.Id))
                        {
                            continue;
                        }

                        if (isPre)
                        {
                            this.PrePlace(worker, cell.Key, resourceInfo);
                        }

                        return true;
                    }
                }

                return false;
            }

            // 对于可以堆叠的资源，先判断是否有相同的资源
            Dictionary<Vector3Int, ResourceInfo> pre = new ();
            int remaining = resourceInfo.Count;

            // 若仓库中存在该id,对应位置的资源数量与该位置预放置资源的数量之和是否超过容量
            if (this.id2Resource.ContainsKey(resourceInfo.Id))
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in this.id2Resource[resourceInfo.Id])
                {
                    int count = this.stackingService.GetAvailableCapacity(
                        this.capacity,
                        cell.Value.Count,
                        this.GetPrePlaceCountByPos(cell.Key));
                    if (count > 0)
                    {
                        // 放置完了
                        if (this.stackingService.CanPlaceAll(remaining, count))
                        {
                            if (isPre)
                            {
                                pre.Add(cell.Key, new ResourceInfo(resourceInfo.Id, remaining));
                                foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre)
                                {
                                    this.PrePlace(worker, pair.Key, pair.Value);
                                }
                            }

                            return true;
                        }

                        // 没有放置完
                        else
                        {
                            if (isPre)
                            {
                                pre.Add(cell.Key, new ResourceInfo(resourceInfo.Id, count));
                            }

                            remaining -= count;
                        }
                    }
                }

                // 该id对应的所有cell满了不能放置资源，需要寻找空的cell
            }

            // 仓库中没有对应id的cell,需要寻找空的cell
            if (!this.id2Resource.ContainsKey(-1))
            {
                // LogManager.Instance.log("仓库满了", LogManager.LogLevel.Error);
                return false;
            }

            // 找到没有预放置的位置
            foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in this.id2Resource[-1])
            {
                // 该位置没有被预放置
                if (!this.IsAreadyPrePlace(cell.Key, resourceInfo.Id))
                {
                    int count = this.stackingService.GetAvailableCapacity(
                        this.capacity,
                        cell.Value.Count,
                        this.GetPrePlaceCountByPos(cell.Key));

                    // 放置完了
                    if (this.stackingService.CanPlaceAll(remaining, count))
                    {
                        if (isPre)
                        {
                            pre.Add(cell.Key, new ResourceInfo(resourceInfo.Id, remaining));
                            foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre)
                            {
                                this.PrePlace(worker, pair.Key, pair.Value);
                            }
                        }

                        return true;
                    }

                    // 没有放置完
                    else
                    {
                        if (isPre)
                        {
                            pre.Add(cell.Key, new ResourceInfo(resourceInfo.Id, count));
                        }

                        remaining -= count;
                    }
                }
            }

            // 有可能被预放置了
            LogManager.Instance.Log("仓库满了", LogManager.LogLevelEnum.Error);
            return false;
        }

        /// <summary>
        /// 判断仓库中是否对应类型的物品，并预申请资源
        /// TODO 没有预取
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="hungry">饥饿值</param>
        /// <param name="isPre">是否预取食物</param>
        /// <returns>是否足够</returns>
        public bool IsEnoughFoodAndPreTake(AWorker worker, float hungry, bool isPre = false)
        {
            if (!this.TypeToResource.ContainsKey(AItem.ItemTypeEnum.Food))
            {
                return false;
            }

            Dictionary<Vector3Int, ResourceInfo> foods = new ();
            foreach (KeyValuePair<Vector3Int, ResourceInfo> food in this.TypeToResource[AItem.ItemTypeEnum.Food])
            {
                float hungry1 = food.Value.Count * 10.0f;

                // 足够吃饱
                if (hungry1 >= hungry)
                {
                    if (isPre)
                    {
                        foods.Add(food.Key, new ResourceInfo(food.Value.Id, (int)(hungry / 10.0f)));
                        foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in foods)
                        {
                            this.PreTake(worker, pair.Key, pair.Value);
                        }
                    }

                    return true;
                }

                // 当前id吃不饱
                else
                {
                    hungry -= hungry1;
                    if (isPre)
                    {
                        foods.Add(food.Key, DataTool.DeepCopyByBinary(food.Value));
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定位置是否有可预取的食物，并预取最多够当前Worker吃饱的数量。
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">食物位置</param>
        /// <param name="hungry">饥饿值缺口</param>
        /// <param name="isPre">是否预取食物</param>
        /// <returns>是否有可吃的食物</returns>
        public bool IsFoodAvailableAndPreTake(AWorker worker, Vector3Int posMap, float hungry, bool isPre = false)
        {
            if (!this.posToResource.ContainsKey(posMap))
            {
                return false;
            }

            ResourceInfo resourceInfo = this.posToResource[posMap];
            if (resourceInfo.Count <= 0 || ItemDataManager.Instance.IdToType(resourceInfo.Id) != AItem.ItemTypeEnum.Food)
            {
                return false;
            }

            int availableCount = resourceInfo.Count - this.GetPreTakeCountByPos(posMap);
            int needCount = this.foodReservationService.GetNeededFoodCount(hungry, 10.0f);
            int preTakeCount = this.foodReservationService.GetPreTakeCount(availableCount, needCount);
            if (preTakeCount <= 0)
            {
                return false;
            }

            if (isPre)
            {
                this.PreTake(worker, posMap, new ResourceInfo(resourceInfo.Id, preTakeCount));
            }

            return true;
        }

        /// <summary>
        /// 是否包含种子并预取
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="isPre">是否预取种子</param>
        /// <returns>位置</returns>
        public Vector3Int IsContainSeedAndPreTake(AWorker worker, bool isPre = false)
        {
            if (!this.TypeToResource.ContainsKey(AItem.ItemTypeEnum.Seed) || this.TypeToResource[AItem.ItemTypeEnum.Seed].Count == 0)
            {
                return default;
            }

            Dictionary<Vector3Int, ResourceInfo>.Enumerator enumerator = this.TypeToResource[AItem.ItemTypeEnum.Seed].GetEnumerator();
            enumerator.MoveNext();
            if (isPre)
            {
                this.PreTake(worker, enumerator.Current.Key, enumerator.Current.Value);
            }

            return enumerator.Current.Key;
        }

        /// <summary>
        /// 添加道具
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void AddItem(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            if (!this.posToResource.ContainsKey(posMap))
            {
                this.posToResource.Add(posMap, resourceInfo);
            }
            else
            {
                if (resourceInfo.Id != this.posToResource[posMap].Id)
                {
                    return;
                }

                this.posToResource[posMap].Count += resourceInfo.Count;
            }

            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, posMap, this.ToString(posMap));
        }

        /// <summary>
        /// 通过预放置添加
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        /// <returns>资源信息</returns>
        public ResourceInfo AddItemByPrePlace(AWorker worker, Vector3Int posMap)
        {
            if (!this.prePlaceResource[worker].ContainsKey(posMap))
            {
                LogManager.Instance.Log("没有预放置资源", LogManager.LogLevelEnum.Error);
                return null;
            }

            ResourceInfo resourceInfo = this.prePlaceResource[worker][posMap];

            // 删除预放置的资源
            this.prePlaceResource[worker].Remove(posMap);

            // 添加到仓库真正的数据
            // 既然已经预放置了，那一定可以放置，不会超出容量
            if (this.posToResource[posMap].Id == -1)
            {
                this.TransferResource(posMap, -1, resourceInfo.Id, AItem.ItemTypeEnum.Null, ItemDataManager.Instance.IdToType(resourceInfo.Id));
            }

            this.posToResource[posMap].Id = resourceInfo.Id;
            this.posToResource[posMap].Count += resourceInfo.Count;
            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, posMap, this.ToString(posMap));
            return resourceInfo;
        }

        /// <summary>
        /// 获取一个预留资源的位置
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>位置</returns>
        public Vector3Int GetPosByPreTake(AWorker worker)
        {
            if (this.preTakeResource.ContainsKey(worker) && this.preTakeResource[worker].Count > 0)
            {
                return this.preTakeResource[worker].First().Key;
            }

            LogManager.Instance.Log("没有预留资源!", LogManager.LogLevelEnum.Warning);
            return default;
        }

        /// <summary>
        /// 通过位置删除所有的道具
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>资源信息</returns>
        public ResourceInfo SubAllItemByPos(Vector3Int posMap)
        {
            if (!this.posToResource.ContainsKey(posMap))
            {
                LogManager.Instance.Log("没有资源，错误", LogManager.LogLevelEnum.Error);
                return null;
            }

            this.TransferResource(posMap, this.posToResource[posMap].Id, -1, ItemDataManager.Instance.IdToType(this.posToResource[posMap].Id), AItem.ItemTypeEnum.Null);
            ResourceInfo resourceInfo = DataTool.DeepCopyByBinary(this.posToResource[posMap]);
            this.posToResource[posMap].Id = -1;
            this.posToResource[posMap].Count = 0;
            ItemMap.Instance.DeleteTile(posMap);
            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, posMap, this.ToString(posMap));
            return resourceInfo;
        }

        /// <summary>
        /// 删除对应数量的道具
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void SubItem(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            if (!this.posToResource.ContainsKey(posMap))
            {
                LogManager.Instance.Log("没有资源，错误", LogManager.LogLevelEnum.Error);
                return;
            }

            this.posToResource[posMap].Count -= resourceInfo.Count;

            // 如果正好取完
            if (this.posToResource[posMap].Count == 0)
            {
                this.TransferResource(posMap, this.posToResource[posMap].Id, -1, ItemDataManager.Instance.IdToType(this.posToResource[posMap].Id), AItem.ItemTypeEnum.Null);
                ItemMap.Instance.DeleteTile(posMap);

                // 食物被吃完删除任务
                if (ItemDataManager.Instance.IdToType(this.posToResource[posMap].Id) == AItem.ItemTypeEnum.Food)
                {
                    WorkerTaskManager.Instance.DeleteHungryTask(posMap);
                }

                this.posToResource[posMap].Id = -1;
            }

            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, posMap, this.ToString(posMap));
        }

        /// <summary>
        /// 根据预取的资源删除仓库中的库存
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        /// <returns>返回从仓库中扣减的数量(预取的资源)</returns>
        public ResourceInfo SubItemByPreTake(AWorker worker, Vector3Int posMap)
        {
            if (!this.preTakeResource.ContainsKey(worker) || !this.preTakeResource[worker].ContainsKey(posMap))
            {
                LogManager.Instance.Log("没有预取资源", LogManager.LogLevelEnum.Error);
                return null;
            }

            ResourceInfo resourceInfo = this.preTakeResource[worker][posMap];

            // 删除预取的资源
            this.preTakeResource[worker].Remove(posMap);
            if (this.preTakeResource[worker].Count == 0)
            {
                this.preTakeResource.Remove(worker);
            }

            // 减少仓库真正的数据
            this.posToResource[posMap].Count -= resourceInfo.Count;

            // 如果正好取完
            if (this.posToResource[posMap].Count <= 0)
            {
                this.TransferResource(posMap, this.posToResource[posMap].Id, -1, ItemDataManager.Instance.IdToType(this.posToResource[posMap].Id), AItem.ItemTypeEnum.Null);
                ItemMap.Instance.DeleteTile(posMap);

                // 食物被吃完删除任务
                if (ItemDataManager.Instance.IdToType(this.posToResource[posMap].Id) == AItem.ItemTypeEnum.Food)
                {
                    WorkerTaskManager.Instance.DeleteHungryTask(posMap);
                }

                this.posToResource[posMap].Id = -1;
            }

            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, posMap, this.ToString(posMap));

            // 不够，既然我已经预取了，那说明肯定是够的
            return resourceInfo;
        }

        /// <summary>
        /// 看是否足够，若足够则预申请资源，按照worker可携带最大资源预取
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="needResource">需要的资源</param>
        /// <param name="isPre">是否预取资源</param>
        /// <returns>是否足够</returns>
        public bool IsEnoughAndPreTake(AWorker worker, Dictionary<int, ResourceInfo> needResource, bool isPre = false)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (this.id2Resource.ContainsKey(need.Key))
                {
                    int count = 0;
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> resource in this.id2Resource[need.Key])
                    {
                        count += resource.Value.Count;
                    }

                    // id对应的总数量减去预取的资源数量，小于需求数量，不满足
                    if (count - this.GetPreTakeCountById(need.Key) < need.Value.Count)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // 预申请资源
            if (isPre)
            {
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                foreach (KeyValuePair<int, ResourceInfo> need in needResource)
                {
                    // 每个Cell预取完之后剩余Cell可预取的数量,至少取need.Value.count
                    int remaining = this.takeReservationService.GetTargetTakeCount(
                        need.Value.Count,
                        workerData.MaxResourceCount);

                    // 按照Worker携带的最大值预取,如果不够最大值就取完所有资源
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> resource in this.id2Resource[need.Key])
                    {
                        int count = this.takeReservationService.GetAvailableTakeCount(
                            resource.Value.Count,
                            this.GetPreTakeCountByPos(resource.Key));
                        if (count < remaining)
                        {
                            remaining -= count;
                            this.PreTake(worker, resource.Key, new ResourceInfo(need.Key, count));
                        }
                        else
                        {
                            // 当前id取够了，不需要再取了
                            this.PreTake(worker, resource.Key, new ResourceInfo(need.Key, remaining));
                            break;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 当在仓库中点击武器或者装备时，显示需要穿戴的Worker列表
        /// </summary>
        /// <param name="pos">位置</param>
        public void ShowWearMenu(Vector3Int pos)
        {
            AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(this.posToResource[pos].Id);
            if (itemType == AItem.ItemTypeEnum.Weapon || itemType == AItem.ItemTypeEnum.Equipment)
            {
                AddWearTaskUI.Instance.ShowWearTask(pos);
            }
        }

        /// <summary>
        /// 仓库信息
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int pos)
        {
            if (!this.posToResource.ContainsKey(pos))
            {
                return string.Empty;
            }

            ResourceInfo resourceInfo = this.posToResource[pos];
            ItemData itemData = ItemDataManager.Instance.GetById(resourceInfo.Id);
            string text;
            if (itemData != null)
            {
                text = $"id:{resourceInfo.Id}\n" +
                    $"name:{itemData.CnName}\n" +
                    $"type:{itemData.Type}\n" +
                    $"count:{resourceInfo.Count}\n" +
                    $"info:{itemData.Info}\n" +
                    $"isStackable:{itemData.IsStackable}\n";

                AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(resourceInfo.Id);
                if (itemType == AItem.ItemTypeEnum.Weapon || itemType == AItem.ItemTypeEnum.Equipment)
                {
                    text += $"equipSlot:{itemData.EquipSlot}\n";
                }
            }
            else
            {
                text = $"id:{resourceInfo.Id}\n" +
                    $"count:{resourceInfo.Count}\n";
            }

            text += $"prePlace:\n";
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    text += prePlace.Key.name + ":\n"
                        + "    " + prePlace.Value[pos].Id + " " + prePlace.Value[pos].Count + "\n";
                }
            }

            text += "preTake:\n";
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> preTake in this.preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    text += preTake.Key.name + ":\n"
                        + "    " + preTake.Value[pos].Id + " " + preTake.Value[pos].Count + "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// 通过位置获取资源
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>资源</returns>
        public ResourceInfo GetResourceByPos(Vector3Int posMap)
        {
            if (this.posToResource.ContainsKey(posMap))
            {
                return this.posToResource[posMap];
            }

            return null;
        }

        /// <summary>
        /// 删除Worker预设(Worker死亡)
        /// </summary>
        /// <param name="worker">Worker</param>
        public void DeleteWorkerPre(AWorker worker)
        {
            if (this.prePlaceResource.ContainsKey(worker))
            {
                this.prePlaceResource.Remove(worker);
            }

            if (this.preTakeResource.ContainsKey(worker))
            {
                this.preTakeResource.Remove(worker);
            }
        }

        /// <summary>
        /// 通过pos获取预放置资源的数量
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>预放置的数量</returns>
        private int GetPrePlaceCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    count += prePlace.Value.Count;
                }
            }

            return count;
        }

        /// <summary>
        /// 预放置资源，不管能不能在超出容量之前放下
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="pos">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        private void PrePlace(AWorker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            if (this.prePlaceResource.ContainsKey(worker))
            {
                if (this.prePlaceResource[worker].ContainsKey(pos))
                {
                    this.prePlaceResource[worker][pos].Count += resourceInfo.Count;
                    ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, pos, this.ToString(pos));
                    return;
                }

                this.prePlaceResource[worker].Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
                ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, pos, this.ToString(pos));
                return;
            }

            Dictionary<Vector3Int, ResourceInfo> dict = new ();
            dict.Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
            this.prePlaceResource.Add(worker, dict);
            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, pos, this.ToString(pos));
        }

        /// <summary>
        /// 该位置是否有其他id已经预放置
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="id">ID</param>
        /// <returns>是否已经预放置过了</returns>
        private bool IsAreadyPrePlace(Vector3Int pos, int id)
        {
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                // 其他的id已经预放置了，换下一个Cell
                if (prePlace.Value.ContainsKey(pos) && prePlace.Value[pos].Id != id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 通过id获取预放置资源的数量
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>预放置的数量</returns>
        private int GetPrePlaceCountById(int id)
        {
            int count = 0;
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pre in prePlace.Value)
                {
                    if (pre.Value.Id == id)
                    {
                        count += pre.Value.Count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 预取资源,没有考虑超过容量，所以封装为isEnough
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="pos">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        private void PreTake(AWorker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            if (!this.preTakeResource.ContainsKey(worker))
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new ();
                dict.Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
                this.preTakeResource.Add(worker, dict);
                ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, pos, this.ToString(pos));
                return;
            }

            if (!this.preTakeResource[worker].ContainsKey(pos))
            {
                this.preTakeResource[worker].Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
                ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, pos, this.ToString(pos));
                return;
            }

            this.preTakeResource[worker][pos].Count += resourceInfo.Count;
            ItemInfoUI.Instance.UpdateInfo(this.GetType().Name, pos, this.ToString(pos));
        }

        private int GetPreTakeCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> preTake in this.preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    count += preTake.Value[pos].Count;
                }
            }

            DebugUI.Instance.UpdateInfo(pos + " " + count);
            return count;
        }

        private int GetPreTakeCountById(int id)
        {
            int count = 0;
            foreach (KeyValuePair<AWorker, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.preTakeResource)
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pre in prePlace.Value)
                {
                    if (pre.Value.Id == id)
                    {
                        count += pre.Value.Count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 更新idToResource,typeToResource
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="oldId">旧的ID</param>
        /// <param name="newId">新的ID</param>
        /// <param name="oldType">旧的类型</param>
        /// <param name="newType">新的类型</param>
        private void TransferResource(Vector3Int pos, int oldId, int newId, AItem.ItemTypeEnum oldType, AItem.ItemTypeEnum newType)
        {
            // idToResource
            this.id2Resource[oldId].Remove(pos);
            if (this.id2Resource.ContainsKey(newId))
            {
                this.id2Resource[newId].Add(pos, this.posToResource[pos]);
            }
            else
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new ();
                dict.Add(pos, this.posToResource[pos]);
                this.id2Resource.Add(newId, dict);
            }

            // typeToResource
            this.TypeToResource[oldType].Remove(pos);
            if (this.TypeToResource.ContainsKey(newType))
            {
                this.TypeToResource[newType].Add(pos, this.posToResource[pos]);
            }
            else
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new ();
                dict.Add(pos, this.posToResource[pos]);
                this.TypeToResource.Add(newType, dict);
            }
        }
    }
}
