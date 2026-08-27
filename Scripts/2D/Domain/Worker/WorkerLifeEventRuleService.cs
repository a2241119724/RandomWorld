namespace LAB2D.Domain.Worker
{
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 单个随机人生事件的效果表（纯数据）。
    /// 平衡三原则落点：①封顶 ③可恢复（负事件只动软维度——士气/精气神/心情，
    /// 绝不直接扣饥饿/疲劳，不把 Worker 打入濒危线）。
    /// </summary>
    public struct WorkerLifeEventDef
    {
        /// <summary>事件类型键（WorkerMindConstant.EVT_*）。</summary>
        public string TypeKey;

        /// <summary>中文名（气泡 / HUD / 日志）。</summary>
        public string Name;

        /// <summary>正负向。</summary>
        public MemoryValence Valence;

        /// <summary>掷骰权重（Roll 概率 ∝ weight）。</summary>
        public float Weight;

        /// <summary>事件强度 0-100（信念增量缩放）。</summary>
        public float Intensity;

        /// <summary>精气神变化（软维度，应用时封顶 ±LifeEventSurvivalDamageCap）。</summary>
        public float SpiritDelta;

        /// <summary>士气变化（软维度，同上）。</summary>
        public float MoraleDelta;

        /// <summary>心情变化（软维度，直接改 Personality.Mood）。</summary>
        public float MoodDelta;

        /// <summary>金币变化（仅正/中事件给钱，负事件恒 0）。</summary>
        public float GoldDelta;

        // 人格漂移（累积到 mind.Drift 桶，Phase 4 迁移；单次封顶 ±LifeEventPersonalityDriftCap）
        /// <summary>心情漂移累积量。</summary>
        public float MoodDrift;

        /// <summary>事业心漂移累积量。</summary>
        public float AmbitionDrift;

        /// <summary>勤奋漂移累积量。</summary>
        public float DiligenceDrift;

        /// <summary>社交漂移累积量。</summary>
        public float SocialityDrift;
    }

    /// <summary>
    /// Worker 随机人生事件纯规则服务 — 事件表、权重掷骰、濒危判定、带封顶的应用。
    /// 平衡三原则（写入规则并单测）：
    /// ①封顶：生存数值单次 ≤±LifeEventSurvivalDamageCap，人格漂移 ≤±LifeEventPersonalityDriftCap。
    /// ②恩典：已濒危（WorkerConditionRuleService 判 Critical）当轮不掷骰；一天最多 1 次。
    /// ③可恢复：负事件只动软维度，由现有吃饭/睡觉/漫游回补，绝不直接扣到饥饿/疲劳致死线。
    /// 零 Unity 依赖，可单测。
    /// </summary>
    public static class WorkerLifeEventRuleService
    {
        private static readonly WorkerConditionRuleService ConditionService = new WorkerConditionRuleService();

        /// <summary>
        /// 事件表：灵感(正)/横财(正)/小确幸(正)/顿悟(中)/变故(负)/疾病(负)/梦魇(负)。
        /// 权重合计 70（正 36 / 中 10 / 负 24，正向略多于负向）。
        /// </summary>
        private static readonly WorkerLifeEventDef[] AllDefs =
        {
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_INSIGHT,
                Name = "灵光一闪",
                Valence = MemoryValence.Positive,
                Weight = 12f,
                Intensity = 60f,
                SpiritDelta = 10f,
                MoraleDelta = 6f,
                MoodDelta = 4f,
                AmbitionDrift = 4f,
            },
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_WIND_FALL,
                Name = "横财入袋",
                Valence = MemoryValence.Positive,
                Weight = 10f,
                Intensity = 80f,
                MoraleDelta = 8f,
                GoldDelta = 40f,
                AmbitionDrift = 3f,
            },
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_SMALL_JOY,
                Name = "小确幸",
                Valence = MemoryValence.Positive,
                Weight = 14f,
                Intensity = 40f,
                SpiritDelta = 8f,
                MoraleDelta = 5f,
                MoodDelta = 3f,
                SocialityDrift = 3f,
            },
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_ENLIGHTENMENT,
                Name = "顿悟",
                Valence = MemoryValence.Neutral,
                Weight = 10f,
                Intensity = 60f,
                SpiritDelta = 6f,
                MoodDelta = 4f,
                DiligenceDrift = 4f,
            },
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_MISFORTUNE,
                Name = "遭遇变故",
                Valence = MemoryValence.Negative,
                Weight = 9f,
                Intensity = 70f,
                SpiritDelta = -10f,
                MoraleDelta = -8f,
                MoodDelta = -5f,
                MoodDrift = -6f,
            },
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_ILLNESS,
                Name = "染上小病",
                Valence = MemoryValence.Negative,
                Weight = 8f,
                Intensity = 80f,
                SpiritDelta = -12f,
                MoraleDelta = -6f,
                MoodDelta = -4f,
                MoodDrift = -5f,
            },
            new WorkerLifeEventDef
            {
                TypeKey = WorkerMindConstant.EVT_NIGHTMARE,
                Name = "噩梦缠身",
                Valence = MemoryValence.Negative,
                Weight = 7f,
                Intensity = 60f,
                SpiritDelta = -8f,
                MoraleDelta = -5f,
                MoodDelta = -3f,
                MoodDrift = -3f,
            },
        };

        /// <summary>事件表（防御性拷贝，避免外部改表）。</summary>
        public static List<WorkerLifeEventDef> GetEventTable()
        {
            return new List<WorkerLifeEventDef>(AllDefs);
        }

        /// <summary>
        /// 按权重掷骰选一个事件。randomValue∈[0,1)，越界自动 clamp。
        /// 权重为 0 的事件不会被掷中。
        /// </summary>
        public static WorkerLifeEventDef Roll(float randomValue)
        {
            float r = (randomValue < 0f) ? 0f : (randomValue > 1f ? 1f : randomValue);

            float total = 0f;
            for (int i = 0; i < AllDefs.Length; i++)
            {
                total += AllDefs[i].Weight;
            }

            if (total <= 0f)
            {
                return AllDefs[0];
            }

            float cursor = r * total;
            float acc = 0f;
            for (int i = 0; i < AllDefs.Length; i++)
            {
                acc += AllDefs[i].Weight;
                if (cursor < acc)
                {
                    return AllDefs[i];
                }
            }

            return AllDefs[AllDefs.Length - 1];
        }

        /// <summary>
        /// 恩典原则判定：是否已濒危（饥饿比例 ≤CriticalRatio 或 疲劳比例 ≥1-CriticalRatio）。
        /// 濒危 Worker 当轮不掷骰，避免负事件叠加把人推过线。
        /// </summary>
        public static bool IsCritical(AWorker.WorkerData wd)
        {
            if (wd == null)
            {
                return true;
            }

            var snapshot = new WorkerAgentSnapshot(
                0L,
                default,
                isIdle: false,
                isPaused: false,
                wd.CurHungry, wd.MaxHungry,
                wd.CurTired, wd.MaxTired);
            return ConditionService.GetState(snapshot) == WorkerConditionState.Critical;
        }

        /// <summary>
        /// 应用人生事件效果（带封顶）。修改 wd 的软维度 + mind 的漂移桶。
        /// 金币仅正/中事件给（负事件 GoldDelta=0），不扣钱。
        /// </summary>
        public static void Apply(AWorker.WorkerData wd, WorkerMindData mind, WorkerLifeEventDef def, int day)
        {
            if (wd == null || mind == null)
            {
                return;
            }

            // ① 封顶：软维度增量单次 ≤±LifeEventSurvivalDamageCap，并 clamp 到 [0, Max]
            wd.CurSpirit = Clamp(wd.CurSpirit + ClampDelta(def.SpiritDelta), 0f, wd.MaxSpirit);
            wd.CurMorale = Clamp(wd.CurMorale + ClampDelta(def.MoraleDelta), 0f, wd.MaxMorale);

            // 心情（软维度）：构造函数自带 clamp [0,100]
            wd.Personality = new WorkerPersonality(
                wd.Personality.Mood + def.MoodDelta,
                wd.Personality.Ambition,
                wd.Personality.Diligence,
                wd.Personality.Sociality);

            // 金币：正/中事件发横财；负事件不扣钱（③ 可恢复）
            if (def.GoldDelta > 0f)
            {
                wd.Wallet += new CurrencyAmount((int)def.GoldDelta);
            }

            // 人格漂移桶累积（Phase 4 迁移；单次封顶 ±LifeEventPersonalityDriftCap，桶饱和 ±DriftBucketAbsCap）
            mind.Drift.MoodDrift = Clamp(mind.Drift.MoodDrift + ClampDrift(def.MoodDrift), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
            mind.Drift.AmbitionDrift = Clamp(mind.Drift.AmbitionDrift + ClampDrift(def.AmbitionDrift), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
            mind.Drift.DiligenceDrift = Clamp(mind.Drift.DiligenceDrift + ClampDrift(def.DiligenceDrift), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
            mind.Drift.SocialityDrift = Clamp(mind.Drift.SocialityDrift + ClampDrift(def.SocialityDrift), -WorkerMindConstant.DriftBucketAbsCap, WorkerMindConstant.DriftBucketAbsCap);
        }

        /// <summary>生存/士气增量封顶。</summary>
        private static float ClampDelta(float v)
        {
            return Math.Max(-WorkerMindConstant.LifeEventSurvivalDamageCap,
                Math.Min(WorkerMindConstant.LifeEventSurvivalDamageCap, v));
        }

        /// <summary>人格漂移单次封顶。</summary>
        private static float ClampDrift(float v)
        {
            return Math.Max(-WorkerMindConstant.LifeEventPersonalityDriftCap,
                Math.Min(WorkerMindConstant.LifeEventPersonalityDriftCap, v));
        }

        private static float Clamp(float v, float min, float max)
        {
            return Math.Max(min, Math.Min(max, v));
        }
    }
}
