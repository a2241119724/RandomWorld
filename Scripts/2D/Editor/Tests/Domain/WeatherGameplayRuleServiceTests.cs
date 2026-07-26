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
