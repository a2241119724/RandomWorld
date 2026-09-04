namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay.AncientCave;
    using NUnit.Framework;

    [TestFixture]
    public class CaveExploreRuleServiceTests
    {
        [Test]
        public void RollRisk_Boundaries()
        {
            Assert.AreEqual(CaveExploreRuleService.RiskOutcome.Danger, CaveExploreRuleService.RollRisk(0f));
            Assert.AreEqual(CaveExploreRuleService.RiskOutcome.Danger, CaveExploreRuleService.RollRisk(0.349f));
            Assert.AreEqual(CaveExploreRuleService.RiskOutcome.Collapse, CaveExploreRuleService.RollRisk(0.35f));
            Assert.AreEqual(CaveExploreRuleService.RiskOutcome.Collapse, CaveExploreRuleService.RollRisk(0.449f));
            Assert.AreEqual(CaveExploreRuleService.RiskOutcome.Safe, CaveExploreRuleService.RollRisk(0.45f));
            Assert.AreEqual(CaveExploreRuleService.RiskOutcome.Safe, CaveExploreRuleService.RollRisk(0.99f));
        }

        [Test]
        public void RollRisk_WeightProportions()
        {
            // 大样本均匀 roll 验证三档占比约 35/10/55
            int danger = 0, collapse = 0, safe = 0;
            const int samples = 10000;
            for (int i = 0; i < samples; i++)
            {
                switch (CaveExploreRuleService.RollRisk(i / (float)samples))
                {
                    case CaveExploreRuleService.RiskOutcome.Danger: danger++; break;
                    case CaveExploreRuleService.RiskOutcome.Collapse: collapse++; break;
                    default: safe++; break;
                }
            }

            Assert.AreEqual(3500, danger, 2);
            Assert.AreEqual(1000, collapse, 2);
            Assert.AreEqual(5500, safe, 2);
        }

        [Test]
        public void RollReward_Boundaries()
        {
            Assert.AreEqual(CaveExploreRuleService.RewardKind.GongFa, CaveExploreRuleService.RollReward(0f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.GongFa, CaveExploreRuleService.RollReward(0.399f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.Supply, CaveExploreRuleService.RollReward(0.40f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.Supply, CaveExploreRuleService.RollReward(0.649f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.Equipment, CaveExploreRuleService.RollReward(0.65f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.Equipment, CaveExploreRuleService.RollReward(0.899f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.Double, CaveExploreRuleService.RollReward(0.90f));
            Assert.AreEqual(CaveExploreRuleService.RewardKind.Double, CaveExploreRuleService.RollReward(0.99f));
        }

        [Test]
        public void RollEnemyCount_Binary()
        {
            Assert.AreEqual(2, CaveExploreRuleService.RollEnemyCount(0f));
            Assert.AreEqual(2, CaveExploreRuleService.RollEnemyCount(0.499f));
            Assert.AreEqual(3, CaveExploreRuleService.RollEnemyCount(0.5f));
            Assert.AreEqual(3, CaveExploreRuleService.RollEnemyCount(0.99f));
        }
    }
}
