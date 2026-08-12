namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 碰撞 Bug 检测结果。
    /// </summary>
    public enum BugCheckResult
    {
        /// <summary>未触发检测。</summary>
        None = 0,

        /// <summary>贴墙滑动 — 碰撞多但有位移，建议重新寻路绕开障碍。</summary>
        Sliding = 1,

        /// <summary>真卡死 — 碰撞多且位移不足，需放弃当前路径/任务。</summary>
        Stuck = 2,
    }

    /// <summary>
    /// 碰撞 Bug 检测器 — 检测角色是否因异常高频碰撞而卡死。
    /// 通过追踪碰撞期间的位移来区分"贴墙滑动"（正常移动）和"原地撞墙"（真卡死）。
    /// </summary>
    public sealed class CollisionBugDetector
    {
        /// <summary>碰撞间隔阈值（Ticks），约 20ms。超过此间隔重置计数器。</summary>
        private const double IntervalTicks = 2e5;

        /// <summary>连续碰撞次数阈值，约 0.5 秒内触发检测（30次 × ~17ms物理帧）。</summary>
        private const int DefaultThreshold = 30;

        /// <summary>
        /// 碰撞期间累计位移超过此值即视为贴墙滑动（正常），触发预防性重新寻路。
        /// </summary>
        private const float MinMovementThreshold = 1.0f;

        /// <summary>
        /// 半卡住时间阈值（秒）：碰撞持续时间超过此值，即使位移足够也建议重新寻路。
        /// </summary>
        private const float StuckTimeThreshold = 3.0f;

        /// <summary>
        /// 上一次碰撞时间（DateTime.Ticks）。
        /// </summary>
        public long LastTime { get; set; }

        /// <summary>
        /// 连续碰撞次数。
        /// </summary>
        public int ColliderCount { get; set; }

        /// <summary>
        /// 第一次碰撞的时间（Time.time），用于时间维度卡住检测。
        /// </summary>
        private float firstCollisionTime;

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
                this.firstCollisionTime = Time.time;
            }

            this.lastPosition = currentPosition;
            this.LastTime = timeTicks;
        }

        /// <summary>
        /// 检测碰撞 Bug 状态。
        /// </summary>
        /// <param name="name">角色名称（调试用，当前未输出日志）。</param>
        /// <param name="threshold">碰撞阈值，默认 30 次。</param>
        /// <returns>
        /// Stuck — 碰撞多 + 位移不足，真卡死；
        /// Sliding — 碰撞多 + 位移足够（贴墙滑动），或碰撞持续时间过长，建议重新寻路；
        /// None — 未触发。
        /// </returns>
        public BugCheckResult CheckBug(string name, int threshold = DefaultThreshold)
        {
            if (this.ColliderCount > threshold)
            {
                // 碰撞多但位移足够 → 贴墙滑动，不是卡死，但建议重新寻路绕开障碍
                if (this.accumulatedMovement >= MinMovementThreshold)
                {
                    this.ColliderCount = 1;
                    this.accumulatedMovement = 0f;
                    this.firstCollisionTime = Time.time;
                    return BugCheckResult.Sliding;
                }

                return BugCheckResult.Stuck; // 碰撞多 + 位移不足 → 真卡死
            }

            // 时间维度检测：碰撞持续时间超过阈值但未达到计数阈值 → 低强度持续碰撞
            if (this.ColliderCount > 10
                && Time.time - this.firstCollisionTime > StuckTimeThreshold
                && this.accumulatedMovement < MinMovementThreshold * 2.0f)
            {
                this.ColliderCount = 1;
                this.accumulatedMovement = 0f;
                this.firstCollisionTime = Time.time;
                return BugCheckResult.Sliding; // 长时间低强度碰撞 → 建议重新寻路
            }

            return BugCheckResult.None;
        }

        /// <summary>
        /// 是否有碰撞 Bug（连续碰撞次数超过阈值 AND 累计位移不足）。
        /// 保留此方法以兼容旧调用方。
        /// </summary>
        /// <param name="name">角色名称。</param>
        /// <param name="threshold">碰撞阈值，默认 30 次。</param>
        /// <returns>是否超过阈值且位移不足（真卡死）。</returns>
        public bool IsBug(string name, int threshold = DefaultThreshold)
        {
            return this.CheckBug(name, threshold) == BugCheckResult.Stuck;
        }
    }
}
