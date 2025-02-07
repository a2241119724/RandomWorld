using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerSleepTask : WorkerTask
    {
        private Worker worker;
        
        public WorkerSleepTask() : base(TaskType.Sleep)
        {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 10.0f;
                // 获取物资
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            stageInit[0].Invoke(worker);
        }

        public override void _execute()
        {
            base._execute();
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            // 如果疲劳值低于阈值，并且有床，则可以睡觉
            return worker.CurTired < Worker.ThresholdTired && worker.BedItem != null && this.worker == worker;
        }

        public override void finish(Worker worker)
        {
            base.finish(worker);
        }

        public class SleepTaskBuilder
        {
            private WorkerSleepTask task;

            public SleepTaskBuilder()
            {
                task = new WorkerSleepTask();
            }

            public SleepTaskBuilder setTarget(Vector3Int posMap)
            {
                task.TargetMap = posMap;
                return this;
            }

            public SleepTaskBuilder setWorker(Worker worker)
            {
                task.worker = worker;
                return this;
            }

            public WorkerSleepTask build()
            {
                return task;
            }
        }
    }
}
