using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// 先预取资源
    /// </summary>
    public class WorkerHungryTask : WorkerTask
    {
        public WorkerHungryTask() : base(TaskType.Hungry)
        {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            InventoryManager.Instance.isEnoughFoodAndPreTake(worker, Worker.MaxHungry - worker.CurHungry,true);
            changeStage(worker,0);
        }

        public override void finish(Worker worker)
        {
            // 将饥饿任务放回任务管理中
            base.finish(worker);
            // 再取食物，并且有可能会由于该位置的食物被取完，从而删除该饥饿任务
            ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
            worker.CurHungry += resourceInfo.count * 10;
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            // 饥饿值小于一定值可以接收饥饿任务
            return worker.CurHungry < Worker.ThresholdHungry
                && InventoryManager.Instance.isEnoughFoodAndPreTake(worker, Worker.MaxHungry - worker.CurHungry);
        }

        public class HungryTaskBuilder
        {
            private WorkerHungryTask task;

            public HungryTaskBuilder()
            {
                task = new WorkerHungryTask();
            }

            public HungryTaskBuilder setTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public WorkerHungryTask build()
            {
                return task;
            }
        }
    }
}
