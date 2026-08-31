namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker移动状态（薄壳）— 移动执行（MoveByPath 消费、到达判定、Sliding/Stuck 熔断）
    /// 已下沉至 WorkerLocomotion 常驻服务层（AWorker.FixedUpdate 统一驱动）。
    /// 本状态只负责：进入时声明 ToMap 意图、到达后按任务有无分流（Seek/Work）、UI 文案。
    /// </summary>
    public class WorkerMoveState : AWorkerState
    {
        private readonly StringBuilder builder = new (128); // 减少GC
        private float recordTime = 0.0f;

        public WorkerMoveState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
            // 声明移动意图：沿 Seek 状态已寻好的路径走向目标。
            // GoTo 内部重置到达标记（原 OnEnter 重置 isTargetReached、避免上次任务残留误判到达的语义不变）。
            this.Character.Locomotion.GoTo(this.Character.Seek.TargetMap);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.Character.HideDialogText();
            // 停止移动/清速度已收口至 WorkerStateManager.ChangeState → Locomotion.ClearGoToIntent
            //（离开移动统一 StopMove 防滑行，见 bug-fixes.md 2026-08-15）。
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            this.builder.Clear();
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            if (this.Character.Locomotion.HasArrived)
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

        // 移动执行（原 OnFixedUpdate：MoveByPath 消费 + Sliding/Stuck 熔断）已由
        // WorkerLocomotion.TickFixed 在 AWorker.FixedUpdate 中统一驱动，不再 override。
    }
}
