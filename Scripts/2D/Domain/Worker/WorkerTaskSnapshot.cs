using System;

namespace LAB2D
{
    /// <summary>
    /// 纯分配规则使用的只读任务候选。
    /// </summary>
    /// <typeparam name="TTask">Unity兼容层维护的任务对象类型。</typeparam>
    public sealed class WorkerTaskSnapshot<TTask>
    {
        public WorkerTaskSnapshot(
            TTask task,
            long taskId,
            int priority,
            GameVector2 targetPosition,
            bool isRunning,
            Func<bool> canAssign)
        {
            this.Task = task;
            this.TaskId = taskId;
            this.Priority = priority;
            this.TargetPosition = targetPosition;
            this.IsRunning = isRunning;
            this.canAssign = canAssign;
        }

        private readonly Func<bool> canAssign;

        public TTask Task { get; }

        public long TaskId { get; }

        public int Priority { get; }

        public GameVector2 TargetPosition { get; }

        public bool IsRunning { get; }

        public bool CanAssign()
        {
            return !this.IsRunning && (this.canAssign == null || this.canAssign());
        }
    }
}
