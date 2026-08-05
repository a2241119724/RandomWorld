namespace LAB2D.Domain.Worker
{
    using System;

    /// <summary>
    /// Worker 人格值对象 — 纯 C# 可变结构体，描述 Worker 的性格倾向。
    /// 遵循 CurrencyAmount 的模式，不依赖 UnityEngine。
    /// 所有值范围 [0, 100]。
    /// </summary>
    [Serializable]
    public struct WorkerPersonality : IEquatable<WorkerPersonality>
    {
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
                mood: UnityEngine.Random.Range(40f, 80f),
                ambition: UnityEngine.Random.Range(40f, 80f),
                diligence: UnityEngine.Random.Range(40f, 80f),
                sociality: UnityEngine.Random.Range(40f, 80f));
        }

        /// <summary>所有值 50 的中性人格。</summary>
        public static WorkerPersonality Neutral => new WorkerPersonality(50f, 50f, 50f, 50f);

        // ---- 人格动态调整方法（返回新值，由调用方写入 WorkerData）----

        /// <summary>完成任务后调整：心情上升，勤奋上升，事业心微升。</summary>
        public WorkerPersonality AfterTaskComplete()
        {
            return new WorkerPersonality(
                Clamp(this.Mood + 2f),
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
            float penalty = (1f - hungryRatio) * 5f + (1f - tiredRatio) * 5f;
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
                Clamp(this.Mood - 1f),
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
