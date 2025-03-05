using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// ��Ԥȡ��Դ
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
            // ����������Ż����������
            base.finish(worker);
            // ��ȡʳ������п��ܻ����ڸ�λ�õ�ʳ�ﱻȡ�꣬�Ӷ�ɾ���ü�������
            ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
            worker.CurHungry += resourceInfo.count * 10;
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            // ����ֵС��һ��ֵ���Խ��ռ�������
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
