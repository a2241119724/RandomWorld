using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// ����2�׶Σ�ȡ�����Ż�
    /// Carry�ڵڶ����׶�Ԥ����Դ
    /// </summary>
    public class WorkerCarryTask : WorkerTask
    {
        /// <summary>
        /// WorkerЯ������Դ
        /// </summary>
        private ResourceInfo resourceInfo;

        public WorkerCarryTask() : base(TaskType.Carry)
        {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                // ���빤��״̬
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) => {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.getPosByPrePlace(worker);
                if (TargetMap == default)
                {
                    LogManager.Instance.log("�ֿ�û��λ����", LogManager.LogLevel.Error);
                }
                // ���빤��״̬
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            InventoryManager.Instance.isEnoughAndPrePlace(worker, resourceInfo, true);
            changeStage(worker,0);
        }

        protected override bool isFinish(Worker worker)
        {
            switch (stage)
            {
                case 0:
                    ItemMap.Instance.pickUpFromDrop(TargetMap, resourceInfo);
                    worker.addResource(resourceInfo);
                    changeStage(worker,1);
                    return false;
                default:
                    return true;
            }
        }

        public override void finish(Worker worker)
        {
            base.finish(worker);
            ItemType itemType = ItemDataManager.Instance.getTypeById(resourceInfo.id);
            // �����������Ķ���
            ItemMap.Instance.showTile(TargetMap, (TileBase)ResourcesManager.Instance
                .getAsset(ItemDataManager.Instance.getById(resourceInfo.id).imageName));
            worker.subResource(resourceInfo);
            InventoryManager.Instance.addItemByPrePlace(worker,TargetMap);
            // �����ʳ��,��Ӽ�������
            if(itemType == ItemType.Food)
            {
                WorkerTaskManager.Instance.addTask(new WorkerHungryTask.HungryTaskBuilder().setTarget(TargetMap).build(), 0);
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
            private WorkerCarryTask task;

            public CarryTaskBuilder()
            {
                task = new WorkerCarryTask();
            }

            public CarryTaskBuilder setStartTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public CarryTaskBuilder setResourceInfo(ResourceInfo resourceInfo)
            {
                task.resourceInfo = Tool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            public WorkerCarryTask build()
            {
                return task;
            }
        }
    }
}

