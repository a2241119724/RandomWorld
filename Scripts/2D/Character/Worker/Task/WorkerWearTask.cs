namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 穿戴任务
    /// </summary>
    [Serializable]
    public class WorkerWearTask : WorkerTask
    {
        private Worker worker;
        private int id; // 穿戴的装备id

        public WorkerWearTask()
            : base(WorkerTaskTypeEnum.Wear)
        {
            this.stageInit.Add((Worker worker) =>
            {
                WorkerTask.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 进入工作状态
                worker.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(Worker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(Worker worker)
        {
            base.Finish(worker);
            Worker.WorkerData workerData = worker.CharacterDataLAB as Worker.WorkerData;

            // Worker拿起装备或者武器
            if (ItemDataManager.Instance.IdToType(this.id) == AItem.ItemTypeEnum.Weapon)
            {
                workerData.Weapon = (AWeapon)ItemInstanceFactory.Instance.GetBackpackItemByName(
                    ItemDataManager.Instance.GetById(this.id).EnName);
            }
            else if (ItemDataManager.Instance.IdToType(this.id) == AItem.ItemTypeEnum.Equipment)
            {
                workerData.AddEquipment(
                    (AEquipment)ItemInstanceFactory.Instance.GetBackpackItemByName(
                    ItemDataManager.Instance.GetById(this.id).EnName), Vector3IntLAB.ToVector3Int(this.TargetMap));
            }

            InventoryManager.Instance.SubItemByPreTake(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));

            // 删除图标
            ItemMap.Instance.DeleteTile(Vector3IntLAB.ToVector3Int(this.TargetMap));
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(Worker worker)
        {
            return this.worker == worker;
        }

#pragma warning disable SA1600 // Elements should be documented
        public class WearTaskBuilder
        {
            private readonly WorkerWearTask task;

            public WearTaskBuilder()
            {
                this.task = new WorkerWearTask();
            }

            public WearTaskBuilder SetWorker(Worker worker)
            {
                this.task.worker = worker;
                return this;
            }

            public WearTaskBuilder SetTarget(Vector3Int posMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(posMap);
                return this;
            }

            public WearTaskBuilder SetEquipmentId(int id)
            {
                this.task.id = id;
                return this;
            }

            public WorkerWearTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
