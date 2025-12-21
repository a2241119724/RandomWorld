namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 锻炼任务
    /// </summary>
    [Serializable]
    public class WorkerExerciseTask : AWorkerTask
    {
        /// <summary>
        /// 没有任务寻路多少次添加锻炼任务
        /// </summary>
        public static readonly long SeekThreshold = 10;
        private AWorker worker;

        public WorkerExerciseTask()
            : base(WorkerTaskTypeEnum.Exercise)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = 10.0f;

                // 获取物资
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 设置Worker位置为目标位置
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(TileMap.Instance.WorldPosToMapPos(worker.transform.position));

                // 进入工作状态
                worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
            });
        }

        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        protected override bool DoIsCanWork(AWorker worker)
        {
            return this.worker == worker;
        }

        public class ExerciseTaskBuilder
        {
            private readonly WorkerExerciseTask task;

            public ExerciseTaskBuilder()
            {
                this.task = new WorkerExerciseTask();
            }

            public ExerciseTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            public ExerciseTaskBuilder SetWorker(AWorker worker)
            {
                this.task.worker = worker;
                return this;
            }

            public WorkerExerciseTask Build()
            {
                return this.task;
            }
        }
    }
}
