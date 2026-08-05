namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// 任务分配规则使用的只读Worker状态。
    /// </summary>
    public sealed class WorkerAgentSnapshot
    {
        public WorkerAgentSnapshot(
            long workerId,
            GameVector2 position,
            bool isIdle,
            bool isPaused,
            float curHungry,
            float maxHungry,
            float curTired,
            float maxTired,
            CurrencyAmount wallet = default,
            WorkerPersonality personality = default)
        {
            this.WorkerId = workerId;
            this.Position = position;
            this.IsIdle = isIdle;
            this.IsPaused = isPaused;
            this.CurHungry = curHungry;
            this.MaxHungry = maxHungry;
            this.CurTired = curTired;
            this.MaxTired = maxTired;
            this.Wallet = wallet;
            this.Personality = personality;
        }

        public long WorkerId { get; }

        public GameVector2 Position { get; }

        public bool IsIdle { get; }

        public bool IsPaused { get; }

        public float CurHungry { get; }

        public float MaxHungry { get; }

        public float CurTired { get; }

        public float MaxTired { get; }

        public CurrencyAmount Wallet { get; }

        /// <summary>Worker 人格数据。</summary>
        public WorkerPersonality Personality { get; }

        public bool CanReceiveTask => this.IsIdle && !this.IsPaused;
    }
}
