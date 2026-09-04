namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.AwakenedPower;
    using NUnit.Framework;

    [TestFixture]
    public class AwakenedPowerRuleServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            AwakenedPowerRuleService.RandomFloatProvider = null;
        }

        [Test]
        public void GetAwakenChance_FullHp_IsBaseChance()
        {
            Assert.AreEqual(AwakenedPowerRuleService.BaseAwakenChance,
                AwakenedPowerRuleService.GetAwakenChance(100f, 100f), 0.0001f);
        }

        [Test]
        public void GetAwakenChance_LowHp_IncreasesTowardMax()
        {
            // 半血：0.03 + 0.5×0.07 = 0.065
            Assert.AreEqual(0.065f, AwakenedPowerRuleService.GetAwakenChance(50f, 100f), 0.0001f);

            // 濒死（hp=0 才是 0% 血——首版用 hp=1 实为 1% 血得 0.0993，差超容差）
            Assert.AreEqual(0.10f, AwakenedPowerRuleService.GetAwakenChance(0f, 100f), 0.0001f);
        }

        [Test]
        public void GetAwakenChance_InvalidMaxHp_ReturnsZero()
        {
            Assert.AreEqual(0f, AwakenedPowerRuleService.GetAwakenChance(100f, 0f), 0.0001f);
        }

        [Test]
        public void CanAwaken_NullOrReachedLimit_ReturnsFalse()
        {
            Assert.IsFalse(AwakenedPowerRuleService.CanAwaken(null));

            GrowthData growth = new GrowthData();
            growth.Ensure(); // 集合字段兜底（构造不自动 Ensure，Manager 层同款约定）
            Assert.IsTrue(AwakenedPowerRuleService.CanAwaken(growth));

            growth.AwakenedPowerIds.Add(AwakenedPowerLibrary.FireBall.Id);
            Assert.IsFalse(AwakenedPowerRuleService.CanAwaken(growth));
        }

        [Test]
        public void RollPowerId_NoProvider_ReturnsNull()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure(); // 集合字段兜底（构造不自动 Ensure，Manager 层同款约定）

            Assert.IsNull(AwakenedPowerRuleService.RollPowerId(growth));
        }

        [Test]
        public void RollPowerId_PicksFromPool()
        {
            // 序列桩固定取 0 → 池首（念力）
            AwakenedPowerRuleService.RandomFloatProvider = (min, max) => 0f;
            GrowthData growth = new GrowthData();
            growth.Ensure(); // 集合字段兜底（构造不自动 Ensure，Manager 层同款约定）

            Assert.AreEqual(AwakenedPowerLibrary.Telekinesis.Id, AwakenedPowerRuleService.RollPowerId(growth));

            // 已达觉醒上限（1 个）后再 roll → null
            growth.AwakenedPowerIds.Add(AwakenedPowerLibrary.Telekinesis.Id);
            Assert.IsNull(AwakenedPowerRuleService.RollPowerId(growth));
        }

        [Test]
        public void Get_NullOrUnknownId_ReturnsNull()
        {
            Assert.IsNull(AwakenedPowerLibrary.Get(null));
            Assert.IsNull(AwakenedPowerLibrary.Get("power_unknown"));
            Assert.AreEqual(AwakenedPowerLibrary.FireBall.Id, AwakenedPowerLibrary.Get("power_fireball").Id);
        }
    }
}
