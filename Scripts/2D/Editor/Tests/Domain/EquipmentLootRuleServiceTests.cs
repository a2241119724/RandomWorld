namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using LAB2D.Enum;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class EquipmentLootRuleServiceTests
    {
        private readonly EquipmentLootRuleService service = new EquipmentLootRuleService();

        [Test]
        public void GetRarityWeightBonus_Wave0_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetRarityWeightBonus(0, 0.03f, 0.5f), 0.0001f);
        }

        [Test]
        public void GetRarityWeightBonus_Wave10_Returns0_3()
        {
            Assert.AreEqual(0.3f, this.service.GetRarityWeightBonus(10, 0.03f, 0.5f), 0.0001f);
        }

        [Test]
        public void GetRarityWeightBonus_Wave30_CappedAtMax()
        {
            Assert.AreEqual(0.5f, this.service.GetRarityWeightBonus(30, 0.03f, 0.5f), 0.0001f);
        }

        [Test]
        public void GetRarityWeightBonus_NegativeWave_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetRarityWeightBonus(-1, 0.03f, 0.5f), 0.0001f);
        }

        [Test]
        public void GetRarityTotalWeight_Wave0_Returns100()
        {
            float total = this.service.GetRarityTotalWeight(0);
            Assert.AreEqual(100f, total, 0.01f);
        }

        [Test]
        public void RollRarityWithRoll_Wave0_LowRoll_ReturnsCommon()
        {
            EquipmentRarityType result = this.service.RollRarityWithRoll(0, 25f);
            Assert.AreEqual(EquipmentRarityType.Common, result);
        }

        [Test]
        public void RollRarityWithRoll_Wave0_HighRoll_ReturnsMythic()
        {
            EquipmentRarityType result = this.service.RollRarityWithRoll(0, 99.9f);
            Assert.AreEqual(EquipmentRarityType.Mythic, result);
        }

        [Test]
        public void RollRarityWithRoll_Wave0_ReturnsUncommon()
        {
            EquipmentRarityType result = this.service.RollRarityWithRoll(0, 60f);
            Assert.AreEqual(EquipmentRarityType.Uncommon, result);
        }

        [Test]
        public void GetStatMultiplier_Common_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetStatMultiplier(EquipmentRarityType.Common), 0.0001f);
        }

        [Test]
        public void GetStatMultiplier_Legendary_Returns2_5()
        {
            Assert.AreEqual(2.5f, this.service.GetStatMultiplier(EquipmentRarityType.Legendary), 0.0001f);
        }

        [Test]
        public void GetStatMultiplier_Mythic_Returns3_2()
        {
            Assert.AreEqual(3.2f, this.service.GetStatMultiplier(EquipmentRarityType.Mythic), 0.0001f);
        }

        [Test]
        public void ApplyRarityToStat_Common_ReturnsSame()
        {
            Assert.AreEqual(10f, this.service.ApplyRarityToStat(10f, EquipmentRarityType.Common), 0.0001f);
        }

        [Test]
        public void ApplyRarityToStat_Epic_Doubles()
        {
            Assert.AreEqual(20f, this.service.ApplyRarityToStat(10f, EquipmentRarityType.Epic), 0.0001f);
        }

        [Test]
        public void GetExtremeStatCount_Legendary_Returns1()
        {
            Assert.AreEqual(1, this.service.GetExtremeStatCount(EquipmentRarityType.Legendary));
        }

        [Test]
        public void GetExtremeStatCount_Mythic_Returns2()
        {
            Assert.AreEqual(2, this.service.GetExtremeStatCount(EquipmentRarityType.Mythic));
        }

        [Test]
        public void GetExtremeStatCount_Common_Returns0()
        {
            Assert.AreEqual(0, this.service.GetExtremeStatCount(EquipmentRarityType.Common));
        }

        [Test]
        public void GetExtremeStatMultiplier_Returns2()
        {
            Assert.AreEqual(2.0f, this.service.GetExtremeStatMultiplier(), 0.0001f);
        }

        [Test]
        public void CountUpgrades_OldNull_ReturnsAfterCount()
        {
            var after = new Dictionary<string, float> { { "ATK", 10f }, { "DEF", 5f } };
            Assert.AreEqual(2, this.service.CountUpgrades(null, after));
        }

        [Test]
        public void CountUpgrades_AfterNull_Returns0()
        {
            Assert.AreEqual(0, this.service.CountUpgrades(null, null));
        }

        [Test]
        public void CountUpgrades_OneImproved_Returns1()
        {
            var before = new Dictionary<string, float> { { "ATK", 10f }, { "DEF", 5f } };
            var after = new Dictionary<string, float> { { "ATK", 15f }, { "DEF", 5f } };
            Assert.AreEqual(1, this.service.CountUpgrades(before, after));
        }

        [Test]
        public void GetStatDiffs_SameLength_ReturnsCorrectDiffs()
        {
            float[] diffs = this.service.GetStatDiffs(
                new float[] { 10f, 5f },
                new float[] { 15f, 3f });
            Assert.AreEqual(2, diffs.Length);
            Assert.AreEqual(5f, diffs[0], 0.0001f);
            Assert.AreEqual(-2f, diffs[1], 0.0001f);
        }

        [Test]
        public void GetStatDiffs_OldNull_NewBecomesDiff()
        {
            float[] diffs = this.service.GetStatDiffs(null, new float[] { 10f, 5f });
            Assert.AreEqual(10f, diffs[0], 0.0001f);
            Assert.AreEqual(5f, diffs[1], 0.0001f);
        }

        [Test]
        public void GetStatDiffs_NewNull_ReturnsEmpty()
        {
            float[] diffs = this.service.GetStatDiffs(new float[] { 10f }, null);
            Assert.AreEqual(0, diffs.Length);
        }

        [Test]
        public void GetStatNames_Returns8Entries()
        {
            string[] names = this.service.GetStatNames();
            Assert.AreEqual(8, names.Length);
            Assert.Contains("ATN", names);
            Assert.Contains("HIT", names);
        }
    }
}
