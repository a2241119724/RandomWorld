namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Player;
    using NUnit.Framework;

    [TestFixture]
    public class PlayerDamagePolicyTests
    {
        private PlayerDamagePolicy policy;

        [SetUp]
        public void SetUp()
        {
            this.policy = new PlayerDamagePolicy();
        }

        [Test]
        public void ClampInvincibilityDuration_Negative_ClampsToZero()
        {
            Assert.AreEqual(0f, this.policy.ClampInvincibilityDuration(-1f), 0.0001f);
            Assert.AreEqual(0f, this.policy.ClampInvincibilityDuration(-0.1f), 0.0001f);
        }

        [Test]
        public void ClampInvincibilityDuration_Zero_PassesThrough()
        {
            Assert.AreEqual(0f, this.policy.ClampInvincibilityDuration(0f), 0.0001f);
        }

        [Test]
        public void ClampInvincibilityDuration_Positive_PassesThrough()
        {
            Assert.AreEqual(0.5f, this.policy.ClampInvincibilityDuration(0.5f), 0.0001f);
            Assert.AreEqual(2f, this.policy.ClampInvincibilityDuration(2f), 0.0001f);
        }

        [Test]
        public void IsInvincible_WithinWindow_ReturnsTrue()
        {
            bool result = this.policy.IsInvincible(1.0f, 0.6f, 0.5f);
            Assert.IsTrue(result, "1.0 - 0.6 = 0.4 < 0.5 应在无敌帧内");
        }

        [Test]
        public void IsInvincible_ExactlyAtEdge_ReturnsFalse()
        {
            // 窗口左闭右开（与 DayNight 相位/波次编号同惯例）：恰耗尽 0.5s 已出无敌帧
            bool result = this.policy.IsInvincible(1.0f, 0.5f, 0.5f);
            Assert.IsFalse(result, "刚好等于无敌帧时长应已出无敌帧（边界不含）");
        }

        [Test]
        public void IsInvincible_AfterWindow_ReturnsFalse()
        {
            bool result = this.policy.IsInvincible(2.0f, 0.5f, 0.5f);
            Assert.IsFalse(result, "2.0 - 0.5 = 1.5 > 0.5 应不在无敌帧内");
        }

        [Test]
        public void IsInvincible_ZeroDuration_ReturnsFalse()
        {
            bool result = this.policy.IsInvincible(1.0f, 0.8f, 0f);
            Assert.IsFalse(result, "无敌帧持续时间为0时应始终不无敌");
        }

        [Test]
        public void IsInvincible_AncientLastDamage_ReturnsFalse()
        {
            bool result = this.policy.IsInvincible(100f, -99f, 0.5f);
            Assert.IsFalse(result, "远古受击时间应不触发无敌帧");
        }

        [Test]
        public void ShouldIgnoreDamage_ZeroDamage_ReturnsTrue()
        {
            Assert.IsTrue(this.policy.ShouldIgnoreDamage(0f, false, 1f, 0f, 0.5f));
            Assert.IsTrue(this.policy.ShouldIgnoreDamage(-5f, false, 1f, 0f, 0.5f));
        }

        [Test]
        public void ShouldIgnoreDamage_Respawning_ReturnsTrue()
        {
            Assert.IsTrue(this.policy.ShouldIgnoreDamage(10f, true, 1f, 0f, 0.5f));
        }

        [Test]
        public void ShouldIgnoreDamage_Invincible_ReturnsTrue()
        {
            Assert.IsTrue(this.policy.ShouldIgnoreDamage(10f, false, 1f, 0.7f, 0.5f));
        }

        [Test]
        public void ShouldIgnoreDamage_NormalDamage_ReturnsFalse()
        {
            Assert.IsFalse(this.policy.ShouldIgnoreDamage(10f, false, 2f, 1f, 0.5f));
        }

        [Test]
        public void ShouldIgnoreDamage_RespawningAndInvincible_PrioritizesRespawning()
        {
            Assert.IsTrue(this.policy.ShouldIgnoreDamage(10f, true, 1f, 0.9f, 0.5f));
        }
    }
}
