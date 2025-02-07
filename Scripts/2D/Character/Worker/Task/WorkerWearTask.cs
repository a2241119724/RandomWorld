using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class WorkerWearTask : WorkerTask
    {
        private Worker worker;
        /// <summary>
        /// 穿戴的装备id
        /// </summary>
        private int id;

        public WorkerWearTask() : base(TaskType.Wear) {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
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

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return this.worker == worker;
        }

        public override void finish(Worker worker)
        {
            base.finish(worker);
            // Worker拿起装备或者武器
            if(ItemDataManager.Instance.getTypeById(id) == ItemType.Weapon)
            {
                worker.WearData.weapon = (Weapon)ItemFactory.Instance.getBackpackItemByName(
                    ItemDataManager.Instance.getById(id).imageName);
            }else if(ItemDataManager.Instance.getTypeById(id) == ItemType.Equipment)
            {
                worker.WearData.equipments.Add(ItemDataManager.Instance.getEquipmentTypeById(id),
                    (Equipment)ItemFactory.Instance.getBackpackItemByName(
                    ItemDataManager.Instance.getById(id).imageName));
            }
            InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
            // 删除图标
            ItemMap.Instance.pickUp(TargetMap);
        }

        public class WearTaskBuilder
        {
            private WorkerWearTask task;

            public WearTaskBuilder()
            {
                task = new WorkerWearTask();
            }

            public WearTaskBuilder setWorker(Worker worker) {
                task.worker = worker;
                return this;
            }

            public WearTaskBuilder setTarget(Vector3Int posMap) {
                task.TargetMap = posMap;
                return this;
            }

            public WearTaskBuilder setEquipmentId(int id)
            {
                task.id = id;
                return this;
            }

            public WorkerWearTask build()
            {
                return task;
            }
        }
    }
}
