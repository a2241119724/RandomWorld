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
                // 工作入口诊断（事件点）：Move 状态"误判到达"导致未到位就进 Work，强制切回 Seek 重寻路。
                // 若高频出现，说明到位判定仍有缺陷，Worker 会反复"Work→Seek"振荡。
                AWorkerTask.LogProvider(
                    $"[StateDiag] {this.Character.name} 进入Work未到位, 切回Seek重寻路: 任务={workerData.Task.TaskType} 目标=({workerData.Task.TargetMap.X},{workerData.Task.TargetMap.Y})",
                    LogManager.LogLevelEnum.Debug);
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

            // Execute 前捕获任务引用：Execute 内部 Finish() 会把 workerData.Task 置空
            //（若 Finish 未重建后继任务，如拾取链最后一项 / 无掉落采集任务），
            // 因此完成日志必须用捕获的 currentTask，不能 deref workerData.Task
            //（否则 NullReference 中断 waitOneFrame 流转，Worker 永久卡在 Work 状态"站着不动"）。
            AWorkerTask currentTask = workerData.Task;

            // 执行任务时概率显示内心独白（受内部计时器控制，6-12秒切换一次）
            this.Character.ShowRandomMonologue(currentTask.TaskType);

            bool isComplete = currentTask.Execute(this.Character, this.Character.DeltaTime);
            if (isComplete)
            {
                // 任务完成诊断（事件点）：记录完成的任务类型与目标，下一帧切回 Seek 触发再决策。
                // 用于核对任务生命周期是否正常收尾（而非被 GiveUpTask 中断）。
                AWorkerTask.LogProvider(
                    $"[StateDiag] {this.Character.name} 任务完成: {currentTask.TaskType} 目标=({currentTask.TargetMap.X},{currentTask.TargetMap.Y})",
                    LogManager.LogLevelEnum.Debug);
                this.waitOneFrame = true;
            }
        }
    }
}