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
        private Vector3Int lastSlidingTarget; // 上次 Sliding 时的寻路目标，用于统计累计次数
        private int slidingStreak;            // 同一目标累计 Sliding 次数（熔断用）

        public WorkerMoveState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
            // 修复：进入 Move 状态时重置到达标记。
            // 避免上次任务到达后的 isTargetReached 残留，导致新任务未走到位就误判到达并切 Work。
            this.isTargetReached = false;
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
                // 位移不足但未完全卡死 → 预防性重新寻路绕开障碍。
                // 但若同一目标累计 Sliding 过多（A* 认为可通而物理被挡，如路径穿过床 sprite），
                // 静默重寻路会无限循环且无任何日志/失败缓存。累计 N 次后视为卡死，
                // 走统一的 HandleMovementStuck：建造任务保留 3 次重试，其他任务
                // RecordFail + GiveUpTask（决策层经 IsRecentFail 失败缓存进入冷却），
                // 打破"Sliding→重寻路→Sliding"死循环（观测 53 次/人，从不入睡）。
                if (this.lastSlidingTarget != this.Character.Seek.TargetMap)
                {
                    this.lastSlidingTarget = this.Character.Seek.TargetMap;
                    this.slidingStreak = 0;
                }

                if (++this.slidingStreak >= 4)
                {
                    this.slidingStreak = 0;
                    this.Character.HandleMovementStuck(); // 内部已切回 Seek / 放弃任务
                    return;
                }

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
