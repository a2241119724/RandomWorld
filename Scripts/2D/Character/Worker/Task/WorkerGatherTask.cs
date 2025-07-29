namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 采集任务
    /// </summary>
    public class WorkerGatherTask : WorkerTask
    {
        private string resourceName = "Tree";

        public WorkerGatherTask()
            : base(WorkerTaskTypeEnum.Gather)
        {
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 10.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[1]);
                this.AvailableNeighborPos.Add(Neighbors[3]);

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
            ResourceMap.Instance.CutTree(this.TargetMap);
            List<DropItem> dropItems = DropDataManager.Instance.GetDropItemsByName(this.resourceName);

            // 采摘掉落木头,苹果
            for (int i = 0; i < dropItems.Count; i++)
            {
                Vector3Int pos = IsAvailableMap.Instance.GenAvailablePosMap(this.TargetMap, 3, true);
                if (pos == default)
                {
                    break;
                }

                ItemMap.Instance.PutDownToDrop(pos, (TileBase)ResourceManager.Instance.GetAsset(dropItems[i].Name), dropItems[i].ResourceInfo);
            }

            // 删除采摘图标
            GatherMap.Instance.CancelGather(this.TargetMap);
        }

        /// <inheritdoc/>
        public override bool IsCanWork(Worker worker)
        {
            if (!base.IsCanWork(worker))
            {
                return false;
            }

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
                this.task.TargetMap = targetMap;

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