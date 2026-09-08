namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    /// <summary>
    /// BuildingDamageRuleService 纯函数单测 — 建筑/山门核心耐久结算。
    /// 覆盖：常规扣血、零/负伤害、溢出击毁、核心降级恢复、常量边界。
    /// </summary>
    [TestFixture]
    public class BuildingDamageRuleServiceTests
    {
        private BuildingDamageRuleService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new BuildingDamageRuleService();
        }

        [Test]
        public void ApplyDamage_NormalHit_ReducesHp()
        {
            BuildingDamageRuleService.BuildingDamageResult result = this.service.ApplyDamage(200f, 30f);
            Assert.AreEqual(170f, result.RemainingHp, 0.0001f);
            Assert.IsFalse(result.IsDestroyed);
        }

        [Test]
        public void ApplyDamage_ZeroOrNegativeDamage_NoChange()
        {
            BuildingDamageRuleService.BuildingDamageResult zero = this.service.ApplyDamage(200f, 0f);
            Assert.AreEqual(200f, zero.RemainingHp, 0.0001f);
            Assert.IsFalse(zero.IsDestroyed);

            BuildingDamageRuleService.BuildingDamageResult negative = this.service.ApplyDamage(200f, -5f);
            Assert.AreEqual(200f, negative.RemainingHp, 0.0001f);
            Assert.IsFalse(negative.IsDestroyed);
        }

        [Test]
        public void ApplyDamage_ExactKill_DestroysWithZeroHp()
        {
            BuildingDamageRuleService.BuildingDamageResult result = this.service.ApplyDamage(50f, 50f);
            Assert.AreEqual(0f, result.RemainingHp, 0.0001f);
            Assert.IsTrue(result.IsDestroyed);
        }

        [Test]
        public void ApplyDamage_Overkill_ClampsToZeroAndDestroys()
        {
            BuildingDamageRuleService.BuildingDamageResult result = this.service.ApplyDamage(10f, 999f);
            Assert.AreEqual(0f, result.RemainingHp, 0.0001f);
            Assert.IsTrue(result.IsDestroyed);
        }

        [Test]
        public void ApplyDamage_ChainedHits_Accumulate()
        {
            float hp = BuildingDamageRuleService.DefaultBuildingMaxHp;
            bool destroyed = false;
            for (int i = 0; i < 100 && !destroyed; i++)
            {
                BuildingDamageRuleService.BuildingDamageResult result = this.service.ApplyDamage(hp, 30f);
                hp = result.RemainingHp;
                destroyed = result.IsDestroyed;
            }

            Assert.IsTrue(destroyed);
            Assert.AreEqual(0f, hp, 0.0001f);
        }

        [Test]
        public void ComputeCoreReviveHp_IsRatioOfMaxHp()
        {
            float reviveHp = this.service.ComputeCoreReviveHp();
            Assert.AreEqual(BuildingDamageRuleService.CoreMaxHp * 0.6f, reviveHp, 0.0001f);
        }

        [Test]
        public void Constants_FormValidDownfallChain()
        {
            // P5 胜负软化：CoreMaxDownfalls 仅作展示参考（山门被破不再终局），数值链仍须自洽
            Assert.GreaterOrEqual(BuildingDamageRuleService.CoreMaxDownfalls, 2);
            Assert.Greater(BuildingDamageRuleService.CoreMaxHp, this.service.ComputeCoreReviveHp());
            Assert.GreaterOrEqual(BuildingDamageRuleService.CoreMaxLevel, 2);
            Assert.Greater(BuildingDamageRuleService.DefaultBuildingMaxHp, 0f);
        }

        [Test]
        public void DownfallChain_Softened_ThreeBreaks_ReviveForever_NoGameOver()
        {
            // P5 胜负软化：山门被破不再终局——连破 CoreMaxDownfalls 次，每次都按比例复活，游戏继续
            float hp = BuildingDamageRuleService.CoreMaxHp;
            int downfalls = 0;
            for (int i = 0; i < BuildingDamageRuleService.CoreMaxDownfalls; i++)
            {
                BuildingDamageRuleService.BuildingDamageResult result = this.service.ApplyDamage(hp, BuildingDamageRuleService.CoreMaxHp);
                Assert.IsTrue(result.IsDestroyed);
                downfalls++;
                hp = this.service.ComputeCoreReviveHp();
                Assert.Greater(hp, 0f); // 复活耐久 > 0，可继续防守
            }

            Assert.AreEqual(BuildingDamageRuleService.CoreMaxDownfalls, downfalls);
            Assert.Greater(hp, 0f); // 超过原上限次数仍存活（软化语义：无终局分支）
        }

        [Test]
        public void GetCoreUpgradeCost_Level1_ReturnsLevel2Cost()
        {
            Assert.AreEqual(200, this.service.GetCoreUpgradeCost(1));
        }

        [Test]
        public void GetCoreUpgradeCost_Level2_ReturnsLevel3Cost()
        {
            Assert.AreEqual(500, this.service.GetCoreUpgradeCost(2));
        }

        [Test]
        public void GetCoreUpgradeCost_AtOrBeyondMaxOrInvalid_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetCoreUpgradeCost(BuildingDamageRuleService.CoreMaxLevel));
            Assert.AreEqual(0, this.service.GetCoreUpgradeCost(BuildingDamageRuleService.CoreMaxLevel + 1));
            Assert.AreEqual(0, this.service.GetCoreUpgradeCost(0));
            Assert.AreEqual(0, this.service.GetCoreUpgradeCost(-1));
        }

        [Test]
        public void GetCoreUpgradeCost_TwoStepChain_Spends200Then500()
        {
            int total = 0;
            for (int level = 1; level < BuildingDamageRuleService.CoreMaxLevel; level++)
            {
                total += this.service.GetCoreUpgradeCost(level);
            }

            Assert.AreEqual(
                BuildingDamageRuleService.CoreUpgradeCostLevel2 + BuildingDamageRuleService.CoreUpgradeCostLevel3,
                total);
        }
    }
}
