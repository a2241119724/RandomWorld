namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 碰撞 Bug 检测器 — 检测角色是否因异常高频碰撞而卡死。
    /// 通过追踪碰撞期间的位移来区分"贴墙滑动"（正常移动）和"原地撞墙"（真卡死）。
    /// </summary>
    public sealed class CollisionBugDetector
    {
        private const double IntervalTicks = 6e5;
        private const int DefaultThreshold = 100;

        /// <summary>
        /// 碰撞期间累计位移超过此值即视为贴墙滑动（正常），重置计数器。
        /// </summary>
        private const float MinMovementThreshold = 1.5f;

        /// <summary>
        /// 上一次碰撞时间（DateTime.Ticks）。
        /// </summary>
        public long LastTime { get; set; }

        /// <summary>
        /// 连续碰撞次数。
        /// </summary>
        public int ColliderCount { get; set; }

        /// <summary>
        /// 上一次记录位置（用于计算累计位移）。
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// 碰撞期间的累计位移。
        /// </summary>
        private float accumulatedMovement;

        /// <summary>
        /// 记录一次碰撞。若距离上次碰撞超过间隔，重置计数器。
        /// </summary>
        /// <param name="timeTicks">当前时间（DateTime.Ticks）。</param>
        /// <param name="currentPosition">角色当前位置（用于追踪位移）。</param>
        public void AddColliderCount(long timeTicks, Vector3 currentPosition)
        {
            if (timeTicks - this.LastTime < IntervalTicks)
            {
                this.ColliderCount++;
                this.accumulatedMovement += Vector3.Distance(this.lastPosition, currentPosition);
            }
            else
            {
                this.ColliderCount = 1;
                this.accumulatedMovement = 0f;
            }

            this.lastPosition = currentPosition;
            this.LastTime = timeTicks;
        }

        /// <summary>
        /// 是否有碰撞 Bug（连续碰撞次数超过阈值 AND 累计位移不足）。
        /// 贴墙滑动时位移大，会在此处自动重置计数器，不会误判为卡死。
        /// </summary>
        /// <param name="name">角色名称（调试用，当前未输出日志）。</param>
        /// <param name="threshold">碰撞阈值，默认 100 次。</param>
        /// <returns>是否超过阈值且位移不足（真卡死）。</returns>
        public bool IsBug(string name, int threshold = DefaultThreshold)
        {
            if (this.ColliderCount > threshold)
            {
                // 碰撞多但位移足够 → 贴墙滑动，不是卡死，重置计数器继续追踪
                if (this.accumulatedMovement >= MinMovementThreshold)
                {
                    this.ColliderCount = 1;
                    this.accumulatedMovement = 0f;
                    return false;
                }

                return true; // 碰撞多 + 位移不足 → 真卡死
            }

            return false;
        }
    }
}
