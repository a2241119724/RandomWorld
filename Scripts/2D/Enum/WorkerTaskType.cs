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

        /// <summary>悬赏 — Worker 自主发布、其他 Worker 领取完成</summary>
        Bounty,

        /// <summary>搬运到任务栏 — 将悬赏产出物搬运到任务栏处</summary>
        CarryToBoard,

        /// <summary>从任务栏拾取 — 发布者去任务栏取回属于自己的物品</summary>
        PickUpFromBoard,

        /// <summary>漫游 — 恢复精气神，小概率发现物品</summary>
        Wander,

        /// <summary>地面睡眠 — 无床时的低效睡眠</summary>
        GroundSleep,

        /// <summary>哨兵值 — 必须始终在最后，用于动态数组大小</summary>
        _Count,
    }
}
