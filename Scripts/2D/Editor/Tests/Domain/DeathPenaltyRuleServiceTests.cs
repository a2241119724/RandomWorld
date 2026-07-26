namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class DeathPenaltyRuleServiceTests
    {
        private readonly DeathPenaltyRuleService service = new DeathPenaltyRuleService();

        [Test]
        public void IsRespawning_DeadlinePositiveAndBeforeDeadline_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsRespawning(100f, 50f));
        }

        [Test]
        public void IsRespawning_DeadlineNegative_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsRespawning(0f, 50f));
        }

        [Test]
        public void IsRespawning_AfterDeadline_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsRespawning(50f, 100f));
        }

        [Test]
        public void GetRespawnRemaining_BeforeDeadline_ReturnsRemaining()
        {
            Assert.AreEqual(30f, this.service.GetRespawnRemaining(100f, 70f), 0.0001f);
        }

        [Test]
        public void GetRespawnRemaining_AfterDeadline_ReturnsZero()
        {
            Assert.AreEqual(0f, this.service.GetRespawnRemaining(50f, 100f), 0.0001f);
        }

        [Test]
        public void GetRespawnDeadline_ReturnsSum()
        {
            Assert.AreEqual(130f, this.service.GetRespawnDeadline(100f, 30f), 0.0001f);
        }

        [Test]
        public void GetExperienceLoss_AppliesPercent()
        {
            Assert.AreEqual(20, this.service.GetExperienceLoss(100, 0.2f));
        }

        [Test]
        public void ApplyExperienceLoss_EnoughExperience_ReturnsReduced()
        {
            Assert.AreEqual(80, this.service.ApplyExperienceLoss(100, 20));
        }

        [Test]
        public void ApplyExperienceLoss_NotEnoughExperience_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.ApplyExperienceLoss(10, 20));
        }

        [Test]
        public void ToCountdownSeconds_RoundsUp()
        {
            Assert.AreEqual(4, this.service.ToCountdownSeconds(3.1f));
        }

        [Test]
        public void GetRestoredHp_ReturnsFraction()
        {
            Assert.AreEqual(30f, this.service.GetRestoredHp(100f, 0.3f), 0.0001f);
        }
    }
}
