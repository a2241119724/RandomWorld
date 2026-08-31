namespace LAB2D.Character.Worker
{
    using LAB2D;
    using System;

    /// <summary>
    /// Worker状态管理器
    /// </summary>
    /// <typeparam name="CS">角色状态</typeparam>
    /// <typeparam name="CST">角色状态类型</typeparam>
    /// <typeparam name="C">角色</typeparam>
    public class WorkerStateManager<CS, CST, C> : CharacterStateManager<CS, CST, C>
        where CS : ICharacterState
        where CST : Enum
        where C : AWorker
    {
        public WorkerStateManager(C character)
            : base(character)
        {
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="type">切换的状态</param>
        public override void ChangeState(CST type)
        {
            // 统一收口：任何状态切换先清除移动意图（仅 ToMap 意图存在时才 StopMove，
            // 离开移动路径统一清刚体速度防滑行，见 bug-fixes.md 2026-08-15）。
            // 必须在新状态 OnEnter 之前执行——新状态可在 OnEnter 内声明新意图不被覆盖。
            this.Character.Locomotion.ClearGoToIntent();

            // 先执行,可以在Enter中更改,不然会被覆盖
            CST from = this.CurrentStateType;
            bool hadPriorState = this.CurrentState != null;
            this.Character.WorkerStateText.text = this.CurrentStateType.ToString();
            base.ChangeState(type);

            // 状态切换诊断轨迹（事件点，Debug 只进 game.log）：记录 from→to + 角色名，
            // 用于排查同一对状态来回切换的异常振荡（如 Seek→Seek 重入、任务完成→寻路→移动循环）。
            // 首次切换（无前置状态）不记录，避免 enum 默认值造成"Move→X"的误导。
            if (hadPriorState)
            {
                AWorkerTask.LogProvider(
                    $"[StateDiag] {this.Character.name} 状态切换 {from} -> {type}",
                    LogManager.LogLevelEnum.Debug);
            }
        }
    }
}
