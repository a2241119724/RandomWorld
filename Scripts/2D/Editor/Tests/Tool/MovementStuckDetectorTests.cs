namespace LAB2D.Editor.Tests.Tool
{
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// MovementStuckDetector 纯逻辑单测：
    /// 正常移动不卡死、原地卡住先 Sliding 后 Stuck、慢速不误判、
    /// RestartWindow 保留连续计数 / Reset 清空。
    /// 窗口时长 1.0s / dt 0.02f 在 float32 累减下有舍入漂移，
    /// 窗口实际在第 51 次 feed 结算——循环次数必须是 51 而非 50，
    /// 否则断言读到的是未结算的 None（空洞通过/失败）。
    /// </summary>
    [TestFixture]
    public class MovementStuckDetectorTests
    {
        private readonly MovementStuckDetector detector = new MovementStuckDetector();

        /// <summary>
        /// 正常移动：每帧实际位移 ≈ 期望位移 → 窗口结算为 None。
        /// speed=6, dt=0.02 → 每帧期望 0.12。
        /// </summary>
        [Test]
        public void Feed_NormalMovement_ReturnsNone()
        {
            Vector3 pos = Vector3.zero;
            for (int i = 0; i < 51; i++)
            {
                pos += new Vector3(0.12f, 0f, 0f);
                this.detector.Feed(0.02f, pos, 6f);
            }

            Assert.AreEqual(BugCheckResult.None, this.detector.LastResult);
        }

        /// <summary>
        /// 原地不动（卡墙）：第 1 个窗口 → Sliding（预防性重寻路）；
        /// 模拟重寻路 RestartWindow 后仍卡住 → 第 2 个窗口 → Stuck（放弃任务）。
        /// </summary>
        [Test]
        public void Feed_StuckInPlace_FirstWindowSlidingThenStuck()
        {
            for (int i = 0; i < 51; i++)
            {
                this.detector.Feed(0.02f, Vector3.zero, 6f);
            }

            Assert.AreEqual(BugCheckResult.Sliding, this.detector.LastResult);

            // 模拟重新寻路：重启窗口但保留连续卡住计数（重寻路不赦免）
            this.detector.RestartWindow();
            Assert.AreEqual(BugCheckResult.None, this.detector.LastResult);

            for (int i = 0; i < 51; i++)
            {
                this.detector.Feed(0.02f, Vector3.zero, 6f);
            }

            Assert.AreEqual(BugCheckResult.Stuck, this.detector.LastResult);
        }

        /// <summary>
        /// 慢速移动不误判：期望速度按同倍率计算（speed=6×0.58=3.48），
        /// 比例只反映"物理是否真挡"，慢速忠诚移动比例 ≈ 1.0。
        /// </summary>
        [Test]
        public void Feed_SlowMovement_ReturnsNone()
        {
            Vector3 pos = Vector3.zero;
            for (int i = 0; i < 51; i++)
            {
                pos += new Vector3(0.0696f, 0f, 0f); // speed=3.48, dt=0.02 → 每帧 0.0696
                this.detector.Feed(0.02f, pos, 3.48f);
            }

            Assert.AreEqual(BugCheckResult.None, this.detector.LastResult);
        }

        /// <summary>
        /// Reset 清空连续卡住计数：Reset 后重新卡住 → 连续计数从 0 重新累计，
        /// 第 1 个窗口仍是 Sliding 而非 Stuck（避免放弃任务后污染下一任务）。
        /// </summary>
        [Test]
        public void Reset_ClearsBlockedStreak()
        {
            for (int i = 0; i < 51; i++)
            {
                this.detector.Feed(0.02f, Vector3.zero, 6f);
            }

            Assert.AreEqual(BugCheckResult.Sliding, this.detector.LastResult);

            this.detector.Reset();

            for (int i = 0; i < 51; i++)
            {
                this.detector.Feed(0.02f, Vector3.zero, 6f);
            }

            // streak 从 0 重新累计，1 < StuckWindowThreshold(2) → 仍为 Sliding
            Assert.AreEqual(BugCheckResult.Sliding, this.detector.LastResult);
        }
    }
}
