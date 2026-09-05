namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LAB2D.Domain.Gameplay.DangerZone;
    using LAB2D.Render;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 危险区管理器（M4 包 4 地图兴趣点）— 空间惩罚 + 风险回报的运行时宿主。
    /// 每图撒 2~3 个圆形毒雾区（圆心距中心 ≥15 格、彼此不重叠），
    /// 区内：移动 ×0.7（Character.GetEffectiveMoveSpeed 接入，玩家/Worker 共用）、
    /// 灵气浓度 ×1.3（LingQiManager.ComposeAt 叠乘）、必撒 3~5 个资源点（险地生灵物）。
    /// 视觉程序化常显（DangerZoneGlow）。三路径同灵脉：新图 OnMapReady 撒 /
    /// 读档 LoadData 恢复 / 旧档迁移撒。
    /// </summary>
    public class DangerZoneManager : ASingletonSaveData<DangerZoneManager>, IInitializable
    {
        /// <summary>每张地图危险区数量下限/上限。</summary>
        public const int MinZoneCount = 2;
        public const int MaxZoneCount = 3;

        /// <summary>危险区半径（格）下限/上限。</summary>
        public const int MinZoneRadius = 10;
        public const int MaxZoneRadius = 14;

        /// <summary>每区资源点数量下限/上限。</summary>
        public const int MinResourcesPerZone = 3;
        public const int MaxResourcesPerZone = 5;

        /// <summary>撒点重试上限（地图受限时容忍少于目标数）。</summary>
        private const int MaxScatterRetries = 500;

        /// <summary>区内资源格采样重试上限（圆内可达富资源格有限时容忍）。</summary>
        private const int MaxResourceRetries = 200;

        internal static Func<TileMap> TileMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out TileMap tm) ? tm : null;

        internal static Func<TerrainConfigDatabase> TerrainDbProvider { get; set; }
            = () => ServiceLocator.TryGet(out TerrainConfigDatabase db) ? db : null;

        internal static Func<ResourceManager> ResourceAssetProvider { get; set; }
            = () => ServiceLocator.TryGet(out ResourceManager rm) ? rm : null;

        internal static Func<ResourceMap> ResourceMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out ResourceMap rmap) ? rmap : null;

        internal static Action<string> TipProvider { get; set; }
            = (msg) =>
            {
                try
                {
                    Core.GameServices.ShowTipProvider(msg);
                }
                catch (Exception)
                {
                    // Tip 不可用时静默降级（初始化早期/测试环境）
                }
            };

        private readonly List<DangerZoneRuleService.DangerZoneModel> zones = new List<DangerZoneRuleService.DangerZoneModel>();
        private readonly List<GameObject> zoneVisuals = new List<GameObject>();
        private bool pendingScatter;
        private bool initialized;

        /// <summary>危险区集合（只读视图，存档/调试用）。</summary>
        public IReadOnlyList<DangerZoneRuleService.DangerZoneModel> Zones => this.zones;

        /// <summary>是否存在危险区（无区地图/初始化早期的快速短路）。</summary>
        public bool HasZones => this.zones.Count > 0;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;

            MapInitCoordinator coordinator = ServiceLocator.Get<MapInitCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            if (coordinator.IsComplete)
            {
                this.OnMapReadyHandler();
            }
            else
            {
                coordinator.OnMapReady += this.OnMapReadyHandler;
            }
        }

        /// <summary>指定地图格是否在危险区内（Domain 纯函数转发）。</summary>
        public bool IsInZoneAt(int x, int y)
        {
            return DangerZoneRuleService.IsInZone(this.zones, x, y);
        }

        /// <summary>指定地图格的移动速度乘数（区内 ×0.7）。</summary>
        public float GetMoveSpeedMultiplier(Vector3Int posMap)
        {
            return DangerZoneRuleService.MoveSpeedMultiplier(this.IsInZoneAt(posMap.x, posMap.y));
        }

        /// <summary>世界坐标处的移动速度乘数（Character.GetEffectiveMoveSpeed 采样入口）。</summary>
        public float GetMoveSpeedMultiplierWorld(Vector3 worldPos)
        {
            TileMap tileMap = TileMapProvider();
            if (tileMap == null)
            {
                return 1f;
            }

            return this.GetMoveSpeedMultiplier(tileMap.WorldPosToMapPos(worldPos));
        }

        /// <summary>指定地图格的灵气浓度乘数（区内 ×1.3，LingQiManager.ComposeAt 叠乘）。</summary>
        public float GetQiDensityMultiplier(Vector3Int posMap)
        {
            return DangerZoneRuleService.QiDensityMultiplier(this.IsInZoneAt(posMap.x, posMap.y));
        }

        /// <summary>
        /// 撒危险区（幂等：已有区直接返回）。
        /// 候选走 GenCanReachPos（三层可达 + 非水），再过 Domain 撒点约束
        /// （距中心 ≥15 格、与既有区不重叠）；每区随即撒资源点。
        /// </summary>
        public void ScatterZones()
        {
            if (this.zones.Count > 0)
            {
                return;
            }

            TileMap tileMap = TileMapProvider();
            TileMap.TileMapData mapData = tileMap?.TileMapDataLAB;
            if (mapData?.MapTiles == null)
            {
                // 地图数据未就绪（旧档协程重生成路径）→ 等 OnMapReady 兜底
                this.pendingScatter = true;
                return;
            }

            int targetCount = UnityEngine.Random.Range(MinZoneCount, MaxZoneCount + 1);
            var mapCenter = new Domain.Common.GameVector2(mapData.Height / 2, mapData.Width / 2);
            int attempts = 0;

            while (this.zones.Count < targetCount && attempts < MaxScatterRetries)
            {
                attempts++;
                Vector3Int candidate = tileMap.GenCanReachPos();
                float radius = UnityEngine.Random.Range(MinZoneRadius, MaxZoneRadius + 1);
                var zone = new DangerZoneRuleService.DangerZoneModel(
                    new Domain.Common.GameVector2(candidate.x, candidate.y), radius);

                if (!DangerZoneRuleService.IsPlacementValid(this.zones, zone.Center, zone.Radius, mapCenter))
                {
                    continue;
                }

                this.zones.Add(zone);
                this.ScatterResourcesForZone(zone);
            }

            this.RebuildZoneVisuals();

            var sb = new StringBuilder();
            foreach (DangerZoneRuleService.DangerZoneModel zone in this.zones)
            {
                sb.Append($" ({zone.Center.X:F0},{zone.Center.Y:F0})r{zone.Radius:F0}");
            }

            AWorkerTask.LogProvider(
                $"[DangerZoneDiag] 危险区撒点完成 {this.zones.Count}/{targetCount}（尝试 {attempts} 次）：{sb}",
                LogManager.LogLevelEnum.Debug);
            TipProvider("远处毒雾弥漫，雾中灵气更浓，但寸步难行……");
        }

        /// <summary>
        /// 区内撒高价值资源点（圆内拒绝采样）：可达 + 非水 + 该地形有资源 tile + 无既有资源。
        /// 经 ResourceMap.ReservePosition 登记，防 GenResource 逐格扫描时同格二次放置
        /// （Dictionary.Add 重复 key 异常）；计数不进 TreeTotal/TreeCur——砍毒雾资源触发的
        /// GenTree 补种在区外随机位，总量守恒，无无限补种风险。
        /// </summary>
        private void ScatterResourcesForZone(DangerZoneRuleService.DangerZoneModel zone)
        {
            ResourceMap resourceMap = ResourceMapProvider();
            TileMap tileMap = TileMapProvider();
            TerrainConfigDatabase terrainDb = TerrainDbProvider();
            ResourceManager resourceAssets = ResourceAssetProvider();
            TileMap.TileMapData mapData = tileMap?.TileMapDataLAB;
            if (resourceMap == null || mapData?.MapTiles == null || terrainDb == null || resourceAssets == null)
            {
                return;
            }

            int target = UnityEngine.Random.Range(MinResourcesPerZone, MaxResourcesPerZone + 1);
            int placed = 0;
            int attempts = 0;
            int radiusInt = Mathf.CeilToInt(zone.Radius);

            while (placed < target && attempts < MaxResourceRetries)
            {
                attempts++;
                int dx = UnityEngine.Random.Range(-radiusInt, radiusInt + 1);
                int dy = UnityEngine.Random.Range(-radiusInt, radiusInt + 1);
                int x = (int)zone.Center.X + dx;
                int y = (int)zone.Center.Y + dy;

                if (dx * dx + dy * dy > radiusInt * radiusInt
                    || x < 0 || y < 0 || x >= mapData.Height || y >= mapData.Width)
                {
                    continue;
                }

                var posMap = new Vector3Int(x, y, 0);
                int terrainId = mapData.MapTiles[x, y];
                if (!tileMap.IsCanReach(posMap)
                    || terrainDb.IsWater(terrainId)
                    || !terrainDb.CanSpawnResources(terrainId)
                    || resourceMap.ResourceMapDataLAB.ContainKey(posMap))
                {
                    continue;
                }

                TileBase tileBase = resourceAssets.GetAssetByTerrainId(terrainId);
                if (tileBase == null)
                {
                    continue;
                }

                resourceMap.ReservePosition(posMap);
                resourceMap.SetTile(posMap, tileBase);
                resourceMap.ResourceMapDataLAB.Add(posMap, tileBase.name);
                placed++;
            }

            AWorkerTask.LogProvider(
                $"[DangerZoneDiag] 危险区 ({zone.Center.X:F0},{zone.Center.Y:F0}) 资源撒放 {placed}/{target}（尝试 {attempts} 次）",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>重建危险区视觉（Destroy+Create，读档恢复/撒点共用）。</summary>
        public void RebuildZoneVisuals()
        {
            foreach (GameObject go in this.zoneVisuals)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            this.zoneVisuals.Clear();

            TileMap tileMap = TileMapProvider();
            if (tileMap == null)
            {
                return;
            }

            // All/DangerZone 层级（参考 RoofManager 的 All/Building 惯例）
            Transform zoneParent = GameObject.Find("All/DangerZone")?.transform;
            if (zoneParent == null)
            {
                zoneParent = new GameObject("DangerZone").transform;
                GameObject all = GameObject.Find("All");
                if (all != null)
                {
                    zoneParent.SetParent(all.transform);
                }
            }

            foreach (DangerZoneRuleService.DangerZoneModel zone in this.zones)
            {
                var go = new GameObject($"DangerZone_{zone.Center.X:F0}_{zone.Center.Y:F0}");
                go.transform.position = tileMap.MapPosToWorldPos(
                    new Vector3Int((int)zone.Center.X, (int)zone.Center.Y, 0));
                go.transform.SetParent(zoneParent);
                DangerZoneGlow glow = go.AddComponent<DangerZoneGlow>();
                glow.RadiusCells = zone.Radius;
                this.zoneVisuals.Add(go);
            }
        }

        /// <summary>地图就绪：新图撒点；旧档无地图数据走协程重生成时（pendingScatter）补撒。</summary>
        private void OnMapReadyHandler()
        {
            if (GlobalData.IsNew)
            {
                this.ScatterZones();
            }
            else if (this.pendingScatter)
            {
                this.pendingScatter = false;
                this.ScatterZones();
                AWorkerTask.LogProvider("[DangerZoneDiag] 旧档迁移撒点（地图协程重生成后兜底）", LogManager.LogLevelEnum.Debug);
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            var data = new DangerZoneManagerData();
            foreach (DangerZoneRuleService.DangerZoneModel zone in this.zones)
            {
                data.Zones.Add(new ZoneEntry { X = (int)zone.Center.X, Y = (int)zone.Center.Y, Radius = (int)zone.Radius });
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            DangerZoneManagerData data = DataTool.LoadDataByBinary<DangerZoneManagerData>(
                GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data?.Zones != null && data.Zones.Count > 0)
            {
                this.zones.Clear();
                foreach (ZoneEntry entry in data.Zones)
                {
                    this.zones.Add(new DangerZoneRuleService.DangerZoneModel(
                        new Domain.Common.GameVector2(entry.X, entry.Y), entry.Radius));
                }

                this.RebuildZoneVisuals();
                AWorkerTask.LogProvider(
                    $"[DangerZoneDiag] 危险区读档恢复 {this.zones.Count} 处（资源点已在 ResourceMap 档内）",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            // 旧档（无危险区数据）→ 迁移撒点；MapTiles 未就绪时 ScatterZones 内部置 pendingScatter
            this.ScatterZones();
        }

        [Serializable]
        public class DangerZoneManagerData
        {
            public List<ZoneEntry> Zones = new List<ZoneEntry>();
        }

        [Serializable]
        public class ZoneEntry
        {
            public int X;
            public int Y;
            public int Radius;
        }
    }
}
