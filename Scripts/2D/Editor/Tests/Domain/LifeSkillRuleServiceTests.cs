namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using NUnit.Framework;

    /// <summary>
    /// LifeSkillRuleService 单测 — 经验/等级/倍率换算（纯函数）。
    /// </summary>
    public class LifeSkillRuleServiceTests
    {
        [Test]
        public void XpPerTask_MapsAllSkills()
        {
            Assert.AreEqual(LifeSkillConstant.XpPerFelling, LifeSkillRuleService.XpPerTask(LifeSkillType.Felling));
            Assert.AreEqual(LifeSkillConstant.XpPerMining, LifeSkillRuleService.XpPerTask(LifeSkillType.Mining));
            Assert.AreEqual(LifeSkillConstant.XpPerFarming, LifeSkillRuleService.XpPerTask(LifeSkillType.Farming));
        }

        [Test]
        public void LevelOf_Thresholds()
        {
            Assert.AreEqual(0, LifeSkillRuleService.LevelOf(0f));
            Assert.AreEqual(0, LifeSkillRuleService.LevelOf(LifeSkillConstant.XpToLevel1 - 0.5f));
            Assert.AreEqual(1, LifeSkillRuleService.LevelOf(LifeSkillConstant.XpToLevel1));
            Assert.AreEqual(2, LifeSkillRuleService.LevelOf(LifeSkillConstant.XpToLevel2));
            Assert.AreEqual(3, LifeSkillRuleService.LevelOf(LifeSkillConstant.XpToLevel3));
            Assert.AreEqual(3, LifeSkillRuleService.LevelOf(LifeSkillConstant.XpToLevel3 + 1000f));
        }

        [Test]
        public void GetMultiplier_IncreasesWithLevel()
        {
            float prev = 0f;
            for (int level = 0; level <= 3; level++)
            {
                float mul = LifeSkillRuleService.GetMultiplier(level);
                Assert.GreaterOrEqual(mul, prev, $"等级 {level} 倍率不应低于更低等级");
                prev = mul;
            }

            Assert.AreEqual(LifeSkillConstant.MultiplierLevel3, LifeSkillRuleService.GetMultiplier(3));
        }

        [Test]
        public void GetMultiplier_ByXp_MatchesLevelLookup()
        {
            Assert.AreEqual(
                LifeSkillRuleService.GetMultiplier(LifeSkillRuleService.LevelOf(15f)),
                LifeSkillRuleService.GetMultiplier(15f));
        }

        [Test]
        public void XpToNextLevel_ProgressionAndMax()
        {
            // 0 经验 → 升 1 级阈值
            Assert.AreEqual(LifeSkillConstant.XpToLevel1, LifeSkillRuleService.XpToNextLevel(0f));
            // 跨过 1 级后 → 升 2 级阈值
            Assert.AreEqual(LifeSkillConstant.XpToLevel2, LifeSkillRuleService.XpToNextLevel(LifeSkillConstant.XpToLevel1));
            // 满级 → -1
            Assert.AreEqual(-1f, LifeSkillRuleService.XpToNextLevel(LifeSkillConstant.XpToLevel3));
        }

        [Test]
        public void GetName_ReturnsChineseNames()
        {
            Assert.IsFalse(string.IsNullOrEmpty(LifeSkillRuleService.GetName(LifeSkillType.Felling)));
            Assert.IsFalse(string.IsNullOrEmpty(LifeSkillRuleService.GetName(LifeSkillType.Mining)));
            Assert.IsFalse(string.IsNullOrEmpty(LifeSkillRuleService.GetName(LifeSkillType.Farming)));
        }

        [Test]
        public void AllSkills_ContainsThreeSkills()
        {
            Assert.AreEqual(3, LifeSkillRuleService.AllSkills.Count);
            CollectionAssert.AllItemsAreUnique(LifeSkillRuleService.AllSkills);
        }
    }
}
