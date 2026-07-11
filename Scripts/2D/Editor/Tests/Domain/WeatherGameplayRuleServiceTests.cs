namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class WeatherGameplayRuleServiceTests
    {
        private WeatherGameplayRuleService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new WeatherGameplayRuleService();
        }

        [Test]
        public void GetPlayerMoveSpeedMultiplier_Clear_ReturnsDefault()
        {
            float result = this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Clear);
            Assert.AreEqual(1.0f, result, 0.001f);
        }

        [Test]
        public void GetPlayerMoveSpeedMultiplier_Rain_ReducesSpeed()
        {
            float result = this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Rain);
            Assert.Less(result, 1.0f, "雨天应降低玩家移速");
        }

        [Test]
        public void GetPlayerMoveSpeedMultiplier_Snow_ReducesMoreThanRain()
        {
            float rainResult = this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Rain);
            float snowResult = this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Snow);
            Assert.Less(snowResult, rainResult, "雪天降低应比雨天更多");
        }

        [Test]
        public void GetEnergyRecoveryMultiplier_Rain_IncreasesRecovery()
        {
            float result = this.service.GetEnergyRecoveryMultiplier(WeatherType.Rain);
            Assert.Greater(result, 1.0f, "雨天应提高灵气恢复");
        }

        [Test]
        public void GetEnergyRecoveryMultiplier_Snow_ReducesRecovery()
        {
            float result = this.service.GetEnergyRecoveryMultiplier(WeatherType.Snow);
            Assert.Less(result, 1.0f, "雪天应降低灵气恢复");
        }

        [Test]
        public void ApplyMultiplier_ValidInput_ReturnsCorrectValue()
        {
            float result = this.service.ApplyMultiplier(100f, 0.5f, 10f);
            Assert.AreEqual(50f, result, 0.001f);
        }

        [Test]
        public void ApplyMultiplier_BelowMinimum_ClampsToMin()
        {
            float result = this.service.ApplyMultiplier(100f, 0.05f, 10f);
            Assert.AreEqual(10f, result, 0.001f, "低于最小值时应夹紧到最小值");
        }

        [Test]
        public void ApplyMultiplier_NegativeMultiplier_ClampsToZero()
        {
            float result = this.service.ApplyMultiplier(100f, -0.5f, 10f);
            Assert.AreEqual(10f, result, 0.001f, "负倍率应被视为0并夹紧到最小值");
        }
    }
}
