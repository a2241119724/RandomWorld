namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class WeatherGameplayRuleServiceTests
    {
        private readonly WeatherGameplayRuleService service = new WeatherGameplayRuleService();

        [Test]
        public void GetPlayerMoveSpeedMultiplier_Rain_Returns0_92()
        {
            Assert.AreEqual(0.92f, this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Rain), 0.0001f);
        }

        [Test]
        public void GetPlayerMoveSpeedMultiplier_Snow_Returns0_84()
        {
            Assert.AreEqual(0.84f, this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Snow), 0.0001f);
        }

        [Test]
        public void GetPlayerMoveSpeedMultiplier_Clear_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetPlayerMoveSpeedMultiplier(WeatherType.Clear), 0.0001f);
        }

        [Test]
        public void GetWorkerMoveSpeedMultiplier_Rain_Returns0_9()
        {
            Assert.AreEqual(0.9f, this.service.GetWorkerMoveSpeedMultiplier(WeatherType.Rain), 0.0001f);
        }

        [Test]
        public void GetWorkerMoveSpeedMultiplier_Snow_Returns0_78()
        {
            Assert.AreEqual(0.78f, this.service.GetWorkerMoveSpeedMultiplier(WeatherType.Snow), 0.0001f);
        }

        [Test]
        public void GetWorkerMoveSpeedMultiplier_Clear_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetWorkerMoveSpeedMultiplier(WeatherType.Clear), 0.0001f);
        }

        [Test]
        public void GetWorkerTaskProgressMultiplier_Rain_Returns0_94()
        {
            Assert.AreEqual(0.94f, this.service.GetWorkerTaskProgressMultiplier(WeatherType.Rain), 0.0001f);
        }

        [Test]
        public void GetWorkerTaskProgressMultiplier_Snow_Returns0_82()
        {
            Assert.AreEqual(0.82f, this.service.GetWorkerTaskProgressMultiplier(WeatherType.Snow), 0.0001f);
        }

        [Test]
        public void GetEnergyRecoveryMultiplier_Rain_Returns1_12()
        {
            Assert.AreEqual(1.12f, this.service.GetEnergyRecoveryMultiplier(WeatherType.Rain), 0.0001f);
        }

        [Test]
        public void GetEnergyRecoveryMultiplier_Clear_Returns1_05()
        {
            Assert.AreEqual(1.05f, this.service.GetEnergyRecoveryMultiplier(WeatherType.Clear), 0.0001f);
        }

        [Test]
        public void GetEnergyRecoveryMultiplier_SpiritRain_Returns1_5()
        {
            Assert.AreEqual(1.5f, this.service.GetEnergyRecoveryMultiplier(WeatherType.SpiritRain), 0.0001f);
        }

        [Test]
        public void GetEnergyRecoveryMultiplier_BloodMoon_FollowsDefault()
        {
            Assert.AreEqual(1.05f, this.service.GetEnergyRecoveryMultiplier(WeatherType.BloodMoon), 0.0001f);
        }

        [Test]
        public void Multipliers_SpiritRain_MoveAndTaskUnchanged()
        {
            Assert.AreEqual(1.0f, this.service.GetPlayerMoveSpeedMultiplier(WeatherType.SpiritRain), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetWorkerMoveSpeedMultiplier(WeatherType.SpiritRain), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetWorkerTaskProgressMultiplier(WeatherType.SpiritRain), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetFatigueDecayMultiplier(WeatherType.SpiritRain), 0.0001f);
        }

        [Test]
        public void Multipliers_BloodMoon_AllDefault()
        {
            Assert.AreEqual(1.0f, this.service.GetPlayerMoveSpeedMultiplier(WeatherType.BloodMoon), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetWorkerMoveSpeedMultiplier(WeatherType.BloodMoon), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetWorkerTaskProgressMultiplier(WeatherType.BloodMoon), 0.0001f);
            Assert.AreEqual(1.05f, this.service.GetEnergyRecoveryMultiplier(WeatherType.BloodMoon), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetFatigueDecayMultiplier(WeatherType.BloodMoon), 0.0001f);
        }

        [Test]
        public void RollWeather_Boundaries()
        {
            // 权重边界：[0,40) Clear、[40,65) Rain、[65,80) Snow、[80,92) SpiritRain、[92,100) BloodMoon
            Assert.AreEqual(WeatherType.Clear, WeatherGameplayRuleService.RollWeather(0.0));
            Assert.AreEqual(WeatherType.Clear, WeatherGameplayRuleService.RollWeather(39.9));
            Assert.AreEqual(WeatherType.Rain, WeatherGameplayRuleService.RollWeather(40.0));
            Assert.AreEqual(WeatherType.Rain, WeatherGameplayRuleService.RollWeather(64.9));
            Assert.AreEqual(WeatherType.Snow, WeatherGameplayRuleService.RollWeather(65.0));
            Assert.AreEqual(WeatherType.Snow, WeatherGameplayRuleService.RollWeather(79.9));
            Assert.AreEqual(WeatherType.SpiritRain, WeatherGameplayRuleService.RollWeather(80.0));
            Assert.AreEqual(WeatherType.SpiritRain, WeatherGameplayRuleService.RollWeather(91.9));
            Assert.AreEqual(WeatherType.BloodMoon, WeatherGameplayRuleService.RollWeather(92.0));
            Assert.AreEqual(WeatherType.BloodMoon, WeatherGameplayRuleService.RollWeather(99.999));
        }

        [Test]
        public void RollWeather_OutOfRangeClamped()
        {
            Assert.AreEqual(WeatherType.Clear, WeatherGameplayRuleService.RollWeather(-5.0));
            Assert.AreEqual(WeatherType.BloodMoon, WeatherGameplayRuleService.RollWeather(150.0));
        }

        [Test]
        public void ApplyMultiplier_NormalCase()
        {
            Assert.AreEqual(50f, this.service.ApplyMultiplier(100f, 0.5f, 10f), 0.0001f);
        }

        [Test]
        public void ApplyMultiplier_ClampedToMin()
        {
            Assert.AreEqual(10f, this.service.ApplyMultiplier(5f, 2f, 10f), 0.0001f);
        }

        [Test]
        public void ApplyMultiplier_NegativeMultiplier_ReturnsMin()
        {
            Assert.AreEqual(10f, this.service.ApplyMultiplier(100f, -1f, 10f), 0.0001f);
        }
    }
}
