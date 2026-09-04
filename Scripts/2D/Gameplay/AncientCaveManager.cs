namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LAB2D.Domain.Gameplay.AncientCave;
    using LAB2D.Render;
    using UnityEngine;

    /// <summary>
    /// 上古洞府管理器（M4 包 4 地图兴趣点）— 探索型 POI 的运行时宿主。
    /// 每图撒 3~4 个（可达格 + 距中心 ≥20 格 + 间距 ≥30 格）；走近（≤8 格）才揭示：
    /// 程序化洞口视觉淡入 + Tip「发现上古洞府！」。状态机 Hidden→Revealed→（轮 3：
    /// Exploring→Explored 探索闭环）。三路径同灵脉：新图 OnMapReady 撒 /
    /// 读档 LoadData 恢复 / 旧档迁移撒。
    /// </summary>
    public class AncientCaveManager : ASingletonSaveData<AncientCaveManager>, IInitializable, ITickable
    {
        /// <summary>每张地图洞府数量下限/上限。</summary>
        public const int MinCaveCount = 3;
        public const int MaxCaveCount = 4;

        /// <summary>撒点重试上限（地图受限时容忍少于目标数）。</summary>
        private const int MaxScatterRetries = 500;

        /// <summary>揭示扫描节流间隔（秒）。</summary>
        private const float RevealScanInterval = 0.5f;

        internal static Func<TileMap> TileMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out TileMap tm) ? tm : null;

        internal static Func<Player> PlayerProvider { get; set; }
            = () => ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;

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

        private readonly List<AncientCaveRuleService.AncientCaveModel> caves = new List<AncientCaveRuleService.AncientCaveModel>();
        private readonly Dictionary<int, GameObject> caveVisuals = new Dictionary<int, GameObject>(); // key = 洞府索引
        private bool pendingScatter;
        private bool initialized;
        private float revealScanTimer;

        /// <summary>洞府集合（只读视图，存档/调试用）。</summary>
        public IReadOnlyList<AncientCaveRuleService.AncientCaveModel> Caves => this.caves;

        /// <summary>未揭示洞府数（UI/调试）。</summary>
        public int HiddenCount
        {
            get
            {
                int count = 0;
                foreach (AncientCaveRuleService.AncientCaveModel cave in this.caves)
                {
                    if (cave.State == AncientCaveRuleService.CaveState.Hidden)
                    {
                        count++;
                    }
                }

                return count;
            }
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
        /// 揭示扫描（Tick 0.5s 节流）：玩家走近 Hidden 洞府 ≤8 格 → Revealed +
        /// 洞口视觉淡入 + Tip。揭示是单向门（Revealed 后不再回退）。
        /// </summary>
        public void Tick(float deltaTime)
        {
            this.revealScanTimer += deltaTime;
            if (this.revealScanTimer < RevealScanInterval)
            {
                return;
            }

            this.revealScanTimer = 0f;
            if (this.HiddenCount == 0)
            {
                return; // 全揭示后零开销短路
            }

            Player player = PlayerProvider();
            TileMap tileMap = TileMapProvider();
            if (player == null || tileMap == null)
            {
                return;
            }

            Vector3Int playerPos = tileMap.WorldPosToMapPos(player.transform.position);
            var playerPoint = new Domain.Common.GameVector2(playerPos.x, playerPos.y);
            for (int i = 0; i < this.caves.Count; i++)
            {
                if (this.caves[i].State != AncientCaveRuleService.CaveState.Hidden)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(playerPoint.SqrDistanceTo(this.caves[i].Pos));
                if (!AncientCaveRuleService.ShouldReveal(distance))
                {
                    continue;
                }

                this.caves[i] = new AncientCaveRuleService.AncientCaveModel(
                    this.caves[i].Pos, AncientCaveRuleService.CaveState.Revealed);
                this.CreateCaveVisual(i, immediate: false);
                TipProvider($"发现上古洞府！洞中似有灵光流转……（走近可探，轮 3 开放探索）");
                AWorkerTask.LogProvider(
                    $"[AncientCaveDiag] 洞府揭示 idx={i} pos=({this.caves[i].Pos.X:F0},{this.caves[i].Pos.Y:F0}) 玩家距 {distance:F1} 格",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>
        /// 查玩家附近的已揭示洞府（轮 3 探索交互入口：N 键提示条定位用）。
        /// </summary>
        /// <param name="posMap">观察者地图格。</param>
        /// <param name="radius">搜索半径（格）。</param>
        /// <returns>最近的已揭示洞府索引，无则 -1。</returns>
        public int FindRevealedCaveNear(Vector3Int posMap, float radius)
        {
            var pos = new Domain.Common.GameVector2(posMap.x, posMap.y);
            int best = -1;
            float bestSqr = radius * radius;
            for (int i = 0; i < this.caves.Count; i++)
            {
                if (this.caves[i].State != AncientCaveRuleService.CaveState.Revealed)
                {
                    continue;
                }

                float sqr = pos.SqrDistanceTo(this.caves[i].Pos);
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// 撒洞府（幂等：已有直接返回）。候选走 GenCanReachPos（三层可达 + 非水），
        /// 再过 Domain 撒点约束（距中心 ≥20 格、间距 ≥30 格）。
        /// </summary>
        public void ScatterCaves()
        {
            if (this.caves.Count > 0)
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

            int targetCount = UnityEngine.Random.Range(MinCaveCount, MaxCaveCount + 1);
            var mapCenter = new Domain.Common.GameVector2(mapData.Height / 2, mapData.Width / 2);
            int attempts = 0;

            while (this.caves.Count < targetCount && attempts < MaxScatterRetries)
            {
                attempts++;
                Vector3Int candidate = tileMap.GenCanReachPos();
                var cave = new AncientCaveRuleService.AncientCaveModel(
                    new Domain.Common.GameVector2(candidate.x, candidate.y), AncientCaveRuleService.CaveState.Hidden);

                if (!AncientCaveRuleService.IsPlacementValid(this.caves, cave.Pos, mapCenter))
                {
                    continue;
                }

                this.caves.Add(cave);
            }

            var sb = new StringBuilder();
            foreach (AncientCaveRuleService.AncientCaveModel cave in this.caves)
            {
                sb.Append($" ({cave.Pos.X:F0},{cave.Pos.Y:F0})");
            }

            AWorkerTask.LogProvider(
                $"[AncientCaveDiag] 洞府撒点完成 {this.caves.Count}/{targetCount}（尝试 {attempts} 次）：{sb}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>创建洞府视觉（仅 Revealed 及之后调用；Hidden 无视觉——走近才揭示）。</summary>
        private void CreateCaveVisual(int index, bool immediate)
        {
            if (this.caveVisuals.ContainsKey(index))
            {
                return;
            }

            TileMap tileMap = TileMapProvider();
            if (tileMap == null)
            {
                return;
            }

            AncientCaveRuleService.AncientCaveModel cave = this.caves[index];
            var go = new GameObject($"AncientCave_{cave.Pos.X:F0}_{cave.Pos.Y:F0}");
            go.transform.position = tileMap.MapPosToWorldPos(
                new Vector3Int((int)cave.Pos.X, (int)cave.Pos.Y, 0));
            AncientCaveGlow glow = go.AddComponent<AncientCaveGlow>();
            if (immediate)
            {
                glow.ShowImmediately();
            }
            else
            {
                glow.BeginFade();
            }

            this.caveVisuals[index] = go;
        }

        /// <summary>地图就绪：新图撒点；旧档无地图数据走协程重生成时（pendingScatter）补撒。</summary>
        private void OnMapReadyHandler()
        {
            if (GlobalData.IsNew)
            {
                this.ScatterCaves();
            }
            else if (this.pendingScatter)
            {
                this.pendingScatter = false;
                this.ScatterCaves();
                AWorkerTask.LogProvider("[AncientCaveDiag] 旧档迁移撒点（地图协程重生成后兜底）", LogManager.LogLevelEnum.Debug);
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            var data = new AncientCaveManagerData();
            foreach (AncientCaveRuleService.AncientCaveModel cave in this.caves)
            {
                data.Caves.Add(new CaveEntry { X = (int)cave.Pos.X, Y = (int)cave.Pos.Y, State = (int)cave.State });
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            AncientCaveManagerData data = DataTool.LoadDataByBinary<AncientCaveManagerData>(
                GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data?.Caves != null && data.Caves.Count > 0)
            {
                this.caves.Clear();
                foreach (CaveEntry entry in data.Caves)
                {
                    var state = (AncientCaveRuleService.CaveState)Mathf.Clamp(entry.State,
                        (int)AncientCaveRuleService.CaveState.Hidden, (int)AncientCaveRuleService.CaveState.Explored);
                    this.caves.Add(new AncientCaveRuleService.AncientCaveModel(
                        new Domain.Common.GameVector2(entry.X, entry.Y), state));
                }

                // 已揭示的洞府重建视觉（立即全显，跳过淡入）；Hidden 保持无视觉
                for (int i = 0; i < this.caves.Count; i++)
                {
                    if (this.caves[i].State != AncientCaveRuleService.CaveState.Hidden)
                    {
                        this.CreateCaveVisual(i, immediate: true);
                    }
                }

                AWorkerTask.LogProvider(
                    $"[AncientCaveDiag] 洞府读档恢复 {this.caves.Count} 处（Hidden {this.HiddenCount}）",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            // 旧档（无洞府数据）→ 迁移撒点；MapTiles 未就绪时 ScatterCaves 内部置 pendingScatter
            this.ScatterCaves();
        }

        [Serializable]
        public class AncientCaveManagerData
        {
            public List<CaveEntry> Caves = new List<CaveEntry>();
        }

        [Serializable]
        public class CaveEntry
        {
            public int X;
            public int Y;
            public int State;
        }
    }
}
