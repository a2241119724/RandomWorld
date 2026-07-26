namespace LAB2D.Constant
{
    using LAB2D;
    /// <summary>
    /// 工人任务队列拥堵提示常量。
    /// 集中维护 F016 的刷新节奏、Tip 冷却、菜单路径、默认文案和建议阈值；修改后需要验证 Tip 频率与 F015 HUD 压力文案一致。
    /// </summary>
    public static class WorkerTaskCongestionConstant
    {
        /// <summary>
        /// 拥堵扫描间隔，避免每帧读取任务快照和拼接建议文案。
        /// 调低会让提示更实时，但会增加运行时统计和字符串构造频率。
        /// </summary>
        public const float MonitorRefreshInterval = 1.5f;

        /// <summary>
        /// 拥堵 Tip 冷却时间，避免任务长期积压时刷屏。
        /// </summary>
        public const float TipCooldownSeconds = 18.0f;

        /// <summary>
        /// 繁忙等待任务阈值。
        /// 复用 F015 HUD 中的中等压力阈值，保持 HUD 和 Tip 判断一致。
        /// </summary>
        public const int BusyWaitingTaskThreshold = WorkerTaskHudConstant.MediumWaitingTaskThreshold;

        /// <summary>
        /// 拥堵等待任务阈值。
        /// 复用 F015 HUD 中的高压力阈值，达到后允许触发 Tip。
        /// </summary>
        public const int CongestedWaitingTaskThreshold = WorkerTaskHudConstant.HighWaitingTaskThreshold;

        /// <summary>
        /// 严重拥堵等待任务阈值。
        /// 只影响提示文案强度，不改变任务调度或优先级。
        /// </summary>
        public const int CriticalWaitingTaskThreshold = WorkerTaskHudConstant.HighWaitingTaskThreshold + 8;

        /// <summary>
        /// 主要积压任务类型的最少等待数量。
        /// 低于该值时不把单一任务类型当作主要问题。
        /// </summary>
        public const int DominantTaskWaitingThreshold = 3;

        /// <summary>
        /// 主要积压任务类型占等待任务的最小比例。
        /// 低于该比例时视为多类型同时积压。
        /// </summary>
        public const float DominantTaskWaitingRatio = 0.5f;

        /// <summary>
        /// 工人任务拥堵提示 Editor 菜单根路径。
        /// 仅供 Editor 脚本复用，运行时代码不会引用编辑器命名空间。
        /// </summary>
        public const string MenuRoot = "工具/任务队列拥堵提示/";

        /// <summary>
        /// 未发现拥堵时的默认文案。
        /// 仅用于 UI 和日志展示，不影响任务队列。
        /// </summary>
        public const string NoCongestionText = "任务队列: 暂无拥堵";

        /// <summary>
        /// WorkerTaskManager 尚未初始化时的默认文案。
        /// 常见于非 Game 场景或 Play Mode 初始化前。
        /// </summary>
        public const string ManagerUnavailableText = "任务队列拥堵提示: WorkerTaskManager 未初始化";

        /// <summary>
        /// 调试日志前缀。
        /// 用于区分普通业务日志和拥堵提示降级日志。
        /// </summary>
        public const string LogPrefix = "[WorkerTaskCongestion]";
    }
}
