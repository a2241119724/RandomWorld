namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Player;
    using LAB2D.Enum;
    using NUnit.Framework;

    [TestFixture]
    public class PlayerVitalAlertRuleServiceTests
    {
        private PlayerVitalAlertRuleService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new PlayerVitalAlertRuleService();
        }

        [Test]
        public void GetLevel_FullHp_ReturnsSafe()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Safe, this.service.GetLevel(1.0f, false));
        }

        [Test]
        public void GetLevel_HalfHp_StillSafe()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Safe, this.service.GetLevel(0.5f, false));
        }

        [Test]
        public void GetLevel_BelowWarning_ReturnsWounded()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Wounded, this.service.GetLevel(0.30f, false));
        }

        [Test]
        public void GetLevel_AtWarningBoundary_ReturnsWounded()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Wounded, this.service.GetLevel(0.35f, false));
        }

        [Test]
        public void GetLevel_BelowCritical_ReturnsCritical()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Critical, this.service.GetLevel(0.17f, false));
        }

        [Test]
        public void GetLevel_AtCriticalBoundary_ReturnsCritical()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Critical, this.service.GetLevel(0.18f, false));
        }

        [Test]
        public void GetLevel_Respawning_ReturnsRespawning()
        {
            Assert.AreEqual(PlayerVitalAlertLevel.Respawning, this.service.GetLevel(1.0f, true));
            Assert.AreEqual(PlayerVitalAlertLevel.Respawning, this.service.GetLevel(0.1f, true));
        }

        [Test]
        public void IsDangerLevel_Safe_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsDangerLevel(PlayerVitalAlertLevel.Safe));
        }

        [Test]
        public void IsDangerLevel_WoundedCriticalRespawning_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsDangerLevel(PlayerVitalAlertLevel.Wounded));
            Assert.IsTrue(this.service.IsDangerLevel(PlayerVitalAlertLevel.Critical));
            Assert.IsTrue(this.service.IsDangerLevel(PlayerVitalAlertLevel.Respawning));
        }

        [Test]
        public void GetSeverity_IncreasingOrder()
        {
            int safe = this.service.GetSeverity(PlayerVitalAlertLevel.Safe);
            int wounded = this.service.GetSeverity(PlayerVitalAlertLevel.Wounded);
            int critical = this.service.GetSeverity(PlayerVitalAlertLevel.Critical);
            int respawning = this.service.GetSeverity(PlayerVitalAlertLevel.Respawning);
            Assert.Less(safe, wounded);
            Assert.Less(wounded, critical);
            Assert.Less(critical, respawning);
        }

        [Test]
        public void IsMoreSevere_WoundedVsSafe_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsMoreSevere(PlayerVitalAlertLevel.Wounded, PlayerVitalAlertLevel.Safe));
        }

        [Test]
        public void IsMoreSevere_SafeVsWounded_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsMoreSevere(PlayerVitalAlertLevel.Safe, PlayerVitalAlertLevel.Wounded));
        }

        [Test]
        public void ToDisplayHealth_NormalValue_RoundsUp()
        {
            int result = this.service.ToDisplayHealth(45.3f);
            Assert.AreEqual(46, result);
        }

        [Test]
        public void ToDisplayHealth_NegativeValue_ClampsToZero()
        {
            Assert.AreEqual(0, this.service.ToDisplayHealth(-10f));
        }

        [Test]
        public void GetSafeRatio_ValidInput_ReturnsCorrect()
        {
            Assert.AreEqual(0.5f, this.service.GetSafeRatio(50f, 100f), 0.0001f);
        }

        [Test]
        public void GetSafeRatio_ZeroMax_ReturnsZero()
        {
            Assert.AreEqual(0.0f, this.service.GetSafeRatio(50f, 0f), 0.0001f);
        }
    }
}
