using System.Collections.Generic;

namespace LAB2D
{
    /// <summary>
    /// Selects the nearest assignable task from the highest available priority group.
    /// This service is pure C# and keeps Unity object access in the caller-provided snapshots.
    /// </summary>
    /// <typeparam name="TTask">Task object type kept by the Unity compatibility layer.</typeparam>
    public sealed class WorkerTaskAssignmentService<TTask>
    {
        public WorkerTaskAssignmentResult<TTask> SelectTask(
            WorkerAgentSnapshot worker,
            IReadOnlyList<WorkerTaskSnapshot<TTask>> tasks)
        {
            if (worker == null || !worker.CanReceiveTask || tasks == null || tasks.Count == 0)
            {
                return WorkerTaskAssignmentResult<TTask>.None();
            }

            WorkerTaskSnapshot<TTask> selected = null;
            float minDistance = 0.0f;

            for (int i = 0; i < tasks.Count; i++)
            {
                WorkerTaskSnapshot<TTask> candidate = tasks[i];
                if (candidate == null || !candidate.CanAssign())
                {
                    continue;
                }

                float distance = worker.Position.SqrDistanceTo(candidate.TargetPosition);
                if (selected == null || distance < minDistance)
                {
                    selected = candidate;
                    minDistance = distance;
                }
            }

            return selected == null
                ? WorkerTaskAssignmentResult<TTask>.None()
                : WorkerTaskAssignmentResult<TTask>.Assigned(selected.Task, selected.Priority, minDistance);
        }
    }

    public readonly struct WorkerTaskAssignmentResult<TTask>
    {
        private WorkerTaskAssignmentResult(bool hasTask, TTask task, int priority, float sqrDistance)
        {
            this.HasTask = hasTask;
            this.Task = task;
            this.Priority = priority;
            this.SqrDistance = sqrDistance;
        }

        public bool HasTask { get; }

        public TTask Task { get; }

        public int Priority { get; }

        public float SqrDistance { get; }

        public static WorkerTaskAssignmentResult<TTask> None()
        {
            return new WorkerTaskAssignmentResult<TTask>(false, default, -1, 0.0f);
        }

        public static WorkerTaskAssignmentResult<TTask> Assigned(TTask task, int priority, float sqrDistance)
        {
            return new WorkerTaskAssignmentResult<TTask>(true, task, priority, sqrDistance);
        }
    }
}
