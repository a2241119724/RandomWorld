namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Wave;
    using LAB2D.Enum;
    using NUnit.Framework;

    [TestFixture]
    public class WaveBossRuleServiceTests
    {
        private readonly WaveBossRuleService service = new WaveBossRuleService();

        [Test]
        public void IsBossWave_Wave5_Interval5_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsBossWave(5, 5));
        }

        [Test]
        public void IsBossWave_Wave10_Interval5_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsBossWave(10, 5));
        }

        [Test]
        public void IsBossWave_Wave3_Interval5_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsBossWave(3, 5));
        }

        [Test]
        public void IsBossWave_Wave0_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsBossWave(0, 5));
        }

        [Test]
        public void IsBossWave_ZeroInterval_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsBossWave(5, 0));
        }

        [Test]
        public void GetEnemyCountForWave_BossWave_AddsGuardians()
        {
            Assert.AreEqual(13, this.service.GetEnemyCountForWave(10, 5, 5, 3));
        }

        [Test]
        public void GetEnemyCountForWave_NonBossWave_NoGuardians()
        {
            Assert.AreEqual(10, this.service.GetEnemyCountForWave(10, 4, 5, 3));
        }

        [Test]
        public void GetEnemyCountForWave_NegativeGuardianCount_TreatedAsZero()
        {
            Assert.AreEqual(10, this.service.GetEnemyCountForWave(10, 5, 5, -3));
        }

        [Test]
        public void GetEnemyCountForWave_BaseCountLessThan1_Returns1()
        {
            // waveIndex 选非 Boss 波（4 不被 5 整除）——首版误选 5 恰为 Boss 波叠加守卫 +3 → 4
            Assert.AreEqual(1, this.service.GetEnemyCountForWave(0, 4, 5, 3));
        }

        [Test]
        public void IsBossEnemySpawn_BossWaveLast_ReturnsTrue()
        {
            Assert.IsTrue(this.service.IsBossEnemySpawn(5, 9, 10, 5));
        }

        [Test]
        public void IsBossEnemySpawn_NonBossWave_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsBossEnemySpawn(4, 9, 10, 5));
        }

        [Test]
        public void IsBossEnemySpawn_NotLastSpawn_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsBossEnemySpawn(5, 0, 10, 5));
        }

        [Test]
        public void ClampWaveIndex_Negative_Returns1()
        {
            Assert.AreEqual(1, this.service.ClampWaveIndex(-1));
        }

        [Test]
        public void GetRewardOptionCount_ConfigLessThanAvailable_ReturnsConfig()
        {
            Assert.AreEqual(3, this.service.GetRewardOptionCount(3, 5));
        }

        [Test]
        public void GetRewardOptionCount_NoRewards_Returns0()
        {
            Assert.AreEqual(0, this.service.GetRewardOptionCount(0, 5));
        }

        [Test]
        public void GetNormalEnemyHealthMultiplier_Wave3_DefaultScale()
        {
            float result = this.service.GetNormalEnemyHealthMultiplier(3, 1f, 0.1f);
            Assert.AreEqual(1.2f, result, 0.0001f);
        }

        [Test]
        public void GetNormalEnemyAttackMultiplier_Wave5_DefaultScale()
        {
            float result = this.service.GetNormalEnemyAttackMultiplier(5, 1f, 0.15f);
            Assert.AreEqual(1.6f, result, 0.0001f);
        }

        [Test]
        public void GetNormalEnemyDefenseMultiplier_Wave1_Returns1()
        {
            Assert.AreEqual(1f, this.service.GetNormalEnemyDefenseMultiplier(1, 0.1f), 0.0001f);
        }

        [Test]
        public void GetBossHealthMultiplier_CompoundsNormalWithBoss()
        {
            Assert.AreEqual(4f, this.service.GetBossHealthMultiplier(2f, 2f), 0.0001f);
        }

        [Test]
        public void GetBossAttackMultiplier_CompoundsNormalWithBoss()
        {
            Assert.AreEqual(3f, this.service.GetBossAttackMultiplier(1.5f, 2f), 0.0001f);
        }

        [Test]
        public void GetRewardValue_Heal_BossReward()
        {
            Assert.AreEqual(0.5f, this.service.GetRewardValue(
                WaveRewardType.Heal, true, 5, 0.3f, 0.5f, 0, 0, 0, 0, 0, 0, 0, 0), 0.0001f);
        }

        [Test]
        public void GetRewardValue_Experience_NormalReward()
        {
            float result = this.service.GetRewardValue(
                WaveRewardType.Experience, false, 3, 0, 0, 100, 200, 0, 0, 0, 0, 0, 0);
            Assert.AreEqual(106f, result, 0.0001f);
        }

        [Test]
        public void GetRewardValue_DamageBoost_BossReward()
        {
            float result = this.service.GetRewardValue(
                WaveRewardType.DamageBoost, true, 0, 0, 0, 0, 0, 0.2f, 0.4f, 0, 0, 0, 0);
            Assert.AreEqual(0.4f, result, 0.0001f);
        }

        [Test]
        public void ToPercentInt_0_5_Returns50()
        {
            Assert.AreEqual(50, this.service.ToPercentInt(0.5f));
        }

        [Test]
        public void ToPercentInt_Negative_Returns0()
        {
            Assert.AreEqual(0, this.service.ToPercentInt(-0.1f));
        }

        [Test]
        public void ToRoundedInt_3_7_Returns4()
        {
            Assert.AreEqual(4, this.service.ToRoundedInt(3.7f));
        }

        [Test]
        public void AddWithCap_BelowMax_ReturnsSum()
        {
            Assert.AreEqual(80f, this.service.AddWithCap(50f, 30f, 100f), 0.0001f);
        }

        [Test]
        public void AddWithCap_ExceedsMax_ReturnsMax()
        {
            Assert.AreEqual(100f, this.service.AddWithCap(90f, 30f, 100f), 0.0001f);
        }

        [Test]
        public void AddWithCap_NegativeInputs_ClampedToZero()
        {
            Assert.AreEqual(30f, this.service.AddWithCap(-10f, 30f, 100f), 0.0001f);
        }

        [Test]
        public void ScaleAttribute_NormalCase()
        {
            Assert.AreEqual(200f, this.service.ScaleAttribute(100f, 2f, 10f), 0.0001f);
        }

        [Test]
        public void ScaleAttribute_ClampedToMin()
        {
            Assert.AreEqual(10f, this.service.ScaleAttribute(1f, 5f, 10f), 0.0001f);
        }
    }
}
