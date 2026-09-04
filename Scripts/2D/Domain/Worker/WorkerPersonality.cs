namespace LAB2D.Domain.Worker
{
    using System;

    /// <summary>
    /// Worker 人格值对象 — 纯 C# 可变结构体，描述 Worker 的性格倾向。
    /// 所有值范围 [0, 100]。
    /// </summary>
    [Serializable]
    public struct WorkerPersonality : IEquatable<WorkerPersonality>
    {
        private static readonly System.Random Rng = new System.Random();
        /// <summary>心情 — 影响工作效率和社交意愿。0=极差, 100=极好。</summary>
        public float Mood;

        /// <summary>事业心 — 影响自主赚钱驱动力。0=躺平, 100=工作狂。</summary>
        public float Ambition;

        /// <summary>勤奋 — 影响空闲容忍度和工作速度。0=懒惰, 100=勤勉。</summary>
        public float Diligence;

        /// <summary>社交 — 影响发布/接受悬赏意愿。0=孤僻, 100=外向。</summary>
        public float Sociality;

        public WorkerPersonality(float mood, float ambition, float diligence, float sociality)
        {
            this.Mood = Clamp(mood);
            this.Ambition = Clamp(ambition);
            this.Diligence = Clamp(diligence);
            this.Sociality = Clamp(sociality);
        }

        /// <summary>生成随机人格（40-80 范围均匀分布，保证多样性）。</summary>
        public static WorkerPersonality Randomize()
        {
            return new WorkerPersonality(
                mood: NextFloat(40f, 80f),
                ambition: NextFloat(40f, 80f),
                diligence: NextFloat(40f, 80f),
                sociality: NextFloat(40f, 80f));
        }

        /// <summary>纯 C# 均匀随机（不走 UnityEngine icall——Domain 纯函数测试在裸 Mono 下可跑）。</summary>
        internal static float NextFloat(float min, float max)
        {
            return min + (float)(Rng.NextDouble() * (max - min));
        }

        /// <summary>所有值 50 的中性人格。</summary>
        public static WorkerPersonality Neutral => new WorkerPersonality(50f, 50f, 50f, 50f);

        // ---- 人格动态调整方法（返回新值，由调用方写入 WorkerData）----

        /// <summary>完成任务后调整：心情上升，勤奋上升，事业心微升。</summary>
        public WorkerPersonality AfterTaskComplete()
        {
            return new WorkerPersonality(
                Clamp(this.Mood + 5f),
                Clamp(this.Ambition + 0.5f),
                Clamp(this.Diligence + 1f),
                this.Sociality);
        }

        /// <summary>赚钱后调整：心情上升，事业心上升。</summary>
        public WorkerPersonality AfterEarnGold(int goldAmount)
        {
            float boost = Math.Min(goldAmount * 0.5f, 10f);
            return new WorkerPersonality(
                Clamp(this.Mood + boost),
                Clamp(this.Ambition + boost * 0.5f),
                this.Diligence,
                this.Sociality);
        }

        /// <summary>花钱后调整：事业心微升（想赚回来），社交微升。</summary>
        public WorkerPersonality AfterSpendGold(int goldAmount)
        {
            float change = Math.Min(goldAmount * 0.3f, 5f);
            return new WorkerPersonality(
                Clamp(this.Mood - change * 0.5f),
                Clamp(this.Ambition + change),
                this.Diligence,
                Clamp(this.Sociality + change * 0.5f));
        }

        /// <summary>饥饿/疲劳惩罚：心情下降。</summary>
        public WorkerPersonality AfterSuffer(float hungryRatio, float tiredRatio)
        {
            // 使用平滑的非线性惩罚：轻度饥饿/疲劳时惩罚很小，严重时加速
            float hungryPenalty = (1f - hungryRatio) * (1f - hungryRatio) * 1.5f;
            float tiredPenalty = (1f - tiredRatio) * (1f - tiredRatio) * 1.5f;
            float penalty = hungryPenalty + tiredPenalty;
            return new WorkerPersonality(
                Clamp(this.Mood - penalty),
                this.Ambition,
                this.Diligence,
                this.Sociality);
        }

        /// <summary>空闲过久：勤奋下降，心情微降。</summary>
        public WorkerPersonality AfterIdle()
        {
            return new WorkerPersonality(
                Clamp(this.Mood - 0.3f),
                this.Ambition,
                Clamp(this.Diligence - 2f),
                this.Sociality);
        }

        /// <summary>接受悬赏：社交上升。</summary>
        public WorkerPersonality AfterAcceptBounty()
        {
            return new WorkerPersonality(
                Clamp(this.Mood + 1f),
                this.Ambition,
                Clamp(this.Diligence + 1f),
                Clamp(this.Sociality + 1f));
        }

        /// <summary>好感度变化后调整心情：Mood += moodDelta（delta 已由 FavorabilityRuleService.GetMoodDelta 计算并 clamp）。</summary>
        public WorkerPersonality AfterFavorabilityChange(float moodDelta)
        {
            return new WorkerPersonality(
                Clamp(this.Mood + moodDelta),
                this.Ambition,
                this.Diligence,
                this.Sociality);
        }

        /// <summary>漫游恢复：心情上升，勤奋微升，不影响其他。</summary>
        public WorkerPersonality AfterWander()
        {
            return new WorkerPersonality(
                Clamp(this.Mood + 3f),
                this.Ambition,
                Clamp(this.Diligence + 1f),
                this.Sociality);
        }

        public override string ToString()
        {
            return $"心情:{this.Mood:F0} 事业心:{this.Ambition:F0} 勤奋:{this.Diligence:F0} 社交:{this.Sociality:F0}";
        }

        public override bool Equals(object obj)
        {
            return obj is WorkerPersonality other && this == other;
        }

        public bool Equals(WorkerPersonality other)
        {
            return Math.Abs(this.Mood - other.Mood) < 0.01f
                && Math.Abs(this.Ambition - other.Ambition) < 0.01f
                && Math.Abs(this.Diligence - other.Diligence) < 0.01f
                && Math.Abs(this.Sociality - other.Sociality) < 0.01f;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Mood, this.Ambition, this.Diligence, this.Sociality);
        }

        public static bool operator ==(WorkerPersonality a, WorkerPersonality b) => a.Equals(b);
        public static bool operator !=(WorkerPersonality a, WorkerPersonality b) => !(a == b);

        private static float Clamp(float value) => Math.Max(0f, Math.Min(100f, value));
    }
}
