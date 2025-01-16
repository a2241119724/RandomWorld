using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// 先预取资源
    /// </summary>
    public class HungryTask : WorkerTask
    {
        public HungryTask()
        {
            Name = TaskType.Hungry.ToString();
            TaskType = TaskType.Hungry;
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 500;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            InventoryManager.Instance.isEnoughFoodAndPreTake(worker, Worker.MaxHungry - worker.CurHungry,true);
            stageInit[0].Invoke(worker);
        }

        protected override void finish(Worker worker)
        {
            ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
            worker.CurHungry += resourceInfo.count * 10;
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return InventoryManager.Instance.isEnoughFoodAndPreTake(worker, Worker.MaxHungry - worker.CurHungry);
        }

        public class HungryTaskBuilder
        {
            private HungryTask task;

            public HungryTaskBuilder()
            {
                task = new HungryTask();
            }

            public HungryTaskBuilder setTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public HungryTask build()
            {
                return task;
            }
        }
    }
}
