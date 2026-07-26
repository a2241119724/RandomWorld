namespace LAB2D.Enum
{
    /// <summary>
    /// Worker 任务类型。
    /// 从 AWorkerTask 中提取为独立枚举，供 Domain 层和 Character 层共享使用。
    /// 任务优先级按枚举定义顺序排列（越靠前优先级越高）。
    /// </summary>
    public enum WorkerTaskType
    {
        /// <summary>建造</summary>
        Build,

        /// <summary>搬运</summary>
        Carry,

        /// <summary>采集</summary>
        Gather,

        /// <summary>吃饭</summary>
        Eat,

        /// <summary>锻炼</summary>
        Exercise,

        /// <summary>穿戴</summary>
        Wear,

        /// <summary>睡觉</summary>
        Sleep,

        /// <summary>种植</summary>
        Plant,
    }
}
