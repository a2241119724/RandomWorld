using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LAB2D
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        /// <summary>
        /// 同一个类型对应的所有位置
        /// </summary>
        public Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>> TypeToResource { get; set; }

        /// <summary>
        /// 根据pos查资源
        /// </summary>
        private Dictionary<Vector3Int, ResourceInfo> posToResource;
        /// <summary>
        /// 同一个id对应的所有位置
        /// </summary>
        private Dictionary<int, Dictionary<Vector3Int, ResourceInfo>> idToResource;
        /// <summary>
        /// 预申请资源
        /// </summary>
        private Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>> preTakeResource;
        /// <summary>
        /// 预放置资源
        /// </summary>
        private Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlaceResource;
        /// <summary>
        /// 单个cell的容量
        /// </summary>
        private int capacity = 1000;

        public InventoryManager() {
            posToResource = new Dictionary<Vector3Int, ResourceInfo>();
            idToResource = new Dictionary<int, Dictionary<Vector3Int, ResourceInfo>>();
            preTakeResource = new Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>>();
            prePlaceResource = new Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>>();
            TypeToResource = new Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>>();
        }

        /// <summary>
        /// 新建仓库时，插入cell
        /// posToResource,idToResource,typeToResource中的ResourceInfo公用
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        public void addCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            // 外层字典需要拷贝,由于idTo中仅包含相同id的信息
            if(!idToResource.ContainsKey(-1))
            {
                idToResource.Add(-1, new Dictionary<Vector3Int, ResourceInfo>());
            }
            Dictionary<Vector3Int, ResourceInfo> idTo = idToResource[-1];
            if(!TypeToResource.ContainsKey(ItemType.Null))
            {
                TypeToResource.Add(ItemType.Null, new Dictionary<Vector3Int, ResourceInfo>());
            }
            Dictionary<Vector3Int, ResourceInfo> typeTo = TypeToResource[ItemType.Null];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = Tool.add(startPos, i, j);
                    ResourceInfo resourceInfo = new ResourceInfo(-1, 0);
                    posToResource.Add(pos, resourceInfo);
                    idTo.Add(pos, resourceInfo);
                    typeTo.Add(pos, resourceInfo);
                }
            }
        }

        /// <summary>
        /// 得到一个预放置的位置
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public Vector3Int getPosByPrePlace(Worker worker)
        {
            if (prePlaceResource.ContainsKey(worker))
            {
                return prePlaceResource[worker].First().Key;
            }
            LogManager.Instance.log("没有预放置资源", LogManager.LogLevel.Error);
            return default;
        }

        /// <summary>
        /// 如果足够放置，那么预放置资源
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="resourceInfo"></param>
        /// <returns></returns>
        public bool isEnoughAndPrePlace(Worker worker, ResourceInfo resourceInfo, bool isPre = false)
        {
            // 对于不可堆叠的资源
            if (!ItemDataManager.Instance.getById(resourceInfo.id).isStackable)
            {
                if (idToResource.ContainsKey(-1))
                {
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in idToResource[-1])
                    {
                        // 该位置没有被预放置
                        if (isAreadyPrePlace(cell.Key, resourceInfo.id)) continue;
                        if (isPre)
                        {
                            prePlace(worker, cell.Key, resourceInfo);
                        }
                        return true;
                    }
                }
                return false;
            }
            // 对于可以堆叠的资源，先判断是否有相同的资源
            Dictionary<Vector3Int, ResourceInfo> pre = new Dictionary<Vector3Int, ResourceInfo>();
            int remaining = resourceInfo.count;
            // 若仓库中存在该id,对应位置的资源数量与该位置预放置资源的数量之和是否超过容量
            if (idToResource.ContainsKey(resourceInfo.id))
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in idToResource[resourceInfo.id])
                {
                    if (cell.Value.count + getPrePlaceCountByPos(cell.Key) < capacity)
                    {
                        int count = capacity - (cell.Value.count + getPrePlaceCountByPos(cell.Key));
                        // 放置完了
                        if (remaining <= count)
                        {
                            if (isPre)
                            {
                                pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, remaining));
                                foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre)
                                {
                                    prePlace(worker, pair.Key, pair.Value);
                                }
                            }
                            return true;
                        }
                        // 没有放置完
                        else
                        {
                            if (isPre)
                            {
                                pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, count));
                            }
                            remaining -= count;
                        }
                    }
                }
                // 该id对应的所有cell满了不能放置资源，需要寻找空的cell
            }
            // 仓库中没有对应id的cell,需要寻找空的cell
            if (!idToResource.ContainsKey(-1))
            {
                //LogManager.Instance.log("仓库满了", LogManager.LogLevel.Error);
                return false;
            }
            // 找到没有预放置的位置
            foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in idToResource[-1])
            {
                // 该位置没有被预放置
                if (!isAreadyPrePlace(cell.Key, resourceInfo.id))
                {
                    int count = capacity - (cell.Value.count + getPrePlaceCountByPos(cell.Key));
                    // 放置完了
                    if (remaining <= count)
                    {
                        if (isPre)
                        {
                            pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, remaining));
                            foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre)
                            {
                                prePlace(worker, pair.Key, pair.Value);
                            }
                        }
                        return true;
                    }
                    // 没有放置完
                    else
                    {
                        if (isPre)
                        {
                            pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, count));
                        }
                        remaining -= count;
                    }
                }
            }
            // 有可能被预放置了
            LogManager.Instance.log("仓库满了", LogManager.LogLevel.Error);
            return false;
        }

        /// <summary>
        /// 该位置是否有其他id已经预放置
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool isAreadyPrePlace(Vector3Int pos, int id)
        {
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource)
            {
                // 其他的id已经预放置了，换下一个Cell
                if (prePlace.Value.ContainsKey(pos) && prePlace.Value[pos].id != id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 通过id获取预放置资源的数量
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private int getPrePlaceCountById(int id) {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource) {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pre in prePlace.Value)
                {
                    if (pre.Value.id == id)
                    {
                        count += pre.Value.count;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 通过pos获取预放置资源的数量
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int getPrePlaceCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource)
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
        /// <param name="worker"></param>
        /// <param name="pos"></param>
        /// <param name="resourceInfo"></param>
        private void prePlace(Worker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            if (prePlaceResource.ContainsKey(worker))
            {
                if (prePlaceResource[worker].ContainsKey(pos))
                {
                    prePlaceResource[worker][pos].count += resourceInfo.count;
                    ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                    return;
                }
                prePlaceResource[worker].Add(pos, Tool.DeepCopyByBinary(resourceInfo));
                ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                return;
            }
            Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
            dict.Add(pos, Tool.DeepCopyByBinary(resourceInfo));
            prePlaceResource.Add(worker, dict);
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
        }

        /// <summary>
        /// 判断仓库中是否对应类型的物品，并预申请资源
        /// TODO没有预取
        /// </summary>
        /// <returns></returns>
        public bool isEnoughFoodAndPreTake(Worker worker, float hungry, bool isPre = false) {
            Dictionary<Vector3Int, ResourceInfo> foods = new Dictionary<Vector3Int, ResourceInfo>();
            foreach (KeyValuePair<Vector3Int, ResourceInfo> food in TypeToResource[ItemType.Food])
            {
                float _hungry = food.Value.count * 10.0f;
                // 足够吃饱
                if (_hungry >= hungry)
                {
                    if (isPre)
                    {
                        foods.Add(food.Key, new ResourceInfo(food.Value.id, (int)(hungry / 10.0f)));
                        foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in foods)
                        {
                            preTake(worker, pair.Key, pair.Value);
                        }
                    }
                    return true;
                }
                // 当前id吃不饱
                else
                {
                    hungry -= _hungry;
                    if (isPre)
                    {
                        foods.Add(food.Key, Tool.DeepCopyByBinary(food.Value));
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 是否包含种子并预取
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="isPre"></param>
        /// <returns></returns>
        public Vector3Int isContainSeedAndPreTake(Worker worker, bool isPre=false)
        {
            if(!TypeToResource.ContainsKey(ItemType.Seed) || TypeToResource[ItemType.Seed].Count == 0) return default;
            Dictionary<Vector3Int, ResourceInfo>.Enumerator enumerator = TypeToResource[ItemType.Seed].GetEnumerator();
            enumerator.MoveNext();
            if (isPre)
            {
                preTake(worker, enumerator.Current.Key, enumerator.Current.Value);
            }
            return enumerator.Current.Key;
        }

        /// <summary>
        /// 预取资源,没有考虑超过容量，所以封装为isEnough
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="pos"></param>
        /// <param name="resourceInfo"></param>
        private void preTake(Worker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            if (!preTakeResource.ContainsKey(worker))
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
                dict.Add(pos, Tool.DeepCopyByBinary(resourceInfo));
                preTakeResource.Add(worker, dict);
                ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                return;
            }
            if (!preTakeResource[worker].ContainsKey(pos))
            {
                preTakeResource[worker].Add(pos, Tool.DeepCopyByBinary(resourceInfo));
                ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                return;
            }
            preTakeResource[worker][pos].count += resourceInfo.count;
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
        }

        public void addItem(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            if (!posToResource.ContainsKey(posMap))
            {
                posToResource.Add(posMap, resourceInfo);
            }
            else
            {
                if (resourceInfo.id != posToResource[posMap].id) return;
                posToResource[posMap].count += resourceInfo.count;
            }
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, posMap, ToString(posMap));
        }

        /// <summary>
        /// 通过预放置添加
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="pos"></param>
        /// <param name="resourceInfo"></param>
        public ResourceInfo addItemByPrePlace(Worker worker, Vector3Int posMap)
        {
            if (!prePlaceResource[worker].ContainsKey(posMap))
            {
                LogManager.Instance.log("没有预放置资源", LogManager.LogLevel.Error);
                return null;
            }
            ResourceInfo resourceInfo = prePlaceResource[worker][posMap];
            // 删除预放置的资源
            prePlaceResource[worker].Remove(posMap);
            // 添加到仓库真正的数据
            // 既然已经预放置了，那一定可以放置，不会超出容量
            if (posToResource[posMap].id == -1)
            {
                transferResource(posMap, -1, resourceInfo.id, ItemType.Null, ItemDataManager.Instance.getTypeById(resourceInfo.id));
            }
            posToResource[posMap].id = resourceInfo.id;
            posToResource[posMap].count += resourceInfo.count;
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, posMap, ToString(posMap));
            return resourceInfo;
        }

        /// <summary>
        /// 获取一个预留资源的位置
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Vector3Int getPosByPreTake(Worker worker) {
            if (preTakeResource.ContainsKey(worker))
            {
                return preTakeResource[worker].First().Key;
            }
            LogManager.Instance.log("没有预留资源", LogManager.LogLevel.Error);
            return default;
        }

        private int getPreTakeCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> preTake in preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    count += preTake.Value[pos].count;
                }
            }
            DebugUI.Instance.updateTaskInfo(pos + " " + count);
            return count;
        }

        private int getPreTakeCountById(int id)
        {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in preTakeResource)
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pre in prePlace.Value)
                {
                    if (pre.Value.id == id)
                    {
                        count += pre.Value.count;
                    }
                }
            }
            return count;
        }

        public ResourceInfo subAllItemByPos(Vector3Int posMap)
        {
            if (!posToResource.ContainsKey(posMap))
            {
                LogManager.Instance.log("没有资源，错误", LogManager.LogLevel.Error);
                return null;
            }
            transferResource(posMap, posToResource[posMap].id, -1, ItemDataManager.Instance.getTypeById(posToResource[posMap].id), ItemType.Null);
            ResourceInfo resourceInfo = Tool.DeepCopyByBinary(posToResource[posMap]);
            posToResource[posMap].id = -1;
            posToResource[posMap].count = 0;
            ItemMap.Instance.hindTile(posMap);
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, posMap, ToString(posMap));
            return resourceInfo;
        }

        public void subItem(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            if (!posToResource.ContainsKey(posMap))
            {
                LogManager.Instance.log("没有资源，错误", LogManager.LogLevel.Error);
                return;
            }
            posToResource[posMap].count -= resourceInfo.count;
            // 如果正好取完
            if (posToResource[posMap].count == 0)
            {
                transferResource(posMap, posToResource[posMap].id, -1, ItemDataManager.Instance.getTypeById(posToResource[posMap].id), ItemType.Null);
                ItemMap.Instance.hindTile(posMap);
                // 食物被吃完删除任务
                if (ItemDataManager.Instance.getTypeById(posToResource[posMap].id) == ItemType.Food)
                {
                    WorkerTaskManager.Instance.deleteHungryTask(posMap);
                }
                posToResource[posMap].id = -1;
            }
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, posMap, ToString(posMap));
        }

        /// <summary>
        /// 根据预取的资源删除仓库中的库存
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="posMap"></param>
        /// <returns>返回从仓库中扣减的数量(预取的资源)</returns>
        public ResourceInfo subItemByPreTake(Worker worker, Vector3Int posMap) {
            if (!preTakeResource[worker].ContainsKey(posMap))
            {
                LogManager.Instance.log("没有预取资源", LogManager.LogLevel.Error);
                return null;
            }
            ResourceInfo resourceInfo = preTakeResource[worker][posMap];
            // 删除预取的资源
            preTakeResource[worker].Remove(posMap);
            // 减少仓库真正的数据
            posToResource[posMap].count -= resourceInfo.count;
            // 如果正好取完
            if (posToResource[posMap].count == 0)
            {
                transferResource(posMap, posToResource[posMap].id, -1, ItemDataManager.Instance.getTypeById(posToResource[posMap].id), ItemType.Null);
                ItemMap.Instance.hindTile(posMap);
                // 食物被吃完删除任务
                if (ItemDataManager.Instance.getTypeById(posToResource[posMap].id) == ItemType.Food)
                {
                    WorkerTaskManager.Instance.deleteHungryTask(posMap);
                }
                posToResource[posMap].id = -1;
            }
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, posMap, ToString(posMap));
            // 不够，既然我已经预取了，那说明肯定是够的
            return resourceInfo;
        }

        /// <summary>
        /// 更新idToResource,typeToResource
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="oldId"></param>
        /// <param name="newId"></param>
        /// <param name="oldType"></param>
        /// <param name="newType"></param>
        private void transferResource(Vector3Int pos, int oldId, int newId, ItemType oldType, ItemType newType) {
            // idToResource
            idToResource[oldId].Remove(pos);
            if (idToResource.ContainsKey(newId))
            {
                idToResource[newId].Add(pos, posToResource[pos]);
            }
            else
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
                dict.Add(pos, posToResource[pos]);
                idToResource.Add(newId, dict);
            }
            // typeToResource
            TypeToResource[oldType].Remove(pos);
            if (TypeToResource.ContainsKey(newType))
            {
                TypeToResource[newType].Add(pos, posToResource[pos]);
            }
            else
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
                dict.Add(pos, posToResource[pos]);
                TypeToResource.Add(newType, dict);
            }
        }

        /// <summary>
        /// 看是否足够，若足够则预申请资源，按照worker可携带最大资源预取
        /// </summary>
        /// <param name="needResource"></param>
        /// <param name="maxValue">最大申请资源的数量</param>
        /// <returns></returns>
        public bool isEnoughAndPreTake(Worker worker, Dictionary<int, ResourceInfo> needResource, bool isPre = false)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (idToResource.ContainsKey(need.Key))
                {
                    int count = 0;
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> resource in idToResource[need.Key])
                    {
                        count += resource.Value.count;
                    }
                    // id对应的总数量减去预取的资源数量，小于需求数量，不满足
                    if (count - getPreTakeCountById(need.Key) < need.Value.count)
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
                foreach (KeyValuePair<int, ResourceInfo> need in needResource)
                {
                    // 每个Cell预取完之后剩余Cell可预取的数量,至少取need.Value.count
                    int remaining = Mathf.Max(need.Value.count, worker.MaxResourceCount);
                    // 按照Worker携带的最大值预取,如果不够最大值就取完所有资源
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> resource in idToResource[need.Key])
                    {
                        int count = resource.Value.count - getPreTakeCountByPos(resource.Key);
                        if (count < remaining)
                        {
                            remaining -= count;
                            preTake(worker, resource.Key, new ResourceInfo(need.Key, count));
                        }
                        else
                        {
                            // 当前id取够了，不需要再取了
                            preTake(worker, resource.Key, new ResourceInfo(need.Key, remaining));
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
        /// <param name="pos"></param>
        public void showWearMenu(Vector3Int pos)
        {
            ItemType itemType = ItemDataManager.Instance.getTypeById(posToResource[pos].id);
            if (itemType == ItemType.Weapon || itemType == ItemType.Equipment) 
            { 
                WearTaskUI.Instance.showWearTask(pos);
            }
        }

        public string ToString(Vector3Int pos)
        {
            if (!posToResource.ContainsKey(pos)) return "";
            ResourceInfo resourceInfo = posToResource[pos];
            string text = $"id:{resourceInfo.id}\n" +
                $"count:{resourceInfo.count}\n" +
                $"prePlace:\n";
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    text += prePlace.Key.name + ":\n"
                        + "    " + prePlace.Value[pos].id + " " + prePlace.Value[pos].count + "\n";
                }
            }
            text += "preTake:\n";
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> preTake in preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    text += preTake.Key.name + ":\n"
                        + "    " + preTake.Value[pos].id + " " + preTake.Value[pos].count + "\n";
                }
            }
            return text;
        }

        public ResourceInfo getByPos(Vector3Int posMap)
        {
            if (posToResource.ContainsKey(posMap))
            {
                return posToResource[posMap];
            }
            return null;
        }
    }
}
