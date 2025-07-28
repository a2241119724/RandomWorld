namespace LAB2D
{
    /// <summary>
    /// Worker状态
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

        /// <summary>
        /// Worker状态类型
        /// </summary>
        public enum WorkerStateTypeEnum
        {
            /// <summary>
            /// 移动状态
            /// </summary>
            Move,

            /// <summary>
            /// 工作状态
            /// </summary>
            Work,

            /// <summary>
            /// 吃饭装她爱
            /// </summary>
            Eat,

            /// <summary>
            /// 死亡状态
            /// </summary>
            Dead,

            /// <summary>
            /// 寻路状态
            /// </summary>
            Seek,

            /// <summary>
            /// 攻击状态
            /// </summary>
            Attack,

            /// <summary>
            /// 逃跑状态
            /// </summary>
            Escape,
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
        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
