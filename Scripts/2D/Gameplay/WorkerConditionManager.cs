namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.AwakenedPower;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.Gameplay.GongFa;
    using LAB2D.Enum;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using GrowthData = LAB2D.Domain.Character.Growth.GrowthData;

    /// <summary>
    /// 工人饥饿与疲劳状态管理器。
    /// 负责在运行时汇总 Worker 状态、派发状态变化事件、提供移动与工作效率倍率，并显示低状态提示。
    /// 本类不修改存档结构，不写入资源，不参与 Photon 同步。
    /// </summary>
    public class WorkerConditionManager : Singleton<WorkerConditionManager>, IWorkerConditionManager
    {
        private readonly Dictionary<int, WorkerConditionSnapshot> snapshots;
        private readonly Dictionary<int, float> lastTipTimes;
        private bool enabled = true;
        private bool tipEnabled = true;
        private IGameTime gameTime;
        private IGameLogger gameLogger;

        private IGameTime GameTime => this.gameTime ?? (this.gameTime = Core.ServiceLocator.Get<IGameTime>());
        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        public WorkerConditionManager()
        {
            this.snapshots = new Dictionary<int, WorkerConditionSnapshot>();
            this.lastTipTimes = new Dictionary<int, float>();
        }

        /// <summary>
        /// Worker 状态变化事件。
        /// HUD 或其他表现层可订阅此事件刷新显示。
        /// </summary>
        public event Action<AWorker, WorkerConditionSnapshot> OnWorkerConditionChanged;

        /// <summary>
        /// Worker 状态提示请求事件。
        /// 外部可订阅此事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnWorkerConditionTipRequested;

        /// <summary>
        /// 工人状态效果是否启用。
        /// 关闭后移动与工作倍率均回到 1，但 HUD 仍可显示原始状态。
        /// </summary>
        public bool IsEnabled
        {
            get { return this.enabled; }
        }

        /// <summary>
        /// 启用工人状态效果。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
        }

        /// <summary>
        /// 禁用工人状态效果，移动与工作倍率回到 1。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
        }

        /// <summary>
        /// 设置是否显示工人状态提示。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip 提示。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 刷新单个 Worker 的状态快照。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        public void UpdateWorkerCondition(AWorker worker)
        {
            if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return;
            }

            // 每帧每 Worker 都会进来（WorkerUpdateSystem 驱动）：先比对状态档位，未变化直接复用旧快照——
            // 避免 FromWorker 每帧 new 一个 class（100 Worker ≈ 6000 分配/秒的持续 GC）。
            // 依据：MoveSpeed/WorkProgress 倍率是 State 的纯函数；比值字段仅 HUD 展示消费，
            // 而 HUD 读数走 GetWorkerCondition（每次重算），此处跳过不影响任何消费方。
            int instanceId = worker.GetInstanceID();
            WorkerConditionState state = WorkerConditionTool.GetState(workerData);
            if (this.snapshots.TryGetValue(instanceId, out WorkerConditionSnapshot previous)
                && previous.State == state)
            {
                return;
            }

            WorkerConditionSnapshot snapshot = WorkerConditionSnapshot.FromWorker(worker, workerData);
            this.snapshots[instanceId] = snapshot;
            this.OnWorkerConditionChanged?.Invoke(worker, snapshot);
            this.TryShowConditionTip(snapshot, previous);
        }

        /// <summary>
        /// 获取 Worker 当前状态快照。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <returns>状态快照，无法读取时返回 null。</returns>
        public WorkerConditionSnapshot GetWorkerCondition(AWorker worker)
        {
            if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return null;
            }

            int instanceId = worker.GetInstanceID();
            WorkerConditionSnapshot snapshot = WorkerConditionSnapshot.FromWorker(worker, workerData);
            this.snapshots[instanceId] = snapshot;
            return snapshot;
        }

        /// <summary>
        /// 获取 Worker 当前移动速度倍率。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <returns>移动速度倍率，禁用或无法读取时返回 1。</returns>
        public float GetWorkerMoveSpeedMultiplier(AWorker worker)
        {
            if (!this.enabled)
            {
                return 1.0f;
            }

            WorkerConditionSnapshot snapshot = this.GetWorkerCondition(worker);
            return snapshot == null ? 1.0f : snapshot.MoveSpeedMultiplier;
        }

        /// <summary>
        /// 获取套用工人状态后的移动速度。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="baseSpeed">基础移动速度。</param>
        /// <returns>套用状态倍率后的安全速度。</returns>
        public float GetAdjustedWorkerMoveSpeed(AWorker worker, float baseSpeed)
        {
            return WeatherGameplayTool.ApplyMultiplier(
                baseSpeed,
                this.GetWorkerMoveSpeedMultiplier(worker),
                0.0f);
        }

        /// <summary>
        /// 获取 Worker 当前任务进度倍率。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="taskType">任务类型。</param>
        /// <returns>任务进度倍率，禁用或无法读取时返回 1。</returns>
        public float GetWorkerTaskProgressMultiplier(AWorker worker, WorkerTaskType taskType)
        {
            if (!this.enabled)
            {
                return 1.0f;
            }

            WorkerConditionSnapshot snapshot = this.GetWorkerCondition(worker);
            if (snapshot == null)
            {
                return 1.0f;
            }

            float multiplier = WorkerConditionTool.GetTaskProgressMultiplier(snapshot.State, taskType);

            // 压力/士气惩罚：高压 → 心智涣散工作变慢；低士气 → 怠工；吃饭/睡觉/地面睡眠不受影响
            if (taskType != WorkerTaskType.Eat
                && taskType != WorkerTaskType.Sleep
                && taskType != WorkerTaskType.GroundSleep
                && WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData wd))
            {
                multiplier *= WorkerConditionTool.GetStressWorkMultiplier(
                    WorkerConditionTool.GetSafeRatio(wd.CurStress, wd.MaxStress));
                multiplier *= WorkerConditionTool.GetMoraleWorkMultiplier(
                    WorkerConditionTool.GetSafeRatio(wd.CurMorale, wd.MaxMorale));
            }

            return multiplier;
        }

        /// <summary>
        /// 构建所有 Worker 的状态摘要。
        /// </summary>
        /// <returns>适合 HUD 和 Editor 菜单展示的多行文本。</returns>
        public string BuildSummaryText()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine(this.enabled ? "工人状态效果: 已启用" : "工人状态效果: 已禁用");

            try
            {
                if (!Core.ServiceLocator.TryGet(out WorkerManager wmgr) || wmgr.Characters == null ||
                    wmgr.Characters.Count == 0)
                {
                    builder.Append(WorkerConditionConstant.EmptyHudText);
                    return builder.ToString();
                }

                List<AWorker> workers = wmgr.Characters;
                for (int i = 0; i < workers.Count; i++)
                {
                    AWorker worker = workers[i];
                    WorkerConditionSnapshot snapshot = this.GetWorkerCondition(worker);
                    if (snapshot == null)
                    {
                        continue;
                    }

                    builder.AppendLine(snapshot.ToDisplayLine());
                    builder.AppendLine(this.BuildLifeSkillLine(worker));
                    builder.AppendLine(this.BuildCultivationLine(worker));
                }
            }
            catch (Exception exception)
            {
                builder.Append("工人状态暂不可用: ").Append(exception.Message);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 拼接单个工人的生活技能进度行（伐木/采矿/农耕 Lv+进度）。
        /// 无任何经验时不显示（返回空串，避免多工人 HUD 空行噪音）。
        /// </summary>
        private string BuildLifeSkillLine(AWorker worker)
        {
            AWorker.WorkerData workerData = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return string.Empty;
            }

            workerData.EnsureLifeSkills();

            StringBuilder builder = new StringBuilder(96);
            builder.Append("生活技能: ");
            bool anyXp = false;
            foreach (LifeSkillType skill in Domain.Worker.LifeSkillRuleService.AllSkills)
            {
                workerData.LifeSkillXp.TryGetValue(skill, out float xp);
                anyXp |= xp > 0f;
                int level = Domain.Worker.LifeSkillRuleService.LevelOf(xp);
                float next = Domain.Worker.LifeSkillRuleService.XpToNextLevel(xp);
                string progress = next < 0f ? "MAX" : $"{xp:F0}/{next:F0}";
                builder.Append($"{Domain.Worker.LifeSkillRuleService.GetName(skill)}Lv{level}({progress}) ");
            }

            return anyXp ? builder.ToString().TrimEnd() : string.Empty;
        }

        /// <summary>
        /// 拼接单个 Worker 的修仙进度行（境界/灵气/灵根/运转内功/觉醒异能）。
        /// 尚未踏入修炼（无灵气/境界/功法/异能）时不显示（返回空串，避免多工人 HUD 空行噪音）。
        /// </summary>
        private string BuildCultivationLine(AWorker worker)
        {
            AWorker.WorkerData workerData = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return string.Empty;
            }

            GrowthData.Ensure(ref workerData.Growth);
            GrowthData growth = workerData.Growth;

            bool hasQi = growth.Qi > 0f;
            bool hasGongFa = growth.LearnedGongFaIds != null && growth.LearnedGongFaIds.Count > 0;
            bool hasPower = growth.AwakenedPowerIds != null && growth.AwakenedPowerIds.Count > 0;
            if (!hasQi && growth.RealmIndex <= 0 && !hasGongFa && !hasPower)
            {
                return string.Empty;
            }

            RealmDef realm = RealmRuleService.GetRealm(growth);
            StringBuilder builder = new StringBuilder(96);
            builder.Append($"修炼: {realm.Name}(灵气 {growth.Qi:F0}/{realm.QiToNext:F0})");

            // 灵根（五行中文名连写，如"金木"）
            if (growth.LingGenElements != null && growth.LingGenElements.Count > 0)
            {
                builder.Append(" 灵根:");
                foreach (int element in growth.LingGenElements)
                {
                    builder.Append(LingGenRuleService.GetElementName((Element)element));
                }
            }

            if (!string.IsNullOrEmpty(growth.ActiveNeiGongId))
            {
                GongFaDef neiGong = GongFaLibrary.Get(growth.ActiveNeiGongId);
                if (neiGong != null)
                {
                    builder.Append($" | 运转:{neiGong.Name}");
                }
            }

            if (hasPower)
            {
                builder.Append(" | 异能:");
                foreach (string powerId in growth.AwakenedPowerIds)
                {
                    AwakenedPowerDef power = AwakenedPowerLibrary.Get(powerId);
                    if (power != null)
                    {
                        builder.Append(power.Name).Append(' ');
                    }
                }
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 状态变化时显示 Tip。
        /// </summary>
        /// <param name="snapshot">当前状态快照。</param>
        /// <param name="previous">上一帧缓存状态。</param>
        private void TryShowConditionTip(WorkerConditionSnapshot snapshot, WorkerConditionSnapshot previous)
        {
            if (!this.tipEnabled || snapshot == null)
            {
                return;
            }

            bool recovered = snapshot.State == WorkerConditionState.Healthy &&
                previous != null &&
                previous.State != WorkerConditionState.Healthy;
            if (!recovered && snapshot.State == WorkerConditionState.Healthy)
            {
                return;
            }

            float now = this.GameTime.Time;
            if (!recovered &&
                this.lastTipTimes.TryGetValue(snapshot.WorkerInstanceId, out float lastTipTime) &&
                now - lastTipTime < WorkerConditionConstant.TipCooldownSeconds)
            {
                return;
            }

            this.lastTipTimes[snapshot.WorkerInstanceId] = now;
            string message = WorkerConditionTool.BuildTipText(
                snapshot.WorkerName,
                snapshot.State,
                snapshot.MoveSpeedMultiplier,
                snapshot.WorkProgressMultiplier);
            this.ShowTip(message);
        }

        /// <summary>
        /// 显示状态提示。
        /// 优先使用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示内容。</param>
        private void ShowTip(string message)
        {
            this.OnWorkerConditionTipRequested?.Invoke(message);

            try
            {
                Core.GameServices.ShowTipProvider(message);
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning("[WorkerCondition] 显示 Tip 失败: " + exception.Message);
            }

            this.GameLogger.Log("[工人状态] " + message);
        }
    }

    /// <summary>
    /// 工人饥饿与疲劳状态快照。
    /// 由 WorkerConditionManager 维护，供 HUD、Editor 菜单和其他业务只读查询。
    /// </summary>
    [Serializable]
    public class WorkerConditionSnapshot
    {
        /// <summary>工人名称。</summary>
        public string WorkerName;

        /// <summary>工人运行时实例 ID。</summary>
        public int WorkerInstanceId;

        /// <summary>工人生存状态。</summary>
        public WorkerConditionState State;

        /// <summary>当前饥饿值比例。</summary>
        public float HungryRatio;

        /// <summary>当前疲劳值比例。</summary>
        public float TiredRatio;

        /// <summary>当前压力值比例。</summary>
        public float StressRatio;

        /// <summary>当前士气值比例。</summary>
        public float MoraleRatio;

        /// <summary>移动速度倍率。</summary>
        public float MoveSpeedMultiplier;

        /// <summary>普通任务进度倍率。</summary>
        public float WorkProgressMultiplier;

        /// <summary>
        /// 从 Worker 数据创建状态快照。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="workerData">工人数据。</param>
        /// <returns>状态快照。</returns>
        public static WorkerConditionSnapshot FromWorker(AWorker worker, AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return new WorkerConditionSnapshot
                {
                    WorkerName = worker == null ? "未知工人" : worker.name,
                    WorkerInstanceId = worker == null ? 0 : worker.GetInstanceID(),
                    State = WorkerConditionState.Healthy,
                    HungryRatio = 1.0f,
                    TiredRatio = 0.0f,
                    StressRatio = 0.0f,
                    MoraleRatio = 1.0f,
                    MoveSpeedMultiplier = 1.0f,
                    WorkProgressMultiplier = 1.0f,
                };
            }

            WorkerConditionState state = WorkerConditionTool.GetState(workerData);
            return new WorkerConditionSnapshot
            {
                WorkerName = worker == null ? "未知工人" : worker.name,
                WorkerInstanceId = worker == null ? 0 : worker.GetInstanceID(),
                State = state,
                HungryRatio = WorkerConditionTool.GetSafeRatio(workerData.CurHungry, workerData.MaxHungry),
                TiredRatio = WorkerConditionTool.GetSafeRatio(workerData.CurTired, workerData.MaxTired),
                StressRatio = WorkerConditionTool.GetSafeRatio(workerData.CurStress, workerData.MaxStress),
                MoraleRatio = WorkerConditionTool.GetSafeRatio(workerData.CurMorale, workerData.MaxMorale),
                MoveSpeedMultiplier = WorkerConditionTool.GetMoveSpeedMultiplier(state),
                WorkProgressMultiplier = WorkerConditionTool.GetTaskProgressMultiplier(
                    state,
                    WorkerTaskType.Build),
            };
        }

        /// <summary>
        /// 生成 HUD 展示行。
        /// </summary>
        /// <returns>带 RichText 颜色的单行状态文本。</returns>
        public string ToDisplayLine()
        {
            return WorkerConditionTool.BuildConditionLine(
                this.WorkerName,
                this.State,
                this.HungryRatio,
                this.TiredRatio,
                this.StressRatio,
                this.MoraleRatio,
                this.MoveSpeedMultiplier,
                this.WorkProgressMultiplier);
        }
    }
}
