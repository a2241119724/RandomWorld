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
        // ---- 状态切换路径零分配化（GC 排查 2026-09-06，仿 ASeek seekPathDiag 范本）----
        // ChangeState 是全队高频事件点（100 Worker ~60 次/s）：enum.ToString 每次装箱/分配、
        // 节流 key 插值（含 .name getter）与捕获局部变量的闭包即使被节流也每次分配。
        // 枚举值有限（状态机全集），文本与 key/委托缓存后命中即零分配；委托体只在真正
        // 打日志时求值（2s 一条，其中枚举插值的装箱可接受）。
        private CST labelState;       // 上次写 WorkerStateText 的枚举值
        private string labelStateText; // 对应缓存字符串
        private string stateDiagKey;
        private Func<string> stateDiagLog;
        private CST stateFromDiag; // 委托读（触发时刻的来源状态）
        private CST stateToDiag;   // 委托读

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
            // enum.ToString 缓存：状态值有限，重复出现直接复用（见字段区注释）
            if (!EqualityComparer<CST>.Default.Equals(this.labelState, from))
            {
                this.labelState = from;
                this.labelStateText = from.ToString();
            }

            this.Character.WorkerStateText.text = this.labelStateText;
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
                // 零分配节流：from/to 拷字段供委托读，key/委托缓存复用（见字段区注释）
                this.stateFromDiag = from;
                this.stateToDiag = type;
                this.stateDiagKey ??= $"{this.Character.name}|StateDiag";
                this.stateDiagLog ??= () => $"[StateDiag] {this.Character.name} 状态切换 {this.stateFromDiag} -> {this.stateToDiag}";
                AWorkerTask.LogProviderThrottled(
                    this.stateDiagKey, 2f,
                    this.stateDiagLog,
                    LogManager.LogLevelEnum.Trace);
            }
        }
    }
}
