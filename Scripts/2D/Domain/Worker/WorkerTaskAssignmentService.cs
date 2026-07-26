using System.Collections.Generic;

namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// 从最高可用优先级组中选择最近的可分配任务。
    /// 此服务为纯C#实现，Unity对象访问由调用方提供的快照处理。
    /// </summary>
    /// <typeparam name="TTask">Unity兼容层维护的任务对象类型。</typeparam>
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
