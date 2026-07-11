namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class SkillRuleServiceTests
    {
        private SkillRuleService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new SkillRuleService();
        }

        [Test]
        public void CalculateSkillDamage_Level1_UsesBaseMultiplier()
        {
            float result = this.service.CalculateSkillDamage(50f, 2.0f, 1, 0.15f);
            Assert.AreEqual(100f, result, 0.01f);
        }

        [Test]
        public void CalculateSkillDamage_Level5_HigherDamage()
        {
            float level1 = this.service.CalculateSkillDamage(50f, 2.0f, 1, 0.15f);
            float level5 = this.service.CalculateSkillDamage(50f, 2.0f, 5, 0.15f);
            Assert.Greater(level5, level1, "5级伤害应高于1级");
        }

        [Test]
        public void CalculateSkillDamage_LowAtn_ReturnsAtLeastOne()
        {
            float result = this.service.CalculateSkillDamage(0.1f, 0.5f, 1, 0.1f);
            Assert.GreaterOrEqual(result, 1.0f, "伤害不应低于1");
        }

        [Test]
        public void CalculateSkillCooldown_Level5_ShorterThanLevel1()
        {
            float level1 = this.service.CalculateSkillCooldown(10f, 1, 0.1f);
            float level5 = this.service.CalculateSkillCooldown(10f, 5, 0.1f);
            Assert.Less(level5, level1, "高等级冷却应更短");
        }

        [Test]
        public void CalculateSkillCooldown_MinCooldown_ClampsToHalfSecond()
        {
            float result = this.service.CalculateSkillCooldown(1f, 5, 0.3f);
            Assert.GreaterOrEqual(result, 0.5f, "冷却不应低于0.5秒");
        }

        [Test]
        public void GetUpgradeCost_Level1_Returns1()
        {
            int cost = this.service.GetUpgradeCost(1);
            Assert.AreEqual(1, cost);
        }

        [Test]
        public void GetUpgradeCost_MaxLevel_ReturnsNegative()
        {
            int cost = this.service.GetUpgradeCost(5);
            Assert.Less(cost, 0, "满级应返回负值表示不可升级");
        }

        [Test]
        public void CalculateBuffMultiplier_Level5_HigherThanLevel1()
        {
            float level1 = this.service.CalculateBuffMultiplier(1.5f, 1, 0.15f);
            float level5 = this.service.CalculateBuffMultiplier(1.5f, 5, 0.15f);
            Assert.Greater(level5, level1, "高级增益倍率应更高");
        }

        [Test]
        public void CalculateHealAmount_ScalesWithLevel()
        {
            float level1 = this.service.CalculateHealAmount(20f, 1, 0.1f);
            float level3 = this.service.CalculateHealAmount(20f, 3, 0.1f);
            Assert.Greater(level3, level1, "高级治疗应更大");
        }

        [Test]
        public void HasEnoughMana_Sufficient_ReturnsTrue()
        {
            Assert.IsTrue(this.service.HasEnoughMana(50f, 30f));
        }

        [Test]
        public void HasEnoughMana_Insufficient_ReturnsFalse()
        {
            Assert.IsFalse(this.service.HasEnoughMana(20f, 30f));
        }
    }
}
