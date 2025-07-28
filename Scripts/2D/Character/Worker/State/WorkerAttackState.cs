namespace LAB2D
{
    /// <summary>
    /// Worker攻击状态
    /// </summary>
    public class WorkerAttackState : WorkerState
    {
        public WorkerAttackState(Worker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.Character.WorkerStateText.text = $"<color=red>攻击</color>";
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
            if (this.Character.WearData.Weapon == null)
            {
                this.Character.Manager.ChangeState(WorkerStateTypeEnum.Escape);
                return;
            }

            // 拿出武器
            // 攻击
        }
    }
}
