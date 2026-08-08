namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Inventory;
    using LAB2D.UnityAdapter;
    using LAB2D.Item;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// 仓库管理（Singleton）。
    ///
    /// 本轮改造（v2）：
    ///   - 内部数据存储已从 3 个并行 Dictionary 迁移到 Domain/InventoryService（包装 InventoryGrid）
    ///   - posToResource / id2Resource / TypeToResource 不再作为独立字典维护，
    ///     所有物品数据操作委托给 InventoryService，由 InventoryGrid 统一管理位置索引
    ///   - preTakeResource / prePlaceResource 的 key 已从 AWorker (MonoBehaviour) 迁移为 int (worker.GetInstanceID())
    ///   - 所有 public API 签名保持不变，调用方无需修改
    ///   - Vector3Int ↔ GameGridPosition 转换在方法边界通过 UnityVectorAdapter 完成
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        internal static System.Action<IGameEvent> EventBusPublishProvider { get; set; }
            = (e) => ServiceLocator.Get<EventBus>().PublishInternal(e);
        internal static System.Action<Vector3Int> ShowWearTaskProvider { get; set; }
            = (pos) => ServiceLocator.Get<AddWearTaskUI>().ShowWearTask(pos);

        /// <summary>
        /// Worker 名称提供者 — 根据 worker instance ID 返回 Worker 的 GameObject 名称。
        /// 默认实现通过 WorkerManager 查询；可在测试中替换为桩。
        /// </summary>
        internal static System.Func<int, string> WorkerNameProvider { get; set; }
            = (workerId) =>
            {
                if (Core.ServiceLocator.TryGet(out WorkerManager wm))
                {
                    foreach (AWorker w in wm.Characters)
                    {
                        if (w != null && w.GetInstanceID() == workerId)
                        {
                            return w.name;
                        }
                    }
                }

                return $"worker_{workerId}";
            };

        // ---- v2: 纯数据操作委托给 Domain InventoryService ----
        private InventoryService inventoryService;

        // ---- Worker 相关的预留字典（v3: key 已从 AWorker 迁移为 int workerId） ----
        private readonly Dictionary<int, Dictionary<Vector3Int, ResourceInfo>> preTakeResource; // 预申请资源
        private readonly Dictionary<int, Dictionary<Vector3Int, ResourceInfo>> prePlaceResource; // 预放置资源

        /// <summary>仓库物品所有权 — 位置 → OwnerId</summary>
        private readonly Dictionary<Vector3Int, int> cellOwners;

        private readonly int capacity = 1000; // 单个cell的容量

        public InventoryManager()
        {
            this.preTakeResource = new Dictionary<int, Dictionary<Vector3Int, ResourceInfo>>();
            this.prePlaceResource = new Dictionary<int, Dictionary<Vector3Int, ResourceInfo>>();
            this.cellOwners = new Dictionary<Vector3Int, int>();
        }

        /// <summary>
        /// 初始化库存服务（延迟初始化，在 AddCells 时完成）。
        /// 支持重复调用以重建网格（如地图变更时）。
        /// </summary>
        private void EnsureInventoryService(int width, int height)
        {
            if (this.inventoryService == null)
            {
                this.inventoryService = new InventoryService(
                    managerName: this.GetType().Name,
                    gridWidth: width,
                    gridHeight: height,
                    cellCapacity: this.capacity,
                    itemTypeResolver: (itemId) => (int)AWorkerTask.ItemTypeProvider(itemId),
                    eventPublisher: (e) => EventBusPublishProvider(e));
            }
        }

        /// <summary>
        /// 同一个类型对应的所有位置。
        /// [v2] 已废弃：改为通过 InventoryService.GetPositionsByType() 查询。
        /// 保留属性以兼容旧调用方，但返回的是实时查询结果（每次访问新建 Dictionary，性能劣于旧实现）。
        /// 建议新代码直接调用 GetPositionsByType()。
        /// </summary>
        public Dictionary<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> TypeToResource
        {
            get
            {
                var result = new Dictionary<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>>();
                if (this.inventoryService == null)
                {
                    return result;
                }

                // 遍历所有已知物品类型
                foreach (AItem.ItemTypeEnum itemType in System.Enum.GetValues(typeof(AItem.ItemTypeEnum)))
                {
                    var positions = this.inventoryService.GetPositionsByType((int)itemType);
                    if (positions.Count > 0)
                    {
                        var dict = new Dictionary<Vector3Int, ResourceInfo>();
                        foreach (GameGridPosition pos in positions)
                        {
                            ResourceInfo info = this.inventoryService.GetResourceInfo(pos);
                            if (info != null && info.Id >= 0)
                            {
                                dict[UnityVectorAdapter.ToVector3Int(pos)] = info;
                            }
                        }

                        if (dict.Count > 0)
                        {
                            result[itemType] = dict;
                        }
                    }
                }

                return result;
            }
            set
            {
                // [v2] 不再支持直接设置 TypeToResource。
                // 设置操作被忽略，数据源已迁移至 InventoryService。
                // 如需批量初始化，请使用 AddCells()。
            }
        }

        /// <summary>
        /// 新建仓库时，插入cell。
        /// [v2] 初始化 InventoryService 网格。
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="width">宽度</param>
        /// <param name="length">高度</param>
        public void AddCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            this.EnsureInventoryService(width, length);

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = VectorTool.Add(startPos, i, j);
                    GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(pos);
                    this.inventoryService.EnsureCell(gridPos, this.capacity);
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
            int workerId = worker.GetInstanceID();
            if (this.prePlaceResource.ContainsKey(workerId))
            {
                return this.prePlaceResource[workerId].First().Key;
            }

            AWorkerTask.LogProvider("没有预放置资源", LogManager.LogLevelEnum.Error);
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
            if (this.inventoryService == null)
            {
                return false;
            }

            // 对于不可堆叠的资源
            if (!AWorkerTask.ItemDataProvider(resourceInfo.Id).IsStackable)
            {
                // 查找空格子（id=-1）
                var emptyPositions = this.inventoryService.GetPositionsById(-1);
                if (emptyPositions.Count > 0)
                {
                    foreach (GameGridPosition pos in emptyPositions)
                    {
                        if (this.IsAreadyPrePlace(UnityVectorAdapter.ToVector3Int(pos), resourceInfo.Id))
                        {
                            continue;
                        }

                        if (isPre)
                        {
                            this.PrePlace(worker, UnityVectorAdapter.ToVector3Int(pos), resourceInfo);
                        }

                        return true;
                    }
                }

                return false;
            }

            // 对于可以堆叠的资源，先判断是否有相同的资源
            Dictionary<Vector3Int, ResourceInfo> pre = new();
            int remaining = resourceInfo.Count;

            // 若仓库中存在该id
            var sameIdPositions = this.inventoryService.GetPositionsById(resourceInfo.Id);
            if (sameIdPositions.Count > 0)
            {
                foreach (GameGridPosition pos in sameIdPositions)
                {
                    Vector3Int unityPos = UnityVectorAdapter.ToVector3Int(pos);
                    InventoryCell cell = this.inventoryService.GetCell(pos);
                    if (cell == null)
                    {
                        continue;
                    }

                    int availableCapacity = this.inventoryService.Stacking.GetAvailableCapacity(
                        this.capacity,
                        cell.Count,
                        this.GetPrePlaceCountByPos(unityPos));
                    if (availableCapacity > 0)
                    {
                        // 放置完了
                        if (this.inventoryService.Stacking.CanPlaceAll(remaining, availableCapacity))
                        {
                            if (isPre)
                            {
                                pre.Add(unityPos, new ResourceInfo(resourceInfo.Id, remaining));
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
                                pre.Add(unityPos, new ResourceInfo(resourceInfo.Id, availableCapacity));
                            }

                            remaining -= availableCapacity;
                        }
                    }
                }
            }

            // 仓库中没有对应id的cell,需要寻找空的cell
            var emptyPositionsForStack = this.inventoryService.GetPositionsById(-1);
            if (emptyPositionsForStack.Count == 0)
            {
                AWorkerTask.LogProvider("仓库满了", LogManager.LogLevelEnum.Error);
                return false;
            }

            // 找到没有预放置的位置
            foreach (GameGridPosition pos in emptyPositionsForStack)
            {
                Vector3Int unityPos = UnityVectorAdapter.ToVector3Int(pos);
                if (this.IsAreadyPrePlace(unityPos, resourceInfo.Id))
                {
                    continue;
                }

                InventoryCell cell = this.inventoryService.GetCell(pos);
                if (cell == null)
                {
                    continue;
                }

                int availableCapacity = this.inventoryService.Stacking.GetAvailableCapacity(
                    this.capacity,
                    cell.Count,
                    this.GetPrePlaceCountByPos(unityPos));

                // 放置完了
                if (this.inventoryService.Stacking.CanPlaceAll(remaining, availableCapacity))
                {
                    if (isPre)
                    {
                        pre.Add(unityPos, new ResourceInfo(resourceInfo.Id, remaining));
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
                        pre.Add(unityPos, new ResourceInfo(resourceInfo.Id, availableCapacity));
                    }

                    remaining -= availableCapacity;
                }
            }

            // 有可能被预放置了
            AWorkerTask.LogProvider("仓库满了", LogManager.LogLevelEnum.Error);
            return false;
        }

        /// <summary>
        /// 判断仓库中是否对应类型的物品，并预申请资源
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="hungry">饥饿值</param>
        /// <param name="isPre">是否预取食物</param>
        /// <returns>是否足够</returns>
        public bool IsEnoughFoodAndPreTake(AWorker worker, float hungry, bool isPre = false)
        {
            if (this.inventoryService == null)
            {
                return false;
            }

            var foodPositions = this.inventoryService.GetPositionsByType((int)AItem.ItemTypeEnum.Food);
            if (foodPositions.Count == 0)
            {
                return false;
            }

            Dictionary<Vector3Int, ResourceInfo> foods = new();
            foreach (GameGridPosition pos in foodPositions)
            {
                Vector3Int unityPos = UnityVectorAdapter.ToVector3Int(pos);
                InventoryCell cell = this.inventoryService.GetCell(pos);
                if (cell == null || cell.IsEmpty)
                {
                    continue;
                }

                float hungryFromCell = cell.Count * 10.0f;

                // 足够吃饱
                if (hungryFromCell >= hungry)
                {
                    if (isPre)
                    {
                        foods.Add(unityPos, new ResourceInfo(cell.ItemId, (int)(hungry / 10.0f)));
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
                    hungry -= hungryFromCell;
                    if (isPre)
                    {
                        foods.Add(unityPos, new ResourceInfo(cell.ItemId, cell.Count));
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
            if (this.inventoryService == null)
            {
                return false;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
            InventoryCell cell = this.inventoryService.GetCell(gridPos);
            if (cell == null || cell.IsEmpty)
            {
                return false;
            }

            if (AWorkerTask.ItemTypeProvider(cell.ItemId) != AItem.ItemTypeEnum.Food)
            {
                return false;
            }

            int availableCount = cell.Count - this.GetPreTakeCountByPos(posMap);
            int needCount = this.inventoryService.FoodReservation.GetNeededFoodCount(hungry, 10.0f);
            int preTakeCount = this.inventoryService.FoodReservation.GetPreTakeCount(availableCount, needCount);
            if (preTakeCount <= 0)
            {
                return false;
            }

            if (isPre)
            {
                this.PreTake(worker, posMap, new ResourceInfo(cell.ItemId, preTakeCount));
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
            if (this.inventoryService == null)
            {
                return default;
            }

            var seedPositions = this.inventoryService.GetPositionsByType((int)AItem.ItemTypeEnum.Seed);
            if (seedPositions.Count == 0)
            {
                return default;
            }

            GameGridPosition firstPos = seedPositions[0];
            Vector3Int unityPos = UnityVectorAdapter.ToVector3Int(firstPos);
            InventoryCell cell = this.inventoryService.GetCell(firstPos);

            if (isPre && cell != null && !cell.IsEmpty)
            {
                this.PreTake(worker, unityPos, new ResourceInfo(cell.ItemId, cell.Count));
            }

            return unityPos;
        }

        /// <summary>
        /// 添加道具
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void AddItem(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            if (this.inventoryService == null)
            {
                return;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);

            // 确保格子存在
            this.inventoryService.EnsureCell(gridPos, this.capacity);

            // 检查是否同 ID（不同 ID 不允许在同一格子叠加）
            InventoryCell cell = this.inventoryService.GetCell(gridPos);
            if (cell != null && !cell.IsEmpty && cell.ItemId != resourceInfo.Id)
            {
                return;
            }

            if (!this.inventoryService.AddItem(gridPos, resourceInfo.Id, resourceInfo.Count))
            {
                return;
            }

            // 传播所有权：首次放入时记录 OwnerId，后续合并保持先到者
            if (resourceInfo.OwnerId != 0 && !this.cellOwners.ContainsKey(posMap))
            {
                this.cellOwners[posMap] = resourceInfo.OwnerId;
            }
        }

        /// <summary>
        /// 通过预放置添加
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        /// <returns>资源信息</returns>
        public ResourceInfo AddItemByPrePlace(AWorker worker, Vector3Int posMap)
        {
            int workerId = worker.GetInstanceID();
            if (!this.prePlaceResource.ContainsKey(workerId) || !this.prePlaceResource[workerId].ContainsKey(posMap))
            {
                AWorkerTask.LogProvider("没有预放置资源", LogManager.LogLevelEnum.Error);
                return null;
            }

            ResourceInfo resourceInfo = this.prePlaceResource[workerId][posMap];

            // 删除预放置的资源
            this.prePlaceResource[workerId].Remove(posMap);

            // 添加到仓库真正的数据
            if (this.inventoryService != null)
            {
                GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
                this.inventoryService.EnsureCell(gridPos, this.capacity);
                this.inventoryService.AddItem(gridPos, resourceInfo.Id, resourceInfo.Count);
            }

            return resourceInfo;
        }

        /// <summary>
        /// 获取一个预留资源的位置
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>位置</returns>
        public Vector3Int GetPosByPreTake(AWorker worker)
        {
            int workerId = worker.GetInstanceID();
            if (this.preTakeResource.ContainsKey(workerId) && this.preTakeResource[workerId].Count > 0)
            {
                return this.preTakeResource[workerId].First().Key;
            }

            AWorkerTask.LogProvider("没有预留资源!", LogManager.LogLevelEnum.Warning);
            return default;
        }

        /// <summary>
        /// 通过位置删除所有的道具
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>资源信息</returns>
        public ResourceInfo SubAllItemByPos(Vector3Int posMap)
        {
            if (this.inventoryService == null)
            {
                return null;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
            InventoryCell cell = this.inventoryService.GetCell(gridPos);
            if (cell == null || cell.IsEmpty)
            {
                AWorkerTask.LogProvider("没有资源，错误", LogManager.LogLevelEnum.Error);
                return null;
            }

            ResourceInfo resourceInfo = new ResourceInfo(cell.ItemId, cell.Count);
            this.inventoryService.ClearCell(gridPos);
            AWorkerTask.ItemMapProvider().DeleteTile(posMap);

            return resourceInfo;
        }

        /// <summary>
        /// 删除对应数量的道具
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void SubItem(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            if (this.inventoryService == null)
            {
                return;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
            InventoryCell cell = this.inventoryService.GetCell(gridPos);
            if (cell == null || cell.IsEmpty)
            {
                AWorkerTask.LogProvider("没有资源，错误", LogManager.LogLevelEnum.Error);
                return;
            }

            int taken = this.inventoryService.TakeItem(gridPos, resourceInfo.Count);

            // 如果正好取完
            cell = this.inventoryService.GetCell(gridPos);
            if (cell == null || cell.IsEmpty)
            {
                AWorkerTask.ItemMapProvider().DeleteTile(posMap);

                // 食物被吃完删除任务
                if (AWorkerTask.ItemTypeProvider(cell != null ? cell.ItemId : -1) == AItem.ItemTypeEnum.Food)
                {
                    AWorkerTask.DeleteHungryTaskProvider(posMap);
                }
            }
        }

        /// <summary>
        /// 根据预取的资源删除仓库中的库存
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        /// <returns>返回从仓库中扣减的数量(预取的资源)</returns>
        public ResourceInfo SubItemByPreTake(AWorker worker, Vector3Int posMap)
        {
            int workerId = worker.GetInstanceID();
            if (!this.preTakeResource.ContainsKey(workerId) || !this.preTakeResource[workerId].ContainsKey(posMap))
            {
                AWorkerTask.LogProvider("没有预取资源", LogManager.LogLevelEnum.Error);
                return null;
            }

            ResourceInfo resourceInfo = this.preTakeResource[workerId][posMap];

            // 删除预取的资源
            this.preTakeResource[workerId].Remove(posMap);
            if (this.preTakeResource[workerId].Count == 0)
            {
                this.preTakeResource.Remove(workerId);
            }

            // 减少仓库真正的数据
            if (this.inventoryService != null)
            {
                GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
                this.inventoryService.TakeItem(gridPos, resourceInfo.Count);

                InventoryCell cell = this.inventoryService.GetCell(gridPos);
                if (cell == null || cell.IsEmpty)
                {
                    AWorkerTask.ItemMapProvider().DeleteTile(posMap);

                    // 食物被吃完删除任务
                    int itemId = cell != null ? cell.ItemId : -1;
                    if (itemId >= 0 && AWorkerTask.ItemTypeProvider(itemId) == AItem.ItemTypeEnum.Food)
                    {
                        AWorkerTask.DeleteHungryTaskProvider(posMap);
                    }
                }
            }

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
            if (this.inventoryService == null)
            {
                return false;
            }

            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                int totalAvailable = this.inventoryService.GetTotalCount(need.Key);
                int preTaken = this.GetPreTakeCountById(need.Key);
                if (totalAvailable - preTaken < need.Value.Count)
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
                    int remaining = this.inventoryService.TakeReservation.GetTargetTakeCount(
                        need.Value.Count,
                        workerData.MaxResourceCount);

                    var positions = this.inventoryService.GetPositionsById(need.Key);
                    foreach (GameGridPosition pos in positions)
                    {
                        Vector3Int unityPos = UnityVectorAdapter.ToVector3Int(pos);
                        InventoryCell cell = this.inventoryService.GetCell(pos);
                        if (cell == null || cell.IsEmpty)
                        {
                            continue;
                        }

                        int availableCount = this.inventoryService.TakeReservation.GetAvailableTakeCount(
                            cell.Count,
                            this.GetPreTakeCountByPos(unityPos));
                        if (availableCount < remaining)
                        {
                            remaining -= availableCount;
                            this.PreTake(worker, unityPos, new ResourceInfo(need.Key, availableCount));
                        }
                        else
                        {
                            this.PreTake(worker, unityPos, new ResourceInfo(need.Key, remaining));
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
            if (this.inventoryService == null)
            {
                return;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(pos);
            InventoryCell cell = this.inventoryService.GetCell(gridPos);
            if (cell == null || cell.IsEmpty)
            {
                return;
            }

            AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(cell.ItemId);
            if (itemType == AItem.ItemTypeEnum.Weapon || itemType == AItem.ItemTypeEnum.Equipment)
            {
                ShowWearTaskProvider(pos);
            }
        }

        /// <summary>
        /// 仓库信息
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int pos)
        {
            if (this.inventoryService == null)
            {
                return string.Empty;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(pos);
            InventoryCell cell = this.inventoryService.GetCell(gridPos);
            if (cell == null)
            {
                return string.Empty;
            }

            ResourceInfo resourceInfo = cell.IsEmpty
                ? new ResourceInfo(-1, 0)
                : new ResourceInfo(cell.ItemId, cell.Count);

            // 空格子（id=-1）不需要查询 ItemData，避免 "没有id的道具" 错误日志
            string text;
            if (resourceInfo.Id >= 0)
            {
                ItemData itemData = AWorkerTask.ItemDataProvider(resourceInfo.Id);
                if (itemData != null)
                {
                    string ownerLabel = this.GetOwnerLabel(pos);
                    text = $"ID:{resourceInfo.Id}\n" +
                        $"名称:{itemData.CnName}\n" +
                        $"英文名:{itemData.EnName}\n" +
                        $"类型:{itemData.Type}\n" +
                        $"数量:{resourceInfo.Count}\n" +
                        $"拥有者:{ownerLabel}\n" +
                        $"信息:{itemData.Info}\n" +
                        $"可堆叠:{itemData.IsStackable}\n";

                    AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(resourceInfo.Id);
                    if (itemType == AItem.ItemTypeEnum.Weapon || itemType == AItem.ItemTypeEnum.Equipment)
                    {
                        text += $"装备槽位:{itemData.EquipSlot}\n";
                    }
                }
                else
                {
                    text = $"ID:{resourceInfo.Id}\n" +
                        $"数量:{resourceInfo.Count}\n";
                }
            }
            else
            {
                text = $"ID:{resourceInfo.Id}\n" +
                    $"数量:{resourceInfo.Count}\n";
            }

            text += $"预放置:\n";
            foreach (KeyValuePair<int, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    text += WorkerNameProvider(prePlace.Key) + ":\n"
                        + "    " + prePlace.Value[pos].Id + " " + prePlace.Value[pos].Count + "\n";
                }
            }

            text += "预取:\n";
            foreach (KeyValuePair<int, Dictionary<Vector3Int, ResourceInfo>> preTake in this.preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    text += WorkerNameProvider(preTake.Key) + ":\n"
                        + "    " + preTake.Value[pos].Id + " " + preTake.Value[pos].Count + "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// 通过位置获取资源。
        /// [v2] 返回新建的 ResourceInfo 副本，不再返回内部可变引用。
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>资源</returns>
        public ResourceInfo GetResourceByPos(Vector3Int posMap)
        {
            if (this.inventoryService == null)
            {
                return null;
            }

            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
            return this.inventoryService.GetResourceInfo(gridPos);
        }

        /// <summary>
        /// 删除Worker预设(Worker死亡)
        /// </summary>
        /// <param name="worker">Worker</param>
        public void DeleteWorkerPre(AWorker worker)
        {
            int workerId = worker.GetInstanceID();
            if (this.prePlaceResource.ContainsKey(workerId))
            {
                this.prePlaceResource.Remove(workerId);
            }

            if (this.preTakeResource.ContainsKey(workerId))
            {
                this.preTakeResource.Remove(workerId);
            }
        }

        // ---- v2 新增：Domain 层查询方法（供测试和未来迁移使用） ----

        /// <summary>
        /// [v2] 获取内部 InventoryService 实例（供测试和未来迁移使用）。
        /// 外部代码应优先使用现有 public API；此方法为过渡期提供。
        /// </summary>
        public InventoryService GetInventoryService()
        {
            return this.inventoryService;
        }

        // ---- 私有方法 ----

        /// <summary>
        /// 通过pos获取预放置资源的数量
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>预放置的数量</returns>
        private int GetPrePlaceCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<int, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    count += prePlace.Value[pos].Count;
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
            int workerId = worker.GetInstanceID();
            if (this.prePlaceResource.ContainsKey(workerId))
            {
                if (this.prePlaceResource[workerId].ContainsKey(pos))
                {
                    this.prePlaceResource[workerId][pos].Count += resourceInfo.Count;
                    return;
                }

                this.prePlaceResource[workerId].Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
                return;
            }

            Dictionary<Vector3Int, ResourceInfo> dict = new();
            dict.Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
            this.prePlaceResource.Add(workerId, dict);
        }

        /// <summary>
        /// 该位置是否有其他id已经预放置
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="id">ID</param>
        /// <returns>是否已经预放置过了</returns>
        private bool IsAreadyPrePlace(Vector3Int pos, int id)
        {
            foreach (KeyValuePair<int, Dictionary<Vector3Int, ResourceInfo>> prePlace in this.prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos) && prePlace.Value[pos].Id != id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 预取资源,没有考虑超过容量，所以封装为isEnough
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="pos">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        private void PreTake(AWorker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            int workerId = worker.GetInstanceID();
            if (!this.preTakeResource.ContainsKey(workerId))
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new();
                dict.Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
                this.preTakeResource.Add(workerId, dict);
                return;
            }

            if (!this.preTakeResource[workerId].ContainsKey(pos))
            {
                this.preTakeResource[workerId].Add(pos, DataTool.DeepCopyByBinary(resourceInfo));
                return;
            }

            this.preTakeResource[workerId][pos].Count += resourceInfo.Count;
        }

        private int GetPreTakeCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<int, Dictionary<Vector3Int, ResourceInfo>> preTake in this.preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    count += preTake.Value[pos].Count;
                }
            }

            return count;
        }

        private int GetPreTakeCountById(int id)
        {
            int count = 0;
            foreach (KeyValuePair<int, Dictionary<Vector3Int, ResourceInfo>> pre in this.preTakeResource)
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre.Value)
                {
                    if (pair.Value.Id == id)
                    {
                        count += pair.Value.Count;
                    }
                }
            }

            return count;
        }

        /// <summary>设置仓库格子所有权。</summary>
        public void SetOwner(Vector3Int pos, int ownerId)
        {
            this.cellOwners[pos] = ownerId;
        }

        /// <summary>获取仓库格子所有权。</summary>
        public int GetOwner(Vector3Int pos)
        {
            return this.cellOwners.TryGetValue(pos, out int id) ? id : 0;
        }

        /// <summary>获取仓库格子的所有权显示文本。</summary>
        public string GetOwnerLabel(Vector3Int pos)
        {
            int ownerId = this.GetOwner(pos);
            if (ownerId == 0) return "无主(Player)";
            return WorkerNameProvider(ownerId);
        }
    }
}
