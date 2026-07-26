namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 锻炼任务
    /// </summary>
    [Serializable]
    public class WorkerExerciseTask : AWorkerTask
    {
        private AWorker worker;

        public WorkerExerciseTask()
            : base(WorkerTaskType.Exercise)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.ExerciseSeconds;
                this.Init();

                // 设置Worker位置为目标位置
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(TileMapWorldToMapProvider(worker.transform.position));
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

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
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
