namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LAB2D.Domain.Gameplay.LingQi;
    using LAB2D.Item.Build.Furniture;
    using LAB2D.Render;
    using UnityEngine;

    /// <summary>
    /// 灵气环境管理器（M4）— 空间灵气浓度图的运行时宿主。
    /// 浓度 M(pos) = T(地形) × V(灵脉) × A(聚灵阵) × W(天气)，合成纯函数在 LingQiRuleService。
    /// 灵脉点集入档（ASingletonSaveData）；聚灵阵点集不入档（BuildMap 已存，2s 重扫恢复）。
    /// 三路径：新图 OnMapReady 撒点 / 读 M4 档 LoadData 恢复 / 读旧档 LoadData 迁移撒点。
    /// </summary>
    public class LingQiManager : ASingletonSaveData<LingQiManager>, ITickable, IInitializable
    {
        /// <summary>每张地图灵脉数量。</summary>
        public const int VeinCount = 8;

        /// <summary>灵脉之间的最小间距（格）。</summary>
        public const int MinVeinDistance = 25;

        /// <summary>灵脉离地图中心（出生区）的最小距离（格）。</summary>
        public const int MinDistanceFromMapCenter = 15;

        /// <summary>已建成聚灵阵重扫间隔（秒）。</summary>
        private const float ArrayScanInterval = 2f;

        /// <summary>撒点重试上限（地图受限时容忍少于 VeinCount 条）。</summary>
        private const int MaxScatterRetries = 500;

        internal static Func<TileMap> TileMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out TileMap tm) ? tm : null;

        internal static Func<BuildMap> BuildMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out BuildMap bm) ? bm : null;

        internal static Func<TerrainConfigDatabase> TerrainDbProvider { get; set; }
            = () => ServiceLocator.TryGet(out TerrainConfigDatabase db) ? db : null;

        internal static Func<IWeatherGameplayService> WeatherProvider { get; set; }
            = () => ServiceLocator.TryGet(out IWeatherGameplayService w) ? w : null;

        private readonly List<Vector3Int> veins = new List<Vector3Int>();
        private readonly List<Vector3Int> arrayCenters = new List<Vector3Int>();
        private readonly List<GameVector2> veinPoints = new List<GameVector2>();
        private readonly List<GameVector2> arrayPoints = new List<GameVector2>();
        private readonly List<GameObject> veinVisuals = new List<GameObject>();
        private bool pendingScatter;
        private bool initialized;
        private float arrayScanTimer;
        private int lastLoggedArrayCount = -1;

        /// <summary>灵脉点集（只读视图，存档/调试用）。</summary>
        public IReadOnlyList<Vector3Int> Veins => this.veins;

        /// <summary>已建成聚灵阵数量（缓存值，Tick 节流刷新）。</summary>
        public int SpiritArrayCount => this.arrayCenters.Count;

        /// <summary>浓度分项 — UI 分解显示用。</summary>
        public struct LocalFactors
        {
            /// <summary>地形倍率（SO qiDensityMultiplier）。</summary>
            public float Terrain;

            /// <summary>是否在灵脉增幅范围内（10 格，×1.5 单层）。</summary>
            public bool VeinBoosted;

            /// <summary>半径 4 格内已建成聚灵阵数（生效封顶 3）。</summary>
            public int Arrays;

            /// <summary>天气倍率（EnergyRecoveryMultiplier）。</summary>
            public float Weather;

            /// <summary>合成浓度（全部因子相乘）。</summary>
            public float Composed;
        }

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

        /// <summary>
        /// 地图就绪：新图撒点；旧档无地图数据走协程重生成时（pendingScatter）补撒。
        /// 读 M4 档不撒（LoadData 已恢复点集）。
        /// </summary>
        private void OnMapReadyHandler()
        {
            if (GlobalData.IsNew)
            {
                this.ScatterVeins();
            }
            else if (this.pendingScatter)
            {
                this.pendingScatter = false;
                this.ScatterVeins();
                AWorkerTask.LogProvider("[LingQiDiag] 旧档迁移撒点（地图协程重生成后兜底）", LogManager.LogLevelEnum.Debug);
            }
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.arrayScanTimer += deltaTime;
            if (this.arrayScanTimer < ArrayScanInterval)
            {
                return;
            }

            this.arrayScanTimer = 0f;
            this.RescanSpiritArrays();
        }

        /// <summary>查指定地图格的灵气浓度（地图未就绪/越界返 1，安全降级）。</summary>
        public float GetDensityAt(Vector3Int posMap)
        {
            return this.ComposeAt(posMap, out _);
        }

        /// <summary>查世界坐标处的灵气浓度（玩家/Worker 采样入口）。</summary>
        public float GetDensityAtWorld(Vector3 worldPos)
        {
            TileMap tileMap = TileMapProvider();
            if (tileMap == null)
            {
                return 1f;
            }

            return this.GetDensityAt(tileMap.WorldPosToMapPos(worldPos));
        }

        /// <summary>查指定地图格的浓度分项（UI 分解显示）。</summary>
        public LocalFactors GetLocalFactors(Vector3Int posMap)
        {
            this.ComposeAt(posMap, out LocalFactors factors);
            return factors;
        }

        /// <summary>
        /// 撒灵脉点（幂等：已有脉点直接返回）。
        /// GenCanReachPos 已含 TileMap/ResourceMap/BuildMap 三层可达 + 非水；
        /// 此处再约束：距地图中心（出生区）> 15 格 + 脉间距 ≥ 25 格。
        /// </summary>
        public void ScatterVeins()
        {
            if (this.veins.Count > 0)
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

            Vector3Int center = new Vector3Int(mapData.Height / 2, mapData.Width / 2, 0);
            int centerSqr = MinDistanceFromMapCenter * MinDistanceFromMapCenter;
            int veinSqr = MinVeinDistance * MinVeinDistance;
            int attempts = 0;

            while (this.veins.Count < VeinCount && attempts < MaxScatterRetries)
            {
                attempts++;
                Vector3Int candidate = tileMap.GenCanReachPos();
                if ((candidate - center).sqrMagnitude < centerSqr)
                {
                    continue;
                }

                bool tooClose = false;
                foreach (Vector3Int vein in this.veins)
                {
                    if ((candidate - vein).sqrMagnitude < veinSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    this.veins.Add(candidate);
                }
            }

            this.RebuildPoints();
            this.RebuildVeinVisuals();

            var sb = new StringBuilder();
            foreach (Vector3Int vein in this.veins)
            {
                sb.Append($" ({vein.x},{vein.y})");
            }

            AWorkerTask.LogProvider(
                $"[LingQiDiag] 灵脉撒点完成 {this.veins.Count}/{VeinCount}（尝试 {attempts} 次）：{sb}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>重建灵脉视觉（Destroy+Create，读档恢复/撒点共用）。</summary>
        public void RebuildVeinVisuals()
        {
            foreach (GameObject go in this.veinVisuals)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            this.veinVisuals.Clear();

            TileMap tileMap = TileMapProvider();
            if (tileMap == null)
            {
                return;
            }

            foreach (Vector3Int vein in this.veins)
            {
                GameObject go = new GameObject($"LingVein_{vein.x}_{vein.y}");
                go.transform.position = tileMap.MapPosToWorldPos(vein);
                go.AddComponent<LingVeinGlow>();
                this.veinVisuals.Add(go);
            }
        }

        /// <summary>
        /// 立即重扫已建成聚灵阵主格（Tick 节流调用）。
        /// 1×1 建筑无副格，IsComplete 即生效（ArrowTowerManager.RescanTowers 同款）。
        /// </summary>
        public void RescanSpiritArrays()
        {
            this.arrayCenters.Clear();
            BuildMap buildMap = BuildMapProvider();
            if (buildMap?.BuildMapDataLAB?.PosMap == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector3IntLAB, BuildMap.BuildTileData> kv in buildMap.BuildMapDataLAB.PosMap)
            {
                BuildMap.BuildTileData tile = kv.Value;
                if (tile != null && tile.IsComplete && tile.Name == nameof(SpiritArray))
                {
                    this.arrayCenters.Add(new Vector3Int(kv.Key.X, kv.Key.Y, 0));
                }
            }

            this.RebuildArrayPoints();

            if (this.arrayCenters.Count != this.lastLoggedArrayCount)
            {
                this.lastLoggedArrayCount = this.arrayCenters.Count;
                AWorkerTask.LogProvider(
                    $"[LingQiDiag] 聚灵阵数量变化：{this.arrayCenters.Count}",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            LingQiManagerData data = new LingQiManagerData();
            foreach (Vector3Int vein in this.veins)
            {
                data.Veins.Add(new VeinEntry { X = vein.x, Y = vein.y, Z = vein.z });
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            LingQiManagerData data = DataTool.LoadDataByBinary<LingQiManagerData>(
                GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data?.Veins != null && data.Veins.Count > 0)
            {
                this.veins.Clear();
                foreach (VeinEntry entry in data.Veins)
                {
                    this.veins.Add(new Vector3Int(entry.X, entry.Y, entry.Z));
                }

                this.RebuildPoints();
                this.RebuildVeinVisuals();
                AWorkerTask.LogProvider(
                    $"[LingQiDiag] 灵脉读档恢复 {this.veins.Count} 条",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            // 旧档（无灵脉数据）→ 迁移撒点；MapTiles 未就绪时 ScatterVeins 内部置 pendingScatter
            this.ScatterVeins();
        }

        /// <summary>合成指定格浓度并输出分项。</summary>
        private float ComposeAt(Vector3Int posMap, out LocalFactors factors)
        {
            factors = default;
            factors.Weather = 1f;

            TileMap tileMap = TileMapProvider();
            TileMap.TileMapData mapData = tileMap?.TileMapDataLAB;
            if (mapData?.MapTiles == null)
            {
                return 1f;
            }

            int x = posMap.x;
            int y = posMap.y;
            if (x < 0 || y < 0 || x >= mapData.Height || y >= mapData.Width)
            {
                return 1f;
            }

            int terrainId = mapData.MapTiles[x, y];
            TerrainConfigDatabase db = TerrainDbProvider();
            factors.Terrain = db != null ? db.GetQiDensityMultiplier(terrainId) : 1f;

            float veinDist = LingQiRuleService.NearestVeinDistance(this.veinPoints, x, y);
            factors.VeinBoosted = veinDist <= LingQiRuleService.VeinBoostRadius;

            factors.Arrays = LingQiRuleService.CountArraysInRange(this.arrayPoints, x, y);

            IWeatherGameplayService weather = WeatherProvider();
            if (weather != null)
            {
                factors.Weather = weather.EnergyRecoveryMultiplier;
            }

            factors.Composed = LingQiRuleService.ComposeMultiplier(
                factors.Terrain, veinDist, factors.Arrays, factors.Weather);
            return factors.Composed;
        }

        private void RebuildPoints()
        {
            this.veinPoints.Clear();
            foreach (Vector3Int vein in this.veins)
            {
                this.veinPoints.Add(new GameVector2(vein.x, vein.y));
            }
        }

        private void RebuildArrayPoints()
        {
            this.arrayPoints.Clear();
            foreach (Vector3Int center in this.arrayCenters)
            {
                this.arrayPoints.Add(new GameVector2(center.x, center.y));
            }
        }

        [Serializable]
        public class LingQiManagerData
        {
            public List<VeinEntry> Veins = new List<VeinEntry>();
        }

        [Serializable]
        public class VeinEntry
        {
            public int X;
            public int Y;
            public int Z;
        }
    }
}
