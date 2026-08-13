namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker移动状态
    /// </summary>
    public class WorkerMoveState : AWorkerState
    {
        private readonly StringBuilder builder = new (128); // 减少GC
        private float recordTime = 0.0f;
        private bool isTargetReached = false;

        public WorkerMoveState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
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
            base.OnUpdate();
            this.builder.Clear();
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            if (this.isTargetReached)
            {
                if (workerData.Task == null)
                {
                    this.recordTime += this.Character.DeltaTime;

                    // 闲逛到达路点后短暂休息，显示内心独白
                    this.Character.ShowRandomMonologue();

                    if (Time.frameCount % 60 == 0)
                    {
                        this.Character.WorkerStateText.text = this.builder.Append("休息: ")
                        .Append(MathHelper.RoundToInt(this.recordTime))
                        .ToString();
                    }

                    // 短暂休息后重新找任务
                    if (this.recordTime < WorkerTaskTimeConfig.IdleRestSeconds)
                    {
                        return;
                    }

                    // 没有任务就进入寻路状态
                    this.Character.Manager.ChangeState(TypeEnum.Seek);
                }
                else
                {
                    // 有任务就进入工作状态，隐藏内心独白
                    this.Character.HideDialogText();
                    this.Character.Manager.ChangeState(TypeEnum.Work);
                }

                return;
            }

            // 移动中：无任务时显示内心独白
            if (workerData.Task == null)
            {
                this.Character.ShowRandomMonologue();
            }
            else
            {
                this.Character.HideDialogText();
            }

            if (Time.frameCount % 60 == 0)
            {
                Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
                this.Character.WorkerStateText.text = this.builder.Append(this.preString)
                    .Append("Target: ")
                    .Append(this.Character.Seek.TargetMap.x)
                    .Append(",")
                    .Append(this.Character.Seek.TargetMap.y)
                    .Append("\nPosition: ")
                    .Append(posMap.x)
                    .Append(",")
                    .Append(posMap.y)
                    .ToString();
            }
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            this.isTargetReached = this.Character.Seek.MoveByPath();

            if (this.isTargetReached)
            {
                return; // 到达/无路径：MoveByPath 内部已重置检测
            }

            BugCheckResult stuckResult = this.Character.Seek.LastStuckResult;
            if (stuckResult == BugCheckResult.Sliding)
            {
                // 位移不足但未完全卡死 → 预防性重新寻路绕开障碍
                this.Character.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                return;
            }

            if (stuckResult == BugCheckResult.Stuck)
            {
                // 真卡死 → 建造重试3次 / 记录失败点位并放弃任务
                this.Character.HandleMovementStuck();
            }
        }
    }
}
