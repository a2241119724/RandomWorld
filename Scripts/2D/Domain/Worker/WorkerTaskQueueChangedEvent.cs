namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// Worker 任务队列变更事件。
    /// WorkerTaskManager 在任务添加、完成、放弃、删除、取消后发布此事件。
    /// DebugUI 等展示层订阅此事件以更新调试 HUD。
    /// </summary>
    public sealed class WorkerTaskQueueChangedEvent : IGameEvent
    {
        /// <summary>
        /// 格式化的任务队列调试信息字符串。
        /// 语义由发布者保证，消费者只需要展示。
        /// </summary>
        public string TaskInfo;
    }
}
