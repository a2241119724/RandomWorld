namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Tech;
    using NUnit.Framework;

    /// <summary>
    /// TechRuleService 单测 — 研究可行性与加成聚合（纯函数）。
    /// </summary>
    public class TechRuleServiceTests
    {
        [Test]
        public void CanResearch_NullDef_ReturnsFalse()
        {
            Assert.IsFalse(TechRuleService.CanResearch(false, 100f, null));
        }

        [Test]
        public void CanResearch_NotEnoughPoints_ReturnsFalse()
        {
            Assert.IsFalse(TechRuleService.CanResearch(false, TechLibrary.SpiritFarming.Cost - 1f, TechLibrary.SpiritFarming));
        }

        [Test]
        public void CanResearch_ExactlyEnoughPoints_ReturnsTrue()
        {
            Assert.IsTrue(TechRuleService.CanResearch(false, TechLibrary.SpiritFarming.Cost, TechLibrary.SpiritFarming));
        }

        [Test]
        public void CanResearch_AlreadyResearched_ReturnsFalse()
        {
            Assert.IsFalse(TechRuleService.CanResearch(true, 9999f, TechLibrary.SpiritFarming));
        }

        [Test]
        public void SumBonus_AggregatesOnlyResearched()
        {
            List<string> researched = new List<string>
            {
                TechLibrary.SpiritFarming.Id,
                TechLibrary.SpiritArray.Id,
            };

            float farm = TechRuleService.SumBonus(researched, t => t.FarmSpeedBonus);
            float meditate = TechRuleService.SumBonus(researched, t => t.MeditateSpeedBonus);
            float research = TechRuleService.SumBonus(researched, t => t.ResearchSpeedBonus);

            Assert.AreEqual(TechLibrary.SpiritFarming.FarmSpeedBonus, farm);
            Assert.AreEqual(TechLibrary.SpiritArray.MeditateSpeedBonus, meditate);
            Assert.AreEqual(0f, research); // 高级研究法未研究
        }

        [Test]
        public void SumBonus_NullOrUnknownIds_ReturnsZero()
        {
            Assert.AreEqual(0f, TechRuleService.SumBonus(null, t => t.FarmSpeedBonus));
            Assert.AreEqual(0f, TechRuleService.SumBonus(new List<string> { "tech_unknown" }, t => t.FarmSpeedBonus));
        }

        [Test]
        public void Library_ContainsThreeTechsWithUniqueIds()
        {
            Assert.AreEqual(3, TechLibrary.All.Count);
            CollectionAssert.AllItemsAreUnique(TechLibrary.All.ConvertAll(t => t.Id));

            // 建筑解锁科技与加成科技字段齐全
            Assert.AreEqual("SpiritArray", TechLibrary.SpiritArray.UnlockBuildName);
            Assert.Greater(TechLibrary.SpiritFarming.FarmSpeedBonus, 0f);
            Assert.Greater(TechLibrary.AdvancedResearch.ResearchSpeedBonus, 0f);
        }
    }
}
