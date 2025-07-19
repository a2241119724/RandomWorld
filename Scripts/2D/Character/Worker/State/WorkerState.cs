namespace LAB2D
{
    /// <summary>
    /// 工作者状态
    /// </summary>
    public class WorkerState : CharacterState<Worker>
    {
        /// <summary>
        /// 信息前缀
        /// </summary>
        protected string preString = string.Empty;

        public WorkerState(Worker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.preString = string.Empty;
            if (this.Character.Manager.Task != null)
            {
                this.preString = $"<color=red>{this.Character.Manager.Task.Name}</color>\n";
            }
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
        }
    }
}
