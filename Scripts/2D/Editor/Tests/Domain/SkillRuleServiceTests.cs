namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class SkillRuleServiceTests
    {
        private readonly SkillRuleService service = new SkillRuleService();

        [Test]
        public void CalculateSkillDamage_Level1_BaseDamage()
        {
            float damage = this.service.CalculateSkillDamage(100f, 1.5f, 1, 0.2f);
            Assert.AreEqual(150f, damage, 0.0001f);
        }

        [Test]
        public void CalculateSkillDamage_Level3_WithUpgradeBonus()
        {
            float damage = this.service.CalculateSkillDamage(100f, 1.5f, 3, 0.2f);
            Assert.AreEqual(210f, damage, 0.0001f);
        }

        [Test]
        public void CalculateSkillDamage_VerySmall_FloorTo1()
        {
            Assert.AreEqual(1f, this.service.CalculateSkillDamage(0.1f, 0.5f, 1, 0f), 0.0001f);
        }

        [Test]
        public void CalculateSkillCooldown_Level1_FullCooldown()
        {
            Assert.AreEqual(10f, this.service.CalculateSkillCooldown(10f, 1, 0.1f), 0.0001f);
        }

        [Test]
        public void CalculateSkillCooldown_Level3_ReducedCooldown()
        {
            Assert.AreEqual(8f, this.service.CalculateSkillCooldown(10f, 3, 0.1f), 0.0001f);
        }

        [Test]
        public void CalculateSkillCooldown_VeryShort_FloorTo0_5()
        {
            Assert.AreEqual(0.5f, this.service.CalculateSkillCooldown(1f, 10, 0.5f), 0.0001f);
        }

        [Test]
        public void ToCooldownDisplaySeconds_2_3_Returns3()
        {
            Assert.AreEqual(3, this.service.ToCooldownDisplaySeconds(2.3f));
        }

        [Test]
        public void GetCooldownProgress_Half_Returns0_5()
        {
            Assert.AreEqual(0.5f, this.service.GetCooldownProgress(5f, 10f), 0.0001f);
        }

        [Test]
        public void GetCooldownProgress_ZeroTotal_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetCooldownProgress(5f, 0f), 0.0001f);
        }

        [Test]
        public void GetCooldownProgress_Completed_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetCooldownProgress(0f, 10f), 0.0001f);
        }

        [Test]
        public void GetUpgradeCost_Level2_Returns2()
        {
            Assert.AreEqual(2, this.service.GetUpgradeCost(2));
        }

        [Test]
        public void GetUpgradeCost_Level4_Returns5()
        {
            Assert.AreEqual(5, this.service.GetUpgradeCost(4));
        }

        [Test]
        public void GetUpgradeCost_MaxLevel_ReturnsNegative1()
        {
            Assert.AreEqual(-1, this.service.GetUpgradeCost(5));
        }

        [Test]
        public void GetUpgradeCost_Invalid_ReturnsNegative1()
        {
            Assert.AreEqual(-1, this.service.GetUpgradeCost(0));
        }

        [Test]
        public void CalculateBuffMultiplier_Level3_WithUpgrade()
        {
            float result = this.service.CalculateBuffMultiplier(1.0f, 3, 0.2f);
            Assert.AreEqual(1.2f, result, 0.0001f);
        }

        [Test]
        public void CalculateHealAmount_Level2_BoostedHeal()
        {
            float heal = this.service.CalculateHealAmount(50f, 2, 0.3f);
            Assert.AreEqual(65f, heal, 0.0001f);
        }

        [Test]
        public void HasEnoughMana_Enough_ReturnsTrue()
        {
            Assert.IsTrue(this.service.HasEnoughMana(50f, 30f));
        }

        [Test]
        public void HasEnoughMana_NotEnough_ReturnsFalse()
        {
            Assert.IsFalse(this.service.HasEnoughMana(20f, 30f));
        }

        [Test]
        public void HasEnoughMana_ExactMatch_ReturnsTrue()
        {
            Assert.IsTrue(this.service.HasEnoughMana(30f, 30f));
        }
    }
}
