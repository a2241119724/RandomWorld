namespace LAB2D.Domain.Worker
{
    using LAB2D.Character.Worker;
    using System;

    /// <summary>
    /// Worker 性格演化（强漂移）纯规则服务 — 事件强度累积到漂移桶，达标一次性迁移到 Personality。
    /// 与 WorkerPersonality.After*（高频小幅微调）分层：本服务负责"经历塑格"的慢变量。
    /// 三防横跳机制：
    /// ① 滞回带 — 迁移后反向需再积 (DriftMigrateThreshold + DriftHysteresisBand) 才回迁；
    /// ② 日限流 — 每游戏日最多 DriftMigrateMaxPerDay 次迁移；
    /// ③ 累积饱和 — 单桶绝对值封顶 DriftBucketAbsCap，同向反复累积不无限涨。
    /// 零 Unity 依赖，可单测。
    /// </summary>
    public static class PersonalityDriftRuleService
    {
        /// <summary>
        /// 事件强度累积到漂移桶（RecordEvent 末尾调用，事件点，非每帧）。
        /// 人生事件类型（EVT_INSIGHT/WIND_FALL/SMALL_JOY/ENLIGHTENMENT/MISFORTUNE/ILLNESS/NIGHTMARE）
        /// 无映射跳过——其漂移已由 WorkerLifeEventRuleService.Apply 按 def 显式累积，此处跳过防双计。
        /// </summary>
        public static void Accumulate(WorkerMindData mind, string typeKey, float intensity)
        {
            if (mind == null || string.IsNullOrEmpty(typeKey))
            {
                return;
            }

            (float mood, float ambition, float diligence, float sociality) dir = GetDriftDir(typeKey);
            if (dir.mood == 0f && dir.ambition == 0f && dir.diligence == 0f && dir.sociality == 0f)
            {
                return;
            }

            float amount = Clamp(
                intensity * WorkerMindConstant.DriftAccumulateScale,
                -WorkerMindConstant.DriftMaxPerEvent,
                WorkerMindConstant.DriftMaxPerEvent);

            PersonalityDrift d = mind.Drift;
            d.MoodDrift = Clamp(d.MoodDrift + (dir.mood * amount), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
            d.AmbitionDrift = Clamp(d.AmbitionDrift + (dir.ambition * amount), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
            d.DiligenceDrift = Clamp(d.DiligenceDrift + (dir.diligence * amount), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
            d.SocialityDrift = Clamp(d.SocialityDrift + (dir.sociality * amount), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
        }

        /// <summary>
        /// 达标迁移（WorkerMindManager 游戏日切换时调用）：检查四桶，首个 |v|≥阈值 的桶向 Personality
        /// 迁移 ±DriftMigrateStep（构造函数自带 clamp [0,100]）后归零并记录方向。
        /// 日限流：同一天只迁一次；滞回：方向与上次迁移相反需 (Threshold + Band)。
        /// </summary>
        /// <returns>本次迁移的次数（0 或 1）。</returns>
        public static int Migrate(AWorker.WorkerData wd, int day)
        {
            if (wd == null || wd.Mind == null)
            {
                return 0;
            }

            WorkerMindData mind = wd.Mind;
            if ((int)mind.LastDriftMigrationDay == day)
            {
                return 0;
            }

            PersonalityDrift d = mind.Drift;
            if (TryMigrateDim(wd, ref d.MoodDrift, ref d.MoodDir, PersonalityDim.Mood))
            {
                return FinalizeDay(mind, day);
            }

            if (TryMigrateDim(wd, ref d.AmbitionDrift, ref d.AmbitionDir, PersonalityDim.Ambition))
            {
                return FinalizeDay(mind, day);
            }

            if (TryMigrateDim(wd, ref d.DiligenceDrift, ref d.DiligenceDir, PersonalityDim.Diligence))
            {
                return FinalizeDay(mind, day);
            }

            if (TryMigrateDim(wd, ref d.SocialityDrift, ref d.SocialityDir, PersonalityDim.Sociality))
            {
                return FinalizeDay(mind, day);
            }

            return 0;
        }

        private static bool TryMigrateDim(AWorker.WorkerData wd, ref float drift, ref int dir, PersonalityDim dim)
        {
            if (Math.Abs(drift) < 0.001f)
            {
                return false;
            }

            // 滞回：迁移后反向需再积 (Threshold + Band) 才回迁；同向/初始按基础阈值
            bool opposite = dir != 0 && ((dir > 0 && drift < 0f) || (dir < 0 && drift > 0f));
            float threshold = opposite
                ? WorkerMindConstant.DriftMigrateThreshold + WorkerMindConstant.DriftHysteresisBand
                : WorkerMindConstant.DriftMigrateThreshold;

            if (Math.Abs(drift) < threshold)
            {
                return false;
            }

            float step = (drift > 0f ? 1f : -1f) * WorkerMindConstant.DriftMigrateStep;

            WorkerPersonality p = wd.Personality;
            wd.Personality = new WorkerPersonality(
                p.Mood + (dim == PersonalityDim.Mood ? step : 0f),
                p.Ambition + (dim == PersonalityDim.Ambition ? step : 0f),
                p.Diligence + (dim == PersonalityDim.Diligence ? step : 0f),
                p.Sociality + (dim == PersonalityDim.Sociality ? step : 0f));

            drift = 0f;
            dir = step > 0f ? 1 : -1;
            return true;
        }

        private static int FinalizeDay(WorkerMindData mind, int day)
        {
            mind.LastDriftMigrationDay = day;
            return 1;
        }

        /// <summary>
        /// 事件类型 → 漂移方向系数（mood/ambition/diligence/sociality，各 ∈ 0/±0.5/±1）。
        /// 人生事件类型走 default（(0,0,0,0)）：其漂移由 WorkerLifeEventDef 显式累积，此处跳过。
        /// </summary>
        private static (float mood, float ambition, float diligence, float sociality) GetDriftDir(string typeKey)
        {
            switch (typeKey)
            {
                // 被攻击 → 心情↓ 社交↓（敌意）
                case WorkerMindConstant.EVT_PLAYER_ATTACK:
                case WorkerMindConstant.EVT_WORKER_ATTACK:
                    return (-1f, 0f, 0f, -0.5f);

                // 被杀 → 心情↓ 事业心↓ 社交↓↓（对社会绝望）
                case WorkerMindConstant.EVT_PLAYER_KILL:
                    return (-1f, -0.5f, 0f, -1f);

                // 濒死 → 心情↓（恐惧）
                case WorkerMindConstant.EVT_NEAR_DEATH:
                    return (-1f, 0f, 0f, 0f);

                // 交易被拒 → 社交↓；交易成功 → 心情↑ 社交↑
                case WorkerMindConstant.EVT_TRADE_REJECTED:
                    return (0f, 0f, 0f, -0.5f);
                case WorkerMindConstant.EVT_TRADE_SUCCESS:
                    return (0.5f, 0f, 0f, 1f);

                // 接取悬赏 → 勤奋↑ 社交↑；完成悬赏 → 心情↑ 事业心↑
                case WorkerMindConstant.EVT_BOUNTY_ACCEPTED:
                    return (0f, 0f, 0.5f, 0.5f);
                case WorkerMindConstant.EVT_BOUNTY_COMPLETED:
                    return (0.5f, 1f, 0f, 0f);

                // 完成任务 → 心情↑ 勤奋↑；阶段升级 → 心情↑ 事业心↑ 社交↑
                case WorkerMindConstant.EVT_TASK_COMPLETED:
                    return (0.5f, 0f, 1f, 0f);
                case WorkerMindConstant.EVT_STAGE_UP:
                    return (0.5f, 0.5f, 0f, 0.5f);

                // 对话 → 社交↑；被帮助 → 心情↑ 社交↑
                case WorkerMindConstant.EVT_CONVERSATION:
                    return (0f, 0f, 0f, 1f);
                case WorkerMindConstant.EVT_PLAYER_HELP:
                    return (1f, 0f, 0f, 0.5f);

                // 野外睡觉 → 心情↑；捡到物品 → 心情↑ 事业心↑
                case WorkerMindConstant.EVT_GROUND_SLEEP:
                    return (0.5f, 0f, 0f, 0f);
                case WorkerMindConstant.EVT_FOUND_ITEM:
                    return (0.5f, 0.5f, 0f, 0f);

                // 人生事件类型与其余中性事件（EVT_BOUNTY_REFUSED 等）→ 无漂移累积
                default:
                    return (0f, 0f, 0f, 0f);
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        private enum PersonalityDim { Mood, Ambition, Diligence, Sociality }
    }
}
