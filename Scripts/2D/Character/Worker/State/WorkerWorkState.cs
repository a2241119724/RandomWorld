namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Serializable;
    using UnityEngine;
    /// <summary>
    /// Worker工作状态
    /// </summary>
    public class WorkerWorkState : AWorkerState
    {
        private bool waitOneFrame; // 等待一帧

        public WorkerWorkState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.waitOneFrame = false;
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task == null)
            {
                return;
            }

            // 进入工作状态前验证 Worker 是否已到达任务目标位置。
            // Move 状态存在"误判到达"缺陷（isTargetReached 残留 / 寻路结果为空），
            // 若未到位就执行任务会导致 Worker 原地建造。兜底：未到位 → 切回 Seek 重新寻路。
            if (!this.IsAtTaskPosition(workerData.Task))
            {
                this.Character.HideDialogText();
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            this.Character.WorkerStateText.text = this.preString +
                $"Target: {workerData.Task.TargetMap.X},{workerData.Task.TargetMap.Y}";
        }

        /// <summary>
        /// 判断 Worker 是否站在任务目标位置（TargetMap 的某个 AvailableNeighborPos 上）。
        /// 邻居换算与 WorkerSeekState 的斜对称逻辑保持一致。
        /// </summary>
        private bool IsAtTaskPosition(AWorkerTask task)
        {
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            foreach (Vector3IntLAB pos in task.AvailableNeighborPos)
            {
                Vector3Int expected = new (task.TargetMap.X + pos.Y, task.TargetMap.Y + pos.X, 0);
                if (expected.x == workerPos.x && expected.y == workerPos.y)
                {
                    return true;
                }
            }

            // AvailableNeighborPos 为空的任务放行，避免破坏未定义邻居的任务流程
            return task.AvailableNeighborPos.Count == 0;
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.Character.HideDialogText();
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            if (this.waitOneFrame)
            {
                // 等待一帧后再进入寻路状态,先去接任务
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            base.OnUpdate();
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task == null)
            {
                return;
            }

            // 执行任务时概率显示内心独白（受内部计时器控制，6-12秒切换一次）
            this.Character.ShowRandomMonologue(workerData.Task.TaskType);

            bool isComplete = workerData.Task.Execute(this.Character, this.Character.DeltaTime);
            if (isComplete)
            {
                this.waitOneFrame = true;
            }
        }
    }
}