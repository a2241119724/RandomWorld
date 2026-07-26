namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Player;
    using LAB2D.Enum;
    using NUnit.Framework;

    [TestFixture]
    public class PlayerVitalAlertRuleServiceTests
    {
        private readonly PlayerVitalAlertRuleService service = new PlayerVitalAlertRuleService();

        [Test]
        public void GetSafeRatio_Half_Returns0_5()
        {
            Assert.AreEqual(0.5f, this.service.GetSafeRatio(50f, 100f), 0.0001f);
        }

        [Test]
        public void GetSafeRatio_ZeroMax_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetSafeRatio(50f, 0f), 0.0001f);
        }

        [Test]
        public void GetSafeRatio_Full_Returns1()
        {
            Assert.AreEqual(1f, this.service.GetSafeRatio(100f, 100f), 0.0001f);
        }

        [Test]
        public void ToPercentInt_0_5_Returns50()
        {
            Assert.AreEqual(50, this.service.ToPercentInt(0.5f));
        }

        [Test]
        public void ToDisplayHealth_Negative_Returns0()
        {
            Assert.AreEqual(0, this.service.ToDisplayHealth(-10f));
        }

        [Test]
        public void ToDisplayHealth_3_7_Returns4()
        {
            Assert.AreEqual(4, this.service.ToDisplayHealth(3.7f));
        }

        [Test]
        public void GetLevel_FullHp_ReturnsSafe()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Safe, this.service.GetLevel(1f, false));
        }

        [Test]
        public void GetLevel_WarningRatio_ReturnsWounded()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Wounded, this.service.GetLevel(0.3f, false));
        }

        [Test]
        public void GetLevel_LowHp_ReturnsCritical()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Critical, this.service.GetLevel(0.1f, false));
        }

        [Test]
        public void GetLevel_Respawning_ReturnsRespawning()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Respawning, this.service.GetLevel(1f, true));
        }

        [Test]
        public void GetLevel_NegativeRatio_ReturnsCritical()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Critical, this.service.GetLevel(-0.5f, false));
        }

        [Test]
        public void IsDangerLevel_Respawning_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsDangerLevel(PlayerVitalAlertLevel.Respawning));
        }

        [Test]
        public void IsDangerLevel_Safe_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsDangerLevel(PlayerVitalAlertLevel.Safe));
        }

        [Test]
        public void IsMoreSevere_CriticalVsWounded_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsMoreSevere(PlayerVitalAlertLevel.Critical, PlayerVitalAlertLevel.Wounded));
        }

        [Test]
        public void IsMoreSevere_SameLevel_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsMoreSevere(PlayerVitalAlertLevel.Wounded, PlayerVitalAlertLevel.Wounded));
        }

        [Test]
        public void GetSeverity_Respawning_Returns3()
        {
            Assert.AreEqual(3, this.service.GetSeverity(PlayerVitalAlertLevel.Respawning));
        }

        [Test]
        public void GetSeverity_Safe_Returns0()
        {
            Assert.AreEqual(0, this.service.GetSeverity(PlayerVitalAlertLevel.Safe));
        }

        [Test]
        public void ClampRefreshInterval_Negative_ReturnsDefault()
        {
            float result = this.service.ClampRefreshInterval(-1f);
            Assert.GreaterOrEqual(result, 0f);
        }
    }
}
