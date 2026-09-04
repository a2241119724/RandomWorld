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

        /// <summary>拾取 — 从任务栏或地面拾取物品</summary>
        PickUp,

        /// <summary>防守待命 — 夜袭驻守至黎明（WorkerDefenceManager 入夜派发）</summary>
        Defend,

        /// <summary>漫游 — 恢复精气神，小概率发现物品</summary>
        Wander,

        /// <summary>地面睡眠 — 无床时的低效睡眠</summary>
        GroundSleep,

        /// <summary>拆除建筑</summary>
        Demolish,

        /// <summary>存取个人四格仓库（自建任务，不入全局任务队列）</summary>
        Storage,

        /// <summary>探索上古洞府 — 寻路到洞府邻格驻留推进，完成时风险/奖励结算（AncientCaveManager 派发）</summary>
        Explore,

        /// <summary>哨兵值 — 必须始终在最后，用于动态数组大小</summary>
        _Count,
    }
}
