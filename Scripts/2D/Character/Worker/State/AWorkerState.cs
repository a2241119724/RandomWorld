namespace LAB2D
{
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    /// Worker状态
    /// </summary>
    public abstract class AWorkerState : ACharacterState<AWorker>
    {
        /// <summary>
        /// 信息前缀
        /// </summary>
        protected string preString = string.Empty;

        public AWorkerState(AWorker worker)
            : base(worker)
        {
        }

        /// <summary>
        /// Worker状态类型
        /// </summary>
        public enum TypeEnum
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
            LogManager.Instance.Log(this.Character.name + " " + this.Character.Manager.CurrentStateType);
            this.preString = string.Empty;
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task != null)
            {
                this.preString += $"<color={PixelUITheme.RichMint}>任务: {workerData.Task.Name}</color>\n";
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
