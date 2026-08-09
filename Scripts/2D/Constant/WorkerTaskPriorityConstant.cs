namespace LAB2D.Constant
{
    /// <summary>
    /// Worker 任务队列优先级常量。
    /// 数字越小优先级越高，WorkerTaskQueue 默认支持 0-3 四级。
    /// 修改这些值会直接影响任务分配循环的调度顺序。
    /// </summary>
    public static class WorkerTaskPriority
    {
        /// <summary>最高优先级：玩家发布的悬赏 / 紧急掉落（食物、敌人战利品）</summary>
        public const int PlayerBounty = 0;

        /// <summary>高优先级：Worker 自主发布的悬赏 / 悬赏掉落搬运到任务栏</summary>
        public const int WorkerBounty = 1;

        /// <summary>默认优先级：系统自动生成的建造等任务</summary>
        public const int SystemDefault = 2;

        /// <summary>低优先级：锻炼/空闲任务</summary>
        public const int Idle = 3;
    }
}
