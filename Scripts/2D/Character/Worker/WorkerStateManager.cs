namespace LAB2D.Character.Worker
{
    using LAB2D;
    using System;
    using System.Collections.Generic;

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

            // 状态切换诊断轨迹：排查同一对状态来回切换的异常振荡（任务完成→寻路→移动循环等）。
            // 100 Worker 收紧（bug-fixes.md 2026-09-06）：常态流转每 Worker 每秒 ~0.6 条、全队
            // 63k 条/6 分钟是第一刷屏源（字符串插值+装箱+写盘 I/O 均在主线程热路径上）。
            // ① 首次切换（无前置状态）不记录，避免 enum 默认值造成"Move→X"的误导；
            // ② from==to 重入不记录——任务接取重入 Seek 属正常路径（每局 21k+ 条），
            //    语义已由 [TaskDiag] SetTask/开始任务 表达；
            // ③ 其余切换走节流（每 Worker 2s 一条，惰性求值：被节流不构造插值串），
            //    节流粒度足以还原振荡模式（正常循环间隔 >2s 只略降采样，异常往返 14ms 级
            //    会连续命中节流窗只出首条——配合 TaskDiag/SeekDiag 链路定位）。
            if (hadPriorState && !EqualityComparer<CST>.Default.Equals(from, type))
            {
                AWorkerTask.LogProviderThrottled(
                    $"{this.Character.name}|StateDiag", 2f,
                    () => $"[StateDiag] {this.Character.name} 状态切换 {from} -> {type}",
                    LogManager.LogLevelEnum.Trace);
            }
        }
    }
}
