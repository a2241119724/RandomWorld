namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 拆除建筑任务 — Worker 拆除已完成的建筑。
    /// 单阶段任务，完成时调用 BuildMap.CancelBuilding 移除建筑。
    /// </summary>
    [Serializable]
    public class WorkerDemolishTask : AWorkerTask
    {
        public WorkerDemolishTask()
            : base(WorkerTaskType.Demolish)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.DemolishSeconds;
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

            // 删除拆除图标
            GatherMapProvider().CancelDemolish(Vector3IntLAB.ToVector3Int(this.TargetMap));

            // 移除建筑（CancelBuilding 对已完成和未完成建筑均适用）
            Core.ServiceLocator.Get<Map.BuildMap>().CancelBuilding(
                Vector3IntLAB.ToVector3Int(this.TargetMap));

            LogProvider(
                $"{worker.name} 拆除了建筑: pos=({this.TargetMap.X},{this.TargetMap.Y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return true;
        }

        /// <inheritdoc/>
        protected override float TiredCostPerSecond => WorkerTaskTimeConfig.MediumWorkTiredCostPerSecond;

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.TrackPositions;

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[0]); // 上
            this.AvailableNeighborPos.Add(Neighbors[1]); // 右
            this.AvailableNeighborPos.Add(Neighbors[2]); // 下
            this.AvailableNeighborPos.Add(Neighbors[3]); // 左
        }

        /// <summary>
        /// 拆除任务建造者
        /// </summary>
        public class DemolishTaskBuilder
        {
            private readonly WorkerDemolishTask task;
            private bool claimFailed;

            public DemolishTaskBuilder()
            {
                this.task = new WorkerDemolishTask();
            }

            public DemolishTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);

                // 认领拆除位置（防止重复拆除同一位置）
                if (!GatherMapProvider().AddDemolish(targetMap))
                {
                    LogProvider($"拆除位置已被认领: pos=({targetMap.x},{targetMap.y})", LogManager.LogLevelEnum.Warning);
                    this.claimFailed = true;
                }

                return this;
            }

            /// <summary>
            /// 构建拆除任务。如果位置已被认领，返回 null。
            /// </summary>
            public WorkerDemolishTask Build()
            {
                return this.claimFailed ? null : this.task;
            }
        }
    }
}
