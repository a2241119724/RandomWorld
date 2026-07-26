namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Player;
    using NUnit.Framework;

    /// <summary>
    /// PlayerMovementPolicy 单元测试 — 验证跑步倍率钳制和应用逻辑。
    /// </summary>
    [TestFixture]
    public class PlayerMovementPolicyTests
    {
        private PlayerMovementPolicy policy;

        [SetUp]
        public void SetUp()
        {
            this.policy = new PlayerMovementPolicy();
        }

        // --- ClampRunSpeedMultiplier ---

        [Test]
        public void ClampRunSpeedMultiplier_Negative_ClampsToOne()
        {
            Assert.AreEqual(1.0f, this.policy.ClampRunSpeedMultiplier(-1.0f), 0.0001f);
            Assert.AreEqual(1.0f, this.policy.ClampRunSpeedMultiplier(-5.0f), 0.0001f);
        }

        [Test]
        public void ClampRunSpeedMultiplier_Zero_ClampsToOne()
        {
            Assert.AreEqual(1.0f, this.policy.ClampRunSpeedMultiplier(0.0f), 0.0001f);
        }

        [Test]
        public void ClampRunSpeedMultiplier_BelowOne_ClampsToOne()
        {
            Assert.AreEqual(1.0f, this.policy.ClampRunSpeedMultiplier(0.5f), 0.0001f);
            Assert.AreEqual(1.0f, this.policy.ClampRunSpeedMultiplier(0.99f), 0.0001f);
        }

        [Test]
        public void ClampRunSpeedMultiplier_ExactlyOne_ReturnsOne()
        {
            Assert.AreEqual(1.0f, this.policy.ClampRunSpeedMultiplier(1.0f), 0.0001f);
        }

        [Test]
        public void ClampRunSpeedMultiplier_AboveOne_ReturnsOriginal()
        {
            Assert.AreEqual(1.6f, this.policy.ClampRunSpeedMultiplier(1.6f), 0.0001f);
            Assert.AreEqual(2.5f, this.policy.ClampRunSpeedMultiplier(2.5f), 0.0001f);
        }

        // --- ApplyRunMultiplier ---

        [Test]
        public void ApplyRunMultiplier_NotRunning_ReturnsBaseSpeed()
        {
            Assert.AreEqual(5.0f, this.policy.ApplyRunMultiplier(5.0f, false, 1.6f), 0.0001f);
            Assert.AreEqual(3.0f, this.policy.ApplyRunMultiplier(3.0f, false, 2.0f), 0.0001f);
        }

        [Test]
        public void ApplyRunMultiplier_Running_AppliesMultiplier()
        {
            Assert.AreEqual(8.0f, this.policy.ApplyRunMultiplier(5.0f, true, 1.6f), 0.0001f);
            Assert.AreEqual(6.0f, this.policy.ApplyRunMultiplier(3.0f, true, 2.0f), 0.0001f);
        }

        [Test]
        public void ApplyRunMultiplier_RunningZeroMultiplier_ClampsToOneAndApplies()
        {
            // 倍率 0 被钳制为 1.0，速度不变
            Assert.AreEqual(5.0f, this.policy.ApplyRunMultiplier(5.0f, true, 0.0f), 0.0001f);
        }

        [Test]
        public void ApplyRunMultiplier_RunningNegativeMultiplier_ClampsToOneAndApplies()
        {
            Assert.AreEqual(5.0f, this.policy.ApplyRunMultiplier(5.0f, true, -1.0f), 0.0001f);
        }

        [Test]
        public void ApplyRunMultiplier_RunningFractionalMultiplier_ClampsToOneAndApplies()
        {
            // 0.5 < 1.0 被钳制，等同于 x1.0
            Assert.AreEqual(5.0f, this.policy.ApplyRunMultiplier(5.0f, true, 0.5f), 0.0001f);
        }
    }
}
