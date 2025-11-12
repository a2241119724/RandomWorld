namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 锻炼任务
    /// </summary>
    [Serializable]
    public class WorkerExerciseTask : WorkerTask
    {
        public WorkerExerciseTask()
            : base(WorkerTaskTypeEnum.Exercise)
        {
        }

#pragma warning disable SA1600 // Elements should be documented
        public class ExerciseTaskBuilder
        {
            private readonly WorkerExerciseTask task;

            public ExerciseTaskBuilder()
            {
                this.task = new WorkerExerciseTask();
            }

            public ExerciseTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            public WorkerExerciseTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
