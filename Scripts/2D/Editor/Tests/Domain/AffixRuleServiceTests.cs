namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Item;
    using LAB2D.Enum;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class AffixRuleServiceTests
    {
        private AffixRuleService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new AffixRuleService();
            AffixRuleService.RandomFloatProvider = null;
        }

        [TearDown]
        public void TearDown()
        {
            AffixRuleService.RandomFloatProvider = null;
        }

        [Test]
        public void Roll_RandomProviderNotInjected_ReturnsEmpty()
        {
            List<EquipmentAffix> affixes = this.service.Roll(EquipmentRarityType.Mythic);
            Assert.AreEqual(0, affixes.Count);
        }

        [Test]
        public void Roll_Mythic_AlwaysMaxCount()
        {
            AffixRuleService.RandomFloatProvider = (min, max) => min;
            List<EquipmentAffix> affixes = this.service.Roll(EquipmentRarityType.Mythic);
            Assert.AreEqual(3, affixes.Count);
        }

        [Test]
        public void Roll_Common_SingleAffix()
        {
            AffixRuleService.RandomFloatProvider = (min, max) => min;
            List<EquipmentAffix> affixes = this.service.Roll(EquipmentRarityType.Common);
            Assert.AreEqual(1, affixes.Count);
        }

        [Test]
        public void Roll_AffixTypesNotRepeated()
        {
            AffixRuleService.RandomFloatProvider = (min, max) => min;
            List<EquipmentAffix> affixes = this.service.Roll(EquipmentRarityType.Mythic);
            Assert.AreEqual(3, affixes.Count);
            Assert.AreNotEqual(affixes[0].Type, affixes[1].Type);
            Assert.AreNotEqual(affixes[1].Type, affixes[2].Type);
            Assert.AreNotEqual(affixes[0].Type, affixes[2].Type);
        }

        [Test]
        public void RollValue_MaxRoll_AppliesRarityMultiplier()
        {
            // (min, max) => max 取区间上限：FlatAtn 上限 8，Mythic 倍率 3.2
            AffixRuleService.RandomFloatProvider = (min, max) => max;
            float value = this.service.RollValue(EquipmentAffixType.FlatAtn, EquipmentRarityType.Mythic);
            Assert.AreEqual(8f * 3.2f, value, 0.0001f);
        }

        [Test]
        public void RollValue_CommonFlatAtn_NoMultiplier()
        {
            AffixRuleService.RandomFloatProvider = (min, max) => max;
            float value = this.service.RollValue(EquipmentAffixType.FlatAtn, EquipmentRarityType.Common);
            Assert.AreEqual(8f, value, 0.0001f);
        }

        [Test]
        public void RollAffixCount_Legendary_Between2And3()
        {
            List<int> seen = new List<int>();
            foreach (float roll in new float[] { 0f, 0.999f })
            {
                AffixRuleService.RandomFloatProvider = (min, max) => roll == 0f ? min : max;
                seen.Add(this.service.RollAffixCount(EquipmentRarityType.Legendary));
            }

            Assert.Contains(2, seen);
            Assert.Contains(3, seen);
        }
    }
}
