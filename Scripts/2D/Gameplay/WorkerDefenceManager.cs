namespace LAB2D.Gameplay
{
    using System.Collections.Generic;
    using LAB2D;
    using LAB2D.Character.Worker.Task.Individual;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Time;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Item.Build;
    using UnityEngine;

    /// <summary>
    /// 防守夜调度（M2A 包 2.1）— 入夜时按 <see cref="DefenceDraftRuleService"/>
    /// 给每名 Worker 决策并派发 <see cref="WorkerDefendTask"/>：参战者驻守山门核心旁、
    /// 躲避者缩回床/家旁、趁乱者溜到远处游荡（觉醒者优先参战，由打分保证）。
    /// 任务时长 = 距黎明秒数，到点自然 Finish，无需结束清理；
    /// 单例，由 GlobalInit 注册（IInitializable 订阅相位事件 + ITickable 补派扫描）。
    /// 生存打断补派（M2A 审查中 4）：防守任务被生存紧急打断（吃饭/睡觉等）后，
    /// Tick 2s 节流扫描把已脱离生存任务的 Worker 拉回防守——否则参战人力整夜流失。
    /// </summary>
    public class WorkerDefenceManager : Singleton<WorkerDefenceManager>, IInitializable, ITickable
    {
        /// <summary>趁乱者游荡点在其当前位置周围的搜索半径（格）。</summary>
        private const int LootPosSearchRadius = 6;

        /// <summary>补派扫描节流间隔（秒）。</summary>
        private const float RedraftScanInterval = 2f;

        /// <summary>躲床/参战/趁乱的派发气泡（每人入夜弹一句）。</summary>
        private static readonly string[] FightBubbles = new[]
        {
            "妖兽来了，守住山门！",
            "该我上阵了！",
            "别想踏进小镇一步！",
        };
        private static readonly string[] ShelterBubbles = new[]
        {
            "呜……太可怕了，我要躲好。",
            "打仗的事交给别人吧……",
            "瑟瑟发抖，千万别找到我……",
        };
        private static readonly string[] LootBubbles = new[]
        {
            "乱起来了……正好溜边看看。",
            "他们打他们的，我走我的。",
            "趁着夜里没人注意……",
        };

        private bool isInitialized;

        /// <summary>上次draft的游戏日（防同夜重派：读档/边界情况下的兜底）。</summary>
        private int lastDraftDay = -1;

        /// <summary>补派扫描节流累加器。</summary>
        private float redraftTimer;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;
            EventBus.Instance.Subscribe<GamePhaseChangedEvent>(this.OnPhaseChanged);
            AWorkerTask.LogProvider("[DefenceDiag] WorkerDefenceManager 初始化完成", LogManager.LogLevelEnum.Debug);
        }

        /// <summary>相位事件：入夜即draft（与 WaveManager.OnPhaseChanged 同模式）。</summary>
        private void OnPhaseChanged(GamePhaseChangedEvent e)
        {
            if (e.NewPhase != GamePhase.Night)
            {
                return;
            }

            this.RunNightDraft();
        }

        /// <summary>入夜点名：逐 Worker 决策 + 派防守待命任务（事件点，每夜一次）。</summary>
        private void RunNightDraft()
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null || wm.Characters == null || wm.Characters.Count == 0)
            {
                return;
            }

            // 无山门核心（未放置）：无处可守，不部署（村民按各自日常过夜）
            if (!Core.ServiceLocator.TryGet<MountainGateManager>(out MountainGateManager gate)
                || !gate.IsCorePlaced)
            {
                AWorkerTask.LogProvider(
                    "[DefenceDiag] 入夜但山门核心未放置，跳过防守draft",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            int day = this.GetGameDayIndex();
            if (day == this.lastDraftDay)
            {
                return;
            }

            this.lastDraftDay = day;

            // 驻守时长 = 距黎明秒数（任务到点自然 Finish，收工回日常）
            float nightSeconds = DayNightRuleService.SecondsUntilPhaseStart(
                GameTimeManager.Instance.CurGameTime,
                GlobalData.GameDayTime,
                GamePhase.Dawn);

            List<Vector3Int> coreCells = ABuildItem.GetOccupiedPositions(
                gate.CorePosition, MountainGateManager.CoreSize, MountainGateManager.CoreSize,
                AWorkerTask.RectType.BottomLeft);
            List<Vector3Int> defendPositions = this.CollectDefendPositions(coreCells);
            int defendPosCursor = 0;
            FavorabilityManager favorability = Core.ServiceLocator.Get<FavorabilityManager>();

            foreach (AWorker worker in wm.Characters)
            {
                this.DraftWorker(worker, favorability, nightSeconds, defendPositions, ref defendPosCursor, true);
            }
        }

        /// <summary>
        /// 补派扫描（M2A 审查中 4，治生存打断后整夜人力流失）：夜间 2s 节流扫描，
        /// 把已脱离防守且不在生存任务/紧急状态/战斗脱身状态的 Worker 拉回防守。
        /// 豁免 Eat/Sleep/GroundSleep/Wander（正在解生存题，与
        /// <see cref="AWorker.CheckSurvivalEmergency"/> 的豁免列表一致）——
        /// 否则补派-打断-再补派死循环；这些任务 Finish 后进 Seek 若选了
        /// 非生存任务，下一轮扫描即被拉回。
        /// </summary>
        public void Tick(float deltaTime)
        {
            this.redraftTimer += deltaTime;
            if (this.redraftTimer < RedraftScanInterval)
            {
                return;
            }

            this.redraftTimer = 0f;

            // 只补派已 draft 过的夜晚（未 draft = 无核心/未入夜，各归各的日常）
            if (this.lastDraftDay != this.GetGameDayIndex()
                || GameTimeManager.Instance.CurrentPhase != GamePhase.Night)
            {
                return;
            }

            if (!Core.ServiceLocator.TryGet<WorkerManager>(out WorkerManager wm)
                || wm.Characters == null || wm.Characters.Count == 0)
            {
                return;
            }

            if (!Core.ServiceLocator.TryGet<MountainGateManager>(out MountainGateManager gate)
                || !gate.IsCorePlaced)
            {
                return;
            }

            // 先收集候选，有人需补派才重算待命位/好感度（常态全员在防时零分配）
            List<AWorker> candidates = null;
            foreach (AWorker worker in wm.Characters)
            {
                if (this.ShouldRedraft(worker))
                {
                    (candidates ??= new List<AWorker>()).Add(worker);
                }
            }

            if (candidates == null || candidates.Count == 0)
            {
                return;
            }

            float remainSeconds = DayNightRuleService.SecondsUntilPhaseStart(
                GameTimeManager.Instance.CurGameTime,
                GlobalData.GameDayTime,
                GamePhase.Dawn);
            List<Vector3Int> coreCells = ABuildItem.GetOccupiedPositions(
                gate.CorePosition, MountainGateManager.CoreSize, MountainGateManager.CoreSize,
                AWorkerTask.RectType.BottomLeft);
            List<Vector3Int> defendPositions = this.CollectDefendPositions(coreCells);
            int defendPosCursor = 0;
            FavorabilityManager favorability = Core.ServiceLocator.Get<FavorabilityManager>();

            foreach (AWorker worker in candidates)
            {
                this.DraftWorker(worker, favorability, remainSeconds, defendPositions, ref defendPosCursor, false);
            }
        }

        /// <summary>补派候选判定：脱离防守 + 不在生存任务/紧急状态/战斗脱身/对话暂停。</summary>
        private bool ShouldRedraft(AWorker worker)
        {
            if (worker == null || worker.CharacterDataLAB == null || worker.IsDialoguePaused)
            {
                return false;
            }

            AWorkerState.TypeEnum state = worker.Manager != null
                ? worker.Manager.CurrentStateType
                : AWorkerState.TypeEnum.Seek;
            if (state == AWorkerState.TypeEnum.Dead
                || state == AWorkerState.TypeEnum.Attack
                || state == AWorkerState.TypeEnum.Escape)
            {
                return false;
            }

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return false;
            }

            // 已在防守（含接敌中——任务保持不动）
            if (wd.Task is WorkerDefendTask)
            {
                return false;
            }

            // 正在解生存题（与 CheckSurvivalEmergency 豁免列表一致）
            if (wd.Task != null)
            {
                switch (wd.Task.TaskType)
                {
                    case WorkerTaskType.Eat:
                    case WorkerTaskType.Sleep:
                    case WorkerTaskType.GroundSleep:
                    case WorkerTaskType.Wander:
                        return false;
                }
            }

            // 紧急中不补派（Seek 决策会优先解决生存；补了马上又被紧急打断）
            return !WorkerConditionRuleService.IsSurvivalEmergency(
                wd.CurHungry, wd.CurTired, wd.MaxTired, wd.CurSpirit, wd.CurStress, wd.MaxStress);
        }

        /// <summary>
        /// 单 Worker 决策 + 派发防守任务（入夜 draft 与生存打断补派共用）。
        /// 补派不弹气泡（防夜间反复打断场景刷屏），日志带补派标记。
        /// </summary>
        private void DraftWorker(
            AWorker worker,
            FavorabilityManager favorability,
            float defendSeconds,
            List<Vector3Int> defendPositions,
            ref int defendPosCursor,
            bool showBubble)
        {
            if (worker == null || worker.CharacterDataLAB == null)
            {
                return;
            }

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            GrowthData.Ensure(ref wd.Growth);
            DefenceDraftInput input = new DefenceDraftInput
            {
                Mood = wd.Personality.Mood,
                Ambition = wd.Personality.Ambition,
                Diligence = wd.Personality.Diligence,
                Sociality = wd.Personality.Sociality,
                Greed = wd.Greed,
                Stress = wd.CurStress,
                Morale = wd.CurMorale,
                FavorWithPlayer = favorability != null
                    ? favorability.GetFavorabilityWithPlayer(worker)
                    : FavorabilityRuleService.InitialFavorability,
                HasAwakenedPower = wd.Growth.AwakenedPowerIds != null
                    && wd.Growth.AwakenedPowerIds.Count > 0,
                RealmIndex = wd.Growth.RealmIndex,
            };

            DefenceResponse response = DefenceDraftRuleService.Decide(in input);
            Vector3Int target = response switch
            {
                // 核心被围死无待命位时退化为躲避：原 coreCells[0] 兜底是核心占用格
                // （不可走→寻路失败→弃任务，Fight 响应形同虚设）
                DefenceResponse.Fight => defendPositions.Count == 0
                    ? this.GetShelterPosition(worker, wd)
                    : this.NextDefendPosition(defendPositions, ref defendPosCursor),
                DefenceResponse.ShelterInBed => this.GetShelterPosition(worker, wd),
                _ => this.FindLootPosition(worker),
            };

            WorkerDefendTask task = new WorkerDefendTask.DefendTaskBuilder()
                .SetTarget(target)
                .SetWorker(worker)
                .SetDuration(defendSeconds)
                .Build();

            // 抢占在执行的任务前先走放弃路径：SetTask 只覆盖引用不清理，
            // 旧任务的队列 MarkRunning 占用/GatherMap 认领锁/库存预留需要 GiveUpTask 释放，
            // 否则入夜瞬间正在采集/做悬赏的任务资源会永久锁死（本派发是首个无条件抢占调用方）
            if (wd.Task != null)
            {
                worker.GiveUpTask();
            }

            worker.SetTask(task, WorkerTaskSource.PushAssignment);
            if (showBubble)
            {
                worker.ShowMindBubble(this.PickBubble(response));
            }

            AWorkerTask.LogProvider(
                $"[DefenceDiag] {worker.name} 防守响应={response} 目标=({target.x},{target.y}) " +
                $"觉醒={input.HasAwakenedPower} 境界={input.RealmIndex} 贪婪={input.Greed:F0} 压力={input.Stress:F0} 士气={input.Morale:F0}" +
                (showBubble ? string.Empty : " (补派)"),
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 收集核心周边可站立的待命位：占用格的 4 正交邻居去重、
        /// 剔除核心自身占用格、按通行性过滤。不足时派发端循环复用（同格可站多人）。
        /// </summary>
        private List<Vector3Int> CollectDefendPositions(List<Vector3Int> coreCells)
        {
            HashSet<Vector3Int> coreSet = new HashSet<Vector3Int>(coreCells);
            List<Vector3Int> result = new List<Vector3Int>();
            HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
            Vector3Int[] offsets = new[]
            {
                new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0),
                new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0),
            };

            foreach (Vector3Int cell in coreCells)
            {
                foreach (Vector3Int offset in offsets)
                {
                    Vector3Int pos = cell + offset;
                    if (coreSet.Contains(pos) || !seen.Add(pos))
                    {
                        continue;
                    }

                    if (AWorkerTask.WalkabilityProvider(pos.x, pos.y))
                    {
                        result.Add(pos);
                    }
                }
            }

            return result;
        }

        /// <summary>取下一个参战待命位（用尽后从头循环复用）。调用方保证列表非空。</summary>
        private Vector3Int NextDefendPosition(List<Vector3Int> defendPositions, ref int cursor)
        {
            Vector3Int pos = defendPositions[cursor % defendPositions.Count];
            cursor++;
            return pos;
        }

        /// <summary>躲避位：有家回床边，无家原地缩着。</summary>
        private Vector3Int GetShelterPosition(AWorker worker, AWorker.WorkerData wd)
        {
            if (wd.HomePosition != null)
            {
                return Vector3IntLAB.ToVector3Int(wd.HomePosition);
            }

            return AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
        }

        /// <summary>趁乱游荡位：在其当前位置周围随机找一块可通行格（远离战场溜边）。</summary>
        private Vector3Int FindLootPosition(AWorker worker)
        {
            Vector3Int origin = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3Int candidate = origin + new Vector3Int(
                    Random.Range(-LootPosSearchRadius, LootPosSearchRadius + 1),
                    Random.Range(-LootPosSearchRadius, LootPosSearchRadius + 1), 0);
                if (AWorkerTask.WalkabilityProvider(candidate.x, candidate.y))
                {
                    return candidate;
                }
            }

            return origin; // 周围全不可通行：原地徘徊
        }

        private string PickBubble(DefenceResponse response)
        {
            return response switch
            {
                DefenceResponse.Fight => FightBubbles[Random.Range(0, FightBubbles.Length)],
                DefenceResponse.ShelterInBed => ShelterBubbles[Random.Range(0, ShelterBubbles.Length)],
                _ => LootBubbles[Random.Range(0, LootBubbles.Length)],
            };
        }

        private int GetGameDayIndex()
        {
            IGameTime gt = Core.ServiceLocator.Get<IGameTime>();
            return gt == null ? 0 : (int)(gt.Time / FavorabilityConstant.GameDaySeconds);
        }
    }
}
