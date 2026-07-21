namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class AchievementRuleServiceTests
    {
        private readonly AchievementRuleService service = new AchievementRuleService();

        [Test]
        public void GetElapsedMinutes_120Seconds_Returns2()
        {
            Assert.AreEqual(2, this.service.GetElapsedMinutes(120f));
        }

        [Test]
        public void GetElapsedMinutes_0Seconds_Returns0()
        {
            Assert.AreEqual(0, this.service.GetElapsedMinutes(0f));
        }

        [Test]
        public void GetElapsedMinutes_Negative_Returns0()
        {
            Assert.AreEqual(0, this.service.GetElapsedMinutes(-1f));
        }

        [Test]
        public void ClampProgressToTarget_UnderTarget_ReturnsProgress()
        {
            Assert.AreEqual(3, this.service.ClampProgressToTarget(3, 5));
        }

        [Test]
        public void ClampProgressToTarget_OverTarget_ReturnsTarget()
        {
            Assert.AreEqual(5, this.service.ClampProgressToTarget(7, 5));
        }

        [Test]
        public void ClampProgressToTarget_Equal_ReturnsTarget()
        {
            Assert.AreEqual(5, this.service.ClampProgressToTarget(5, 5));
        }

        [Test]
        public void GetProgressRatio_Half_Returns0_5()
        {
            Assert.AreEqual(0.5f, this.service.GetProgressRatio(5, 10), 0.0001f);
        }

        [Test]
        public void GetProgressRatio_Complete_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetProgressRatio(10, 10), 0.0001f);
        }

        [Test]
        public void GetProgressRatio_ZeroTarget_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetProgressRatio(5, 0), 0.0001f);
        }
    }
}
