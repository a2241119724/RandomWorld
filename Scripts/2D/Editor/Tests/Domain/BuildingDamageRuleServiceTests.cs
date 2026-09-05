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
            // 宽闸门曲线：第 1/2 次被击破降级，第 CoreMaxDownfalls 次终局
            Assert.GreaterOrEqual(BuildingDamageRuleService.CoreMaxDownfalls, 2);
            Assert.Greater(BuildingDamageRuleService.CoreMaxHp, this.service.ComputeCoreReviveHp());
            Assert.GreaterOrEqual(BuildingDamageRuleService.CoreMaxLevel, 2);
            Assert.Greater(BuildingDamageRuleService.DefaultBuildingMaxHp, 0f);
        }

        [Test]
        public void DownfallChain_SimulatedThreeBreaks_EndsInGameOver()
        {
            // 数值层模拟核心被毁三次：前两次降级恢复、第三次终局（MountainGateManager 的闸门逻辑同此）
            float hp = BuildingDamageRuleService.CoreMaxHp;
            int downfalls = 0;
            bool gameOver = false;
            while (!gameOver)
            {
                BuildingDamageRuleService.BuildingDamageResult result = this.service.ApplyDamage(hp, BuildingDamageRuleService.CoreMaxHp);
                hp = result.RemainingHp;
                if (result.IsDestroyed)
                {
                    downfalls++;
                    if (downfalls >= BuildingDamageRuleService.CoreMaxDownfalls)
                    {
                        gameOver = true;
                    }
                    else
                    {
                        hp = this.service.ComputeCoreReviveHp();
                    }
                }
            }

            Assert.IsTrue(gameOver);
            Assert.AreEqual(BuildingDamageRuleService.CoreMaxDownfalls, downfalls);
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
