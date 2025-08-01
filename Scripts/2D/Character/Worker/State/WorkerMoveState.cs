namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Worker移动状态
    /// </summary>
    public class WorkerMoveState : WorkerState
    {
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
            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
            this.Character.WorkerStateText.text = this.preString +
                $"Target: {this.Character.TargetMap.x},{this.Character.TargetMap.y}\n" +
                $"Position: {posMap.x},{posMap.y}\n" + $"Hungry:{Mathf.RoundToInt(this.Character.CurHungry)}\n";
            bool isTarget = this.Character.MoveByPath();
            if (isTarget)
            {
                if (this.Character.Manager.Task == null)
                {
                    this.recordTime += Time.deltaTime;

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
            }
        }
    }
}
