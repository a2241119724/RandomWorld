namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class GameplaySessionStatsRuleServiceTests
    {
        private readonly GameplaySessionStatsRuleService service = new GameplaySessionStatsRuleService();

        [Test]
        public void ClampComboTimeout_Valid_ReturnsSame()
        {
            Assert.AreEqual(3f, this.service.ClampComboTimeout(3f), 0.0001f);
        }

        [Test]
        public void ClampComboTimeout_TooSmall_Returns0_1()
        {
            Assert.AreEqual(0.1f, this.service.ClampComboTimeout(0.05f), 0.0001f);
        }

        [Test]
        public void ToRecordedDamage_Positive_ReturnsRounded()
        {
            Assert.AreEqual(25, this.service.ToRecordedDamage(25.4f));
        }

        [Test]
        public void ToRecordedDamage_Negative_Returns0()
        {
            Assert.AreEqual(0, this.service.ToRecordedDamage(-10f));
        }

        [Test]
        public void GetSessionDuration_Normal_ReturnsDuration()
        {
            Assert.AreEqual(60f, this.service.GetSessionDuration(120f, 60f), 0.0001f);
        }

        [Test]
        public void GetSessionDuration_Negative_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetSessionDuration(50f, 100f), 0.0001f);
        }

        [Test]
        public void GetNextCombo_WithinTimeout_ReturnsIncremented()
        {
            Assert.AreEqual(4, this.service.GetNextCombo(10f, 8f, 3f, 3));
        }

        [Test]
        public void GetNextCombo_ExceededTimeout_Returns1()
        {
            Assert.AreEqual(1, this.service.GetNextCombo(10f, 5f, 3f, 5));
        }

        [Test]
        public void GetMaxCombo_NewHigher_ReturnsNew()
        {
            Assert.AreEqual(10, this.service.GetMaxCombo(5, 10));
        }

        [Test]
        public void GetMaxCombo_CurrentHigher_ReturnsCurrent()
        {
            Assert.AreEqual(10, this.service.GetMaxCombo(10, 5));
        }

        [Test]
        public void ToClampedScore_WithinRange_ReturnsRounded()
        {
            Assert.AreEqual(50, this.service.ToClampedScore(49.6f, 0, 100));
        }

        [Test]
        public void ToClampedScore_BelowMin_ReturnsMin()
        {
            Assert.AreEqual(0, this.service.ToClampedScore(-10f, 0, 100));
        }

        [Test]
        public void ToClampedScore_AboveMax_ReturnsMax()
        {
            Assert.AreEqual(100, this.service.ToClampedScore(200f, 0, 100));
        }
    }
}
