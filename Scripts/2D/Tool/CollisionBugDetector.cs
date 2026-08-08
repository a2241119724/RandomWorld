namespace LAB2D
{
    using System;

    /// <summary>
    /// 碰撞 Bug 检测器 — 检测角色是否因异常高频碰撞而卡死。
    /// 从 Character.CheckBug 提取为独立工具类，供所有角色类型复用。
    /// </summary>
    public sealed class CollisionBugDetector
    {
        private const double IntervalTicks = 6e5;
        private const int DefaultThreshold = 100;

        /// <summary>
        /// 上一次碰撞时间（DateTime.Ticks）。
        /// </summary>
        public long LastTime { get; set; }

        /// <summary>
        /// 连续碰撞次数。
        /// </summary>
        public int ColliderCount { get; set; }

        /// <summary>
        /// 是否有碰撞 Bug（连续碰撞次数超过阈值）。
        /// </summary>
        /// <param name="name">角色名称（调试用，当前未输出日志）。</param>
        /// <param name="threshold">碰撞阈值，默认 100 次。</param>
        /// <returns>是否超过阈值。</returns>
        public bool IsBug(string name, int threshold = DefaultThreshold)
        {
            bool bug = this.ColliderCount > threshold;
            // 调试日志已注释，按需启用
            // if (bug) { LogManager.Instance.Log(name + "碰撞次数:" + ColliderCount, LogManager.LogLevel.Info); }
            return bug;
        }

        /// <summary>
        /// 记录一次碰撞。若距离上次碰撞超过间隔，重置计数器。
        /// </summary>
        /// <param name="timeTicks">当前时间（DateTime.Ticks）。</param>
        public void AddColliderCount(long timeTicks)
        {
            if (timeTicks - this.LastTime < IntervalTicks)
            {
                this.ColliderCount++;
            }
            else
            {
                this.ColliderCount = 1;
            }

            this.LastTime = timeTicks;
        }
    }
}
