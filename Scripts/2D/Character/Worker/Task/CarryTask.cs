using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// Carry在第二个阶段预留资源
    /// </summary>
    public class CarryTask : WorkerTask
    {
        /// <summary>
        /// Worker携带的资源
        /// </summary>
        private ResourceInfo resourceInfo;
        /// <summary>
        /// 任务2阶段：取货，放货
        /// </summary>
        private int stage;

        public CarryTask()
        {
            Name = TaskType.Carry.ToString();
            TaskType = TaskType.Carry;
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 100;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) => {
                maxProgress = 100;
                stage = 1;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.getOneByPrePlace(worker);
                if (TargetMap == default)
                {
                    Debug.Log("仓库没有位置了");
                }
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            InventoryManager.Instance.isEnoughAndPrePlace(worker, resourceInfo, true);
            stageInit[0].Invoke(worker);
        }

        protected override bool isFinish(Worker worker)
        {
            switch (stage)
            {
                case 0:
                    // 删除拿起来的东西
                    DropResourceManager.Instance.subDropByAll(TargetMap, resourceInfo);
                    ResourceMap.Instance.pickUp(TargetMap);
                    worker.addResource(Tool.DeepCopyByBinary(resourceInfo));
                    stageInit[1].Invoke(worker);
                    return false;
                default:
                    return true;
            }
        }

        protected override void finish(Worker worker)
        {
            ItemType itemType = ItemDataManager.Instance.getTypeById(resourceInfo.id);
            // 放下拿起来的东西
            ResourceMap.Instance.putDown(TargetMap, (TileBase)ResourcesManager.Instance
                .getAsset(ItemDataManager.Instance.getById(resourceInfo.id).imageName));
            worker.subResource(Tool.DeepCopyByBinary(resourceInfo));
            InventoryManager.Instance.addItemByPrePlace(worker,TargetMap);
            // 如果是食物,添加饥饿任务
            if(itemType == ItemType.Food)
            {
                WorkerTaskManager.Instance.addTask(new HungryTask.HungryTaskBuilder().setTarget(TargetMap).build());
            }
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return InventoryManager.Instance.isEnoughAndPrePlace(worker, resourceInfo);
        }

        public class CarryTaskBuilder
        {
            private CarryTask task;

            public CarryTaskBuilder()
            {
                task = new CarryTask();
            }

            public CarryTaskBuilder setStartTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public CarryTaskBuilder setResourceInfo(ResourceInfo resourceInfo)
            {
                task.resourceInfo = resourceInfo;
                return this;
            }

            public CarryTask build()
            {
                return task;
            }
        }
    }
}

