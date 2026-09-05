namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task.Individual;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.AncientCave;
    using LAB2D.Domain.Gameplay.GongFa;
    using LAB2D.Domain.Wave;
    using LAB2D.Render;
    using UnityEngine;
    using GameCharacter = LAB2D.Character.Character;

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

        /// <summary>探索交互半径（格）：N/O 键与提示条在此范围内定位洞府。</summary>
        public const float ExploreInteractRadius = 2f;

        /// <summary>妖兽生成距离（格，洞府旁环带）。</summary>
        private const float DangerSpawnRadius = 3f;

        /// <summary>奖励掉落按第 5 波档（waveNumber 从 0 计，4=第 5 波）。</summary>
        private const int RewardWaveNumber = 4;

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

        // 玩家亲自探索读条状态（N 键触发；移动/受击打断，打坐同款）
        private int playerExploreCaveIndex = -1;
        private float playerExploreElapsed;
        private Vector3 playerExploreStartPos;

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
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(this.OnCharacterDamaged);

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
            this.TickPlayerExplore(deltaTime);

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

        /// <summary>玩家读条进行中（HUD 进度条用）。</summary>
        public bool IsPlayerExploring => this.playerExploreCaveIndex >= 0;

        /// <summary>玩家读条进度 [0,1]（非读条中为 0）。</summary>
        public float PlayerExploreProgress
        {
            get
            {
                if (this.playerExploreCaveIndex < 0)
                {
                    return 0f;
                }

                return Mathf.Clamp01(this.playerExploreElapsed / CaveExploreRuleService.PlayerExploreSeconds);
            }
        }

        /// <summary>
        /// N 键 — 玩家在已揭示洞府旁开始亲自探索读条（30s，移动/受击打断）。
        /// 洞府置 Exploring 占用（O 键派工与重复开始被状态机拒绝）。
        /// </summary>
        public bool TryStartPlayerExplore()
        {
            if (this.playerExploreCaveIndex >= 0)
            {
                return false; // 已在读条
            }

            Player player = PlayerProvider();
            TileMap tileMap = TileMapProvider();
            if (player == null || tileMap == null)
            {
                return false;
            }

            Vector3Int posMap = tileMap.WorldPosToMapPos(player.transform.position);
            int index = this.FindRevealedCaveNear(posMap, ExploreInteractRadius);
            if (index < 0)
            {
                return false;
            }

            this.playerExploreCaveIndex = index;
            this.playerExploreElapsed = 0f;
            this.playerExploreStartPos = player.transform.position;
            this.SetCaveState(index, AncientCaveRuleService.CaveState.Exploring);
            TipProvider($"开始探索上古洞府（{CaveExploreRuleService.PlayerExploreSeconds:F0} 秒）……保持静止，移动或受击会打断");
            AWorkerTask.LogProvider(
                $"[AncientCaveDiag] 玩家开始探索 idx={index} pos=({this.caves[index].Pos.X:F0},{this.caves[index].Pos.Y:F0})",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>O 键 — 派最近的空闲 Worker 探索附近洞府（WorkerExploreTask 驻留 60s）。</summary>
        public bool TryDispatchWorkerExplore()
        {
            Player player = PlayerProvider();
            TileMap tileMap = TileMapProvider();
            if (player == null || tileMap == null)
            {
                return false;
            }

            Vector3Int posMap = tileMap.WorldPosToMapPos(player.transform.position);
            int index = this.FindRevealedCaveNear(posMap, ExploreInteractRadius);
            if (index < 0)
            {
                TipProvider("附近没有可探索的洞府");
                return false;
            }

            if (!Core.ServiceLocator.TryGet(out WorkerManager workerManager))
            {
                return false;
            }

            // 最近的空闲 Worker（无任务即空闲；死亡 Worker 的 Task 为 null 但状态是 Dead）
            AWorker best = null;
            float bestSqr = float.MaxValue;
            foreach (AWorker worker in workerManager.Characters)
            {
                if (worker == null || worker.CharacterDataLAB == null || worker.CharacterDataLAB.Hp <= 0)
                {
                    continue;
                }

                var workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData == null || workerData.Task != null)
                {
                    continue;
                }

                float sqr = (worker.transform.position - player.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = worker;
                }
            }

            if (best == null)
            {
                TipProvider("没有空闲的 Worker 可派去探索");
                return false;
            }

            var task = new WorkerExploreTask.ExploreTaskBuilder()
                .SetTarget(new Vector3Int((int)this.caves[index].Pos.X, (int)this.caves[index].Pos.Y, 0))
                .SetWorker(best)
                .SetCaveIndex(index)
                .Build();
            this.SetCaveState(index, AncientCaveRuleService.CaveState.Exploring);
            best.SetTask(task, WorkerTaskSource.PushAssignment); // 推送分配：打断当前事务改派
            TipProvider($"{best.name} 前往探索上古洞府（驻留 {CaveExploreRuleService.WorkerExploreSeconds:F0} 秒）");
            AWorkerTask.LogProvider(
                $"[AncientCaveDiag] 派工探索 idx={index} worker={best.name} pos=({this.caves[index].Pos.X:F0},{this.caves[index].Pos.Y:F0})",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>Worker 任务放弃/失败回滚：Exploring→Revealed（洞府可再被探索）。</summary>
        public void CancelWorkerExplore(int caveIndex)
        {
            if (caveIndex < 0 || caveIndex >= this.caves.Count)
            {
                return;
            }

            if (this.caves[caveIndex].State == AncientCaveRuleService.CaveState.Exploring)
            {
                this.SetCaveState(caveIndex, AncientCaveRuleService.CaveState.Revealed);
                AWorkerTask.LogProvider($"[AncientCaveDiag] 派工探索中断回滚 idx={caveIndex}", LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>
        /// 探索完成结算（玩家读条满 或 WorkerExploreTask.Finish 调用）：
        /// 风险 roll（妖兽/塌方/平安）+ 奖励 roll（功法/物资/装备/双倍）+ 状态 Explored 枯竭。
        /// </summary>
        /// <param name="caveIndex">洞府索引。</param>
        /// <param name="explorer">探索的 Worker（玩家亲自探索传 null）。</param>
        public void CompleteExplore(int caveIndex, AWorker explorer = null)
        {
            if (caveIndex < 0 || caveIndex >= this.caves.Count)
            {
                return;
            }

            AncientCaveRuleService.CaveState state = this.caves[caveIndex].State;
            if (state != AncientCaveRuleService.CaveState.Exploring && state != AncientCaveRuleService.CaveState.Revealed)
            {
                return; // 已 Explored 幂等拒绝
            }

            TileMap tileMap = TileMapProvider();
            if (tileMap == null)
            {
                return;
            }

            // 若是玩家读条完成的调用路径，索引已提前清掉；此处兜底再清一次
            if (this.playerExploreCaveIndex == caveIndex)
            {
                this.playerExploreCaveIndex = -1;
            }

            var cave = this.caves[caveIndex];
            Vector3 caveWorld = tileMap.MapPosToWorldPos(
                new Vector3Int((int)cave.Pos.X, (int)cave.Pos.Y, 0));
            GameCharacter settler = explorer != null ? (GameCharacter)explorer : (GameCharacter)PlayerProvider();

            // 风险 roll
            var risk = CaveExploreRuleService.RollRisk(UnityEngine.Random.value);
            string riskMsg;
            switch (risk)
            {
                case CaveExploreRuleService.RiskOutcome.Danger:
                    int count = CaveExploreRuleService.RollEnemyCount(UnityEngine.Random.value);
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * DangerSpawnRadius;
                        EnemyManager.Instance.Create(caveWorld + new Vector3(offset.x, offset.y, 0f),
                            (int)WaveEnemyKind.Common);
                    }

                    riskMsg = $"惊动洞中妖兽×{count}！";
                    break;

                case CaveExploreRuleService.RiskOutcome.Collapse:
                    float maxHp = settler != null ? settler.CharacterDataLAB.MaxHp : 0f;
                    settler?.ReduceHp(maxHp * CaveExploreRuleService.CollapseDamageRatio, null);
                    riskMsg = "洞顶塌方，探索者被砸伤！";
                    break;

                default:
                    riskMsg = "洞内平安无事。";
                    break;
            }

            // 奖励 roll
            string rewardMsg = this.SettleReward(CaveExploreRuleService.RollReward(UnityEngine.Random.value), caveWorld);

            // 枯竭：状态 + 视觉变暗
            this.SetCaveState(caveIndex, AncientCaveRuleService.CaveState.Explored);
            if (this.caveVisuals.TryGetValue(caveIndex, out GameObject visual))
            {
                visual.GetComponent<AncientCaveGlow>()?.MarkExplored();
            }

            string explorerName = settler != null ? settler.name : "探索者";
            TipProvider($"洞府探索完毕（{explorerName}）：{riskMsg}{rewardMsg}");
            AWorkerTask.LogProvider(
                $"[AncientCaveDiag] 探索结算 idx={caveIndex} risk={risk} reward 已roll pos=({cave.Pos.X:F0},{cave.Pos.Y:F0})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>奖励落地：返回播报文本（Learn/TryDrop 内部自带各自 Tip）。</summary>
        private string SettleReward(CaveExploreRuleService.RewardKind kind, Vector3 caveWorld)
        {
            switch (kind)
            {
                case CaveExploreRuleService.RewardKind.GongFa:
                    return this.TryLearnRandomGongFa(out string gongFaName)
                        ? $"寻得功法秘籍《{gongFaName}》！"
                        : "洞中典籍早已残缺。";

                case CaveExploreRuleService.RewardKind.Supply:
                    this.DropSupplyAt(caveWorld);
                    return "寻得一批修炼物资！";

                case CaveExploreRuleService.RewardKind.Equipment:
                    this.DropEquipmentAt(caveWorld);
                    return "寻得一件遗物装备！";

                case CaveExploreRuleService.RewardKind.Double:
                    string gongFaPart = this.TryLearnRandomGongFa(out string name)
                        ? $"功法《{name}》"
                        : "典籍残缺";
                    this.DropSupplyAt(caveWorld);
                    return $"双倍收获：{gongFaPart} + 一批物资！";

                default:
                    return "空手而归。";
            }
        }

        /// <summary>物资掉落（ForceDrop 必掉包裹，掉完恢复原开关）。</summary>
        private void DropSupplyAt(Vector3 worldPos)
        {
            if (!Core.ServiceLocator.TryGet(out EnemyLootManager loot))
            {
                return;
            }

            bool prev = EnemyLootManager.ForceDrop;
            EnemyLootManager.ForceDrop = true;
            try
            {
                loot.TryDropLoot(worldPos, RewardWaveNumber, null);
            }
            finally
            {
                EnemyLootManager.ForceDrop = prev;
            }
        }

        /// <summary>装备掉落（ForceDrop 必掉包裹；ownerId=0 归玩家，不入 Worker 拾取队列）。</summary>
        private void DropEquipmentAt(Vector3 worldPos)
        {
            if (!Core.ServiceLocator.TryGet(out EnemyLootManager loot))
            {
                return;
            }

            bool prev = EnemyLootManager.ForceDrop;
            EnemyLootManager.ForceDrop = true;
            try
            {
                loot.TryDropEquipment(worldPos, RewardWaveNumber, 0);
            }
            finally
            {
                EnemyLootManager.ForceDrop = prev;
            }
        }

        /// <summary>随机学一本玩家未学功法（洗牌后逐本尝试，境界/槽位不满足自动跳下一本）。</summary>
        private bool TryLearnRandomGongFa(out string learnedName)
        {
            learnedName = null;
            Player player = PlayerProvider();
            var data = player?.CharacterDataLAB;
            if (data == null || !Core.ServiceLocator.TryGet(out GongFaManager gongFa))
            {
                return false;
            }

            GrowthData.Ensure(ref data.Growth);
            var candidates = new List<GongFaDef>();
            foreach (GongFaDef def in GongFaLibrary.All)
            {
                if (!data.Growth.LearnedGongFaIds.Contains(def.Id))
                {
                    candidates.Add(def);
                }
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            foreach (GongFaDef def in candidates)
            {
                if (gongFa.Learn(def.Id))
                {
                    learnedName = def.Name;
                    return true;
                }
            }

            return false;
        }

        /// <summary>玩家读条每帧推进（Tick 最前，不受揭示扫描节流）：移动打断 / 计满结算。</summary>
        private void TickPlayerExplore(float deltaTime)
        {
            if (this.playerExploreCaveIndex < 0)
            {
                return;
            }

            Player player = PlayerProvider();
            if (player == null || player.CharacterDataLAB == null || player.CharacterDataLAB.Hp <= 0)
            {
                this.CancelPlayerExplore("玩家不在场");
                return;
            }

            // 移动打断（打坐同款累计位移阈值）
            if ((player.transform.position - this.playerExploreStartPos).sqrMagnitude
                > CultivationManager.MeditateBreakMoveDistance * CultivationManager.MeditateBreakMoveDistance)
            {
                this.CancelPlayerExplore("移动打断");
                return;
            }

            this.playerExploreElapsed += deltaTime;
            if (this.playerExploreElapsed >= CaveExploreRuleService.PlayerExploreSeconds)
            {
                int index = this.playerExploreCaveIndex;
                this.playerExploreCaveIndex = -1;
                this.CompleteExplore(index, explorer: null);
            }
        }

        /// <summary>读条打断：清状态 + Exploring 回滚 Revealed。</summary>
        private void CancelPlayerExplore(string reason)
        {
            int index = this.playerExploreCaveIndex;
            this.playerExploreCaveIndex = -1;
            if (index >= 0 && this.caves[index].State == AncientCaveRuleService.CaveState.Exploring)
            {
                this.SetCaveState(index, AncientCaveRuleService.CaveState.Revealed);
            }

            TipProvider($"探索被打断（{reason}）");
            AWorkerTask.LogProvider($"[AncientCaveDiag] 玩家读条打断 idx={index} 原因={reason}", LogManager.LogLevelEnum.Debug);
        }

        /// <summary>受击打断（打坐同款）：仅玩家自身受击时打断读条。</summary>
        private void OnCharacterDamaged(CharacterDamagedEvent e)
        {
            if (this.playerExploreCaveIndex < 0)
            {
                return;
            }

            Player player = PlayerProvider();
            var data = player?.CharacterDataLAB;
            if (data != null && e.TargetId == data.Id)
            {
                this.CancelPlayerExplore("受击打断");
            }
        }

        /// <summary>写洞府状态（struct 不可变，整只替换）。</summary>
        private void SetCaveState(int index, AncientCaveRuleService.CaveState state)
        {
            this.caves[index] = new AncientCaveRuleService.AncientCaveModel(this.caves[index].Pos, state);
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
                LogManager.LogLevelEnum.Warning);
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
                    if (state == AncientCaveRuleService.CaveState.Exploring)
                    {
                        // 任务不存档：读档后进行中的探索已丢，Exploring 回滚 Revealed 可重探
                        state = AncientCaveRuleService.CaveState.Revealed;
                    }

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
                    LogManager.LogLevelEnum.Warning);
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
