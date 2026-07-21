namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Wave;
    using NUnit.Framework;

    [TestFixture]
    public class WaveRuleServiceTests
    {
        private readonly WaveRuleService service = new WaveRuleService();

        private static WaveConfigModel EmptyConfig()
        {
            return new WaveConfigModel();
        }

        private static WaveConfigModel TestConfig()
        {
            return new WaveConfigModel
            {
                BaseEnemyCount = 5,
                EnemiesPerWaveIncrease = 2,
                TotalWaves = 10,
                DifficultyScalePerWave = 0.1f,
                MaxAliveEnemies = 15,
            };
        }

        [Test]
        public void GetDifficultyScale_Wave5_Returns1_5()
        {
            Assert.AreEqual(1.5f, this.service.GetDifficultyScale(5, TestConfig()), 0.0001f);
        }

        [Test]
        public void GetDifficultyScale_ZeroWaves_Returns1()
        {
            Assert.AreEqual(1f, this.service.GetDifficultyScale(0, TestConfig()), 0.0001f);
        }

        [Test]
        public void GetDifficultyScale_NullConfig_Returns1()
        {
            Assert.AreEqual(1f, this.service.GetDifficultyScale(5, null), 0.0001f);
        }

        [Test]
        public void AreAllWavesCleared_AllCleared_ReturnsTrue()
        {
            Assert.IsTrue(this.service.AreAllWavesCleared(10, TestConfig()));
        }

        [Test]
        public void AreAllWavesCleared_NotAll_ReturnsFalse()
        {
            Assert.IsFalse(this.service.AreAllWavesCleared(5, TestConfig()));
        }

        [Test]
        public void AreAllWavesCleared_ZeroTotalWaves_ReturnsFalse()
        {
            Assert.IsFalse(this.service.AreAllWavesCleared(10, EmptyConfig()));
        }

        [Test]
        public void GetEnemyCountForWave_Wave3_Returns9()
        {
            Assert.AreEqual(9, this.service.GetEnemyCountForWave(3, TestConfig()));
        }

        [Test]
        public void GetEnemyCountForWave_Wave1_ReturnsBase()
        {
            Assert.AreEqual(5, this.service.GetEnemyCountForWave(1, TestConfig()));
        }

        [Test]
        public void GetEnemyCountForWave_Wave0_ClampsTo1()
        {
            Assert.AreEqual(1, this.service.GetEnemyCountForWave(0, TestConfig()));
        }

        [Test]
        public void GetEffectiveMaxAliveEnemies_ConfigLessThanRuntime_ReturnsConfig()
        {
            Assert.AreEqual(15, this.service.GetEffectiveMaxAliveEnemies(15, 30));
        }

        [Test]
        public void GetEffectiveMaxAliveEnemies_RuntimeSmaller_ReturnsRuntime()
        {
            Assert.AreEqual(15, this.service.GetEffectiveMaxAliveEnemies(30, 15));
        }

        [Test]
        public void IsWaveCleared_SpawnedWithNoEnemies_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsWaveCleared(5, 0, 1));
        }

        [Test]
        public void IsWaveCleared_StillAlive_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsWaveCleared(5, 2, 1));
        }

        [Test]
        public void IsWaveCleared_NoneSpawned_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsWaveCleared(0, 0, 0));
        }

        [Test]
        public void GetRemainingRestTime_MidRest_ReturnsHalf()
        {
            Assert.AreEqual(5f, this.service.GetRemainingRestTime(10f, 5f), 0.0001f);
        }

        [Test]
        public void GetRemainingRestTime_PastRest_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetRemainingRestTime(10f, 15f), 0.0001f);
        }
    }
}
