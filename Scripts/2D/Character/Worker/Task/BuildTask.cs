using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace LAB2D
{
    /// <summary>
    /// Build�ڵ�һ���׶�Ԥ����Դ
    /// </summary>
    public class BuildTask : WorkerTask
    {
        private NeedResource needResource;
        private NeedResource temp;
        /// <summary>
        /// ����2�׶Σ��ò��ϣ�����
        /// </summary>
        private int stage;
        /// <summary>
        /// �����λ��
        /// </summary>
        private Vector3Int buildPos;

        /// <summary>
        /// û��
        /// </summary>
        public BuildItem BuildItem { get; private set; }

        public BuildTask() {
            Name = TaskType.Build.ToString();
            TaskType = TaskType.Build;
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 100;
                // ��ȡ����
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.getOneByPreTake(worker);
                if (TargetMap == default)
                {
                    giveUpTask(worker);
                }
                // ���빤��״̬
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) =>
            {
                stage = 1;
                maxProgress = 200;
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
            if (worker.isEnough(needResource))
            {
                Debug.Log("Я����Դ����");
                stageInit[1].Invoke(worker);
                return;
            }
            // ���ʣ�಻��������
            NeedResource remaining = worker.getRemaining(needResource);
            InventoryManager.Instance.isEnoughAndPreTake(worker, remaining, true);
            // ������ȡ��Դ
            stageInit[0].Invoke(worker);
        }

        protected override bool isFinish(Worker worker)
        {
            // ֻworkerЯ������Դ����ʱ,ȡ��������
            switch (stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
                    worker.addResource(Tool.DeepCopyByBinary(resourceInfo));
                    // �������������
                    foreach(KeyValuePair<int, ResourceInfo> pair in temp.needs)
                    {
                        if (pair.Key == resourceInfo.id)
                        {
                            pair.Value.count -= resourceInfo.count;
                            if(pair.Value.count <= 0)
                            {
                                temp.needs.Remove(resourceInfo.id);
                            }
                            break;
                        }
                    }
                    // ��ȡ������еĲ���
                    if (temp.needs.Count == 0)
                    {
                        stageInit[1].Invoke(worker);
                        return false;
                    }
                    stageInit[0].Invoke(worker);
                    return false;
                default:
                    return true;
            }
        }

        protected override void finish(Worker worker)
        {
            // ����workerЯ������Դ
            worker.subResource(needResource);
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
            if (worker.isEnough(needResource)) return true;
            // ���յ����������Դȡ���Ƿ��㹻
            // ���ʣ�಻��������
            NeedResource remaining = worker.getRemaining(needResource);
            return InventoryManager.Instance.isEnoughAndPreTake(worker, remaining);
        }

        public override void giveUpTask(Worker worker)
        {
            base.giveUpTask(worker);
            temp = Tool.DeepCopyByBinary(needResource);
        }

        public class BuildTaskBuilder {
            private BuildTask task;

            public BuildTaskBuilder(){
                task = new BuildTask();
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

            public BuildTaskBuilder setNeedResource(NeedResource needResource) {
                task.temp = needResource;
                task.needResource = Tool.DeepCopyByBinary(needResource);
                return this;
            }

            public BuildTask build() {
                return task;
            }
        }
    }

    [Serializable]
    public class NeedResource {
        public Dictionary<int,ResourceInfo> needs;

        public NeedResource()
        {
            needs = new Dictionary<int, ResourceInfo>();
        }

        public NeedResource(Dictionary<int, ResourceInfo> needs)
        {
            this.needs = needs;
        }
    }
}

