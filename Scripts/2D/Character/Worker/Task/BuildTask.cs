using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace LAB2D
{
    /// <summary>
    /// Build在第一个阶段预留资源
    /// </summary>
    public class BuildTask : WorkerTask
    {
        private NeedResource needResource;
        private NeedResource temp;
        /// <summary>
        /// 任务2阶段：拿材料，建造
        /// </summary>
        private int stage;
        /// <summary>
        /// 建造的位置
        /// </summary>
        private Vector3Int buildPos;

        /// <summary>
        /// 没用
        /// </summary>
        public BuildItem BuildItem { get; private set; }

        public BuildTask() {
            Name = TaskType.Build.ToString();
            TaskType = TaskType.Build;
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 100;
                // 获取物资
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.getOneByPreTake(worker);
                if (TargetMap == default)
                {
                    giveUpTask(worker);
                }
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) =>
            {
                stage = 1;
                maxProgress = 200;
                // 建造
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
            // 自身携带资源足够
            if (worker.isEnough(needResource))
            {
                Debug.Log("携带资源充足");
                stageInit[1].Invoke(worker);
                return;
            }
            // 获得剩余不够的数量
            NeedResource remaining = worker.getRemaining(needResource);
            InventoryManager.Instance.isEnoughAndPreTake(worker, remaining, true);
            // 不够就取资源
            stageInit[0].Invoke(worker);
        }

        protected override bool isFinish(Worker worker)
        {
            // 只worker携带的资源不够时,取建筑材料
            switch (stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
                    worker.addResource(Tool.DeepCopyByBinary(resourceInfo));
                    // 减少需求的数量
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
                    // 获取完成所有的材料
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
            // 减少worker携带的资源
            worker.subResource(needResource);
            // 将建造完成的Tile从Building变为Build中
            BuildMap.Instance.setComplete(buildPos);
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            // 如果worker携带的资源已经满足建造
            if (worker.isEnough(needResource)) return true;
            // 按照单个任务的资源取看是否足够
            // 获得剩余不够的数量
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

