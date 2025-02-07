using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerExerciseTask : WorkerTask
    {
        public WorkerExerciseTask() : base(TaskType.Exercise)
        {
        }

        public class ExerciseTaskBuilder
        {
            private WorkerExerciseTask task;

            public ExerciseTaskBuilder()
            {
                task = new WorkerExerciseTask();
            }

            public ExerciseTaskBuilder setTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public WorkerExerciseTask build()
            {
                return task;
            }
        }
    }
}
