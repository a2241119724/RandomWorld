namespace LAB2D
{
    /// <summary>
    /// 任务分配规则使用的只读Worker状态。
    /// </summary>
    public sealed class WorkerAgentSnapshot
    {
        public WorkerAgentSnapshot(long workerId, GameVector2 position, bool isIdle, bool isPaused)
        {
            this.WorkerId = workerId;
            this.Position = position;
            this.IsIdle = isIdle;
            this.IsPaused = isPaused;
        }

        public long WorkerId { get; }

        public GameVector2 Position { get; }

        public bool IsIdle { get; }

        public bool IsPaused { get; }

        public bool CanReceiveTask => this.IsIdle && !this.IsPaused;
    }
}
