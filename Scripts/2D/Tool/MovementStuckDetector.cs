namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 每秒位移卡死检测器 — 对比"窗口累计位移"与"期望位移"，判断角色是否被卡住。
    /// 纯逻辑、无 Unity 组件依赖，由寻路移动每固定帧喂入移动数据。
    /// position 传"移动前"的真实位置：MovePosition 受碰撞约束不穿透，
    /// 撞墙时位置停在约束处不再前进，累计位移≈0 → 检出 Stuck。
    /// 结算用"每帧位移之和"而非"窗口净位移"：Worker 长距离蛇形绕障碍时净位移远小于
    /// 实际路程，用净位移会误判 Sliding → 正常走路一卡一卡（见 bug-fixes.md 2026-08-15）。
    /// </summary>
    public sealed class MovementStuckDetector
    {
        /// <summary>检测窗口时长（秒）。</summary>
        public float WindowSeconds = 1.0f;

        /// <summary>位移比例阈值 — 实际位移 &lt; 期望 * 此值 → Sliding（位移不足）。</summary>
        public float SlidingRatio = 0.4f;

        /// <summary>位移比例阈值 — 实际位移 &lt; 期望 * 此值 → 硬卡死窗口。</summary>
        public float StuckRatio = 0.15f;

        /// <summary>窗口内期望位移低于此值不判定（短路径/到终点/极慢）。</summary>
        public float MinExpectedDistance = 0.5f;

        /// <summary>连续硬卡死窗口数阈值 → 判定真卡死。</summary>
        public int StuckWindowThreshold = 2;

        /// <summary>最近一次结算的位移比例（调试/验证用）。</summary>
        public float LastRatio { get; private set; }

        /// <summary>最近一次结算的检测结果。</summary>
        public BugCheckResult LastResult => this.lastResult;

        private float windowRemaining;
        private float expectedDistance;
        private float actualDistance; // 窗口内累计位移（每帧位移之和）
        private Vector3 prevPosition; // 上一帧位置，用于累计本帧位移
        private bool hasWindowStart;
        private int blockedWindowStreak;
        private BugCheckResult lastResult = BugCheckResult.None;

        /// <summary>
        /// 完全清空：清空窗口与连续卡住计数（到达终点/放弃任务时调用）。
        /// </summary>
        public void Reset()
        {
            this.windowRemaining = 0f;
            this.expectedDistance = 0f;
            this.actualDistance = 0f;
            this.hasWindowStart = false;
            this.blockedWindowStreak = 0;
            this.lastResult = BugCheckResult.None;
        }

        /// <summary>
        /// 重启窗口：新路径开始新窗口，但保留连续卡住计数（重新寻路/停止移动时调用）。
        /// 否则 Sliding 一触发就重新寻路、计数被清空，Stuck 永远不会到达。
        /// </summary>
        public void RestartWindow()
        {
            this.windowRemaining = 0f;
            this.expectedDistance = 0f;
            this.actualDistance = 0f;
            this.hasWindowStart = false;
            this.lastResult = BugCheckResult.None;
        }

        /// <summary>
        /// 每固定帧喂入一次移动数据。
        /// </summary>
        /// <param name="deltaTime">本帧步长（Time.fixedDeltaTime）。</param>
        /// <param name="position">本次移动前的实际位置（物理结算后）。</param>
        /// <param name="expectedSpeed">本帧期望速度（含天气/地形/状态倍率）。</param>
        /// <returns>窗口到期时的结算结果；未到期返回上次结果。</returns>
        public BugCheckResult Feed(float deltaTime, Vector3 position, float expectedSpeed)
        {
            if (deltaTime <= 0f)
            {
                return this.lastResult;
            }

            if (this.windowRemaining <= 0f)
            {
                this.windowRemaining = this.WindowSeconds;
                this.expectedDistance = 0f;
                this.actualDistance = 0f;
                this.hasWindowStart = false;
            }

            if (this.hasWindowStart)
            {
                // 累计每帧位移（prev→cur）：正常蛇形绕路每帧仍前进，累计≈实际路程；
                // 真卡死（撞墙穿透→物理推回→回到原点）每帧位移≈0 → 累计≈0 → 检出 Stuck。
                this.actualDistance += Vector3.Distance(this.prevPosition, position);
            }

            this.prevPosition = position;
            this.hasWindowStart = true;
            this.expectedDistance += expectedSpeed * deltaTime;
            this.windowRemaining -= deltaTime;

            if (this.windowRemaining > 0f)
            {
                return this.lastResult = BugCheckResult.None;
            }

            // 窗口到期结算：用窗口内累计位移（每帧位移之和）对比期望位移。
            // 修复（2026-08-15）：原用窗口起点→终点的净位移，Worker 长距离蛇形绕障碍时
            // 净位移远小于路程 → 误判 Sliding → 正常走路一卡一卡（日志观测 32% Sliding
            // ratio 落在 0.15-0.4，样本 pathIdx 推进、位置移动但被判位移不足）。
            BugCheckResult result;
            if (this.expectedDistance < this.MinExpectedDistance)
            {
                this.blockedWindowStreak = 0;
                result = BugCheckResult.None;
            }
            else
            {
                float ratio = this.actualDistance / this.expectedDistance;
                this.LastRatio = ratio;
                if (ratio < this.SlidingRatio)
                {
                    this.blockedWindowStreak++;
                    bool hardStuck = ratio < this.StuckRatio;
                    result = (hardStuck && this.blockedWindowStreak >= this.StuckWindowThreshold)
                        ? BugCheckResult.Stuck
                        : BugCheckResult.Sliding;
                }
                else
                {
                    this.blockedWindowStreak = 0; // 有实质进展，清零
                    result = BugCheckResult.None;
                }
            }

            this.lastResult = result;
            this.windowRemaining = this.WindowSeconds;
            this.expectedDistance = 0f;
            this.actualDistance = 0f;
            this.hasWindowStart = false;
            return result;
        }
    }
}
