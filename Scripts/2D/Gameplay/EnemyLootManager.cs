namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using LAB2D.Item;
    using LAB2D.Item.Backpack.Equipment;
    using LAB2D.Map;
    using LAB2D.MVC.Backpack.Controller;
    using LAB2D.UI.Panel;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 敌人掉落管理器。
    /// 统一管理敌人死亡时的通用物品掉落（从 DropDataManager 默认配置）和装备稀有度掉落。
    /// 单例，由 GlobalInit 初始化。
    /// </summary>
    public class EnemyLootManager : Singleton<EnemyLootManager>, IInitializable
    {
        internal static System.Func<List<AItem>> ItemFactoryGenAllProvider { get; set; }
            = () => ServiceLocator.Get<ItemInstanceFactory>().GenBackpackItems();
        internal static System.Action<Vector3, string> FloatingTextStatusProvider { get; set; }
            = (pos, text) => { if (ServiceLocator.TryGet(out FloatingTextManager ftm)) ftm.SpawnStatusText(pos, text); };
        internal static System.Func<Player> PlayerMineProvider { get; set; }
            = () => ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
        internal static System.Func<Vector3> PlayerPositionProvider { get; set; }
            = () =>
            {
                Player p = ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
                return p != null ? p.transform.position : Vector3.zero;
            };
        internal static System.Func<EquipmentComparePopup> EquipmentComparePopupProvider { get; set; }
            = () => EquipmentComparePopup.Instance;

        /// <summary>
        /// 整数随机数提供者（minInclusive, maxExclusive）。
        /// 默认实现封装 UnityEngine.Random.Range；可在测试中替换为确定性桩。
        /// </summary>
        internal static System.Func<int, int, int> RandomIntProvider { get; set; }
            = (minInclusive, maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive);

        /// <summary>
        /// 浮点随机数提供者（minInclusive, maxInclusive）。
        /// 默认实现封装 UnityEngine.Random.Range；可在测试中替换为确定性桩。
        /// </summary>
        internal static System.Func<float, float, float> RandomFloatProvider { get; set; }
            = (minInclusive, maxInclusive) => UnityEngine.Random.Range(minInclusive, maxInclusive);

        /// <summary>是否已初始化</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 强制 100% 掉落开关（调试用）。
        /// 开启后敌人死亡必定掉落装备/武器 + 通用物品。
        /// </summary>
        public static bool ForceDrop { get; set; }

        /// <summary>
        /// 通用物品掉落概率表（来自 DropDataManager 默认掉落配置）。
        /// key: 累积概率值, value: 掉落物品
        /// </summary>
        private Dictionary<int, DropItem> probToDropItem;

        /// <summary>通用掉落总概率值</summary>
        private int dropTotal;

        /// <summary>
        /// 等待对比的装备数据：key=地图坐标，value=装备属性数据
        /// 当玩家拾取装备时，从此字典查找并弹出对比弹窗。
        /// </summary>
        private Dictionary<Vector3Int, PendingEquipmentDrop> pendingDrops = new Dictionary<Vector3Int, PendingEquipmentDrop>();

        /// <summary>
        /// 待处理的装备掉落数据。
        /// </summary>
        public class PendingEquipmentDrop
        {
            /// <summary>装备物品数据</summary>
            public AEquipment Equipment;

            /// <summary>稀有度等级</summary>
            public EquipmentRarityType Rarity;

            /// <summary>掉落世界坐标</summary>
            public Vector3 DropPosition;

            /// <summary>装备 Id（对应 ItemData.Id）</summary>
            public long EquipmentId;

            /// <summary>Tilemap 坐标（用于光束特效追踪和清理）</summary>
            public Vector3Int MapPosition;
        }

        /// <summary>
        /// 初始化管理器。
        /// </summary>
        public void Initialize()
        {
            if (this.IsInitialized) return;

            // 从 DropDataManager 默认掉落配置构建通用掉落概率表
            this.probToDropItem = new Dictionary<int, DropItem>();
            this.dropTotal = 0;
            List<DropItem> dropItems = AWorkerTask.DropDataProvider(-1);
            this.AddDropItem(10, null); // 不掉落
            foreach (DropItem dropItem in dropItems)
            {
                this.AddDropItem(10, dropItem);
            }

            this.pendingDrops = new Dictionary<Vector3Int, PendingEquipmentDrop>();
            AWorkerTask.EquipmentBeamProvider().Initialize();
            this.IsInitialized = true;
            AWorkerTask.LogProvider("EnemyLootManager 初始化完成", LogManager.LogLevelEnum.Trace);
        }

        /// <summary>
        /// 尝试掉落敌人战利品。
        /// 包含通用物品掉落（来自 DropDataManager 默认配置）和装备稀有度掉落。
        /// 掉落物 OwnerId 设置为最终击杀者，使掉落物归属击杀者所有。
        /// </summary>
        /// <param name="worldPos">死亡世界坐标</param>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <param name="killer">最终击杀者（LastAttacker），null 时掉落物为无主之物</param>
        public void TryDropLoot(Vector3 worldPos, int waveNumber, GameCharacter killer = null)
        {
            if (!this.IsInitialized) return;

            // 根据击杀者类型计算 OwnerId：
            // - Player 击杀 → OwnerId = 0（无主/Player，任何人都可拾取）
            // - Worker 击杀 → OwnerId = Worker 的 instance ID（只有该 Worker 可拾取）
            // - 无击杀者 → OwnerId = 0
            int ownerId = ResolveOwnerId(killer);

            this.TryDropCommonItem(worldPos, ownerId);
            this.TryDropEquipment(worldPos, waveNumber, ownerId);
        }

        /// <summary>
        /// 根据击杀者类型解析掉落物 OwnerId。
        /// </summary>
        private static int ResolveOwnerId(GameCharacter killer)
        {
            if (killer == null) return 0;

            // Player 击杀：OwnerId = 0（Player/无主，任何人可拾取）
            if (killer.IsPlayerCharacter) return 0;

            // Worker 击杀：OwnerId = Worker 的 instance ID
            return killer.GetInstanceID();
        }

        /// <summary>
        /// 从 DropDataManager 默认掉落配置中概率选取一个通用物品掉落。
        /// </summary>
        /// <param name="worldPos">掉落世界坐标</param>
        /// <param name="ownerId">掉落物归属者 ID（最终击杀者）</param>
        private void TryDropCommonItem(Vector3 worldPos, int ownerId)
        {
            if (this.dropTotal == 0) return;

            DropItem selectedDrop;
            if (ForceDrop)
            {
                // 强制掉落模式：从有效物品中随机选一个（跳过"不掉落"）
                List<DropItem> validDrops = new List<DropItem>();
                foreach (KeyValuePair<int, DropItem> dropItem in this.probToDropItem)
                {
                    if (dropItem.Value != null) validDrops.Add(dropItem.Value);
                }

                if (validDrops.Count == 0) return;
                selectedDrop = validDrops[RandomIntProvider(0, validDrops.Count)];
            }
            else
            {
                int rand = RandomIntProvider(0, this.dropTotal);
                selectedDrop = null;
                foreach (KeyValuePair<int, DropItem> dropItem in this.probToDropItem)
                {
                    if (rand <= dropItem.Key)
                    {
                        selectedDrop = dropItem.Value;
                        break;
                    }
                }

                if (selectedDrop == null) return;
            }

            Vector3Int center = AWorkerTask.TileMapWorldToMapProvider(worldPos);

            // 深拷贝模板 ResourceInfo 后设置归属，避免污染共享模板
            ResourceInfo ownedResource = DataTool.DeepCopyByBinary(selectedDrop.ResourceInfo);
            ownedResource.OwnerId = ownerId;

            // 可堆叠优先合并到附近同类堆叠，找不到则放空地
            Vector3Int pos = AWorkerTask.TryMergeOrPlaceDrop(
                center, ownedResource, selectedDrop.Name);

            if (pos != default)
            {
                Vector3 beamWorldPos = AWorkerTask.TileMapPositionProvider(pos);
                AWorkerTask.EquipmentBeamProvider().SpawnBeam(pos, beamWorldPos, EquipmentRarityType.Common);

                // 替换自动创建的 Carry 任务为 PickUp 拾取任务
                ReplaceCarryWithPickUpTask(pos, ownedResource, ownerId);

                AWorkerTask.LogProvider(
                    string.Format("[EnemyLoot] 通用掉落: pos=({0},{1}) ownerId={2}",
                        pos.x, pos.y, ownerId),
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>
        /// 替换 PutDownToDrop 自动创建的 Carry 任务为 PickUp 拾取任务。
        /// - ownerId > 0（Worker 击杀）：专属拾取任务，只有该 Worker 可接取。
        /// - ownerId == 0（Player 击杀/无主）：公开拾取任务，任何 Worker 可接取。
        /// </summary>
        private static void ReplaceCarryWithPickUpTask(Vector3Int pos, ResourceInfo resource, int ownerId)
        {
            // 移除 PutDownToDrop 自动创建的 WorkerCarryTask
            ServiceLocator.Get<WorkerTaskManager>().RemoveCarryTaskAt(pos);

            // 创建 FromGround 模式拾取任务
            WorkerPickUpTask pickUpTask = new WorkerPickUpTask.PickUpTaskBuilder()
                .SetMode(WorkerPickUpTask.PickUpMode.FromGround)
                .SetTargetPosition(pos)
                .SetGroundResource(resource)
                .SetOwnerId(ownerId)
                .Build();

            // 发布到任务队列（最高优先级：敌人战利品）
            AWorkerTask.TaskAddProvider(
                pickUpTask,
                new GameGridPosition(pos.x, pos.y, pos.z),
                WorkerTaskPriority.PlayerBounty);

            string ownerLabel = ownerId > 0 ? $"Worker#{ownerId}" : "Player/公开";
            AWorkerTask.LogProvider(
                string.Format("[EnemyLoot] 发布拾取任务: pos=({0},{1}) ownerId={2}({3}) taskType=PickUp",
                    pos.x, pos.y, ownerId, ownerLabel),
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 尝试在敌人死亡位置生成装备掉落。
        /// 基础掉落概率由 EquipmentLootConstant.BaseEquipmentDropChance 控制。
        /// </summary>
        /// <param name="worldPos">死亡世界坐标</param>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <param name="ownerId">掉落物归属者 ID（最终击杀者）</param>
        /// <returns>是否成功生成装备掉落</returns>
        public bool TryDropEquipment(Vector3 worldPos, int waveNumber, int ownerId = 0)
        {
            if (!this.IsInitialized)
            {
                return false;
            }

            // 基础概率判定（ForceDrop 模式跳过）
            if (!ForceDrop)
            {
                float roll = RandomFloatProvider(0f, 1f);
                if (roll > EquipmentLootConstant.BaseEquipmentDropChance)
                {
                    return false;
                }
            }

            // 随机稀有度
            EquipmentRarityType rarity = EquipmentLootTool.RollRarity(waveNumber);

            // 从已有物品工厂获取可用的装备列表
            List<AItem> allItems = ItemFactoryGenAllProvider();
            List<AEquipment> availableEquipment = new List<AEquipment>();
            foreach (AItem item in allItems)
            {
                if (item is AEquipment eq && eq.Type != AEquipment.EquipTypeEnum.Null)
                {
                    availableEquipment.Add(eq);
                }
            }

            if (availableEquipment.Count == 0)
            {
                return false;
            }

            // 随机选择一个装备类型
            AEquipment template = availableEquipment[RandomIntProvider(0, availableEquipment.Count)];

            // 对模板属性应用稀有度倍率
            EquipmentLootTool.ApplyRarityToAttributes(template.Attribute, rarity);
            // 将稀有度写入装备的 Quality 字段，供装备面板持续展示
            template.Quality = EquipmentLootTool.MapRarityToQuality(rarity);

            // 放置装备到地面（装备不可堆叠，TryMergeOrPlaceDrop 自动跳过合并）
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(worldPos);
            ResourceInfo resourceInfo = new ResourceInfo(template.Id, 1) { OwnerId = ownerId };
            Vector3Int availablePos = AWorkerTask.TryMergeOrPlaceDrop(
                posMap, resourceInfo, template.Tile.name);
            if (availablePos == default)
            {
                return false;
            }

            // 记录待处理掉落，供拾取时对比使用
            PendingEquipmentDrop pending = new PendingEquipmentDrop
            {
                Equipment = template,
                Rarity = rarity,
                DropPosition = worldPos,
                EquipmentId = template.Id,
                MapPosition = availablePos,
            };
            this.pendingDrops[availablePos] = pending;

            // 在掉落位置生成光束特效（按稀有度着色）
            Vector3 beamWorldPos = AWorkerTask.TileMapPositionProvider(availablePos);
            AWorkerTask.EquipmentBeamProvider().SpawnBeam(availablePos, beamWorldPos, rarity);

            // 替换自动创建的 Carry 任务为 PickUp 拾取任务
            ReplaceCarryWithPickUpTask(availablePos, resourceInfo, ownerId);

            // 在掉落位置生成浮动稀有度标签
            string rarityLabel = EquipmentLootTool.FormatRarityLabel(rarity);
            FloatingTextStatusProvider(worldPos, rarityLabel);

            ItemData itemData = AWorkerTask.ItemDataProvider(template.Id);
            string itemName = itemData != null ? itemData.CnName : template.Id.ToString();
            AWorkerTask.LogProvider(
                string.Format("装备掉落: {0} [{1}] at ({2:F0},{3:F0}) ownerId={4}",
                    itemName, EquipmentLootTool.GetRarityName(rarity), worldPos.x, worldPos.y, ownerId),
                LogManager.LogLevelEnum.Trace);

            return true;
        }

        /// <summary>
        /// 强制生成装备掉落（跳过概率判定，用于测试/调试）。
        /// </summary>
        /// <param name="worldPos">掉落世界坐标</param>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <returns>是否成功生成装备掉落</returns>
        public bool ForceDropEquipment(Vector3 worldPos, int waveNumber)
        {
            if (!this.IsInitialized)
            {
                this.Initialize();
            }

            // 随机稀有度
            EquipmentRarityType rarity = EquipmentLootTool.RollRarity(waveNumber);

            // 从已有物品工厂获取可用的装备列表
            List<AItem> allItems = ItemFactoryGenAllProvider();
            List<AEquipment> availableEquipment = new List<AEquipment>();
            foreach (AItem item in allItems)
            {
                if (item is AEquipment eq && eq.Type != AEquipment.EquipTypeEnum.Null)
                {
                    availableEquipment.Add(eq);
                }
            }

            if (availableEquipment.Count == 0)
            {
                return false;
            }

            AEquipment template = availableEquipment[RandomIntProvider(0, availableEquipment.Count)];
            EquipmentLootTool.ApplyRarityToAttributes(template.Attribute, rarity);
            template.Quality = EquipmentLootTool.MapRarityToQuality(rarity);

            // 放置装备到地面（装备不可堆叠，TryMergeOrPlaceDrop 自动跳过合并）
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(worldPos);
            ResourceInfo resourceInfo = new ResourceInfo(template.Id, 1);
            Vector3Int availablePos = AWorkerTask.TryMergeOrPlaceDrop(
                posMap, resourceInfo, template.Tile.name);
            if (availablePos == default)
            {
                return false;
            }

            PendingEquipmentDrop pending = new PendingEquipmentDrop
            {
                Equipment = template,
                Rarity = rarity,
                DropPosition = worldPos,
                EquipmentId = template.Id,
                MapPosition = availablePos,
            };
            this.pendingDrops[availablePos] = pending;

            Vector3 beamWorldPos = AWorkerTask.TileMapPositionProvider(availablePos);
            AWorkerTask.EquipmentBeamProvider().SpawnBeam(availablePos, beamWorldPos, rarity);

            string rarityLabel = EquipmentLootTool.FormatRarityLabel(rarity);
            FloatingTextStatusProvider(worldPos, rarityLabel);

            ItemData itemData = AWorkerTask.ItemDataProvider(template.Id);
            string itemName = itemData != null ? itemData.CnName : template.Id.ToString();
            AWorkerTask.LogProvider(
                string.Format("强制装备掉落: {0} [{1}] at ({2:F0},{3:F0})",
                    itemName, EquipmentLootTool.GetRarityName(rarity), worldPos.x, worldPos.y),
                LogManager.LogLevelEnum.Trace);

            return true;
        }

        /// <summary>
        /// 在物品放入仓库时尝试生成光束。
        /// 如果该物品是之前掉落的装备（pendingDrops 中有记录），则在仓库位置生成光束。
        /// </summary>
        /// <param name="posMap">仓库放置坐标</param>
        /// <param name="itemId">物品 Id</param>
        public void TrySpawnBeamForInventory(Vector3Int posMap, long itemId)
        {
            if (!this.IsInitialized || this.pendingDrops == null) return;

            Vector3Int? foundKey = null;
            PendingEquipmentDrop pending = null;
            foreach (KeyValuePair<Vector3Int, PendingEquipmentDrop> kv in this.pendingDrops)
            {
                if (kv.Value.EquipmentId == itemId)
                {
                    foundKey = kv.Key;
                    pending = kv.Value;
                    break;
                }
            }

            if (foundKey.HasValue && pending != null)
            {
                AWorkerTask.EquipmentBeamProvider().RemoveBeamAt(pending.MapPosition);
                this.pendingDrops.Remove(foundKey.Value);

                pending.MapPosition = posMap;
                pending.DropPosition = AWorkerTask.TileMapPositionProvider(posMap);
                this.pendingDrops[posMap] = pending;

                Vector3 beamWorldPos = AWorkerTask.TileMapPositionProvider(posMap);
                AWorkerTask.EquipmentBeamProvider().SpawnBeam(posMap, beamWorldPos, pending.Rarity);
            }
        }

        /// <summary>
        /// 当玩家拾取装备时调用，弹出对比弹窗。
        /// </summary>
        /// <param name="equipment">被拾取的装备</param>
        /// <param name="equipmentId">装备 Id</param>
        /// <param name="posMap">装备所在 tilemap 坐标（用于清理地面）</param>
        public void OnEquipmentPickup(AEquipment equipment, long equipmentId, Vector3Int posMap)
        {
            if (equipment == null) return;

            EquipmentRarityType rarity = EquipmentRarityType.Common;
            Vector3Int? foundKey = null;
            PendingEquipmentDrop pending = null;
            foreach (KeyValuePair<Vector3Int, PendingEquipmentDrop> kv in this.pendingDrops)
            {
                if (kv.Value.EquipmentId == equipmentId)
                {
                    foundKey = kv.Key;
                    pending = kv.Value;
                    break;
                }
            }

            if (foundKey.HasValue && pending != null)
            {
                rarity = pending.Rarity;
                EquipmentLootTool.ApplyRarityToAttributes(equipment.Attribute, rarity);
                equipment.Quality = EquipmentLootTool.MapRarityToQuality(rarity);
                this.pendingDrops.Remove(foundKey.Value);
            }

            AEquipment.EquipTypeEnum slotType = equipment.Type;
            GameCharacter.CharacterData charData = PlayerMineProvider()?.CharacterDataLAB;
            AEquipment currentEquipped = null;
            if (charData != null)
            {
                Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = charData.GetEquipments();
                if (equipments != null && equipments.TryGetValue(slotType, out AEquipment existing))
                {
                    currentEquipped = existing;
                }
            }

            EquipmentComparePopup popup = EquipmentComparePopupProvider();
            if (popup == null)
            {
                EquipmentComparePopup.EnsureRuntimePopup();
                popup = EquipmentComparePopupProvider();
            }

            if (popup != null)
            {
                popup.ShowCompare(currentEquipped?.Attribute, equipment.Attribute, rarity, slotType, () =>
                {
                    // 替换：旧装备放入背包，新装备装备上
                    if (charData != null)
                    {
                        Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = charData.GetEquipments();
                        if (equipments != null && equipments.TryGetValue(slotType, out AEquipment oldEquipment))
                        {
                            // 先从槽位移除旧装备，避免 AddEquipment 内部调用 EquipmentSwapDropProvider 放回地面
                            equipments.Remove(slotType);
                            // 旧装备放入玩家背包
                            Core.ServiceLocator.Get<BackpackController>().AddItem(oldEquipment);
                        }

                        Vector3Int playerPos = AWorkerTask.TileMapWorldToMapProvider(
                            PlayerPositionProvider());
                        charData.AddEquipment(equipment, playerPos);
                    }

                    CleanupEquipmentPickup(posMap, equipment);
                }, () =>
                {
                    // 丢弃：将新装备放回地面（玩家当前位置），不入背包
                    if (charData != null)
                    {
                        Vector3Int playerPos = AWorkerTask.TileMapWorldToMapProvider(
                            PlayerPositionProvider());
                        if (equipment.Tile != null)
                        {
                            AWorkerTask.TryMergeOrPlaceDrop(
                                playerPos, new ResourceInfo(equipment.Id, 1), equipment.Tile.name);
                        }
                    }

                    CleanupEquipmentPickup(posMap, equipment);
                });
            }
        }

        /// <summary>
        /// 装备拾取后的地面清理：记录收集、清理 DropManager。
        /// 替换和丢弃回调均需调用此方法。
        /// </summary>
        private static void CleanupEquipmentPickup(Vector3Int posMap, AEquipment equipment)
        {
            // 记录收集
            Core.ServiceLocator.Get<ItemCollectionTracker>().RecordItemCollected(
                new ResourceInfo(equipment.Id, 1));

            // 清理 DropManager
            if (Core.ServiceLocator.TryGet(out DropManager dropManager))
            {
                ResourceInfo resourceInfo = dropManager.GetDropByAll(posMap);
                if (resourceInfo != null)
                {
                    dropManager.SubDropByAll(posMap, resourceInfo);
                }
            }

            // 移除光束特效
            AWorkerTask.EquipmentBeamProvider().RemoveBeamAt(posMap);

            // 删除地面 tile
            ItemMap.Instance?.DeleteTile(posMap);
        }

        /// <summary>
        /// 根据地图坐标查找待处理装备掉落的稀有度。
        /// 用于拾取列表按品质着色道具名称等场景。
        /// </summary>
        /// <param name="mapPos">Tilemap 坐标</param>
        /// <returns>稀有度，未找到时返回 null</returns>
        public EquipmentRarityType? TryGetRarityByMapPosition(Vector3Int mapPos)
        {
            if (this.pendingDrops == null) return null;
            if (this.pendingDrops.TryGetValue(mapPos, out PendingEquipmentDrop pending))
                return pending.Rarity;
            return null;
        }

        /// <summary>
        /// 根据地图坐标移除待处理掉落记录和光束特效。
        /// 由 ItemMap 在物品被拾取/移除时回调。
        /// </summary>
        /// <param name="mapPos">Tilemap 坐标</param>
        public void RemoveDropByMapPosition(Vector3Int mapPos)
        {
            // 始终移除光束：普通物品掉落（TryDropCommonItem）也会生成光束，
            // 但不会记录到 pendingDrops，因此光束移除不能依赖 pendingDrops 是否为空。
            AWorkerTask.EquipmentBeamProvider().RemoveBeamAt(mapPos);

            if (this.pendingDrops != null)
            {
                this.pendingDrops.Remove(mapPos);
            }
        }

        /// <summary>
        /// 清理待处理掉落记录（拾取超时或远离后清理）。
        /// </summary>
        public void CleanupStaleDrops()
        {
            if (this.pendingDrops == null) return;
            List<Vector3Int> staleKeys = new List<Vector3Int>();
            Vector3 playerPos = PlayerPositionProvider();

            foreach (KeyValuePair<Vector3Int, PendingEquipmentDrop> kv in this.pendingDrops)
            {
                float dist = Vector3.Distance(kv.Value.DropPosition, playerPos);
                if (dist > 30f)
                {
                    staleKeys.Add(kv.Key);
                }
            }

            foreach (Vector3Int key in staleKeys)
            {
                if (this.pendingDrops.TryGetValue(key, out PendingEquipmentDrop pending))
                {
                    AWorkerTask.EquipmentBeamProvider().RemoveBeamAt(pending.MapPosition);
                }

                this.pendingDrops.Remove(key);
            }

            AWorkerTask.EquipmentBeamProvider().CleanupStaleBeams();
        }

        /// <summary>
        /// 打印稀有度分布测试报告（供 Editor 菜单调用）。
        /// </summary>
        /// <param name="waveNumber">测试波次</param>
        /// <param name="sampleCount">采样次数</param>
        /// <returns>分布统计报告文本</returns>
        public string TestRarityDistribution(int waveNumber, int sampleCount)
        {
            if (sampleCount <= 0) sampleCount = 1000;

            int[] counts = new int[6];
            for (int i = 0; i < sampleCount; i++)
            {
                EquipmentRarityType r = EquipmentLootTool.RollRarity(waveNumber);
                counts[(int)r]++;
            }

            return string.Format(
                "波次 {0} 稀有度分布 ({1} 次采样):\n" +
                "  普通: {2} ({3:F1}%)\n" +
                "  不凡: {4} ({5:F1}%)\n" +
                "  稀有: {6} ({7:F1}%)\n" +
                "  史诗: {8} ({9:F1}%)\n" +
                "  传说: {10} ({11:F1}%)\n" +
                "  神话: {12} ({13:F1}%)",
                waveNumber, sampleCount,
                counts[0], 100f * counts[0] / sampleCount,
                counts[1], 100f * counts[1] / sampleCount,
                counts[2], 100f * counts[2] / sampleCount,
                counts[3], 100f * counts[3] / sampleCount,
                counts[4], 100f * counts[4] / sampleCount,
                counts[5], 100f * counts[5] / sampleCount);
        }

        /// <summary>
        /// 添加可掉落物品到概率表中.
        /// </summary>
        /// <param name="value">概率范围</param>
        /// <param name="dropItem">掉落物品</param>
        private void AddDropItem(int value, DropItem dropItem)
        {
            this.dropTotal += value;
            this.probToDropItem.Add(this.dropTotal, dropItem);
        }
    }
}
