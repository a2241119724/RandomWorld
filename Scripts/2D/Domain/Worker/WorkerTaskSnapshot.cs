using System;

namespace LAB2D
{
    /// <summary>
    /// Read-only task candidate used by pure assignment rules.
    /// </summary>
    /// <typeparam name="TTask">Task object type kept by the Unity compatibility layer.</typeparam>
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
