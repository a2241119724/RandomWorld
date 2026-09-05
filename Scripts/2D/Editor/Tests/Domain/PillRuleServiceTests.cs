namespace LAB2D.Editor.Tests.Domain
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Alchemy;
    using NUnit.Framework;

    [TestFixture]
    public class PillRuleServiceTests
    {
        private Func<float> originalRandom;

        [SetUp]
        public void SetUp()
        {
            this.originalRandom = PillRuleService.RandomFloatProvider;
        }

        [TearDown]
        public void TearDown()
        {
            // 静态随机桩残留会连坐后续测试（bug-fixes 教训：TurnBattle UseSequence 桩）
            PillRuleService.RandomFloatProvider = this.originalRandom;
        }

        [Test]
        public void CanCraft_NullInputs_ReturnsFalse()
        {
            Assert.IsFalse(PillRuleService.CanCraft(null, PillLibrary.HuiQiSan));
            Assert.IsFalse(PillRuleService.CanCraft(new GrowthData(), null));
        }

        [Test]
        public void CanCraft_RealmTooLow_ReturnsFalse()
        {
            GrowthData growth = new GrowthData { RealmIndex = 0, Qi = 99999f };
            Assert.IsFalse(PillRuleService.CanCraft(growth, PillLibrary.HuiQiSan));

            growth.RealmIndex = 1;
            Assert.IsTrue(PillRuleService.CanCraft(growth, PillLibrary.HuiQiSan));
        }

        [Test]
        public void CanCraft_InsufficientQi_ReturnsFalse()
        {
            GrowthData growth = new GrowthData { RealmIndex = 1, Qi = 39.9f };
            Assert.IsFalse(PillRuleService.CanCraft(growth, PillLibrary.HuiQiSan));

            growth.Qi = 40f;
            Assert.IsTrue(PillRuleService.CanCraft(growth, PillLibrary.HuiQiSan));
        }

        [Test]
        public void RollQuality_BoundaryMappings()
        {
            Assert.AreEqual(PillQuality.Common, PillRuleService.RollQuality(0f));
            Assert.AreEqual(PillQuality.Common, PillRuleService.RollQuality(0.59f));

            Assert.AreEqual(PillQuality.Superior, PillRuleService.RollQuality(0.6f));
            Assert.AreEqual(PillQuality.Superior, PillRuleService.RollQuality(0.89f));

            Assert.AreEqual(PillQuality.Premium, PillRuleService.RollQuality(0.9f));
            Assert.AreEqual(PillQuality.Premium, PillRuleService.RollQuality(0.99f));
        }

        [Test]
        public void QualityToMultiplier_MatchesDesign()
        {
            Assert.AreEqual(1f, PillRuleService.QualityToMultiplier(PillQuality.Common));
            Assert.AreEqual(1.5f, PillRuleService.QualityToMultiplier(PillQuality.Superior));
            Assert.AreEqual(2f, PillRuleService.QualityToMultiplier(PillQuality.Premium));
        }

        [Test]
        public void TryCraft_Success_ConsumesQi_AndAppliesQualityMultiplier()
        {
            // 桩到极品区间：60 基准 ×2 = 120
            PillRuleService.RandomFloatProvider = () => 0.95f;
            GrowthData growth = new GrowthData { RealmIndex = 1, Qi = 100f };

            Assert.IsTrue(PillRuleService.TryCraft(growth, PillLibrary.HuiQiSan, out PillCraftResult result));
            Assert.IsTrue(result.Success);
            Assert.AreEqual(PillQuality.Premium, result.Quality);
            Assert.AreEqual(120f, result.EffectValue, 0.0001f);
            Assert.AreEqual(60f, growth.Qi, 0.0001f); // 100 - 40 成本
        }

        [Test]
        public void TryCraft_Failure_LeavesQiUnchanged()
        {
            GrowthData growth = new GrowthData { RealmIndex = 0, Qi = 100f };

            Assert.IsFalse(PillRuleService.TryCraft(growth, PillLibrary.HuiQiSan, out PillCraftResult result));
            Assert.IsFalse(result.Success);
            Assert.AreEqual(100f, growth.Qi, 0.0001f);
        }

        [Test]
        public void TryCraft_PermanentStats_ScalesWithQuality()
        {
            // 九转金丹凡品：全维 8 不变；上品 ×1.5 = 12
            GrowthData growth = new GrowthData { RealmIndex = 4, Qi = 99999f };

            PillRuleService.RandomFloatProvider = () => 0f;
            Assert.IsTrue(PillRuleService.TryCraft(growth, PillLibrary.JiuZhuanJinDan, out PillCraftResult common));
            Assert.AreEqual(8f, common.PermanentBonus.ATN, 0.0001f);

            PillRuleService.RandomFloatProvider = () => 0.7f;
            Assert.IsTrue(PillRuleService.TryCraft(growth, PillLibrary.JiuZhuanJinDan, out PillCraftResult superior));
            Assert.AreEqual(12f, superior.PermanentBonus.ATN, 0.0001f);
            Assert.AreEqual(0.03f, superior.PermanentBonus.CRT, 0.0001f);
        }

        [Test]
        public void TryCraft_BreakthroughAid_PremiumDoublesDiscount()
        {
            PillRuleService.RandomFloatProvider = () => 0.9f;
            GrowthData growth = new GrowthData { RealmIndex = 3, Qi = 99999f };

            Assert.IsTrue(PillRuleService.TryCraft(growth, PillLibrary.DuJieDan, out PillCraftResult result));
            // 渡劫丹基准 15% 减免，极品 ×2 = 30%
            Assert.AreEqual(0.3f, result.EffectValue, 0.0001f);
        }

        [Test]
        public void CanCraft_BreakthroughAid_AtMaxRealm_ReturnsFalse()
        {
            // 化神巅峰无可破之境：渡劫丹禁炼（防白扣灵气——等效补灵气在此 QiToNext=0）
            GrowthData growth = new GrowthData { RealmIndex = 5, Qi = 99999f };
            Assert.IsFalse(PillRuleService.CanCraft(growth, PillLibrary.DuJieDan));

            // 同条件下其他丹方不受影响（九转金丹同为元婴档以上，改验回气散门槛本身就够）
            growth.RealmIndex = 4;
            Assert.IsTrue(PillRuleService.CanCraft(growth, PillLibrary.DuJieDan));
        }

        [Test]
        public void Library_AllPillsHaveUniqueIds_AndValidCosts()
        {
            var ids = new HashSet<string>();
            foreach (PillDef pill in PillLibrary.All)
            {
                Assert.IsTrue(ids.Add(pill.Id), $"丹方 Id 重复: {pill.Id}");
                Assert.Greater(pill.QiCost, 0f, $"{pill.Name} 成本应为正");
                Assert.GreaterOrEqual(pill.RequiredRealmIndex, 1, $"{pill.Name} 练气起炼");
            }

            Assert.AreEqual(5, ids.Count);
        }

        [Test]
        public void Library_FindById_RoundTrip()
        {
            foreach (PillDef pill in PillLibrary.All)
            {
                Assert.AreSame(pill, PillLibrary.FindById(pill.Id));
            }

            Assert.IsNull(PillLibrary.FindById("pill_not_exist"));
            Assert.IsNull(PillLibrary.FindById(null));
        }
    }
}
