namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 装备掉落管理器。
    /// 在敌人死亡时按稀有度权重随机生成装备掉落，管理掉落概率和拾取对比流程。
    /// 单例 MonoBehaviour，由 GlobalInit 初始化。
    /// 不修改 EnemyDropManager 核心逻辑，作为额外掉落增强层运行。
    /// </summary>
    public class EquipmentLootManager : Singleton<EquipmentLootManager>
    {
        /// <summary>是否已初始化</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 等待对比的装备数据：key=装备Id，value=装备属性数据
        /// 当玩家拾取装备时，从此字典查找并弹出对比弹窗。
        /// </summary>
        private Dictionary<long, PendingEquipmentDrop> pendingDrops = new Dictionary<long, PendingEquipmentDrop>();

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
            this.pendingDrops = new Dictionary<long, PendingEquipmentDrop>();
            EquipmentBeamManager.Instance.Initialize();
            this.IsInitialized = true;
            LogManager.Instance.Log("EquipmentLootManager 初始化完成", LogManager.LogLevelEnum.Trace);
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
            float roll = Random.Range(0f, 1f);
            if (roll > EquipmentLootConstant.BaseEquipmentDropChance)
            {
                return false;
            }

            // 随机稀有度
            EquipmentRarityType rarity = EquipmentLootTool.RollRarity(waveNumber);

            // 从已有物品工厂获取可用的装备列表
            List<AItem> allItems = ItemInstanceFactory.Instance.GenBackpackItems();
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
            AEquipment template = availableEquipment[Random.Range(0, availableEquipment.Count)];

            // 对模板属性应用稀有度倍率
            EquipmentLootTool.ApplyRarityToAttributes(template.Attribute, rarity);
            // 将稀有度写入装备的 Quality 字段，供装备面板持续展示
            template.Quality = EquipmentLootTool.MapRarityToQuality(rarity);

            // 通过 ItemMap 将装备放置到地面
            ItemData itemData = ItemDataManager.Instance.GetById(template.Id);
            if (itemData == null)
            {
                LogManager.Instance.Log("EquipmentLootManager: 未找到 ItemData，Id=" + template.Id, LogManager.LogLevelEnum.Warning);
                return false;
            }

            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(worldPos);
            Vector3Int availablePos = IsAvailableMap.Instance.GenAvailablePosMap(posMap, 3, true);
            if (availablePos == default)
            {
                return false;
            }

            ResourceInfo resourceInfo = new ResourceInfo(template.Id, 1);
            ItemMap.Instance.PutDownToDrop(availablePos, template.Tile, resourceInfo);

            // 记录待处理掉落，供拾取时对比使用
            PendingEquipmentDrop pending = new PendingEquipmentDrop
            {
                Equipment = template,
                Rarity = rarity,
                DropPosition = worldPos,
                EquipmentId = template.Id,
                MapPosition = availablePos,
            };
            this.pendingDrops[template.Id] = pending;

            // 在掉落位置生成光束特效（按稀有度着色）
            Vector3 beamWorldPos = TileMap.Instance.MapPosToWorldPos(availablePos);
            EquipmentBeamManager.Instance.SpawnBeam(availablePos, beamWorldPos, rarity);

            // 在掉落位置生成浮动稀有度标签
            string rarityLabel = EquipmentLootTool.FormatRarityLabel(rarity);
            FloatingTextManager.Instance?.SpawnStatusText(worldPos, rarityLabel);

            LogManager.Instance.Log(
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
            List<AItem> allItems = ItemInstanceFactory.Instance.GenBackpackItems();
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

            AEquipment template = availableEquipment[Random.Range(0, availableEquipment.Count)];
            EquipmentLootTool.ApplyRarityToAttributes(template.Attribute, rarity);
            template.Quality = EquipmentLootTool.MapRarityToQuality(rarity);

            ItemData itemData = ItemDataManager.Instance.GetById(template.Id);
            if (itemData == null)
            {
                LogManager.Instance.Log("EquipmentLootManager: 未找到 ItemData，Id=" + template.Id, LogManager.LogLevelEnum.Warning);
                return false;
            }

            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(worldPos);
            Vector3Int availablePos = IsAvailableMap.Instance.GenAvailablePosMap(posMap, 5, true);
            if (availablePos == default)
            {
                return false;
            }

            ResourceInfo resourceInfo = new ResourceInfo(template.Id, 1);
            ItemMap.Instance.PutDownToDrop(availablePos, template.Tile, resourceInfo);

            PendingEquipmentDrop pending = new PendingEquipmentDrop
            {
                Equipment = template,
                Rarity = rarity,
                DropPosition = worldPos,
                EquipmentId = template.Id,
                MapPosition = availablePos,
            };
            this.pendingDrops[template.Id] = pending;

            Vector3 beamWorldPos = TileMap.Instance.MapPosToWorldPos(availablePos);
            EquipmentBeamManager.Instance.SpawnBeam(availablePos, beamWorldPos, rarity);

            string rarityLabel = EquipmentLootTool.FormatRarityLabel(rarity);
            FloatingTextManager.Instance?.SpawnStatusText(worldPos, rarityLabel);

            LogManager.Instance.Log(
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

            if (this.pendingDrops.TryGetValue(itemId, out PendingEquipmentDrop pending))
            {
                // 移除旧位置的光束
                EquipmentBeamManager.Instance.RemoveBeamAt(pending.MapPosition);

                // 更新记录的坐标
                pending.MapPosition = posMap;
                pending.DropPosition = TileMap.Instance.MapPosToWorldPos(posMap);

                // 在仓库位置生成新光束
                Vector3 beamWorldPos = TileMap.Instance.MapPosToWorldPos(posMap);
                EquipmentBeamManager.Instance.SpawnBeam(posMap, beamWorldPos, pending.Rarity);
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

            // 查找是否有待处理的稀有度信息
            EquipmentRarityType rarity = EquipmentRarityType.Common;
            if (this.pendingDrops.TryGetValue(equipmentId, out PendingEquipmentDrop pending))
            {
                rarity = pending.Rarity;
                // 确保稀有度已应用到属性
                EquipmentLootTool.ApplyRarityToAttributes(equipment.Attribute, rarity);
                equipment.Quality = EquipmentLootTool.MapRarityToQuality(rarity);
                this.pendingDrops.Remove(equipmentId);
            }

            // 获取当前已装备的同槽位装备
            AEquipment.EquipTypeEnum slotType = equipment.Type;
            Character.CharacterData charData = PlayerManager.Instance?.Mine?.CharacterDataLAB;
            AEquipment currentEquipped = null;
            if (charData != null)
            {
                Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = charData.GetEquipments();
                if (equipments != null && equipments.TryGetValue(slotType, out AEquipment existing))
                {
                    currentEquipped = existing;
                }
            }

            // 显示对比弹窗
            EquipmentComparePopup popup = EquipmentComparePopup.Instance;
            if (popup == null)
            {
                EquipmentComparePopup.EnsureRuntimePopup();
                popup = EquipmentComparePopup.Instance;
            }

            if (popup != null)
            {
                popup.ShowCompare(currentEquipped?.Attribute, equipment.Attribute, rarity, slotType, () =>
                {
                    // 替换：装备新装备
                    if (charData != null)
                    {
                        Vector3Int playerPos = TileMap.Instance.WorldPosToMapPos(
                            PlayerManager.Instance.Mine.transform.position);
                        charData.AddEquipment(equipment, playerPos);
                    }
                }, () =>
                {
                    // 丢弃：放回地面
                    if (charData != null)
                    {
                        Vector3Int playerPos = TileMap.Instance.WorldPosToMapPos(
                            PlayerManager.Instance.Mine.transform.position);
                        Vector3Int dropPos = IsAvailableMap.Instance.GenAvailablePosMap(playerPos, 2, true);
                        if (dropPos != default && equipment.Tile != null)
                        {
                            ItemMap.Instance.PutDownToDrop(dropPos, equipment.Tile,
                                new ResourceInfo(equipment.Id, 1));
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 根据地图坐标移除待处理掉落记录和光束特效。
        /// 由 ItemMap.DeleteTile() 在物品被拾取/移除时回调。
        /// 非装备 tile 安全：坐标不存在时无操作。
        /// </summary>
        /// <param name="mapPos">Tilemap 坐标</param>
        public void RemoveDropByMapPosition(Vector3Int mapPos)
        {
            if (this.pendingDrops == null || this.pendingDrops.Count == 0) return;

            // 遍历查找匹配 MapPosition 的待处理掉落
            long? matchedKey = null;
            foreach (KeyValuePair<long, PendingEquipmentDrop> kv in this.pendingDrops)
            {
                if (kv.Value.MapPosition == mapPos)
                {
                    matchedKey = kv.Key;
                    break;
                }
            }

            if (matchedKey.HasValue)
            {
                this.pendingDrops.Remove(matchedKey.Value);
            }

            // 移除光束特效（安全调用：位置不存在光束时无操作）
            EquipmentBeamManager.Instance.RemoveBeamAt(mapPos);
        }

        /// <summary>
        /// 清理待处理掉落记录（拾取超时或远离后清理）。
        /// </summary>
        public void CleanupStaleDrops()
        {
            if (this.pendingDrops == null) return;
            List<long> staleKeys = new List<long>();
            Vector3 playerPos = PlayerManager.Instance?.Mine?.transform.position ?? Vector3.zero;

            foreach (KeyValuePair<long, PendingEquipmentDrop> kv in this.pendingDrops)
            {
                float dist = Vector3.Distance(kv.Value.DropPosition, playerPos);
                if (dist > 30f) // 30 单位外的掉落视为过期
                {
                    staleKeys.Add(kv.Key);
                }
            }

            foreach (long key in staleKeys)
            {
                // 移除光束特效
                if (this.pendingDrops.TryGetValue(key, out PendingEquipmentDrop pending))
                {
                    EquipmentBeamManager.Instance.RemoveBeamAt(pending.MapPosition);
                }

                this.pendingDrops.Remove(key);
            }

            // 同步清理光束（tile 已被移除但 pendingDrops 未记录的情况）
            EquipmentBeamManager.Instance.CleanupStaleBeams();
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
    }
}
