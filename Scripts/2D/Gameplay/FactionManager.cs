namespace LAB2D.Gameplay
{
    using LAB2D;
    using Character = LAB2D.Character.Character;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Character.Worker.Task.Individual;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.Faction;
    using LAB2D.Domain.Time;
    using LAB2D.Domain.Worker;
    using LAB2D.Item.Build;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 阵营管理器 — Worker 阵营的创建/加入/退出/核心放置与读档重放。
    /// Worker 侧：高事业心+高社交者由 WorkerBrain 检测（FactionCreationRuleService）
    /// 攒钱后经 TryCreateFactionByWorker 自立门户；无阵营 Worker 由每日入伙评估
    /// （FactionJoinRuleService 确定性打分）自动投奔。玩家侧：扣金币创建/加入。
    /// 核心选址=成员房间质心（FactionCoreSiteService 环搜 + 间距校验），落位走
    /// FactionCore.PlaceBySystem 建造管线（幂等，读档重放同路）。
    /// 单例存档（ASingletonSaveData）+ 10s 节流 Tick（日更守卫内做评估）。
    /// P1 范围：贡献/选举/战争等扩展位已留（账本/NextElectionDay 落存档）。
    /// </summary>
    public class FactionManager : ASingletonSaveData<FactionManager>, ITickable, IInitializable
    {
        /// <summary>核心瓦片名（BuildMap PosMap 与 ResourceManager 资产名一致）。</summary>
        public const string CoreTileName = "FactionCore";

        /// <summary>核心占用边长（格）——3×3，BottomLeft 参考点即主格。</summary>
        public const int CoreSize = 3;

        /// <summary>Tick 节流间隔（秒）。</summary>
        private const float TickInterval = 10f;

        /// <summary>修仙风阵营名称池（用尽后按序号补）。</summary>
        private static readonly string[] NamePool = new[]
        {
            "青云盟", "赤火堂", "玄水阁", "厚土帮", "罡风寨", "紫雷宗", "白霜门", "苍松会",
        };

        private readonly List<WorkerFaction> factions = new List<WorkerFaction>();

        private int nextFactionId = 1;

        /// <summary>玩家阵营 ID（0=无）。</summary>
        private int playerFactionId;

        private float tickTimer;

        /// <summary>上次日更的游戏日（防同日重复评估）。</summary>
        private int lastDailyDay = -1;

        /// <summary>读档后有核心待重放（地图数据就绪前的兜底标记）。</summary>
        private bool restorePending;

        /// <summary>玩家在贡献账本中的记账名（玩家不占 MemberNames 成员位）。</summary>
        public const string PlayerAccountName = "Player";

        /// <summary>兑换日限账本（名字 → 当日已兑换金币；内存态，跨日重置不落档）。</summary>
        private readonly Dictionary<string, (int Day, int GoldToday)> exchangeLedger = new Dictionary<string, (int, int)>();

        /// <summary>核心格 → 阵营 ID（3×3 每格登记；DamageBuilding O(1) 伤害路由，内存态随核心重建）。</summary>
        private readonly Dictionary<Vector3IntLAB, int> coreCellToFactionId = new Dictionary<Vector3IntLAB, int>();

        /// <summary>阵营对关系矩阵（有序对 → 关系值 [-100,100]，缺省 0=中立）。</summary>
        private readonly Dictionary<(int A, int B), float> relations = new Dictionary<(int A, int B), float>();

        /// <summary>交战对集合（有序对；宣战加入、和谈/解散移除）。</summary>
        private readonly HashSet<(int A, int B)> warPairs = new HashSet<(int A, int B)>();

        /// <summary>互殴日限账本（对 → 当日已生效次数；跨日重置，内存态不落档）。</summary>
        private readonly Dictionary<(int A, int B), (int Day, int Hits)> crossAttackLedger
            = new Dictionary<(int A, int B), (int Day, int Hits)>();

        private bool initialized;

        /// <summary>阵营状态变化（创建/加入/退出/核心落位）— FactionHUD 订阅刷新。</summary>
        public event Action FactionChanged;

        /// <summary>阵营只读视图。</summary>
        public IReadOnlyList<WorkerFaction> Factions => this.factions;

        /// <summary>玩家阵营 ID（0=无）。</summary>
        public int PlayerFactionId => this.playerFactionId;

        private void RaiseFactionChanged()
        {
            this.FactionChanged?.Invoke();
        }

        // ---- 初始化与 Tick ----

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;

            // P5：入夜强制休战（召回全部攻伐任务——夜间共同抗敌）
            EventBus.Instance.Subscribe<Domain.Time.GamePhaseChangedEvent>(this.OnWarPhaseChanged);

            if (!ServiceLocator.TryGet(out MapInitCoordinator coordinator) || coordinator == null)
            {
                return;
            }

            if (coordinator.IsComplete)
            {
                this.TryRestoreCores();
            }
            else
            {
                coordinator.OnMapReady += this.OnMapReadyHandler;
            }
        }

        private void OnMapReadyHandler()
        {
            this.TryRestoreCores();
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.tickTimer += deltaTime;
            if (this.tickTimer < TickInterval)
            {
                return;
            }

            this.tickTimer = 0f;

            // 读档核心重放兜底（LoadData 时地图未就绪 → 等 Tick 轮询补放）
            if (this.restorePending)
            {
                this.TryRestoreCores();
            }

            // 日更守卫：游戏日变更才做核心重试与入伙评估
            int day = this.GetGameDayIndex();
            if (day == this.lastDailyDay)
            {
                return;
            }

            this.lastDailyDay = day;
            this.RetryPendingCorePlacements();
            this.EvaluateDailyJoins();
            this.GenerateDailyQuests();
            this.RunElections();
            this.EvaluateWars();
        }

        /// <summary>立即执行一次核心重试 + 入伙评估 + 任务生成 + 选举 + 战争调度（Editor 调试菜单入口，跳过日更守卫）。</summary>
        public void ForceDailyEvaluation()
        {
            this.RetryPendingCorePlacements();
            this.EvaluateDailyJoins();
            this.GenerateDailyQuests();
            this.RunElections();
            this.EvaluateWars();
            this.RaiseFactionChanged();
        }

        // ---- 查询 ----

        /// <summary>按 ID 取阵营（无则 null）。</summary>
        public WorkerFaction GetFactionById(int factionId)
        {
            if (factionId <= 0)
            {
                return null;
            }

            foreach (WorkerFaction faction in this.factions)
            {
                if (faction.FactionId == factionId)
                {
                    return faction;
                }
            }

            return null;
        }

        /// <summary>取 Worker 所属阵营（无阵营返回 null）。</summary>
        public WorkerFaction GetFactionOfWorker(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            return wd == null || wd.FactionId == 0 ? null : this.GetFactionById(wd.FactionId);
        }

        /// <summary>
        /// 从 WorkerData 采集创始人快照（Brain 攒钱判定/创建复核共用）。
        /// </summary>
        public FactionFounderSnapshot BuildFounderSnapshot(AWorker.WorkerData wd)
        {
            int lifeStageIndex = wd.LifeStage switch
            {
                WorkerLifeStage.Settled => 1,
                WorkerLifeStage.Established => 2,
                _ => 0,
            };

            return new FactionFounderSnapshot
            {
                Ambition = wd.Personality.Ambition,
                Sociality = wd.Personality.Sociality,
                Mood = wd.Personality.Mood,
                Greed = wd.Greed,
                WalletGold = wd.Wallet.Gold,
                HasFaction = wd.FactionId != 0,
                WorkerCount = Core.GameServices.WorkerCountProvider(),
                ExistingFactionCount = this.factions.Count,
                LifeStageIndex = lifeStageIndex,
            };
        }

        // ---- 创建 ----

        /// <summary>
        /// Worker 创建阵营：门槛复核（性格/名额/金币）→ 扣创始金（全额入阵营金库）→
        /// 落籍（FactionId/成员/首任领袖）→ 核心选址放置 → 心智事件 + 气泡 + 播报。
        /// </summary>
        /// <returns>是否创建成功。</returns>
        public bool TryCreateFactionByWorker(AWorker founder)
        {
            AWorker.WorkerData wd = founder?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.FactionId != 0)
            {
                return false;
            }

            FactionFounderSnapshot snapshot = this.BuildFounderSnapshot(wd);
            if (!FactionCreationRuleService.CanFound(in snapshot))
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] {founder.name} 建阵营被拒（门槛/名额/金币不满足 ambition={snapshot.Ambition:F0} social={snapshot.Sociality:F0} wallet={snapshot.WalletGold}）",
                    LogManager.LogLevelEnum.Debug);
                return false;
            }

            WorkerFaction faction = this.CreateFactionInternal(founder.name, false);

            wd.Wallet -= new CurrencyAmount(FactionCreationRuleService.FoundingCostGold);
            faction.TreasuryGold += FactionCreationRuleService.FoundingCostGold;
            wd.FactionId = faction.FactionId;
            faction.MemberNames.Add(founder.name);
            faction.Contribution[founder.name] = 0f;

            this.TryPlaceCore(faction, founder.name);
            this.RaiseFactionChanged();

            this.RecordFactionEvent(
                founder, WorkerMindConstant.EVT_FACTION_FOUND, faction.Name,
                $"自立门户，创立了「{faction.Name}」", 80f);
            ShowTip($"{founder.name} 自立门户，创立了「{faction.Name}」！");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{faction.Name}」成立 founder={founder.name} id={faction.FactionId} 金库={faction.TreasuryGold} 核心={(faction.Core.IsPlaced ? $"({faction.Core.X},{faction.Core.Y})" : "待放置")}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// 玩家创建阵营：扣玩家金币（CurrencyManager.TrySpendPlayerGold），
        /// 核心以玩家当前位置为锚点选址；玩家阵营不占 Worker 成员位。
        /// </summary>
        public bool TryCreateFactionByPlayer()
        {
            if (this.playerFactionId != 0)
            {
                ShowTip("你已有阵营，无需再建");
                return false;
            }

            CurrencyManager currency = ServiceLocator.Get<CurrencyManager>();
            if (currency == null || !currency.TrySpendPlayerGold(FactionCreationRuleService.FoundingCostGold))
            {
                ShowTip($"金币不足：创建阵营需 {FactionCreationRuleService.FoundingCostGold} 金币");
                return false;
            }

            WorkerFaction faction = this.CreateFactionInternal("Player", true);
            faction.TreasuryGold += FactionCreationRuleService.FoundingCostGold;
            this.playerFactionId = faction.FactionId;

            this.TryPlaceCore(faction, null);
            this.RaiseFactionChanged();

            ShowTip($"你创立了「{faction.Name}」！");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 玩家创立「{faction.Name}」id={faction.FactionId} 核心={(faction.Core.IsPlaced ? $"({faction.Core.X},{faction.Core.Y})" : "待放置")}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        private WorkerFaction CreateFactionInternal(string leaderName, bool byPlayer)
        {
            WorkerFaction faction = new WorkerFaction
            {
                FactionId = this.nextFactionId++,
                Name = this.PickFactionName(),
                ColorIndex = (this.nextFactionId - 2) % NamePool.Length,
                LeaderName = leaderName,
                IsPlayerFounded = byPlayer,
                NextElectionDay = FactionElectionRuleService.GetNextElectionDay(this.GetGameDayIndex()),
            };

            this.factions.Add(faction);
            return faction;
        }

        private string PickFactionName()
        {
            foreach (string name in NamePool)
            {
                bool used = false;
                foreach (WorkerFaction faction in this.factions)
                {
                    if (faction.Name == name)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                {
                    return name;
                }
            }

            return $"第{this.factions.Count + 1}盟";
        }

        // ---- 加入/退出 ----

        /// <summary>Worker 加入指定阵营（满员/已有阵营返回 false；心智事件由 Manager 出）。</summary>
        public bool TryJoinFaction(AWorker worker, WorkerFaction faction)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.FactionId != 0 || faction == null
                || faction.MemberNames.Count >= FactionJoinRuleService.MaxMembers)
            {
                return false;
            }

            wd.FactionId = faction.FactionId;
            faction.MemberNames.Add(worker.name);
            faction.Contribution.TryGetValue(worker.name, out float kept);
            faction.Contribution[worker.name] = kept; // 保留历史贡献（退出再入伙不清零）

            this.RaiseFactionChanged();
            this.RecordFactionEvent(
                worker, WorkerMindConstant.EVT_FACTION_JOIN, faction.Name,
                $"加入了「{faction.Name}」", 50f);
            AWorkerTask.LogProvider(
                $"[FactionDiag] {worker.name} 加入「{faction.Name}」（{faction.MemberNames.Count}/{FactionJoinRuleService.MaxMembers}）",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// Worker 退出阵营：领袖出走由贡献最高成员接任；全员出走的阵营保留
        /// （核心还在，等待新人入伙——避免无主核心瓦片残留拆除管线）。
        /// </summary>
        public bool TryLeaveFaction(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            WorkerFaction faction = wd == null || wd.FactionId == 0
                ? null
                : this.GetFactionById(wd.FactionId);
            if (faction == null)
            {
                return false;
            }

            wd.FactionId = 0;
            faction.MemberNames.Remove(worker.name);

            if (faction.LeaderName == worker.name)
            {
                string heir = null;
                float best = float.MinValue;
                foreach (string memberName in faction.MemberNames)
                {
                    float c = faction.Contribution.TryGetValue(memberName, out float v) ? v : 0f;
                    if (c > best)
                    {
                        best = c;
                        heir = memberName;
                    }
                }

                faction.LeaderName = heir; // null = 群龙无首，P3 选举补位
            }

            this.RaiseFactionChanged();
            this.RecordFactionEvent(
                worker, WorkerMindConstant.EVT_FACTION_LEAVE, faction.Name,
                $"退出了「{faction.Name}」", 30f);

            if (faction.MemberNames.Count == 0)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 「{faction.Name}」已无成员，阵营保留待新人（核心 {(faction.Core.IsPlaced ? "在" : "无")}）",
                    LogManager.LogLevelEnum.Debug);
            }

            AWorkerTask.LogProvider(
                $"[FactionDiag] {worker.name} 退出「{faction.Name}」（余 {faction.MemberNames.Count} 人）",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>玩家加入 Worker 阵营（玩家阵营身份绑定，不占成员位）。</summary>
        public bool TryJoinFactionByPlayer(int factionId)
        {
            if (this.playerFactionId != 0)
            {
                ShowTip("你已有效忠的阵营");
                return false;
            }

            WorkerFaction faction = this.GetFactionById(factionId);
            if (faction == null || faction.IsPlayerFounded)
            {
                return false;
            }

            this.playerFactionId = faction.FactionId;
            this.RaiseFactionChanged();
            ShowTip($"你加入了「{faction.Name}」");
            AWorkerTask.LogProvider($"[FactionDiag] 玩家加入「{faction.Name}」", LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>玩家退出阵营（自建阵营保留待回，Worker 阵营仅解绑身份）。</summary>
        public bool TryLeaveFactionByPlayer()
        {
            if (this.playerFactionId == 0)
            {
                return false;
            }

            WorkerFaction faction = this.GetFactionById(this.playerFactionId);
            this.playerFactionId = 0;
            this.RaiseFactionChanged();
            ShowTip(faction != null ? $"你退出了「{faction.Name}」" : "你退出了阵营");
            AWorkerTask.LogProvider("[FactionDiag] 玩家退出阵营", LogManager.LogLevelEnum.Debug);
            return true;
        }

        // ---- 核心选址与放置 ----

        /// <summary>
        /// 为阵营放置核心：成员房间质心选址（FactionCoreSiteService），找不到合法空地
        /// 记待放置态每日重试。锚点兜底链：成员家 → 指定成员当前位置 → 玩家位置（玩家阵营）。
        /// </summary>
        public bool TryPlaceCore(WorkerFaction faction, string fallbackMemberName)
        {
            if (faction == null || faction.Core.IsPlaced)
            {
                return false;
            }

            TileMap tileMap = ServiceLocator.Get<TileMap>();
            if (tileMap?.TileMapDataLAB?.MapTiles == null)
            {
                return false; // 地图未就绪，日更重试
            }

            List<Vector3Int> anchors = this.CollectMemberHomeAnchors(faction);
            if (anchors.Count == 0 && !string.IsNullOrEmpty(fallbackMemberName)
                && this.FindWorkerByName(fallbackMemberName) is AWorker fallback)
            {
                anchors.Add(AWorkerTask.TileMapWorldToMapProvider(fallback.transform.position));
            }

            if (anchors.Count == 0 && faction.IsPlayerFounded
                && ServiceLocator.TryGet(out PlayerManager playerManager) && playerManager.Mine != null)
            {
                anchors.Add(AWorkerTask.TileMapWorldToMapProvider(playerManager.Mine.transform.position));
            }

            var mapCenter = new Vector3Int(
                tileMap.TileMapDataLAB.Height / 2, tileMap.TileMapDataLAB.Width / 2, 0);
            Vector3Int? pos = FactionCoreSiteService.FindCorePosition(
                anchors, this.CollectOtherCorePositions(faction), mapCenter);
            if (!pos.HasValue)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 「{faction.Name}」核心选址失败（质心附近 {FactionCoreSiteService.SiteSearchRadius} 格无合法空地），待每日重试",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            this.CommitCorePlacement(faction, pos.Value);
            return true;
        }

        /// <summary>落核心：走 FactionCore 建造管线（幂等）+ 实体状态初始化 + 伤害路由登记。</summary>
        private void CommitCorePlacement(WorkerFaction faction, Vector3Int pos)
        {
            new FactionCore().PlaceBySystem(pos);
            faction.Core.IsPlaced = true;
            faction.Core.X = pos.x;
            faction.Core.Y = pos.y;
            faction.Core.Level = Math.Max(1, faction.Core.Level);
            faction.Core.Hp = FactionCoreRuleService.GetMaxHp(faction.Core.Level);
            faction.Core.DownfallCount = Math.Max(0, faction.Core.DownfallCount);
            this.RegisterCoreCells(faction);
            this.RaiseFactionChanged();
            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{faction.Name}」核心落位 ({pos.x},{pos.y}) hp={faction.Core.Hp:F0}",
                LogManager.LogLevelEnum.Debug);
        }

        private List<Vector3Int> CollectMemberHomeAnchors(WorkerFaction faction)
        {
            var anchors = new List<Vector3Int>();
            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null || faction.MemberNames.Count == 0)
            {
                return anchors;
            }

            var memberSet = new HashSet<string>(faction.MemberNames);
            foreach (AWorker worker in workerManager.Characters)
            {
                AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
                if (wd == null || !memberSet.Contains(worker.name))
                {
                    continue;
                }

                Vector3IntLAB home = wd.HomePosition ?? wd.PlannedHomePosition;
                if (home != null)
                {
                    anchors.Add(Vector3IntLAB.ToVector3Int(home));
                }
            }

            return anchors;
        }

        private List<Vector3Int> CollectOtherCorePositions(WorkerFaction self)
        {
            var cores = new List<Vector3Int>();
            foreach (WorkerFaction faction in this.factions)
            {
                if (faction != self && faction.Core.IsPlaced)
                {
                    cores.Add(new Vector3Int(faction.Core.X, faction.Core.Y, 0));
                }
            }

            // 山门核心也是「核心」——阵营核心须与其保持间距（山门=无阵营者与玩家的守卫锚点）
            if (ServiceLocator.TryGet(out MountainGateManager gate) && gate.IsCorePlaced)
            {
                cores.Add(gate.CorePosition);
            }

            return cores;
        }

        private void RetryPendingCorePlacements()
        {
            foreach (WorkerFaction faction in this.factions)
            {
                if (!faction.Core.IsPlaced)
                {
                    this.TryPlaceCore(faction, faction.LeaderName);
                }
            }
        }

        /// <summary>读档核心瓦片重放：幂等（AddBuild 覆盖已有 PosMap 条目，山门同款）。</summary>
        private void TryRestoreCores()
        {
            if (!this.restorePending)
            {
                return;
            }

            // 地图数据未就绪（旧档协程重生成路径）→ 保持 pending 等 OnMapReady/Tick 兜底
            if (!ServiceLocator.TryGet(out MapInitCoordinator coordinator)
                || coordinator == null || !coordinator.IsComplete)
            {
                return;
            }

            BuildMap buildMap = ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null)
            {
                return;
            }

            int restored = 0;
            foreach (WorkerFaction faction in this.factions)
            {
                if (!faction.Core.IsPlaced)
                {
                    continue;
                }

                new FactionCore().PlaceBySystem(new Vector3Int(faction.Core.X, faction.Core.Y, 0));
                this.RegisterCoreCells(faction);
                restored++;
            }

            this.restorePending = false;
            if (restored > 0)
            {
                AWorkerTask.LogProvider($"[FactionDiag] 核心瓦片读档重放 {restored} 处", LogManager.LogLevelEnum.Debug);
                this.RaiseFactionChanged();
            }
        }

        // ---- 每日入伙评估 ----

        /// <summary>
        /// 日更入伙评估：无阵营 + 至少 Settled + 有家（距离项才有意义）的 Worker，
        /// 对核心已放置的阵营按 FactionJoinRuleService 确定性打分，过 55 分自动投奔。
        /// 跳槽=Leave 后次日再评估（P1 只做首次入伙）。
        /// </summary>
        private void EvaluateDailyJoins()
        {
            if (this.factions.Count == 0)
            {
                return;
            }

            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null || workerManager.Characters.Count == 0)
            {
                return;
            }

            List<WorkerFaction> joinable = null;
            foreach (WorkerFaction faction in this.factions)
            {
                if (faction.Core.IsPlaced)
                {
                    (joinable ??= new List<WorkerFaction>()).Add(faction);
                }
            }

            if (joinable == null || joinable.Count == 0)
            {
                return;
            }

            FavorabilityManager favorability = ServiceLocator.Get<FavorabilityManager>();

            // 名字 → AWorker 索引（成员好感/创始人定位共用，日更一次）
            var workersByName = new Dictionary<string, AWorker>();
            foreach (AWorker worker in workerManager.Characters)
            {
                if (worker?.CharacterDataLAB is AWorker.WorkerData)
                {
                    workersByName[worker.name] = worker;
                }
            }

            var candidates = new List<FactionCandidateSnapshot>();
            foreach (AWorker worker in workerManager.Characters)
            {
                AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
                if (wd == null || wd.FactionId != 0
                    || wd.LifeStage == WorkerLifeStage.Bootstrap
                    || (wd.HomePosition == null && wd.PlannedHomePosition == null))
                {
                    continue;
                }

                var personality = new JoinPersonalitySnapshot
                {
                    Ambition = wd.Personality.Ambition,
                    Sociality = wd.Personality.Sociality,
                    Greed = wd.Greed,
                    Mood = wd.Personality.Mood,
                };

                Vector3Int home = Vector3IntLAB.ToVector3Int(wd.HomePosition ?? wd.PlannedHomePosition);
                candidates.Clear();
                foreach (WorkerFaction faction in joinable)
                {
                    candidates.Add(this.BuildCandidateSnapshot(worker, wd, faction, home, favorability, workersByName));
                }

                if (FactionJoinRuleService.DecideJoin(in personality, candidates, out int bestFactionId, out float bestScore)
                    && this.GetFactionById(bestFactionId) is WorkerFaction target)
                {
                    AWorkerTask.LogProvider(
                        $"[FactionDiag] {worker.name} 入伙评估命中「{target.Name}」score={bestScore:F1}",
                        LogManager.LogLevelEnum.Debug);
                    this.TryJoinFaction(worker, target);
                }
            }
        }

        /// <summary>组装单阵营入伙快照（亲密度/好感均值 + 家到核心 Chebyshev 距离）。</summary>
        private FactionCandidateSnapshot BuildCandidateSnapshot(
            AWorker worker,
            AWorker.WorkerData wd,
            WorkerFaction faction,
            Vector3Int home,
            FavorabilityManager favorability,
            Dictionary<string, AWorker> workersByName)
        {
            float affinitySum = 0f;
            float favorSum = 0f;
            int relationCount = 0;
            foreach (string memberName in faction.MemberNames)
            {
                if (string.IsNullOrEmpty(memberName) || memberName == worker.name
                    || !workersByName.TryGetValue(memberName, out AWorker member))
                {
                    continue;
                }

                relationCount++;
                favorSum += favorability != null ? favorability.GetFavorability(worker, member) : 0f;

                // Mind.Relations 低频列表（≤20 成员 × 日更一次，可接受）；无记录按 0 亲密度
                float affinity = 0f;
                List<WorkerRelationEntry> relations = wd.Mind?.Relations;
                if (relations != null)
                {
                    foreach (WorkerRelationEntry entry in relations)
                    {
                        if (entry.TargetName == memberName)
                        {
                            affinity = entry.Affinity;
                            break;
                        }
                    }
                }

                affinitySum += affinity;
            }

            var corePos = new Vector3Int(faction.Core.X, faction.Core.Y, 0);
            return new FactionCandidateSnapshot
            {
                FactionId = faction.FactionId,
                MemberCount = faction.MemberNames.Count,
                TreasuryGold = faction.TreasuryGold,
                CoreLevel = faction.Core.Level,
                CorePlaced = faction.Core.IsPlaced,
                AvgAffinity = relationCount > 0 ? affinitySum / relationCount : 0f,
                AvgFavor = relationCount > 0 ? favorSum / relationCount : 0f,
                DistanceHomeToCore = Math.Max(
                    Math.Abs(home.x - corePos.x), Math.Abs(home.y - corePos.y)),
            };
        }

        // ---- P2：阵营任务 / 贡献账本 / 兑换捐献 / 核心升级 ----

        /// <summary>Quest 候选资源距核心的最大 Chebyshev 半径（格）。</summary>
        private const int QuestResourceRadius = 40;

        /// <summary>
        /// 日更阵营任务生成：核心附近资源点包装为采集 Quest（数量走
        /// FactionContributionRuleService.GetQuestCountToday），优先级 SystemDefault。
        /// 生成前清理该阵营未接取的旧任务（防堆积）；金库不足付款时提前收手。
        /// </summary>
        private void GenerateDailyQuests()
        {
            if (this.factions.Count == 0)
            {
                return;
            }

            WorkerTaskManager taskManager = ServiceLocator.Get<WorkerTaskManager>();
            ResourceMap resourceMap = ServiceLocator.Get<ResourceMap>();
            ItemDataManager itemDataManager = ServiceLocator.Get<ItemDataManager>();
            if (taskManager == null || resourceMap?.ResourceMapDataLAB?.PosMap == null || itemDataManager == null)
            {
                return;
            }

            int questCount = FactionContributionRuleService.GetQuestCountToday(this.GetGameDayIndex());
            foreach (WorkerFaction faction in this.factions)
            {
                taskManager.RemoveFactionQuestTasks(faction.FactionId);
                if (!faction.Core.IsPlaced || faction.MemberNames.Count == 0)
                {
                    continue;
                }

                int placed = this.PlaceQuestsNearCore(faction, questCount, taskManager, resourceMap, itemDataManager);
                if (placed > 0)
                {
                    AWorkerTask.LogProvider(
                        $"[FactionDiag] 「{faction.Name}」日更阵营任务 x{placed}（金库 {faction.TreasuryGold}）",
                        LogManager.LogLevelEnum.Debug);
                }
            }
        }

        /// <summary>
        /// 为单阵营在核心 QuestResourceRadius 半径内挑最近资源生成 Quest。
        /// 候选被认领（GatherTaskBuilder 返回 null）时顺延下一个，金库不足即停。
        /// </summary>
        private int PlaceQuestsNearCore(
            WorkerFaction faction,
            int questCount,
            WorkerTaskManager taskManager,
            ResourceMap resourceMap,
            ItemDataManager itemDataManager)
        {
            var core = new Vector3Int(faction.Core.X, faction.Core.Y, 0);
            List<(Vector3Int Pos, int ItemId)> candidates = null;
            foreach (KeyValuePair<Vector3IntLAB, string> pair in resourceMap.ResourceMapDataLAB.PosMap)
            {
                int dx = Math.Abs(pair.Key.X - core.x);
                int dy = Math.Abs(pair.Key.Y - core.y);
                if (Math.Max(dx, dy) > QuestResourceRadius || string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                if (!itemDataManager.TryGetByName(pair.Value, out ItemData itemData))
                {
                    continue;
                }

                (candidates ??= new List<(Vector3Int, int)>()).Add((new Vector3Int(pair.Key.X, pair.Key.Y, 0), itemData.Id));
            }

            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            // 就近排序（曼哈顿）：成员接任务顺路；日更低频事件点允许一次性排序
            candidates.Sort((a, b) =>
                (Math.Abs(a.Pos.x - core.x) + Math.Abs(a.Pos.y - core.y))
                - (Math.Abs(b.Pos.x - core.x) + Math.Abs(b.Pos.y - core.y)));

            int placed = 0;
            foreach ((Vector3Int pos, int itemId) in candidates)
            {
                if (placed >= questCount || faction.TreasuryGold < FactionContributionRuleService.QuestRewardGold)
                {
                    break;
                }

                WorkerGatherTask gather = new WorkerGatherTask.GatherTaskBuilder()
                    .SetTarget(pos)
                    .SetResourceInfo(new ResourceInfo(itemId))
                    .Build();
                if (gather == null)
                {
                    continue; // 资源已被认领，顺延下一候选
                }

                WorkerFactionQuestTask quest = new WorkerFactionQuestTask.QuestTaskBuilder()
                    .SetInnerTask(gather)
                    .SetFaction(faction.FactionId)
                    .Build();
                taskManager.AddTask(
                    quest,
                    new GameGridPosition(pos.x, pos.y, pos.z),
                    WorkerTaskPriority.SystemDefault);
                placed++;
            }

            return placed;
        }

        /// <summary>
        /// 阵营任务结算（WorkerFactionQuestTask 完成时调用）：金库付款进执行者钱包 +
        /// 贡献入账（FactionRankRuleService 派生职位，晋职播心智事件）。
        /// </summary>
        public bool TrySettleQuest(AWorker executor, int factionId)
        {
            WorkerFaction faction = this.GetFactionById(factionId);
            AWorker.WorkerData wd = executor?.CharacterDataLAB as AWorker.WorkerData;
            if (faction == null || wd == null || wd.FactionId != factionId)
            {
                return false;
            }

            int gold = Math.Min(
                FactionContributionRuleService.QuestRewardGold,
                Math.Max(0, faction.TreasuryGold));
            if (gold > 0)
            {
                faction.TreasuryGold -= gold;
                wd.Wallet += new CurrencyAmount(gold);
            }

            float before = faction.Contribution.TryGetValue(executor.name, out float kept) ? kept : 0f;
            float after = before + FactionContributionRuleService.QuestContribution;
            faction.Contribution[executor.name] = after;

            this.RecordFactionEvent(
                executor, WorkerMindConstant.EVT_FACTION_QUEST_DONE, faction.Name,
                $"为「{faction.Name}」办差，贡献 +{FactionContributionRuleService.QuestContribution:F0}", 40f);

            FactionRank oldRank = FactionRankRuleService.GetRank(before);
            FactionRank newRank = FactionRankRuleService.GetRank(after);
            if (FactionRankRuleService.IsPromotion(oldRank, newRank))
            {
                this.RecordFactionEvent(
                    executor, WorkerMindConstant.EVT_FACTION_RANK_UP, faction.Name,
                    $"在「{faction.Name}」晋升为{FactionRankRuleService.GetRankName(newRank)}", 70f);
            }

            this.RaiseFactionChanged();
            AWorkerTask.LogProvider(
                $"[FactionDiag] {executor.name} 完成阵营任务 贡献{before:F0}→{after:F0}({FactionRankRuleService.GetRankName(newRank)}) 金库付 {gold}G 余 {faction.TreasuryGold}G",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>查询成员贡献（无记录 0）。</summary>
        public float GetContribution(int factionId, string memberName)
        {
            WorkerFaction faction = this.GetFactionById(factionId);
            return faction != null && faction.Contribution.TryGetValue(memberName, out float value) ? value : 0f;
        }

        /// <summary>查询成员职位（贡献派生，无记录为 Novice）。</summary>
        public FactionRank GetMemberRank(int factionId, string memberName)
        {
            return FactionRankRuleService.GetRank(this.GetContribution(factionId, memberName));
        }

        private void AddContribution(WorkerFaction faction, string memberName, float amount)
        {
            faction.Contribution.TryGetValue(memberName, out float kept);
            faction.Contribution[memberName] = kept + amount;
        }

        /// <summary>Worker 捐献金币入金库（1:1 折贡献）。</summary>
        public bool TryDonateGold(AWorker worker, int gold)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            WorkerFaction faction = wd == null || wd.FactionId == 0 ? null : this.GetFactionById(wd.FactionId);
            if (faction == null || gold <= 0 || !wd.Wallet.HasEnough(new CurrencyAmount(gold)))
            {
                return false;
            }

            wd.Wallet -= new CurrencyAmount(gold);
            faction.TreasuryGold += gold;
            this.AddContribution(faction, worker.name, FactionContributionRuleService.ComputeDonationContribution(gold));
            this.RaiseFactionChanged();
            AWorkerTask.LogProvider(
                $"[FactionDiag] {worker.name} 向「{faction.Name}」捐献 {gold}G（金库 {faction.TreasuryGold}G）",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>玩家捐献金币入自己阵营金库（1:1 折贡献，记账名 Player）。</summary>
        public bool TryDonateGoldByPlayer(int gold)
        {
            if (gold <= 0 || this.playerFactionId == 0)
            {
                return false;
            }

            WorkerFaction faction = this.GetFactionById(this.playerFactionId);
            CurrencyManager currency = ServiceLocator.Get<CurrencyManager>();
            if (faction == null || currency == null || !currency.TrySpendPlayerGold(gold))
            {
                return false;
            }

            faction.TreasuryGold += gold;
            this.AddContribution(faction, PlayerAccountName, FactionContributionRuleService.ComputeDonationContribution(gold));
            this.RaiseFactionChanged();
            ShowTip($"向「{faction.Name}」捐献 {gold}G（金库 {faction.TreasuryGold}G）");
            return true;
        }

        /// <summary>Worker 用贡献兑换金币（2:1，日限 50 金，金库付款）。</summary>
        public bool TryExchangeContribution(AWorker worker, int goldWanted)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            WorkerFaction faction = wd == null || wd.FactionId == 0 ? null : this.GetFactionById(wd.FactionId);
            if (faction == null)
            {
                return false;
            }

            if (!this.TryExchangeInternal(worker.name, faction, goldWanted))
            {
                return false;
            }

            wd.Wallet += new CurrencyAmount(goldWanted);
            AWorkerTask.LogProvider(
                $"[FactionDiag] {worker.name} 兑换 {goldWanted}G（金库 {faction.TreasuryGold}G）",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>玩家用贡献兑换金币（2:1，日限 50 金，金库付款给玩家钱包）。</summary>
        public bool TryExchangeContributionByPlayer(int goldWanted)
        {
            if (this.playerFactionId == 0)
            {
                return false;
            }

            WorkerFaction faction = this.GetFactionById(this.playerFactionId);
            if (faction == null || !this.TryExchangeInternal(PlayerAccountName, faction, goldWanted))
            {
                return false;
            }

            ServiceLocator.Get<CurrencyManager>()?.AddPlayerGold(goldWanted);
            return true;
        }

        /// <summary>
        /// 兑换公共段：日限裁剪 → 贡献余额裁剪 → 金库余额裁剪，三重取最小后
        /// 扣贡献 + 金库付款 + 记日限账。调用方负责把金币付给正确钱包。
        /// </summary>
        private bool TryExchangeInternal(string memberName, WorkerFaction faction, int goldWanted)
        {
            if (goldWanted <= 0)
            {
                return false;
            }

            int day = this.GetGameDayIndex();
            this.exchangeLedger.TryGetValue(memberName, out var record);
            if (record.Day != day)
            {
                record = (day, 0);
            }

            int allowed = FactionContributionRuleService.ClampExchangeToday(goldWanted, record.GoldToday);
            float balance = this.GetContribution(faction.FactionId, memberName);

            if (allowed > 0 && balance < FactionContributionRuleService.ComputeExchangeCost(allowed))
            {
                allowed = (int)(balance / FactionContributionRuleService.ExchangeContributionPerGold);
            }

            if (allowed > 0 && faction.TreasuryGold < allowed)
            {
                allowed = faction.TreasuryGold;
            }

            if (allowed <= 0)
            {
                ShowTip($"今日可兑 0 金（限额/贡献/金库不足）");
                return false;
            }

            faction.Contribution[memberName] = balance - FactionContributionRuleService.ComputeExchangeCost(allowed);
            faction.TreasuryGold -= allowed;
            this.exchangeLedger[memberName] = (day, record.GoldToday + allowed);
            this.RaiseFactionChanged();
            return true;
        }

        /// <summary>
        /// 核心升级：金库金币 + 发起者个人贡献双池扣减（FactionCoreRuleService），
        /// 升级后耐久回满。P3 后领袖职权（仅领袖可发起）在调用侧约束。
        /// </summary>
        public bool TryUpgradeCore(int factionId, string sponsorName)
        {
            WorkerFaction faction = this.GetFactionById(factionId);
            if (faction == null || !faction.Core.IsPlaced)
            {
                return false;
            }

            int goldCost = FactionCoreRuleService.GetUpgradeGoldCost(faction.Core.Level);
            if (goldCost <= 0)
            {
                ShowTip("核心已满级");
                return false;
            }

            float contributionCost = FactionCoreRuleService.GetUpgradeContributionCost(faction.Core.Level);
            float balance = this.GetContribution(factionId, sponsorName);
            if (faction.TreasuryGold < goldCost || balance < contributionCost)
            {
                ShowTip($"升级需金库 {goldCost}G + 个人贡献 {contributionCost:F0}（当前 {faction.TreasuryGold}G / {balance:F0}）");
                return false;
            }

            faction.TreasuryGold -= goldCost;
            faction.Contribution[sponsorName] = balance - contributionCost;
            faction.Core.Level++;
            faction.Core.Hp = FactionCoreRuleService.GetMaxHp(faction.Core.Level);
            this.RaiseFactionChanged();

            string rankName = FactionRankRuleService.GetRankName(FactionRankRuleService.GetRank(faction.Contribution[sponsorName]));
            ShowTip($"「{faction.Name}」核心升至 {faction.Core.Level} 级！（发起：{sponsorName}/{rankName}）");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{faction.Name}」核心升级 Lv{faction.Core.Level - 1}→Lv{faction.Core.Level} 发起={sponsorName} 消耗金库{goldCost}G+贡献{contributionCost:F0}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>玩家发起自己阵营的核心升级（P2 入口，P3 后受领袖职权约束）。</summary>
        public bool TryUpgradeCoreByPlayer()
        {
            return this.playerFactionId != 0 && this.TryUpgradeCore(this.playerFactionId, PlayerAccountName);
        }

        /// <summary>立即生成一轮阵营任务（Editor 调试菜单入口，跳过日更守卫）。</summary>
        public void ForceDailyQuests()
        {
            this.GenerateDailyQuests();
            this.RaiseFactionChanged();
        }

        // ---- P3：领袖选举 ----

        /// <summary>
        /// 日更选举调度：到期（dayIndex ≥ NextElectionDay）且成员 ≥2 的阵营开选。
        /// 候选人拉票（气泡+心智事件+加成）→ 全员加权投票（FactionElectionRuleService.Tally）
        /// → 换届播报。无人过候选资格线时现任留任，周期照常顺延。
        /// </summary>
        private void RunElections()
        {
            if (this.factions.Count == 0)
            {
                return;
            }

            int day = this.GetGameDayIndex();
            foreach (WorkerFaction faction in this.factions)
            {
                if (!FactionElectionRuleService.IsElectionDue(day, faction.NextElectionDay))
                {
                    continue;
                }

                faction.NextElectionDay = FactionElectionRuleService.GetNextElectionDay(day);
                if (faction.MemberNames.Count >= FactionElectionRuleService.MinMemberCount)
                {
                    this.HoldElection(faction);
                }
            }
        }

        /// <summary>单阵营一届选举：快照组装 → 拉票 → Tally → 换届（心智事件+播报）。</summary>
        private void HoldElection(WorkerFaction faction)
        {
            // 名字 → AWorker 索引（候选/投票快照共用）
            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null)
            {
                return;
            }

            var members = new Dictionary<string, AWorker>();
            var memberNames = new HashSet<string>(faction.MemberNames);
            foreach (AWorker worker in workerManager.Characters)
            {
                if (worker?.CharacterDataLAB is AWorker.WorkerData && memberNames.Contains(worker.name))
                {
                    members[worker.name] = worker;
                }
            }

            if (members.Count < FactionElectionRuleService.MinMemberCount)
            {
                return; // 实际在场成员不足（死亡未清名等），本届跳过
            }

            // 全成员候选快照（资格/排序在 BuildCandidates 内裁）
            var memberSnapshots = new List<ElectionCandidateSnapshot>();
            foreach (KeyValuePair<string, AWorker> pair in members)
            {
                AWorker.WorkerData wd = pair.Value.CharacterDataLAB as AWorker.WorkerData;
                memberSnapshots.Add(new ElectionCandidateSnapshot
                {
                    Name = pair.Key,
                    Ambition = wd.Personality.Ambition,
                    Sociality = wd.Personality.Sociality,
                    Contribution = faction.Contribution.TryGetValue(pair.Key, out float c) ? c : 0f,
                    IsIncumbent = pair.Key == faction.LeaderName,
                });
            }

            List<ElectionCandidateSnapshot> candidates =
                FactionElectionRuleService.BuildCandidates(memberSnapshots);
            if (candidates.Count == 0)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 「{faction.Name}」选举：无人过野心线（≥{FactionElectionRuleService.MinCandidateAmbition:F0}），现任 {faction.LeaderName} 留任",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            // 选举日拉票：每候选人一条气泡 + 心智事件 + 加成（低频，5 游戏日一届）
            for (int i = 0; i < candidates.Count; i++)
            {
                if (members.TryGetValue(candidates[i].Name, out AWorker campaigner))
                {
                    this.RecordFactionEvent(
                        campaigner, WorkerMindConstant.EVT_FACTION_CAMPAIGN, faction.Name,
                        $"为竞选「{faction.Name}」领袖四处拉票", 10f);
                }

                ElectionCandidateSnapshot snapshot = candidates[i];
                snapshot.CampaignBonus = FactionElectionRuleService.CampaignBonusPerRally;
                candidates[i] = snapshot;
            }

            // 全员投票：票重 = 职位票重 + 社交因子；亲密度取自投票人 Mind.Relations
            var voters = new List<ElectionVoterSnapshot>(members.Count);
            foreach (KeyValuePair<string, AWorker> pair in members)
            {
                AWorker.WorkerData wd = pair.Value.CharacterDataLAB as AWorker.WorkerData;
                var affinity = new Dictionary<string, float>(candidates.Count);
                List<WorkerRelationEntry> relations = wd.Mind?.Relations;
                if (relations != null)
                {
                    foreach (WorkerRelationEntry entry in relations)
                    {
                        affinity[entry.TargetName] = entry.Affinity; // 多记无害，只查候选名
                    }
                }

                voters.Add(new ElectionVoterSnapshot
                {
                    Name = pair.Key,
                    VoteWeight = FactionElectionRuleService.ComputeVoteWeight(
                        FactionRankRuleService.GetRank(
                            faction.Contribution.TryGetValue(pair.Key, out float c) ? c : 0f),
                        wd.Personality.Sociality),
                    AffinityByCandidate = affinity,
                });
            }

            string winner = FactionElectionRuleService.Tally(candidates, voters);
            if (string.IsNullOrEmpty(winner) || winner == faction.LeaderName)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 「{faction.Name}」选举：{faction.LeaderName} 连任",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            string formerLeader = faction.LeaderName;
            faction.LeaderName = winner;
            this.RaiseFactionChanged();

            if (members.TryGetValue(winner, out AWorker winnerWorker))
            {
                this.RecordFactionEvent(
                    winnerWorker, WorkerMindConstant.EVT_FACTION_ELECTION_WIN, faction.Name,
                    $"当选「{faction.Name}」领袖", 80f);
            }

            if (!string.IsNullOrEmpty(formerLeader) && members.TryGetValue(formerLeader, out AWorker loserWorker))
            {
                this.RecordFactionEvent(
                    loserWorker, WorkerMindConstant.EVT_FACTION_ELECTION_LOSE, faction.Name,
                    $"在「{faction.Name}」选举中让贤", 30f);
            }

            ShowTip($"「{faction.Name}」换届：{winner} 当选领袖");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{faction.Name}」换届 {formerLeader ?? "空缺"} → {winner}（候选 {candidates.Count} 人/投票 {voters.Count} 人）",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>立即召开所有到期未到期阵营的选举（Editor 调试菜单入口）。</summary>
        public void ForceElection()
        {
            int day = this.GetGameDayIndex();
            foreach (WorkerFaction faction in this.factions)
            {
                faction.NextElectionDay = day; // 视为立即到期
                if (faction.MemberNames.Count >= FactionElectionRuleService.MinMemberCount)
                {
                    this.HoldElection(faction);
                }

                faction.NextElectionDay = FactionElectionRuleService.GetNextElectionDay(day);
            }

            this.RaiseFactionChanged();
        }

        // ---- P4：核心受击 / 陷落 / 解散 / 攻城目标 / 防御击杀 ----

        /// <summary>登记核心 3×3 占用格 → 阵营 ID（落位/读档重放后调用，重复登记幂等覆盖）。</summary>
        private void RegisterCoreCells(WorkerFaction faction)
        {
            foreach (Vector3Int cell in ABuildItem.GetOccupiedPositions(
                new Vector3Int(faction.Core.X, faction.Core.Y, 0),
                CoreSize, CoreSize, AWorkerTask.RectType.BottomLeft))
            {
                this.coreCellToFactionId[Vector3IntLAB.ToVector3IntLAB(cell)] = faction.FactionId;
            }
        }

        /// <summary>清除核心占用格登记（解散删瓦片时调用）。</summary>
        private void UnregisterCoreCells(WorkerFaction faction)
        {
            foreach (Vector3Int cell in ABuildItem.GetOccupiedPositions(
                new Vector3Int(faction.Core.X, faction.Core.Y, 0),
                CoreSize, CoreSize, AWorkerTask.RectType.BottomLeft))
            {
                this.coreCellToFactionId.Remove(Vector3IntLAB.ToVector3IntLAB(cell));
            }
        }

        /// <summary>该格是否为某阵营核心占用格（BuildMap.DamageBuilding 路由用，O(1)）。</summary>
        public bool IsCoreCell(Vector3Int pos)
        {
            return this.coreCellToFactionId.ContainsKey(Vector3IntLAB.ToVector3IntLAB(pos));
        }

        /// <summary>是否存在已放置核心（敌人攻城可用性判断，SeekEnemyMoveState 触发条件）。</summary>
        public bool HasPlacedCore
        {
            get
            {
                foreach (WorkerFaction faction in this.factions)
                {
                    if (faction.Core.IsPlaced)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 取距 from 最近已放置阵营核心主格（敌人攻城目标选择；无已放置核心返回 null）。
        /// 阵营数 ≤6，每 4 帧一次 Chebyshev 比较可忽略。
        /// </summary>
        public Vector3Int? GetNearestPlacedCore(Vector3Int from)
        {
            Vector3Int? nearest = null;
            int bestDist = int.MaxValue;
            foreach (WorkerFaction faction in this.factions)
            {
                if (!faction.Core.IsPlaced)
                {
                    continue;
                }

                int dist = Math.Max(
                    Math.Abs(from.x - faction.Core.X),
                    Math.Abs(from.y - faction.Core.Y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = new Vector3Int(faction.Core.X, faction.Core.Y, 0);
                }
            }

            return nearest;
        }

        /// <summary>
        /// 核心受击结算（BuildMap.DamageBuilding 阵营核心分支调用）：扣耐久 →
        /// 击破走陷落处理（复活+惩罚 / 超限解散）。不触发 GameOver（胜负软化）。
        /// </summary>
        public void DamageCoreAt(Vector3Int pos, float damage, Character attacker)
        {
            if (!this.coreCellToFactionId.TryGetValue(Vector3IntLAB.ToVector3IntLAB(pos), out int factionId)
                || this.GetFactionById(factionId) is not WorkerFaction faction)
            {
                return;
            }

            FactionCoreRuleService.CoreDamageResult result =
                FactionCoreRuleService.ApplyDamage(faction.Core.Hp, damage);
            faction.Core.Hp = result.RemainingHp;

            // 受击高频（攻城战多弹命中）：节流 2s/条；hp 变化由 HUD 轮询补显
            WorkerFaction factionRef = faction;
            AWorkerTask.LogProviderThrottled(
                $"FactionCoreHit|{factionId}", 2f,
                () => $"[FactionDiag] 「{factionRef.Name}」核心受击 hp={factionRef.Core.Hp:F0}/{FactionCoreRuleService.GetMaxHp(factionRef.Core.Level):F0} attacker={attacker?.name ?? "null"}",
                LogManager.LogLevelEnum.Debug);

            if (result.IsDestroyed)
            {
                this.HandleCoreDownfall(faction);
                this.RaiseFactionChanged();
            }
        }

        /// <summary>
        /// 核心被击破：DownfallCount++；达到解散闸门（2 次）→ 解散流散；
        /// 否则半血复活 + 阵营惩罚（全员贡献 -10%、士气 -10、心智事件）。
        /// </summary>
        private void HandleCoreDownfall(WorkerFaction faction)
        {
            faction.Core.DownfallCount++;

            if (FactionCoreRuleService.ShouldDissolve(faction.Core.DownfallCount))
            {
                this.DissolveFaction(faction);
                return;
            }

            faction.Core.Hp = FactionCoreRuleService.ComputeReviveHp(faction.Core.Level);

            // 阵营惩罚：全员贡献 -10%（原地缩放）
            var keys = new List<string>(faction.Contribution.Keys);
            foreach (string key in keys)
            {
                faction.Contribution[key] = faction.Contribution[key] * 0.9f;
            }

            // 在场成员：士气打击 + 心智事件
            foreach (string memberName in faction.MemberNames)
            {
                if (this.FindWorkerByName(memberName) is AWorker member
                    && member.CharacterDataLAB is AWorker.WorkerData memberWd)
                {
                    memberWd.CurMorale = Math.Max(0f, memberWd.CurMorale - 10f);
                    this.RecordFactionEvent(
                        member, WorkerMindConstant.EVT_FACTION_CORE_DOWNFALL, faction.Name,
                        $"「{faction.Name}」核心被击破（第 {faction.Core.DownfallCount}/{FactionCoreRuleService.MaxDownfalls} 次）", 60f);
                }
            }

            ShowTip($"「{faction.Name}」核心被击破！（{faction.Core.DownfallCount}/{FactionCoreRuleService.MaxDownfalls}，全员士气与贡献受挫）");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{faction.Name}」核心第 {faction.Core.DownfallCount} 次被击破 → 半血复活 hp={faction.Core.Hp:F0}，全员贡献-10%",
                LogManager.LogLevelEnum.Warning);
        }

        /// <summary>
        /// 阵营解散：删核心瓦片 + 清路由登记 + 金库按贡献瓜分给在场成员 +
        /// 成员流散（FactionId=0，次日入伙评估改投）+ 心智事件/播报。
        /// </summary>
        private void DissolveFaction(WorkerFaction faction)
        {
            // 删核心瓦片（9 格逐一走 CancelBuilding：移除瓦片+PosMap+碰撞）
            BuildMap buildMap = ServiceLocator.Get<BuildMap>();
            if (buildMap != null)
            {
                foreach (Vector3Int cell in ABuildItem.GetOccupiedPositions(
                    new Vector3Int(faction.Core.X, faction.Core.Y, 0),
                    CoreSize, CoreSize, AWorkerTask.RectType.BottomLeft))
                {
                    buildMap.CancelBuilding(cell);
                }
            }

            this.UnregisterCoreCells(faction);
            faction.Core.IsPlaced = false;

            // 金库按贡献占比瓜分给在场成员（无贡献记录/无人在场则上缴归零）
            float totalContribution = 0f;
            foreach (KeyValuePair<string, float> pair in faction.Contribution)
            {
                totalContribution += Math.Max(0f, pair.Value);
            }

            if (totalContribution > 0f && faction.TreasuryGold > 0)
            {
                foreach (string memberName in faction.MemberNames)
                {
                    if (!faction.Contribution.TryGetValue(memberName, out float c) || c <= 0f)
                    {
                        continue;
                    }

                    if (this.FindWorkerByName(memberName) is AWorker member
                        && member.CharacterDataLAB is AWorker.WorkerData memberWd)
                    {
                        int share = (int)(faction.TreasuryGold * c / totalContribution);
                        memberWd.Wallet += new CurrencyAmount(share);
                    }
                }
            }

            faction.TreasuryGold = 0;

            // P5：清交战对与关系项（阵营已不存在），未结算的出征任务就地撤销
            foreach ((int A, int B) key in new List<(int A, int B)>(this.warPairs))
            {
                if (key.A == faction.FactionId || key.B == faction.FactionId)
                {
                    this.warPairs.Remove(key);
                }
            }

            foreach ((int A, int B) key in new List<(int A, int B)>(this.relations.Keys))
            {
                if (key.A == faction.FactionId || key.B == faction.FactionId)
                {
                    this.relations.Remove(key);
                }
            }

            // 成员流散：解绑阵营 + 心智事件（次日入伙评估自动改投）
            foreach (string memberName in new List<string>(faction.MemberNames))
            {
                if (this.FindWorkerByName(memberName) is AWorker member
                    && member.CharacterDataLAB is AWorker.WorkerData memberWd)
                {
                    if (memberWd.Task is WorkerFactionRaidTask)
                    {
                        member.GiveUpTask();
                    }

                    memberWd.FactionId = 0;
                    this.RecordFactionEvent(
                        member, WorkerMindConstant.EVT_FACTION_DISSOLVE, faction.Name,
                        $"「{faction.Name}」树倒猢狲散", 70f);
                }
            }

            faction.MemberNames.Clear();

            if (this.playerFactionId == faction.FactionId)
            {
                this.playerFactionId = 0;
            }

            this.factions.Remove(faction);
            ShowTip($"「{faction.Name}」核心两度陷落，阵营解散！（成员流散、金库按贡献瓜分）");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{faction.Name}」解散（核心陷落 {faction.Core.DownfallCount} 次）",
                LogManager.LogLevelEnum.Warning);
        }

        /// <summary>
        /// 防御击杀贡献：Worker 击杀敌人（Character.ReduceHp 死亡分支调用），
        /// 击杀者有所属阵营时 +DefenseKillContribution。
        /// </summary>
        public void RecordDefenseKill(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            WorkerFaction faction = wd == null || wd.FactionId == 0 ? null : this.GetFactionById(wd.FactionId);
            if (faction == null)
            {
                return;
            }

            this.AddContribution(faction, worker.name, FactionContributionRuleService.DefenseKillContribution);
            AWorkerTask.LogProviderThrottled(
                $"FactionKill|{worker.name}", 2f,
                () => $"[FactionDiag] {worker.name} 击杀敌人 贡献+{FactionContributionRuleService.DefenseKillContribution:F0}（「{faction.Name}」）",
                LogManager.LogLevelEnum.Debug);
        }

        // ---- P5：关系与战争 ----

        /// <summary>取阵营对关系值（缺省 0=中立）。</summary>
        public float GetRelation(int factionIdA, int factionIdB)
        {
            (int a, int b) key = FactionRelationRuleService.GetPairKey(factionIdA, factionIdB);
            return this.relations.TryGetValue(key, out float value) ? value : 0f;
        }

        /// <summary>是否交战中。</summary>
        public bool IsAtWar(int factionIdA, int factionIdB)
        {
            return this.warPairs.Contains(FactionRelationRuleService.GetPairKey(factionIdA, factionIdB));
        }

        /// <summary>交战对只读视图（HUD/调试枚举）。</summary>
        public IReadOnlyCollection<(int A, int B)> WarPairs => this.warPairs;

        /// <summary>应用关系增减（钳制区间 + 事件点日志）。</summary>
        private void ApplyRelationDelta(int factionIdA, int factionIdB, float delta, string reason)
        {
            (int A, int B) key = FactionRelationRuleService.GetPairKey(factionIdA, factionIdB);
            float before = this.relations.TryGetValue(key, out float v) ? v : 0f;
            float after = FactionRelationRuleService.ApplyEvent(before, delta);
            if (Math.Abs(after - before) < 0.0001f)
            {
                return;
            }

            this.relations[key] = after;
            WorkerFaction factionA = this.GetFactionById(key.A);
            WorkerFaction factionB = this.GetFactionById(key.B);
            AWorkerTask.LogProvider(
                $"[FactionDiag] 关系 「{factionA?.Name ?? key.A.ToString()}」↔「{factionB?.Name ?? key.B.ToString()}」 {before:F0}→{after:F0}（{reason}）",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 跨阵营互殴关系累计（Character.ReduceHp 调用，双方均为不同阵营 Worker）：
        /// -3/次，每日每对封顶 -9（crossAttackLedger 账本）。
        /// </summary>
        public void RecordCrossFactionAttack(AWorker attacker, AWorker target)
        {
            AWorker.WorkerData atkWd = attacker?.CharacterDataLAB as AWorker.WorkerData;
            AWorker.WorkerData defWd = target?.CharacterDataLAB as AWorker.WorkerData;
            if (atkWd == null || defWd == null
                || atkWd.FactionId <= 0 || defWd.FactionId <= 0
                || atkWd.FactionId == defWd.FactionId)
            {
                return;
            }

            (int a, int b) key = FactionRelationRuleService.GetPairKey(atkWd.FactionId, defWd.FactionId);
            int day = this.GetGameDayIndex();
            (int Day, int Hits) ledger = this.crossAttackLedger.TryGetValue(key, out (int, int) stored)
                ? stored
                : (day, 0);
            if (ledger.Day != day)
            {
                ledger = (day, 0);
            }

            if (!FactionRelationRuleService.CanApplyCrossAttack(ledger.Hits))
            {
                return; // 当日封顶（互殴高频，不刷日志）
            }

            ledger.Hits++;
            this.crossAttackLedger[key] = ledger;
            this.ApplyRelationDelta(
                atkWd.FactionId, defWd.FactionId,
                FactionRelationRuleService.CrossAttackDelta, "跨阵营互殴");
        }

        /// <summary>
        /// 宣战（EvaluateWars 日更判定 / 玩家领袖 / Editor 强制）。
        /// 闸门：关系低于实际阈值（随发起方领袖好战度放宽）且长老支持率 ≥60%；
        /// ignoreGate=true 跳过闸门（玩家是自己的领袖；Editor 调试）。
        /// </summary>
        /// <returns>是否成功开战。</returns>
        public bool DeclareWar(int initiatorId, int targetId, bool ignoreGate)
        {
            if (initiatorId == targetId
                || this.GetFactionById(initiatorId) is not WorkerFaction initiator
                || this.GetFactionById(targetId) is not WorkerFaction target)
            {
                return false;
            }

            (int a, int b) key = FactionRelationRuleService.GetPairKey(initiatorId, targetId);
            if (this.warPairs.Contains(key))
            {
                return false;
            }

            if (!ignoreGate)
            {
                float bellicosity = this.ComputeLeaderBellicosity(initiator);
                float support = this.ComputeCouncilSupport(initiator);
                if (!FactionWarRuleService.CanDeclareWar(this.GetRelation(initiatorId, targetId), bellicosity, support))
                {
                    return false;
                }
            }

            this.warPairs.Add(key);
            this.NotifyFactionWar(initiator, target.Name, WorkerMindConstant.EVT_FACTION_WAR_DECLARE, $"与「{target.Name}」开战", 60f);
            this.NotifyFactionWar(target, initiator.Name, WorkerMindConstant.EVT_FACTION_WAR_DECLARE, $"「{initiator.Name}」来犯", 60f);
            ShowTip($"「{initiator.Name}」与「{target.Name}」开战！");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 宣战：「{initiator.Name}」→「{target.Name}」（关系 {this.GetRelation(initiatorId, targetId):F0}）",
                LogManager.LogLevelEnum.Warning);
            this.RaiseFactionChanged();
            return true;
        }

        /// <summary>和谈（战争疲劳自动 / 玩家领袖 / Editor）：移除交战对，关系落到冷静期。</summary>
        public bool MakePeace(int factionIdA, int factionIdB)
        {
            (int a, int b) key = FactionRelationRuleService.GetPairKey(factionIdA, factionIdB);
            if (!this.warPairs.Remove(key))
            {
                return false;
            }

            this.relations[key] = FactionWarRuleService.PeaceSettleRelation;
            if (this.GetFactionById(factionIdA) is WorkerFaction factionA && this.GetFactionById(factionIdB) is WorkerFaction factionB)
            {
                this.NotifyFactionWar(factionA, factionB.Name, WorkerMindConstant.EVT_FACTION_PEACE, $"与「{factionB.Name}」休战", 40f);
                this.NotifyFactionWar(factionB, factionA.Name, WorkerMindConstant.EVT_FACTION_PEACE, $"与「{factionA.Name}」休战", 40f);
                ShowTip($"「{factionA.Name}」与「{factionB.Name}」罢兵休战");
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 和谈：「{factionA.Name}」↔「{factionB.Name}」（关系复位 {FactionWarRuleService.PeaceSettleRelation:F0}）",
                    LogManager.LogLevelEnum.Debug);
            }

            this.RaiseFactionChanged();
            return true;
        }

        /// <summary>玩家宣战（作为自己阵营的领袖，免关系/支持率闸门）。</summary>
        public bool TryDeclareWarByPlayer(int targetFactionId)
        {
            return this.playerFactionId != 0 && this.DeclareWar(this.playerFactionId, targetFactionId, true);
        }

        /// <summary>玩家和谈（对自己阵营的交战对生效）。</summary>
        public bool TryMakePeaceByPlayer(int targetFactionId)
        {
            return this.playerFactionId != 0 && this.MakePeace(this.playerFactionId, targetFactionId);
        }

        /// <summary>
        /// 日更战争调度：交战对疲劳回升（达自动和谈线即停战）→ 非交战对关系日衰减 →
        /// 未交战对宣战判定（双向各试一次）→ 攻伐窗口日派兵。
        /// </summary>
        private void EvaluateWars()
        {
            if (this.factions.Count < 2)
            {
                return;
            }

            int day = this.GetGameDayIndex();

            // 1. 交战对：疲劳回升 + 自动和谈（迭代中 MakePeace 改集合 → 快照）
            if (this.warPairs.Count > 0)
            {
                foreach ((int A, int B) key in new List<(int A, int B)>(this.warPairs))
                {
                    float relation = FactionRelationRuleService.ClampRelation(
                        this.GetRelation(key.A, key.B) + FactionWarRuleService.WarDailyRelationRecovery);
                    this.relations[key] = relation;
                    if (FactionWarRuleService.ShouldAutoPeace(relation))
                    {
                        this.MakePeace(key.A, key.B);
                    }
                }
            }

            // 2. 非交战对：无互动日衰减（向 0 回归；归零清项省存档体积）
            if (this.relations.Count > 0)
            {
                foreach ((int A, int B) key in new List<(int A, int B)>(this.relations.Keys))
                {
                    if (this.warPairs.Contains(key))
                    {
                        continue;
                    }

                    float current = this.relations[key];
                    float decayed = FactionRelationRuleService.ComputeDecay(current);
                    if (Math.Abs(decayed - current) < 0.0001f)
                    {
                        continue;
                    }

                    if (decayed == 0f)
                    {
                        this.relations.Remove(key);
                    }
                    else
                    {
                        this.relations[key] = decayed;
                    }
                }
            }

            // 3. 宣战判定：每对先试 i 发起、再试 j 发起（先过闸门者开战）
            for (int i = 0; i < this.factions.Count; i++)
            {
                for (int j = i + 1; j < this.factions.Count; j++)
                {
                    int aId = this.factions[i].FactionId;
                    int bId = this.factions[j].FactionId;
                    if (this.warPairs.Contains(FactionRelationRuleService.GetPairKey(aId, bId)))
                    {
                        continue;
                    }

                    if (!this.DeclareWar(aId, bId, false))
                    {
                        this.DeclareWar(bId, aId, false);
                    }
                }
            }

            // 4. 攻伐窗口（每 3 天白天一次）
            if (FactionWarRuleService.IsRaidWindowDay(day))
            {
                this.DispatchRaidWindows();
            }
        }

        /// <summary>领袖好战度（领袖不在场按 0=最严阈值 -40）。</summary>
        private float ComputeLeaderBellicosity(WorkerFaction faction)
        {
            if (this.FindWorkerByName(faction.LeaderName) is AWorker leader
                && leader.CharacterDataLAB is AWorker.WorkerData wd)
            {
                return FactionWarRuleService.ComputeBellicosity(wd.Personality.Ambition, wd.Greed);
            }

            return 0f;
        }

        /// <summary>长老支持率：议事席（精英及以上；无则全员）中野心 ≥50 占比。</summary>
        private float ComputeCouncilSupport(WorkerFaction faction)
        {
            int supporters = 0;
            int council = 0;
            foreach (string memberName in faction.MemberNames)
            {
                if (this.FindWorkerByName(memberName) is not AWorker member
                    || member.CharacterDataLAB is not AWorker.WorkerData wd)
                {
                    continue;
                }

                FactionRank rank = FactionRankRuleService.GetRank(
                    faction.Contribution.TryGetValue(memberName, out float c) ? c : 0f);
                if (rank != FactionRank.Elite && rank != FactionRank.Elder)
                {
                    continue;
                }

                council++;
                if (wd.Personality.Ambition >= 50f)
                {
                    supporters++;
                }
            }

            if (council == 0)
            {
                // 无议事席成员：退化为全员公投
                foreach (string memberName in faction.MemberNames)
                {
                    if (this.FindWorkerByName(memberName)?.CharacterDataLAB is AWorker.WorkerData wd)
                    {
                        council++;
                        if (wd.Personality.Ambition >= 50f)
                        {
                            supporters++;
                        }
                    }
                }
            }

            return FactionWarRuleService.ComputeCouncilSupportRatio(supporters, council);
        }

        /// <summary>
        /// 攻伐窗口派兵（日更/Editor 强制入口）：每交战对双方各派 ≤4 名闲散成员
        /// （无任务者，按野心降序）出征对方核心。对方核心未放置则本侧不出击。
        /// </summary>
        /// <returns>总出征人数。</returns>
        public int DispatchRaidWindows()
        {
            if (this.warPairs.Count == 0)
            {
                return 0;
            }

            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null)
            {
                return 0;
            }

            int dispatched = 0;
            foreach ((int A, int B) pair in new List<(int A, int B)>(this.warPairs))
            {
                dispatched += this.DispatchRaidSide(workerManager, pair.A, pair.B);
                dispatched += this.DispatchRaidSide(workerManager, pair.B, pair.A);
            }

            if (dispatched > 0)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 攻伐窗口：出征 {dispatched} 人（交战对 {this.warPairs.Count} 组）",
                    LogManager.LogLevelEnum.Debug);
            }

            return dispatched;
        }

        /// <summary>单侧派兵：raiderFactionId 出征 targetFactionId 核心。</summary>
        private int DispatchRaidSide(WorkerManager workerManager, int raiderFactionId, int targetFactionId)
        {
            if (this.GetFactionById(raiderFactionId) is not WorkerFaction raiderFaction
                || this.GetFactionById(targetFactionId) is not WorkerFaction targetFaction
                || !targetFaction.Core.IsPlaced)
            {
                return 0;
            }

            // 出征目标：敌核心最近可走格
            Vector3Int raidPos = AWorkerTask.GenCanReachPosProvider(
                new Vector3Int(targetFaction.Core.X, targetFaction.Core.Y, 0));

            // 挑人：闲散（无任务）成员，按野心降序取前 MaxRaidersPerWindow
            List<AWorker> idle = null;
            foreach (string memberName in raiderFaction.MemberNames)
            {
                if (this.FindWorkerByName(memberName) is not AWorker member
                    || member.CharacterDataLAB is not AWorker.WorkerData wd
                    || wd.Task != null)
                {
                    continue;
                }

                (idle ??= new List<AWorker>()).Add(member);
            }

            if (idle == null || idle.Count == 0)
            {
                return 0;
            }

            idle.Sort((x, y) => ((y.CharacterDataLAB as AWorker.WorkerData)?.Personality.Ambition ?? 0f)
                .CompareTo((x.CharacterDataLAB as AWorker.WorkerData)?.Personality.Ambition ?? 0f));

            int count = FactionWarRuleService.ComputeRaiderCount(idle.Count);
            for (int i = 0; i < count; i++)
            {
                AWorker raider = idle[i];
                AWorker.WorkerData wd = raider.CharacterDataLAB as AWorker.WorkerData;
                Vector3Int home = wd.HomePosition != null
                    ? Vector3IntLAB.ToVector3Int(wd.HomePosition)
                    : AWorkerTask.TileMapWorldToMapProvider(raider.transform.position);
                WorkerFactionRaidTask raid = new WorkerFactionRaidTask.RaidTaskBuilder()
                    .SetTargetMap(raidPos)
                    .SetReturnPos(home)
                    .SetRaider(raider, targetFactionId)
                    .Build();
                raider.SetTask(raid, WorkerTaskSource.PushAssignment);
                this.RecordFactionEvent(
                    raider, WorkerMindConstant.EVT_FACTION_WAR_RAID, targetFaction.Name,
                    $"出征攻打「{targetFaction.Name}」", 50f);
            }

            AWorkerTask.LogProvider(
                $"[FactionDiag] 「{raiderFaction.Name}」出征 {count} 人 → 「{targetFaction.Name}」核心({targetFaction.Core.X},{targetFaction.Core.Y})",
                LogManager.LogLevelEnum.Debug);
            return count;
        }

        /// <summary>
        /// 攻伐掠夺结算（WorkerFactionRaidTask 抵达敌核心调用）：目标金库 20% 转入
        /// 出击方金库，出征者 +战功贡献，关系额外恶化，双方心智事件与播报。
        /// </summary>
        public void SettleRaidLoot(AWorker raider, int targetFactionId)
        {
            if (raider?.CharacterDataLAB is not AWorker.WorkerData wd || wd.FactionId == 0
                || this.GetFactionById(wd.FactionId) is not WorkerFaction raiderFaction
                || this.GetFactionById(targetFactionId) is not WorkerFaction targetFaction)
            {
                return;
            }

            int loot = FactionWarRuleService.ComputeLootGold(targetFaction.TreasuryGold);
            targetFaction.TreasuryGold -= loot;
            raiderFaction.TreasuryGold += loot;
            this.AddContribution(raiderFaction, raider.name, FactionWarRuleService.RaidContribution);
            this.ApplyRelationDelta(
                raiderFaction.FactionId, targetFactionId,
                FactionWarRuleService.RaidLootRelationDelta, "攻伐掠夺");

            this.RecordFactionEvent(
                raider, WorkerMindConstant.EVT_FACTION_WAR_RAID, targetFaction.Name,
                $"攻破「{targetFaction.Name}」金库，掠 {loot} 金", 50f);

            // 被掠方：全员士气 -5 + 心智事件（低频事件点）
            foreach (string memberName in targetFaction.MemberNames)
            {
                if (this.FindWorkerByName(memberName) is AWorker member
                    && member.CharacterDataLAB is AWorker.WorkerData memberWd)
                {
                    memberWd.CurMorale = Math.Max(0f, memberWd.CurMorale - 5f);
                    this.RecordFactionEvent(
                        member, WorkerMindConstant.EVT_FACTION_WAR_RAID, raiderFaction.Name,
                        $"金库被「{raiderFaction.Name}」掠夺 {loot} 金", 40f);
                }
            }

            ShowTip($"「{raiderFaction.Name}」攻入「{targetFaction.Name}」，掠走 {loot} 金！");
            AWorkerTask.LogProvider(
                $"[FactionDiag] 掠夺结算：「{raiderFaction.Name}」←「{targetFaction.Name}」{loot} 金（{raider.name} 战功+{FactionWarRuleService.RaidContribution:F0}）",
                LogManager.LogLevelEnum.Warning);
            this.RaiseFactionChanged();
        }

        /// <summary>相位事件：入夜强制休战，召回全部攻伐任务（夜间共同抗敌）。</summary>
        private void OnWarPhaseChanged(Domain.Time.GamePhaseChangedEvent e)
        {
            if (e.NewPhase != Domain.Time.GamePhase.Night)
            {
                return;
            }

            this.RecallRaids();
        }

        /// <summary>召回进行中的攻伐任务（未抵达=零战果；已掠夺者在阶段切换时已结算）。</summary>
        private void RecallRaids()
        {
            if (this.warPairs.Count == 0)
            {
                return;
            }

            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null)
            {
                return;
            }

            int recalled = 0;
            foreach (AWorker worker in workerManager.Characters)
            {
                if ((worker.CharacterDataLAB as AWorker.WorkerData)?.Task is WorkerFactionRaidTask)
                {
                    worker.GiveUpTask();
                    recalled++;
                }
            }

            if (recalled > 0)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] 入夜休战：召回攻伐 {recalled} 人",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>阵营全员战争心智事件+气泡（宣战/和谈播报，低频事件点）。</summary>
        private void NotifyFactionWar(WorkerFaction faction, string opponentName, string typeKey, string description, float weight)
        {
            foreach (string memberName in faction.MemberNames)
            {
                if (this.FindWorkerByName(memberName) is AWorker member)
                {
                    this.RecordFactionEvent(member, typeKey, opponentName, description, weight);
                }
            }
        }

        // ---- 存档 ----

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            var data = new FactionManagerData
            {
                NextFactionId = this.nextFactionId,
                PlayerFactionId = this.playerFactionId,
            };

            foreach (WorkerFaction faction in this.factions)
            {
                var factionData = new FactionData
                {
                    Id = faction.FactionId,
                    Name = faction.Name,
                    ColorIndex = faction.ColorIndex,
                    LeaderName = faction.LeaderName,
                    Members = new List<string>(faction.MemberNames),
                    TreasuryGold = faction.TreasuryGold,
                    CorePlaced = faction.Core.IsPlaced,
                    CoreX = faction.Core.X,
                    CoreY = faction.Core.Y,
                    CoreHp = faction.Core.Hp,
                    CoreLevel = faction.Core.Level,
                    CoreDownfallCount = faction.Core.DownfallCount,
                    NextElectionDay = faction.NextElectionDay,
                    IsPlayerFounded = faction.IsPlayerFounded,
                };

                foreach (KeyValuePair<string, float> pair in faction.Contribution)
                {
                    factionData.ContributionNames.Add(pair.Key);
                    factionData.ContributionValues.Add(pair.Value);
                }

                data.Factions.Add(factionData);
            }

            foreach (KeyValuePair<(int A, int B), float> pair in this.relations)
            {
                data.RelationA.Add(pair.Key.A);
                data.RelationB.Add(pair.Key.B);
                data.RelationValue.Add(pair.Value);
            }

            foreach ((int a, int b) war in this.warPairs)
            {
                data.WarA.Add(war.a);
                data.WarB.Add(war.b);
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            FactionManagerData data = DataTool.LoadDataByBinary<FactionManagerData>(
                GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                // 旧档（无阵营数据）→ 空状态起步，不自动生成阵营（Worker 攒钱自然形成）
                AWorkerTask.LogProvider("[FactionDiag] 旧档无阵营数据，空状态起步", LogManager.LogLevelEnum.Debug);
                return;
            }

            this.nextFactionId = Math.Max(1, data.NextFactionId);
            this.playerFactionId = data.PlayerFactionId;
            this.factions.Clear();
            foreach (FactionData factionData in data.Factions)
            {
                var faction = new WorkerFaction
                {
                    FactionId = factionData.Id,
                    Name = factionData.Name,
                    ColorIndex = factionData.ColorIndex,
                    LeaderName = factionData.LeaderName,
                    TreasuryGold = factionData.TreasuryGold,
                    NextElectionDay = factionData.NextElectionDay,
                    IsPlayerFounded = factionData.IsPlayerFounded,
                    MemberNames = factionData.Members ?? new List<string>(),
                    Contribution = new Dictionary<string, float>(),
                };

                if (factionData.ContributionNames != null)
                {
                    for (int i = 0; i < factionData.ContributionNames.Count && i < factionData.ContributionValues.Count; i++)
                    {
                        faction.Contribution[factionData.ContributionNames[i]] = factionData.ContributionValues[i];
                    }
                }

                faction.Core.IsPlaced = factionData.CorePlaced;
                faction.Core.X = factionData.CoreX;
                faction.Core.Y = factionData.CoreY;
                faction.Core.Level = Math.Max(1, factionData.CoreLevel);
                faction.Core.Hp = factionData.CoreHp > 0f
                    ? factionData.CoreHp
                    : FactionCoreRuleService.GetMaxHp(faction.Core.Level);
                faction.Core.DownfallCount = factionData.CoreDownfallCount;
                this.factions.Add(faction);
            }

            this.relations.Clear();
            this.warPairs.Clear();
            if (data.RelationA != null)
            {
                for (int i = 0; i < data.RelationA.Count
                    && i < data.RelationB.Count && i < data.RelationValue.Count; i++)
                {
                    this.relations[(data.RelationA[i], data.RelationB[i])] = data.RelationValue[i];
                }
            }

            if (data.WarA != null)
            {
                for (int i = 0; i < data.WarA.Count && i < data.WarB.Count; i++)
                {
                    this.warPairs.Add((data.WarA[i], data.WarB[i]));
                }
            }

            AWorkerTask.LogProvider($"[FactionDiag] 阵营读档恢复 {this.factions.Count} 个（交战对 {this.warPairs.Count} 组）", LogManager.LogLevelEnum.Debug);

            // 核心瓦片重放：地图已就绪立即放，否则等 OnMapReady/Tick 兜底
            this.restorePending = false;
            foreach (WorkerFaction faction in this.factions)
            {
                if (faction.Core.IsPlaced)
                {
                    this.restorePending = true;
                    break;
                }
            }

            if (this.restorePending)
            {
                this.TryRestoreCores();
            }
        }

        // ---- 工具 ----

        private AWorker FindWorkerByName(string workerName)
        {
            if (string.IsNullOrEmpty(workerName))
            {
                return null;
            }

            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null)
            {
                return null;
            }

            foreach (AWorker worker in workerManager.Characters)
            {
                if (worker != null && worker.name == workerName)
                {
                    return worker;
                }
            }

            return null;
        }

        /// <summary>心智事件 + 气泡（服务未就绪时静默降级，测试环境安全）。</summary>
        private void RecordFactionEvent(AWorker worker, string typeKey, string factionName, string description, float weight)
        {
            try
            {
                if (ServiceLocator.TryGet(out WorkerMindService mindService))
                {
                    mindService.RecordEvent(worker, typeKey, MemoryValence.Positive, factionName, weight, description);
                }

                string thought = WorkerInnerMonologue.GetEventThought(typeKey, factionName);
                if (!string.IsNullOrEmpty(thought))
                {
                    worker.ShowMindBubble(thought);
                }
            }
            catch (Exception)
            {
                // 心智服务/气泡不可用（测试环境）时静默降级
            }
        }

        private int GetGameDayIndex()
        {
            IGameTime gameTime = ServiceLocator.Get<IGameTime>();
            return gameTime == null ? 0 : (int)(gameTime.Time / FavorabilityConstant.GameDaySeconds);
        }

        private static void ShowTip(string text)
        {
            try
            {
                Core.GameServices.ShowTipProvider(text);
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }
    }
}
