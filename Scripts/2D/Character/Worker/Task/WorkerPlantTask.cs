using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerPlantTask : WorkerTask
    {
        private ResourceInfo resourceInfo;

        public WorkerPlantTask() : base(TaskType.Plant)
        {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.isContainSeedAndPreTake(worker, true);
                if (TargetMap == default) 
                {
                    giveUpTask(worker);
                    return;
                }
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = FarmlandManager.Instance.isEnoughAndPrePlant(worker, resourceInfo, true);
                if (TargetMap == default)
                {
                    giveUpTask(worker);
                    return;
                }
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            changeStage(worker,0);
        }

        protected override bool isFinish(Worker worker)
        {
            switch (stage)
            {
                case 0:
                    resourceInfo = InventoryManager.Instance.subAllItemByPos(TargetMap);
                    worker.addResource(resourceInfo);
                    changeStage(worker,1);
                    return false;
                case 1:
                    // 可以继续种植
                    if(isCanWork(worker) && resourceInfo.count > 0)
                    {
                        FarmlandManager.Instance.plantByPrePlant(worker,TargetMap);
                        resourceInfo.count--;
                        changeStage(worker, 1);
                        return false;
                    }
                    changeStage(worker, 0);
                    return false;
                default:
                    return true;
            }
        }

        public override void finish(Worker worker)
        {
            base.finish(worker);
            // TODO 可以将种子放回
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return FarmlandManager.Instance.isEnoughAndPrePlant(worker, null) != default &&
                InventoryManager.Instance.isContainSeedAndPreTake(worker) != default;
        }

        public class PlantTaskBuilder
        {
            private WorkerPlantTask task;

            public PlantTaskBuilder()
            {
                task = new WorkerPlantTask();
            }

            public WorkerPlantTask build()
            {
                return task;
            }
        }
    }
}
