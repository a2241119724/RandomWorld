namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 先预取资源
    /// </summary>
    public class WorkerHungryTask : WorkerTask
    {
        public WorkerHungryTask()
            : base(WorkerTaskTypeEnum.Eat)
        {
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
            });
        }

        /// <inheritdoc/>
        public override void Start(Worker worker)
        {
            base.Start(worker);
            InventoryManager.Instance.IsEnoughFoodAndPreTake(worker, Worker.MaxHungry - worker.CurHungry, true);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(Worker worker)
        {
            // 将饥饿任务放回任务管理中
            base.Finish(worker);

            // 再取食物，并且有可能会由于该位置的食物被取完，从而删除该饥饿任务
            ResourceInfo resourceInfo = InventoryManager.Instance.SubItemByPreTake(worker, this.TargetMap);
            worker.CurHungry += resourceInfo.Count * 10;
        }

        /// <inheritdoc/>
        public override bool IsCanWork(Worker worker)
        {
            if (!base.IsCanWork(worker))
            {
                return false;
            }

            // 饥饿值小于一定值可以接收饥饿任务
            return worker.CurHungry < Worker.ThresholdHungry
                && InventoryManager.Instance.IsEnoughFoodAndPreTake(worker, Worker.MaxHungry - worker.CurHungry);
        }

#pragma warning disable SA1600 // Elements should be documented
        /// <summary>
        /// 建造者
        /// </summary>
        public class HungryTaskBuilder
        {
            private readonly WorkerHungryTask task;

            public HungryTaskBuilder()
            {
                this.task = new WorkerHungryTask();
            }

            public HungryTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = targetMap;
                return this;
            }

            public WorkerHungryTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
