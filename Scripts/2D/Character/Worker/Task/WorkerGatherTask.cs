namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 采集任务
    /// </summary>
    [Serializable]
    public class WorkerGatherTask : WorkerTask
    {
        private string resourceName = "Tree";

        public WorkerGatherTask()
            : base(WorkerTaskTypeEnum.Gather)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                WorkerTask.maxProgress = 10.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[1]);
                this.AvailableNeighborPos.Add(Neighbors[3]);

                // 进入工作状态
                worker.Manager.ChangeState(WorkerState.TypeEnum.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);
            ResourceMap.Instance.CutTree(Vector3IntLAB.ToVector3Int(this.TargetMap));
            List<DropItem> dropItems = DropDataManager.Instance.GetDropItemsByName(this.resourceName);

            // 采摘掉落木头,苹果
            for (int i = 0; i < dropItems.Count; i++)
            {
                Vector3Int pos = IsAvailableMap.Instance.GenAvailablePosMap(Vector3IntLAB.ToVector3Int(this.TargetMap), 3, true);
                if (pos == default)
                {
                    break;
                }

                ItemMap.Instance.PutDownToDrop(pos, (TileBase)ResourceManager.Instance.GetAsset(dropItems[i].Name), dropItems[i].ResourceInfo);
            }

            // 删除采摘图标
            GatherMap.Instance.CancelGather(Vector3IntLAB.ToVector3Int(this.TargetMap));
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return ResourceMap.Instance.ResourceMapDataLAB.TreeCurCount > 0;
        }

#pragma warning disable SA1600 // Elements should be documented
        /// <summary>
        /// 建造者
        /// </summary>
        public class GatherTaskBuilder
        {
            private readonly WorkerGatherTask task;

            public GatherTaskBuilder()
            {
                this.task = new WorkerGatherTask();
            }

            public GatherTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);

                // 显示正在采摘图标
                GatherMap.Instance.AddGather(targetMap);
                return this;
            }

            public GatherTaskBuilder SetGatherName(string name)
            {
                this.task.resourceName = name;
                return this;
            }

            public WorkerGatherTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}