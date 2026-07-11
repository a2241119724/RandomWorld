namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using NUnit.Framework;

    /// <summary>
    /// DamageCalculator 单元测试 — 验证伤害计算的防御减伤、暴击、生命值应用。
    /// </summary>
    [TestFixture]
    public class DamageCalculatorTests
    {
        private DamageCalculator calculator;

        [SetUp]
        public void SetUp()
        {
            this.calculator = new DamageCalculator();
        }

        [Test]
        public void ApplyDefense_PositiveDefense_ReducesDamage()
        {
            float result = this.calculator.ApplyDefense(100f, 20f);
            Assert.Less(result, 100f, "防御应减少伤害");
            Assert.Greater(result, 0f, "减伤后伤害应大于0");
        }

        [Test]
        public void ApplyDefense_ZeroDefense_NoReduction()
        {
            float result = this.calculator.ApplyDefense(100f, 0f);
            Assert.AreEqual(100f, result, 0.01f, "零防御不应减少伤害");
        }

        [Test]
        public void ApplyDefense_NegativeDefense_IncreasesDamage()
        {
            float result = this.calculator.ApplyDefense(100f, -30f);
            Assert.Greater(result, 100f, "负防御应增加伤害");
        }

        [Test]
        public void GetOutgoingDamage_Critical_AppliesCritMultiplier()
        {
            float normalDamage = this.calculator.GetOutgoingDamage(50f, 2.0f, false);
            float critDamage = this.calculator.GetOutgoingDamage(50f, 2.0f, true);
            Assert.Greater(critDamage, normalDamage, "暴击伤害应高于普通伤害");
        }

        [Test]
        public void ApplyDamageToHealth_LethalDamage_ReturnsDead()
        {
            var result = this.calculator.ApplyDamageToHealth(50f, 100f);
            Assert.IsTrue(result.IsDead, "超过生命值的伤害应导致死亡");
            Assert.LessOrEqual(result.RemainingHp, 0f, "剩余生命值应≤0");
        }

        [Test]
        public void ApplyDamageToHealth_NonLethalDamage_ReturnsAlive()
        {
            var result = this.calculator.ApplyDamageToHealth(100f, 30f);
            Assert.IsFalse(result.IsDead, "非致命伤害不应导致死亡");
            Assert.AreEqual(70f, result.RemainingHp, 0.01f);
        }

        [Test]
        public void ApplyHealingToHealth_Overheal_ClampsToMax()
        {
            float result = this.calculator.ApplyHealingToHealth(90f, 100f, 30f);
            Assert.AreEqual(100f, result);
        }

        [Test]
        public void ApplyHealingToHealth_NormalHeal_IncreasesHp()
        {
            float result = this.calculator.ApplyHealingToHealth(40f, 100f, 30f);
            Assert.AreEqual(70f, result, 0.01f);
        }
    }
}
