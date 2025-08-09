namespace LAB2D
{
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker移动状态
    /// </summary>
    public class WorkerMoveState : WorkerState
    {
        private readonly StringBuilder builder = new (128); // 减少GC
        private float recordTime = 0.0f;

        public WorkerMoveState(Worker worker)
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
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            this.builder.Clear();
            bool isTarget = this.Character.Seek.MoveByPath();
            if (isTarget)
            {
                if (this.Character.Manager.Task == null)
                {
                    this.recordTime += Time.deltaTime;
                    if (Time.frameCount % 60 == 0)
                    {
                        this.Character.WorkerStateText.text = this.builder.Append("休息: ")
                        .Append(Mathf.RoundToInt(this.recordTime))
                        .ToString();
                    }

                    // 休息2秒
                    if (this.recordTime < 2)
                    {
                        return;
                    }

                    // 没有任务就进入寻路状态
                    this.Character.Manager.ChangeState(WorkerStateTypeEnum.Seek);
                }
                else
                {
                    // 有任务就进入工作状态
                    this.Character.Manager.ChangeState(WorkerStateTypeEnum.Work);
                }

                return;
            }

            if (Time.frameCount % 60 == 0)
            {
                Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
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
    }
}
