using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class ExerciseTask : WorkerTask
    {
        public class ExerciseTaskBuilder
        {
            private ExerciseTask task;

            public ExerciseTaskBuilder()
            {
                task = new ExerciseTask();
            }

            public ExerciseTaskBuilder setTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public ExerciseTask build()
            {
                return task;
            }
        }
    }
}
