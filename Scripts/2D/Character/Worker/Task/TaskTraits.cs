namespace LAB2D.Character.Worker.Task
{
    using System;

    /// <summary>
    /// Worker 任务特征标志，用于 WorkerTaskManager 的分派逻辑。
    /// 新增任务类型时只需在子类中重写 Traits 虚属性，无需修改 Manager。
    /// </summary>
    [Flags]
    public enum TaskTraits
    {
        /// <summary>无特殊特征</summary>
        None = 0,

        /// <summary>同一地图位置只能存在一个此类型的任务（Eat/Wear 去重）</summary>
        OnePerPosition = 1 << 0,

        /// <summary>需要记录任务位置供外部查询和取消（Gather 的 CancelGatherTask）</summary>
        TrackPositions = 1 << 1,

        /// <summary>任务完成后标记为空闲而非从队列移除（Eat 任务复用机制）</summary>
        ReturnToIdle = 1 << 2,

        /// <summary>仅特定 Worker 可接此任务（Sleep/Wear/Exercise 绑定到具体 Worker）</summary>
        WorkerSpecific = 1 << 3,
    }
}
