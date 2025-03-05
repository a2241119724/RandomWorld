using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace LAB2D
{
    /// <summary>
    /// ����2�׶Σ��ò��ϣ�����
    /// Build�ڵ�һ���׶�Ԥ����Դ
    /// </summary>
    public class WorkerBuildTask : WorkerTask
    {
        private Dictionary<int, ResourceInfo> needs;
        private Dictionary<int, ResourceInfo> temp;
        /// <summary>
        /// �����λ��
        /// </summary>
        private Vector3Int buildPos;

        /// <summary>
        /// û��
        /// </summary>
        public BuildItem BuildItem { get; private set; }

        public WorkerBuildTask() : base(TaskType.Build) {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                // ��ȡ����
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.getPosByPreTake(worker);
                if (TargetMap == default)
                {
                    giveUpTask(worker);
                }
                // ���빤��״̬
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 2.0f;
                // ����
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[0]);
                AvailableNeighborPos.Add(neighbors[1]);
                AvailableNeighborPos.Add(neighbors[2]);
                AvailableNeighborPos.Add(neighbors[3]);
                TargetMap = buildPos;
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            // ����Я����Դ�㹻
            if (worker.isEnough(needs))
            {
                //LogManager.Instance.log("Я����Դ����", LogManager.LogLevel.Info);
                changeStage(worker,1);
                return;
            }
            // ���ʣ�಻��������
            Dictionary<int, ResourceInfo> remaining = worker.getRemaining(needs);
            InventoryManager.Instance.isEnoughAndPreTake(worker, remaining, true);
            // ������ȡ��Դ
            changeStage(worker,0);
        }

        protected override bool isFinish(Worker worker)
        {
            // ֻworkerЯ������Դ����ʱ,ȡ��������
            switch (stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
                    worker.addResource(resourceInfo);
                    // �������������
                    foreach(KeyValuePair<int, ResourceInfo> pair in temp)
                    {
                        if (pair.Key == resourceInfo.id)
                        {
                            pair.Value.count -= resourceInfo.count;
                            if(pair.Value.count <= 0)
                            {
                                temp.Remove(resourceInfo.id);
                            }
                            break;
                        }
                    }
                    // ��ȡ������еĲ���
                    if (temp.Count == 0)
                    {
                        changeStage(worker,1);
                        return false;
                    }
                    changeStage(worker,0);
                    return false;
                default:
                    return true;
            }
        }

        public override void finish(Worker worker)
        {
            base.finish(worker);
            // ����workerЯ������Դ
            worker.subResource(needs);
            // ��������ɵ�Tile��Building��ΪBuild��
            BuildMap.Instance.setComplete(buildPos);
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            // ���workerЯ������Դ�Ѿ����㽨��
            if (worker.isEnough(needs)) return true;
            // ���յ����������Դȡ���Ƿ��㹻
            // ���ʣ�಻��������
            Dictionary<int, ResourceInfo> remaining = worker.getRemaining(needs);
            return InventoryManager.Instance.isEnoughAndPreTake(worker, remaining);
        }

        public override void giveUpTask(Worker worker)
        {
            base.giveUpTask(worker);
            // �ָ���Դ
            temp = Tool.DeepCopyByBinary(needs);
        }

        public class BuildTaskBuilder {
            private WorkerBuildTask task;

            public BuildTaskBuilder(){
                task = new WorkerBuildTask();
            }

            public BuildTaskBuilder setBuild(BuildItem buildItem)
            {
                task.BuildItem = buildItem;
                return this;
            }

            public BuildTaskBuilder setBuildPos(Vector3Int pos)
            {
                task.buildPos = pos;
                return this;
            }

            public BuildTaskBuilder setNeedResource(Dictionary<int, ResourceInfo> needResource) {
                task.temp = Tool.DeepCopyByBinary(needResource);
                task.needs = Tool.DeepCopyByBinary(needResource);
                return this;
            }

            public WorkerBuildTask build() {
                return task;
            }
        }
    }
}

