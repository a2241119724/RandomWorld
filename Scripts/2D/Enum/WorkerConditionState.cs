namespace LAB2D.Enum
{
    /// <summary>
    /// 工人生存状态。
    /// 用于统一表达 Worker 饥饿值与疲劳值组合后的业务状态，可被任务、移动、HUD 和调试菜单复用。
    /// 后续允许追加更细的状态值，但不得删除或重命名已有值，避免破坏 UI 与统计逻辑。
    /// </summary>
    public enum WorkerConditionState
    {
        /// <summary>
        /// 状态良好：饥饿值与疲劳值都处于安全区间，移动和工作不受惩罚。
        /// </summary>
        Healthy,

        /// <summary>
        /// 饥饿：饥饿值低于警戒比例，工人会降低移动与普通工作效率。
        /// </summary>
        Hungry,

        /// <summary>
        /// 疲劳：疲劳值低于警戒比例，工人会降低移动与普通工作效率。
        /// </summary>
        Tired,

        /// <summary>
        /// 饥饿且疲劳：两项状态都低于警戒比例，惩罚比单项异常更明显。
        /// </summary>
        Exhausted,

        /// <summary>
        /// 濒临停工：饥饿值或疲劳值接近归零，使用最强的非致命效率惩罚。
        /// </summary>
        Critical,
    }
}
