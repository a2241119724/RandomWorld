namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 采集任务
    /// </summary>
    [Serializable]
    public class WorkerGatherTask : AWorkerTask
    {
        /// <summary>
        /// Worker携带的资源
        /// </summary>
        private ResourceInfo resourceInfo;

        public WorkerGatherTask()
            : base(WorkerTaskType.Gather)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.GatherSeconds;
                this.Init();
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
            ResourceMapProvider().CutTree(Vector3IntLAB.ToVector3Int(this.TargetMap));
            List<DropItem> dropItems = DropDataProvider(this.resourceInfo.Id);

            // 悬赏任务：OwnerId=发布者（!=0）；普通任务：OwnerId=采集者
            int workerId = AWorkerTask.BountyOwnerOverride != 0
                ? AWorkerTask.BountyOwnerOverride
                : worker.GetInstanceID();

            AWorkerTask.LogProvider(
                $"[GatherOwner] executor={worker.name}({worker.GetInstanceID()}) override={AWorkerTask.BountyOwnerOverride} finalOwner={workerId}",
                LogManager.LogLevelEnum.Info);

            // 采摘掉落木头,苹果
            for (int i = 0; i < dropItems.Count; i++)
            {
                Vector3Int targetPos = Vector3IntLAB.ToVector3Int(this.TargetMap);

                // 设置所有权：采集所得归采集者
                dropItems[i].ResourceInfo.OwnerId = workerId;

                // 可堆叠物品优先合并到周围同类堆叠，否则找空地放置
                Vector3Int pos = TryMergeOrPlaceDrop(targetPos, dropItems[i].ResourceInfo, dropItems[i].Name);

                if (pos == default)
                {
                    // 地图满到极限，放进背包不丢物品
                    worker.AddResource(dropItems[i].ResourceInfo);
                }
            }

            // 删除采摘图标
            GatherMapProvider().CancelGather(Vector3IntLAB.ToVector3Int(this.TargetMap));
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return true;
        }

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.TrackPositions;

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[1]);
            this.AvailableNeighborPos.Add(Neighbors[3]);
        }

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
                GatherMapProvider().AddGather(targetMap);
                return this;
            }

            public GatherTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            public WorkerGatherTask Build()
            {
                return this.task;
            }
        }
    }
}
