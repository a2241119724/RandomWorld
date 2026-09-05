namespace LAB2D.Domain.Worker
{
    using LAB2D.Character.Worker;
    using LAB2D.Constant;
    using LAB2D.Gameplay;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 心智层门面服务 — 自主意志判定、命令结果记录、强制命令、被救危感恩、事件记忆与信念演化。
    /// 非 MonoBehaviour，注册到 ServiceLocator。纯规则部分委托 CommandAcceptanceRuleService /
    /// WorkerMemoryRuleService / WorkerBeliefRuleService。
    /// Phase 1（可拒绝/拖延/强制玩家命令）+ Phase 2（事件记忆 + 信念演化）。
    /// </summary>
    public class WorkerMindService
    {
        /// <summary>存档/运行时中表示"对玩家"的 TargetName 哨兵（与 FavorabilityManager 一致）。</summary>
        public const string PlayerTargetName = "PLAYER";

        /// <summary>HUD「最近想法」队列上限（内存态，不入档）。</summary>
        public const int RecentThoughtsCap = 6;

        /// <summary>最近想法队列（运行时代理，不入档，仅在内存中滚动）。</summary>
        private readonly Queue<string> recentThoughts = new Queue<string>(RecentThoughtsCap);

        /// <summary>读档兜底：确保 Mind 非空。</summary>
        public void Ensure(AWorker.WorkerData wd)
        {
            WorkerMindData.Ensure(wd);
        }

        /// <summary>距下次可接玩家命令的冷却剩余秒数（&gt;0 = 在拒绝冷却中）。</summary>
        public float GetRefusalCooldownRemaining(AWorker.WorkerData wd)
        {
            WorkerMindData.Ensure(wd);
            float elapsed = Time.time - wd.Mind.LastPlayerCommandRefusalTime;
            return Mathf.Max(0f, WorkerMindConstant.RefusalDelayCooldownSeconds - elapsed);
        }

        /// <summary>是否处于强制命令放行窗口（窗口内不拒绝玩家命令）。</summary>
        public bool IsInForceWindow(AWorker.WorkerData wd)
        {
            WorkerMindData.Ensure(wd);
            return Time.time < wd.Mind.ForcedUntilTime;
        }

        /// <summary>
        /// 评估该 Worker 对玩家命令的接受意愿。
        /// 附带怨恨/感恩的随时间自然消退（副作用，低频节流）。
        /// </summary>
        /// <param name="worker">目标 Worker。</param>
        /// <param name="randomValue">[0,1) 随机值（调用方用 UnityEngine.Random.value）。</param>
        /// <param name="reasonKey">输出判定理由键。</param>
        /// <returns>接受/拖延/拒绝。</returns>
        public CommandAcceptance EvaluateCommand(AWorker worker, float randomValue, out string reasonKey)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                reasonKey = WorkerMindConstant.ReasonAccept;
                return CommandAcceptance.Accept;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            this.DecayOverTime(mind);

            // 强制命令放行窗口内一律接受
            if (Time.time < mind.ForcedUntilTime)
            {
                reasonKey = WorkerMindConstant.ReasonAccept;
                return CommandAcceptance.Accept;
            }

            float favor = this.GetFavorabilityWithPlayer(worker);
            float cooldownRemaining = this.GetRefusalCooldownRemaining(wd);

            return CommandAcceptanceRuleService.Evaluate(
                wd.CurHungry, wd.MaxHungry,
                wd.CurTired, wd.MaxTired,
                wd.CurSpirit,
                favor,
                wd.Personality.Mood, wd.CurMorale,
                mind.ResentmentToPlayer, mind.GratitudeToPlayer, mind.WillingnessToObey,
                cooldownRemaining,
                randomValue,
                out reasonKey);
        }

        /// <summary>
        /// 记录一次玩家命令的结果（接受/拒绝），累积怨恨/感恩、设置拖延冷却。
        /// 生存/冷却理由不视为"真正的自主拒绝"，不累积怨恨。
        /// </summary>
        public void RecordCommandOutcome(AWorker worker, bool accepted, CommandAcceptance outcome, string reasonKey)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;

            if (accepted)
            {
                mind.AcceptedPlayerBountyCount++;
                mind.GratitudeToPlayer = Mathf.Clamp(mind.GratitudeToPlayer + WorkerMindConstant.AcceptGratitudeGain, 0f, 100f);
                mind.ResentmentToPlayer = Mathf.Clamp(mind.ResentmentToPlayer - WorkerMindConstant.AcceptResentmentRecover, 0f, 100f);
                return;
            }

            // 生存/冷却不是自主拒绝，不累积怨恨
            if (reasonKey == WorkerMindConstant.ReasonSurvival || reasonKey == WorkerMindConstant.ReasonCooldown)
            {
                return;
            }

            mind.RefusedPlayerBountyCount++;
            switch (reasonKey)
            {
                case WorkerMindConstant.ReasonResentment:
                case WorkerMindConstant.ReasonFavorability:
                    mind.ResentmentToPlayer = Mathf.Clamp(mind.ResentmentToPlayer + 4f, 0f, 100f);
                    break;
                case WorkerMindConstant.ReasonWillingness:
                    mind.WillingnessToObey = Mathf.Clamp(mind.WillingnessToObey - 2f, 0f, 100f);
                    mind.ResentmentToPlayer = Mathf.Clamp(mind.ResentmentToPlayer + 1f, 0f, 100f);
                    break;
                default: // ReasonRandomMood 等轻微
                    mind.ResentmentToPlayer = Mathf.Clamp(mind.ResentmentToPlayer + 1f, 0f, 100f);
                    break;
            }

            mind.LastPlayerCommandRefusalTime = Time.time;
        }

        /// <summary>
        /// 玩家救危：提升感恩与信任，并写入 EVT_PLAYER_HELP 事件记忆（信念同步演化）。
        /// </summary>
        public void RecordPlayerHelp(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            mind.GratitudeToPlayer = Mathf.Clamp(mind.GratitudeToPlayer + WorkerMindConstant.HelpGratitudeGain, 0f, 100f);
            mind.TrustInPlayer = Mathf.Clamp(mind.TrustInPlayer + WorkerMindConstant.HelpTrustGain, 0f, 100f);

            this.RecordEvent(worker, WorkerMindConstant.EVT_PLAYER_HELP, MemoryValence.Positive,
                PlayerTargetName, 60f, "玩家救了我一命");
        }

        /// <summary>
        /// 玩家强制命令：放行窗口内该 Worker 必接玩家悬赏，同时怨恨/好感/信任受损。
        /// 强制后进入拒绝冷却，防止玩家无限强刷怨恨。
        /// </summary>
        public void ForceCommand(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;

            // 防连击：短间隔内禁止重复强制（防止玩家无限点按强刷怨恨）。
            // LastForceCommandTime <= 0 视为从未强制（老档兜底），不受限。
            if (mind.LastForceCommandTime > 0f
                && Time.time - mind.LastForceCommandTime < WorkerMindConstant.ForceCommandCooldownSeconds)
            {
                AWorkerTask.LogProvider(
                    $"[MindDiag] {worker.name} 强制命令冷却中（剩 {WorkerMindConstant.ForceCommandCooldownSeconds - (Time.time - mind.LastForceCommandTime):F1}s），忽略",
                    LogManager.LogLevelEnum.Debug);
                return;
            }
            mind.LastForceCommandTime = Time.time;

            bool inCooldown = Time.time - mind.LastPlayerCommandRefusalTime < WorkerMindConstant.RefusalDelayCooldownSeconds;
            float resentmentGain = WorkerMindConstant.ForceResentmentPenalty
                + (inCooldown ? WorkerMindConstant.ForceResentmentPenaltyDuringCooldown : 0f);

            mind.ResentmentToPlayer = Mathf.Clamp(mind.ResentmentToPlayer + resentmentGain, 0f, 100f);
            mind.TrustInPlayer = Mathf.Clamp(mind.TrustInPlayer - WorkerMindConstant.ForceTrustPenalty, 0f, 100f);
            mind.ForcedUntilTime = Time.time + WorkerMindConstant.ForceWindowSeconds;
            mind.ForcedCommandCount++;
            mind.LastPlayerCommandRefusalTime = -999f; // 强制放行，清除拖延冷却

            if (Core.ServiceLocator.TryGet<FavorabilityManager>(out FavorabilityManager fm))
            {
                fm.ModifyWithPlayer(worker, -WorkerMindConstant.ForceFavorabilityPenalty, "玩家强制命令");
            }

            worker.ShowMindBubble(WorkerInnerMonologue.GetForcedReason());
            AWorkerTask.LogProvider(
                $"[MindDiag] {worker.name} 被玩家强制命令 怨恨+{resentmentGain:F0} 好感-{WorkerMindConstant.ForceFavorabilityPenalty:F0}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 玩家提前撤销强制命令：立即结束放行窗口（已付的怨恨/信任/好感代价不退还）。
        /// 窗口本就未生效时不做任何事。
        /// </summary>
        public void CancelForceCommand(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            if (Time.time >= wd.Mind.ForcedUntilTime)
            {
                return;
            }

            wd.Mind.ForcedUntilTime = 0f;
            wd.Mind.LastForceCommandTime = 0f; // 解除防连击：取消后允许立即重新强制（新命令照常付代价）
            AWorkerTask.LogProvider(
                $"[MindDiag] {worker.name} 强制命令被玩家提前撤销",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 通用事件记忆入口：写入记忆 + 信念演化 + 最近想法 + [MindDiag] 日志。
        /// 只允许在事件点调用（完成悬赏/被攻击/交易/对话等），绝不在每帧循环调用。
        /// </summary>
        /// <param name="worker">经历事件的 Worker。</param>
        /// <param name="typeKey">事件类型键（WorkerMindConstant.EVT_*）。</param>
        /// <param name="valence">事件正负向。</param>
        /// <param name="targetName">相关目标："PLAYER" 哨兵或 Worker 稳定名；空 = 无目标。</param>
        /// <param name="intensity">事件强度 0-100。</param>
        /// <param name="description">供 HUD「最近想法」与日志展示的短句。</param>
        public void RecordEvent(
            AWorker worker, string typeKey, MemoryValence valence,
            string targetName, float intensity, string description)
        {
            if (worker == null)
            {
                return;
            }

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;

            int day = this.GetGameDayIndex();
            WorkerMemoryRuleService.AddMemory(mind, day, typeKey, valence, targetName, intensity);
            WorkerBeliefRuleService.Apply(mind, WorkerBeliefRuleService.GetDelta(typeKey, intensity));
            PersonalityDriftRuleService.Accumulate(mind, typeKey, intensity);

            // 关系喂食（Worker-Worker 事件；对玩家"PLAYER"的关系由好感度系统管理，不在此处理）。
            // 关系等级变化时弹一句关系气泡（友谊建立/记仇/敌意/爱慕）。
            if (!string.IsNullOrEmpty(targetName) && targetName != PlayerTargetName)
            {
                if (WorkerRelationshipRuleService.Feed(mind, targetName, typeKey, intensity, day))
                {
                    WorkerRelationEntry rel = WorkerRelationshipRuleService.Find(mind, targetName);
                    if (rel != null)
                    {
                        string thought = WorkerInnerMonologue.GetRelationThought(rel.Kind);
                        if (!string.IsNullOrEmpty(thought))
                        {
                            worker.ShowMindBubble(thought);
                        }
                    }
                }
            }

            this.PushRecentThought(worker.name, description);

            AWorkerTask.LogProvider(
                $"[MindDiag] {worker.name} 事件记忆 {typeKey} 目标={targetName ?? "无"} 强度={intensity:F0}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 拾获物品的小确幸（WorkerPickUpTask.FromGround 成功拾取）：拾取任务高频
        /// （采集链/悬赏链每天大量），按游戏日节流同一天只记一次（RecordNearDeath 同款）。
        /// </summary>
        public void RecordFoundItem(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            int day = this.GetGameDayIndex();
            if (mind.LastFoundItemDay == day)
            {
                return;
            }

            mind.LastFoundItemDay = day;
            this.RecordEvent(worker, WorkerMindConstant.EVT_FOUND_ITEM, MemoryValence.Positive,
                null, 20f, "捡到了不错的东西");
            string thought = WorkerInnerMonologue.GetEventThought(WorkerMindConstant.EVT_FOUND_ITEM, null);
            if (!string.IsNullOrEmpty(thought))
            {
                worker.ShowMindBubble(thought);
            }
        }

        /// <summary>
        /// 濒死经历（极端饥饿紧急打断等）：按游戏日节流，同一天只记一次。
        /// 直接叠加记忆/信念副作用，并弹一句恐惧气泡。
        /// </summary>
        public void RecordNearDeath(AWorker worker)
        {
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            int day = this.GetGameDayIndex();
            if (mind.LastNearDeathDay == day)
            {
                return;
            }

            mind.LastNearDeathDay = day;
            this.RecordEvent(worker, WorkerMindConstant.EVT_NEAR_DEATH, MemoryValence.Negative,
                null, 75f, "差点没命，太可怕了");
            worker.ShowMindBubble(WorkerInnerMonologue.GetEventThought(WorkerMindConstant.EVT_NEAR_DEATH, null));
        }

        /// <summary>
        /// 应用随机人生事件（WorkerMindManager 在游戏日切换时驱动）：
        /// 软维度效果 + 事件记忆/信念 + 最近想法 + 气泡 + 正/中事件概率催生执念。
        /// 日限 1、濒危免骰（Manager 已判一次，此处双保险）。
        /// </summary>
        /// <param name="worker">经历事件的 Worker。</param>
        /// <param name="rollValue">[0,1) 掷骰值（决定命中哪个事件，UnityEngine.Random.value）。</param>
        public void ApplyLifeEvent(AWorker worker, float rollValue)
        {
            if (worker == null)
            {
                return;
            }

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            int day = this.GetGameDayIndex();

            // 日限 1（防重复触发，LifeEventMaxPerDay）
            if (mind.LastLifeEventDay == day)
            {
                return;
            }

            // 恩典：濒危免骰
            if (WorkerLifeEventRuleService.IsCritical(wd))
            {
                return;
            }

            WorkerLifeEventDef def = WorkerLifeEventRuleService.Roll(rollValue);

            mind.LastLifeEventDay = day;
            mind.LifeEventCount++;

            WorkerLifeEventRuleService.Apply(wd, mind, def, day);

            // 事件记忆 + 信念 + 最近想法（复用 RecordEvent）
            this.RecordEvent(worker, def.TypeKey, def.Valence, null, def.Intensity, def.Name);

            // 气泡
            string bubble = WorkerInnerMonologue.GetEventThought(def.TypeKey, null);
            if (!string.IsNullOrEmpty(bubble))
            {
                worker.ShowMindBubble(bubble);
            }

            // 正/中事件有概率催生执念（ActiveDream 为空且未达历史上限）
            if (def.Valence != MemoryValence.Negative)
            {
                WorkerDreamRuleService.TryBirth(mind, day, Random.value);
            }

            AWorkerTask.LogProvider(
                $"[MindDiag] {worker.name} 人生事件: {def.Name}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>最近想法（HUD「最近想法」行用，最多 RecentThoughtsCap 条，最新在后）。</summary>
        public List<string> GetRecentThoughts()
        {
            List<string> result = new List<string>(this.recentThoughts);
            return result;
        }

        /// <summary>对话预设意图今日已用次数（M3 包2.4 防刷；老档 List null 兜底）。</summary>
        public int GetIntentUseCountToday(AWorker.WorkerData wd, string intentKey)
        {
            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            if (mind.DialogueIntentUses == null)
            {
                mind.DialogueIntentUses = new List<DialogueIntentUse>();
                return 0;
            }

            int day = this.GetGameDayIndex();
            foreach (DialogueIntentUse use in mind.DialogueIntentUses)
            {
                if (use != null && use.IntentKey == intentKey && use.Day == day)
                {
                    return use.Count;
                }
            }

            return 0;
        }

        /// <summary>对话预设意图使用计数 +1（跨日自动重置）。</summary>
        public void RecordIntentUse(AWorker.WorkerData wd, string intentKey)
        {
            WorkerMindData.Ensure(wd);
            WorkerMindData mind = wd.Mind;
            if (mind.DialogueIntentUses == null)
            {
                mind.DialogueIntentUses = new List<DialogueIntentUse>();
            }

            int day = this.GetGameDayIndex();
            foreach (DialogueIntentUse use in mind.DialogueIntentUses)
            {
                if (use != null && use.IntentKey == intentKey)
                {
                    if (use.Day != day)
                    {
                        use.Day = day;
                        use.Count = 0;
                    }

                    use.Count++;
                    return;
                }
            }

            mind.DialogueIntentUses.Add(new DialogueIntentUse { IntentKey = intentKey, Day = day, Count = 1 });
        }

        /// <summary>
        /// 把悬赏/攻击的 instanceID 解析为稳定引用名："PLAYER"（0）或 Worker 稳定名。
        /// 解析失败返回 null（调用方可回退为 "unknown"）。
        /// </summary>
        public static string ResolveTargetName(int workerId)
        {
            if (workerId == 0)
            {
                return PlayerTargetName;
            }

            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null || wm.Characters == null)
            {
                return null;
            }

            foreach (AWorker w in wm.Characters)
            {
                if (w != null && w.GetInstanceID() == workerId)
                {
                    return w.name;
                }
            }

            return null;
        }

        /// <summary>当前游戏日索引（沿用 FavorabilityManager 的日口径）。</summary>
        private int GetGameDayIndex()
        {
            IGameTime gt = Core.ServiceLocator.Get<IGameTime>();
            if (gt == null)
            {
                return 0;
            }

            return (int)(gt.Time / FavorabilityConstant.GameDaySeconds);
        }

        /// <summary>推送一条最近想法，超上限丢弃最旧。</summary>
        private void PushRecentThought(string workerName, string description)
        {
            string line = $"{workerName} · {description}";
            if (this.recentThoughts.Count >= RecentThoughtsCap)
            {
                this.recentThoughts.Dequeue();
            }

            this.recentThoughts.Enqueue(line);
        }

        /// <summary>怨恨/感恩随时间的自然消退（防止怨恨永久锁死 Worker 拒绝一切玩家命令）。</summary>
        private void DecayOverTime(WorkerMindData mind)
        {
            if (mind.ResentmentToPlayer <= 0f && mind.GratitudeToPlayer <= 0f)
            {
                return;
            }

            if (Time.time - mind.LastResentmentDecayTime < WorkerMindConstant.ResentmentDecayIntervalSeconds)
            {
                return;
            }

            mind.LastResentmentDecayTime = Time.time;
            if (mind.ResentmentToPlayer > 0f)
            {
                mind.ResentmentToPlayer = Mathf.Max(0f, mind.ResentmentToPlayer - WorkerMindConstant.ResentmentDecayPerInterval);
            }
            if (mind.GratitudeToPlayer > 0f)
            {
                mind.GratitudeToPlayer = Mathf.Max(0f, mind.GratitudeToPlayer - WorkerMindConstant.GratitudeDecayPerInterval);
            }
        }

        private float GetFavorabilityWithPlayer(AWorker worker)
        {
            if (Core.ServiceLocator.TryGet<FavorabilityManager>(out FavorabilityManager fm))
            {
                return fm.GetFavorabilityWithPlayer(worker);
            }
            return FavorabilityRuleService.InitialFavorability;
        }
    }
}
