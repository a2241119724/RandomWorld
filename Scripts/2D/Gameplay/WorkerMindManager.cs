namespace LAB2D.Gameplay
{
    /// <summary>
    /// Worker 心智层日驱动管理器 — 检测游戏日切换，驱动每 Worker 记忆逐日衰减与遗忘剔除。
    /// 数据本体在 WorkerData.Mind（随角色二进制存档），本类只持有运行时节流状态，无独立存档字段。
    /// 注册到 ServiceLocator 并由 GlobalInit.BuildTickableList 驱动。
    /// </summary>
    public class WorkerMindManager : ASingletonSaveData<WorkerMindManager>, ITickable
    {
        /// <summary>日检节流间隔（秒）。</summary>
        private const float DayCheckIntervalSeconds = 5f;

        private float dayCheckTimer;
        private int lastProcessedDay = -1;

        /// <summary>上次人生事件掷骰日（-999 起，首个掷骰日即可掷）。</summary>
        private int lastLifeEventRollDay = -999;

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.dayCheckTimer += deltaTime;
            if (this.dayCheckTimer < DayCheckIntervalSeconds)
            {
                return;
            }

            this.dayCheckTimer = 0f;

            int day = this.GetGameDayIndex();
            if (day == this.lastProcessedDay)
            {
                return;
            }

            this.lastProcessedDay = day;
            this.ProcessDayRollover(day);
        }

        /// <summary>游戏日切换：记忆衰减 + 执念热情消退 + 人生事件掷骰（每 2 游戏日一次）。</summary>
        private void ProcessDayRollover(int day)
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null || wm.Characters == null)
            {
                return;
            }

            // 人生事件按间隔掷骰（每 LifeEventRollIntervalDays 日一次；濒危免骰由 TryRollLifeEvent 把关）
            bool rollLifeEvents = day >= this.lastLifeEventRollDay + WorkerMindConstant.LifeEventRollIntervalDays;
            if (rollLifeEvents)
            {
                this.lastLifeEventRollDay = day;
            }

            foreach (AWorker worker in wm.Characters)
            {
                if (worker == null)
                {
                    continue;
                }

                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd == null)
                {
                    continue;
                }

                WorkerMindData.Ensure(wd);
                int removed = WorkerMemoryRuleService.TickDay(wd.Mind, day);
                if (removed > 0)
                {
                    AWorkerTask.LogProvider(
                        $"[MindDiag] {worker.name} 记忆逐日衰减，遗忘 {removed} 条",
                        LogManager.LogLevelEnum.Debug);
                }

                WorkerDreamRuleService.Decay(wd.Mind, day);

                int migrated = PersonalityDriftRuleService.Migrate(wd, day);
                if (migrated > 0)
                {
                    AWorkerTask.LogProvider(
                        $"[MindDiag] {worker.name} 人格漂移迁移 {migrated} 维",
                        LogManager.LogLevelEnum.Debug);
                }

                // 关系每日维护：记仇/爱慕衰减、亲密度向 0 回归（长期不互动关系变淡）
                WorkerRelationshipRuleService.Decay(wd.Mind, day);

                if (rollLifeEvents)
                {
                    this.TryRollLifeEvent(worker, wd);
                }
            }
        }

        /// <summary>单个 Worker 的人生事件掷骰：恩典（濒危免骰）+ 基准概率，命中则交给 WorkerMindService 应用。</summary>
        private void TryRollLifeEvent(AWorker worker, AWorker.WorkerData wd)
        {
            // 恩典原则：已濒危当轮不掷骰，避免负事件叠加把人推过线
            if (WorkerLifeEventRuleService.IsCritical(wd))
            {
                return;
            }

            if (UnityEngine.Random.value >= WorkerMindConstant.LifeEventBaseChancePerRoll)
            {
                return;
            }

            WorkerMindService mindService = Core.ServiceLocator.Get<WorkerMindService>();
            if (mindService != null)
            {
                mindService.ApplyLifeEvent(worker, UnityEngine.Random.value);
            }
        }

        private int GetGameDayIndex()
        {
            IGameTime gt = Core.ServiceLocator.Get<IGameTime>();
            if (gt == null)
            {
                return 0;
            }

            return (int)(gt.Time / FavorabilityConstant.GameDaySeconds);
        }
    }
}
