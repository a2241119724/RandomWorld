using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace LAB2D
{
    /// <summary>
    /// 任务2阶段：拿材料，建造
    /// Build在第一个阶段预留资源
    /// </summary>
    public class WorkerBuildTask : WorkerTask
    {
        private Dictionary<int, ResourceInfo> needs;
        private Dictionary<int, ResourceInfo> temp;
        /// <summary>
        /// 建造的位置
        /// </summary>
        private Vector3Int buildPos;

        /// <summary>
        /// 没用
        /// </summary>
        public BuildItem BuildItem { get; private set; }

        public WorkerBuildTask() : base(TaskType.Build) {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                // 获取物资
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.getPosByPreTake(worker);
                if (TargetMap == default)
                {
                    giveUpTask(worker);
                }
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 2.0f;
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
            if (worker.isEnough(needs))
            {
                //LogManager.Instance.log("携带资源充足", LogManager.LogLevel.Info);
                changeStage(worker,1);
                return;
            }
            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.getRemaining(needs);
            InventoryManager.Instance.isEnoughAndPreTake(worker, remaining, true);
            // 不够就取资源
            changeStage(worker,0);
        }

        protected override bool isFinish(Worker worker)
        {
            // 只worker携带的资源不够时,取建筑材料
            switch (stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, TargetMap);
                    worker.addResource(resourceInfo);
                    // 减少需求的数量
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
                    // 获取完成所有的材料
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
            // 减少worker携带的资源
            worker.subResource(needs);
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
            if (worker.isEnough(needs)) return true;
            // 按照单个任务的资源取看是否足够
            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.getRemaining(needs);
            return InventoryManager.Instance.isEnoughAndPreTake(worker, remaining);
        }

        public override void giveUpTask(Worker worker)
        {
            base.giveUpTask(worker);
            // 恢复资源
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

