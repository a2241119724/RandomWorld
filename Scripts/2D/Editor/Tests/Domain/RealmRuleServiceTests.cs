namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;
    using NUnit.Framework;

    [TestFixture]
    public class RealmRuleServiceTests
    {
        [Test]
        public void GetRealm_NullGrowth_ReturnsMortal()
        {
            Assert.AreEqual(0, RealmRuleService.GetRealm(null).Index);
            Assert.AreEqual("凡人", RealmRuleService.GetRealm(null).Name);
        }

        [Test]
        public void QiToNext_MatchesLibrary()
        {
            GrowthData growth = new GrowthData();
            Assert.AreEqual(100f, RealmRuleService.QiToNext(growth), 0.0001f);

            growth.RealmIndex = 1;
            Assert.AreEqual(400f, RealmRuleService.QiToNext(growth), 0.0001f);

            growth.RealmIndex = 2;
            Assert.AreEqual(1200f, RealmRuleService.QiToNext(growth), 0.0001f);

            growth.RealmIndex = 3;
            Assert.AreEqual(0f, RealmRuleService.QiToNext(growth), 0.0001f);
        }

        [Test]
        public void CanBreakthrough_RequiresEnoughQi()
        {
            GrowthData growth = new GrowthData { Qi = 99f };
            Assert.IsFalse(RealmRuleService.CanBreakthrough(growth));

            growth.Qi = 100f;
            Assert.IsTrue(RealmRuleService.CanBreakthrough(growth));
        }

        [Test]
        public void CanBreakthrough_AtMaxRealm_ReturnsFalse()
        {
            GrowthData growth = new GrowthData { RealmIndex = 3, Qi = 99999f };
            Assert.IsFalse(RealmRuleService.CanBreakthrough(growth));
            Assert.IsFalse(RealmRuleService.CanBreakthrough(null));
        }

        [Test]
        public void Breakthrough_ConsumesQiAndAdvances()
        {
            GrowthData growth = new GrowthData { Qi = 150f };
            Assert.IsTrue(RealmRuleService.Breakthrough(growth));

            Assert.AreEqual(1, growth.RealmIndex);
            Assert.AreEqual(50f, growth.Qi, 0.0001f);
        }

        [Test]
        public void Breakthrough_BonusAccumulatesAcrossRealms()
        {
            GrowthData growth = new GrowthData { Qi = 100f };

            // 突破到练气：ATN+4 DEF+2
            Assert.IsTrue(RealmRuleService.Breakthrough(growth));
            Assert.AreEqual(4f, growth.PermanentRealmBonus.Stats.ATN, 0.0001f);
            Assert.AreEqual(2f, growth.PermanentRealmBonus.Stats.DEF, 0.0001f);

            // 突破到筑基（需要 400 灵气）：加成累进 ATN+12 DEF+6 RES+4 MaxHp+50
            growth.Qi = 400f;
            Assert.IsTrue(RealmRuleService.Breakthrough(growth));
            Assert.AreEqual(2, growth.RealmIndex);
            Assert.AreEqual(12f, growth.PermanentRealmBonus.Stats.ATN, 0.0001f);
            Assert.AreEqual(6f, growth.PermanentRealmBonus.Stats.DEF, 0.0001f);
            Assert.AreEqual(4f, growth.PermanentRealmBonus.Stats.RES, 0.0001f);
            Assert.AreEqual(50f, growth.PermanentRealmBonus.MaxHpFlat, 0.0001f);
        }

        [Test]
        public void Breakthrough_InsufficientQi_DataUnchanged()
        {
            GrowthData growth = new GrowthData { Qi = 50f };
            Assert.IsFalse(RealmRuleService.Breakthrough(growth));

            Assert.AreEqual(0, growth.RealmIndex);
            Assert.AreEqual(50f, growth.Qi, 0.0001f);
            Assert.AreEqual(BattleStats.Zero.ATN, growth.PermanentRealmBonus.Stats.ATN);
        }

        [Test]
        public void ComputeQiGain_BaseRateTimesSeconds()
        {
            GrowthData growth = new GrowthData();
            Assert.AreEqual(20f, RealmRuleService.ComputeQiGain(growth, 10f), 0.0001f);
        }

        [Test]
        public void ComputeQiGain_NullGrowthOrInvalidSeconds_ReturnsZero()
        {
            Assert.AreEqual(0f, RealmRuleService.ComputeQiGain(null, 10f), 0.0001f);
            Assert.AreEqual(0f, RealmRuleService.ComputeQiGain(new GrowthData(), 0f), 0.0001f);
            Assert.AreEqual(0f, RealmRuleService.ComputeQiGain(new GrowthData(), -1f), 0.0001f);
        }

        [Test]
        public void ComputeQiGain_AppliesSpeedBonusAndScale()
        {
            GrowthData growth = new GrowthData
            {
                Special = new GrowthBonus(default, cultivationSpeedMul: 0.5f),
            };

            // (1 + 0.5 内功 + 0.5 聚灵阵) × 2/s × 10s = 40
            Assert.AreEqual(40f, RealmRuleService.ComputeQiGain(growth, 10f, 0.5f), 0.0001f);

            // 地面睡眠半额：1 × 2 × 0.5 × 10 = 10
            Assert.AreEqual(10f, RealmRuleService.ComputeQiGain(growth, 10f, 0f, 0.5f), 0.0001f);
        }
    }
}
