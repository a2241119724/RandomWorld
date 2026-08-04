namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 货币交易事件 — 当货币在 Worker 之间（或 Worker 与系统之间）转移时发布。
    /// 通过 EventBus 分发，供 UI 层和日志系统订阅。
    /// </summary>
    public sealed class CurrencyTransactionEvent : IGameEvent
    {
        /// <summary>付款方 Worker instance ID（0 表示系统）</summary>
        public int FromWorkerId;

        /// <summary>收款方 Worker instance ID（0 表示系统）</summary>
        public int ToWorkerId;

        /// <summary>交易金额</summary>
        public CurrencyAmount Amount;

        /// <summary>交易原因（如 "BountyReward"、"BountyPost"、"InitialFunds"、"Refund"）</summary>
        public string Reason;
    }
}
