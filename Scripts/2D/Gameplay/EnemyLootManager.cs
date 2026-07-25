namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using LAB2D.Item.Backpack.Equipment;
    using LAB2D.UI.Panel;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;
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
        /// </summary>
        /// <param name="worldPos">死亡世界坐标</param>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        public void TryDropLoot(Vector3 worldPos, int waveNumber)
        {
            if (!this.IsInitialized) return;

            this.TryDropCommonItem(worldPos);
            this.TryDropEquipment(worldPos, waveNumber);
        }

        /// <summary>
        /// 从 DropDataManager 默认掉落配置中概率选取一个通用物品掉落。
        /// </summary>
        private void TryDropCommonItem(Vector3 worldPos)
        {
            if (this.dropTotal == 0) return;

            int rand = RandomIntProvider(0, this.dropTotal);
            Vector3Int pos = AWorkerTask.AvailablePositionProvider(
                AWorkerTask.TileMapWorldToMapProvider(worldPos), 3, true);
            if (pos == default) return;

            foreach (KeyValuePair<int, DropItem> dropItem in this.probToDropItem)
            {
                if (rand <= dropItem.Key)
                {
                    if (dropItem.Value == null) break;

                    TileBase tile = (TileBase)AWorkerTask.ResourceLoadProvider(dropItem.Value.Name);
                    AWorkerTask.ItemMapProvider().PutDownToDrop(pos, tile, dropItem.Value.ResourceInfo);

                    Vector3 beamWorldPos = AWorkerTask.TileMapPositionProvider(pos);
                    AWorkerTask.EquipmentBeamProvider().SpawnBeam(pos, beamWorldPos, EquipmentRarityType.Common);
                    break;
                }
            }
        }

        /// <summary>
        /// 尝试在敌人死亡位置生成装备掉落。
        /// 基础掉落概率由 EquipmentLootConstant.BaseEquipmentDropChance 控制。
        /// </summary>
        /// <param name="worldPos">死亡世界坐标</param>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <returns>是否成功生成装备掉落</returns>
        public bool TryDropEquipment(Vector3 worldPos, int waveNumber)
        {
            if (!this.IsInitialized)
            {
                return false;
            }

            // 基础概率判定
            float roll = RandomFloatProvider(0f, 1f);
            if (roll > EquipmentLootConstant.BaseEquipmentDropChance)
            {
                return false;
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

            // 通过 ItemMap 将装备放置到地面
            ItemData itemData = AWorkerTask.ItemDataProvider(template.Id);
            if (itemData == null)
            {
                AWorkerTask.LogProvider("EnemyLootManager: 未找到 ItemData，Id=" + template.Id, LogManager.LogLevelEnum.Warning);
                return false;
            }

            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(worldPos);
            Vector3Int availablePos = AWorkerTask.AvailablePositionProvider(posMap, 3, true);
            if (availablePos == default)
            {
                return false;
            }

            ResourceInfo resourceInfo = new ResourceInfo(template.Id, 1);
            AWorkerTask.ItemMapProvider().PutDownToDrop(availablePos, template.Tile, resourceInfo);

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

            // 在掉落位置生成浮动稀有度标签
            string rarityLabel = EquipmentLootTool.FormatRarityLabel(rarity);
            FloatingTextStatusProvider(worldPos, rarityLabel);

            AWorkerTask.LogProvider(
                string.Format("装备掉落: {0} [{1}] at ({2:F0},{3:F0})",
                    itemData.CnName, EquipmentLootTool.GetRarityName(rarity), worldPos.x, worldPos.y),
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

            ItemData itemData = AWorkerTask.ItemDataProvider(template.Id);
            if (itemData == null)
            {
                AWorkerTask.LogProvider("EnemyLootManager: 未找到 ItemData，Id=" + template.Id, LogManager.LogLevelEnum.Warning);
                return false;
            }

            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(worldPos);
            Vector3Int availablePos = AWorkerTask.AvailablePositionProvider(posMap, 5, true);
            if (availablePos == default)
            {
                return false;
            }

            ResourceInfo resourceInfo = new ResourceInfo(template.Id, 1);
            AWorkerTask.ItemMapProvider().PutDownToDrop(availablePos, template.Tile, resourceInfo);

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

            AWorkerTask.LogProvider(
                string.Format("强制装备掉落: {0} [{1}] at ({2:F0},{3:F0})",
                    itemData.CnName, EquipmentLootTool.GetRarityName(rarity), worldPos.x, worldPos.y),
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
        public void OnEquipmentPickup(AEquipment equipment, long equipmentId)
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
                    if (charData != null)
                    {
                        Vector3Int playerPos = AWorkerTask.TileMapWorldToMapProvider(
                            PlayerPositionProvider());
                        charData.AddEquipment(equipment, playerPos);
                    }
                }, () =>
                {
                    if (charData != null)
                    {
                        Vector3Int playerPos = AWorkerTask.TileMapWorldToMapProvider(
                            PlayerPositionProvider());
                        Vector3Int dropPos = AWorkerTask.AvailablePositionProvider(playerPos, 2, true);
                        if (dropPos != default && equipment.Tile != null)
                        {
                            AWorkerTask.ItemMapProvider().PutDownToDrop(dropPos, equipment.Tile,
                                new ResourceInfo(equipment.Id, 1));
                        }
                    }
                });
            }
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
            if (this.pendingDrops == null || this.pendingDrops.Count == 0) return;

            this.pendingDrops.Remove(mapPos);
            AWorkerTask.EquipmentBeamProvider().RemoveBeamAt(mapPos);
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
