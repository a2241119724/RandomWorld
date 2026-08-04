namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// 悬赏状态枚举。
    /// </summary>
    public enum BountyState
    {
        /// <summary>已发布，等待领取</summary>
        Posted,

        /// <summary>已被 Worker 领取，执行中</summary>
        Accepted,

        /// <summary>已完成（悬赏金已结算）</summary>
        Completed,

        /// <summary>已过期（无人领取，悬赏金已退款）</summary>
        Expired,

        /// <summary>发布者取消（悬赏金已退款）</summary>
        Cancelled,
    }

    /// <summary>
    /// 悬赏元数据 — 纯 C# 结构体，描述悬赏的金额、发布者、过期时间和状态。
    /// 不依赖 UnityEngine，供 Domain 层和 Character 层共享使用。
    /// </summary>
    public struct BountyData
    {
        /// <summary>悬赏金额</summary>
        public readonly CurrencyAmount Reward;

        /// <summary>发布者 Worker 的 GameObject instance ID</summary>
        public readonly int IssuerWorkerId;

        /// <summary>过期时间（游戏时间秒，由 IGameTime 提供）</summary>
        public readonly float ExpirationGameTime;

        /// <summary>当前状态</summary>
        public BountyState State;

        public BountyData(CurrencyAmount reward, int issuerWorkerId, float expirationGameTime)
        {
            this.Reward = reward;
            this.IssuerWorkerId = issuerWorkerId;
            this.ExpirationGameTime = expirationGameTime;
            this.State = BountyState.Posted;
        }

        /// <summary>
        /// 是否已过期（仅 Posted 状态的悬赏需要检查）。
        /// </summary>
        /// <param name="currentGameTime">当前游戏时间（秒）</param>
        public bool IsExpired(float currentGameTime)
        {
            return this.State == BountyState.Posted && currentGameTime >= this.ExpirationGameTime;
        }

        /// <summary>
        /// 创建状态变更后的副本。
        /// </summary>
        /// <param name="newState">新状态</param>
        public BountyData WithState(BountyState newState)
        {
            return new BountyData(this.Reward, this.IssuerWorkerId, this.ExpirationGameTime)
            {
                State = newState,
            };
        }
    }
}
