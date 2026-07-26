namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class SessionResultRuleServiceTests
    {
        private readonly SessionResultRuleService service = new SessionResultRuleService();

        [Test]
        public void CalculateCriticalHitRate_ZeroHits_Returns0()
        {
            Assert.AreEqual(0f, this.service.CalculateCriticalHitRate(5, 0), 0.0001f);
        }

        [Test]
        public void CalculateCriticalHitRate_HalfCriticals_Returns50Percent()
        {
            float rate = this.service.CalculateCriticalHitRate(5, 100);
            Assert.LessOrEqual(rate, 100f);
            Assert.Greater(rate, 0f);
        }

        [Test]
        public void CalculateDamageEfficiency_NormalRatio_ReturnsRatio()
        {
            Assert.AreEqual(2.0f, this.service.CalculateDamageEfficiency(200, 100), 0.0001f);
        }

        [Test]
        public void CalculateDamageEfficiency_ZeroTaken_ReturnsDamageDealt()
        {
            Assert.AreEqual(500f, this.service.CalculateDamageEfficiency(500, 0), 0.0001f);
        }

        [Test]
        public void CalculateKillScore_10Kills_Returns1000()
        {
            Assert.AreEqual(1000f, this.service.CalculateKillScore(10), 0.0001f);
        }

        [Test]
        public void CalculateKillScore_100Kills_CappedAt3500()
        {
            Assert.AreEqual(3500f, this.service.CalculateKillScore(100), 0.0001f);
        }

        [Test]
        public void CalculateKillScore_Negative_TreatsAsZero()
        {
            Assert.AreEqual(0f, this.service.CalculateKillScore(-5), 0.0001f);
        }

        [Test]
        public void CalculateComboScore_10Combo_Returns500()
        {
            Assert.AreEqual(500f, this.service.CalculateComboScore(10), 0.0001f);
        }

        [Test]
        public void CalculateComboScore_100Combo_CappedAt2500()
        {
            Assert.AreEqual(2500f, this.service.CalculateComboScore(100), 0.0001f);
        }

        [Test]
        public void CalculateSurvivalScore_Survived_Returns2000()
        {
            Assert.AreEqual(2000f, this.service.CalculateSurvivalScore(true, 0), 0.0001f);
        }

        [Test]
        public void CalculateSurvivalScore_1Death_Returns1500()
        {
            Assert.AreEqual(1500f, this.service.CalculateSurvivalScore(false, 1), 0.0001f);
        }

        [Test]
        public void CalculateSurvivalScore_ManyDeaths_FloorZero()
        {
            Assert.AreEqual(0f, this.service.CalculateSurvivalScore(false, 10), 0.0001f);
        }

        [Test]
        public void CalculateEfficiencyScore_5efficiency_Returns1500()
        {
            Assert.AreEqual(1500f, this.service.CalculateEfficiencyScore(5f), 0.0001f);
        }

        [Test]
        public void CalculateCollectionScore_50Items_Returns250()
        {
            Assert.AreEqual(250f, this.service.CalculateCollectionScore(50), 0.0001f);
        }

        [Test]
        public void CalculateCombatScore_TypicalGame_ReturnsModerateScore()
        {
            int score = this.service.CalculateCombatScore(
                totalDefeatedEnemyCount: 20,
                maxCombo: 15,
                hasSurvived: true,
                playerDeathCount: 0,
                damageEfficiency: 2.0f,
                totalCollectedItemCount: 30);
            Assert.Greater(score, 0);
            Assert.LessOrEqual(score, 10000);
        }

        [Test]
        public void CalculateCombatScore_ZeroEverything_Returns0()
        {
            Assert.AreEqual(0, this.service.CalculateCombatScore(0, 0, false, 0, 0f, 0));
        }

        [Test]
        public void GetStarRating_9000_Returns5()
        {
            Assert.AreEqual(5, this.service.GetStarRating(9000));
        }

        [Test]
        public void GetStarRating_7000_Returns4()
        {
            Assert.AreEqual(4, this.service.GetStarRating(7000));
        }

        [Test]
        public void GetStarRating_5000_Returns3()
        {
            Assert.AreEqual(3, this.service.GetStarRating(5000));
        }

        [Test]
        public void GetStarRating_3000_Returns2()
        {
            Assert.AreEqual(2, this.service.GetStarRating(3000));
        }

        [Test]
        public void GetStarRating_1000_Returns1()
        {
            Assert.AreEqual(1, this.service.GetStarRating(1000));
        }

        [Test]
        public void GetGradeText_9000_ReturnsS()
        {
            Assert.AreEqual("S", this.service.GetGradeText(9000));
        }

        [Test]
        public void GetGradeText_7000_ReturnsA()
        {
            Assert.AreEqual("A", this.service.GetGradeText(7000));
        }

        [Test]
        public void GetGradeText_5000_ReturnsB()
        {
            Assert.AreEqual("B", this.service.GetGradeText(5000));
        }

        [Test]
        public void GetGradeText_3000_ReturnsC()
        {
            Assert.AreEqual("C", this.service.GetGradeText(3000));
        }

        [Test]
        public void GetGradeText_500_ReturnsD()
        {
            Assert.AreEqual("D", this.service.GetGradeText(500));
        }
    }
}
