namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class ComboBonusRuleServiceTests
    {
        private readonly ComboBonusRuleService service = new ComboBonusRuleService();
        private readonly int[] thresholds = { 0, 5, 10, 20 };
        private readonly float[] damageMultipliers = { 1.0f, 1.2f, 1.5f, 2.0f };
        private readonly float[] expMultipliers = { 1.0f, 1.3f, 1.6f, 2.5f };
        private readonly string[] labels = { "", "Good", "Great", "Perfect" };

        [Test]
        public void FindTierIndex_Combo1_Returns0()
        {
            Assert.AreEqual(0, this.service.FindTierIndex(1, this.thresholds));
        }

        [Test]
        public void FindTierIndex_Combo5_Returns1()
        {
            Assert.AreEqual(1, this.service.FindTierIndex(5, this.thresholds));
        }

        [Test]
        public void FindTierIndex_Combo12_Returns2()
        {
            Assert.AreEqual(2, this.service.FindTierIndex(12, this.thresholds));
        }

        [Test]
        public void FindTierIndex_Combo25_Returns3()
        {
            Assert.AreEqual(3, this.service.FindTierIndex(25, this.thresholds));
        }

        [Test]
        public void FindTierIndex_NullArray_Returns0()
        {
            Assert.AreEqual(0, this.service.FindTierIndex(10, null));
        }

        [Test]
        public void FindTierIndex_EmptyArray_Returns0()
        {
            Assert.AreEqual(0, this.service.FindTierIndex(10, new int[0]));
        }

        [Test]
        public void GetDamageMultiplier_Tier0_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetDamageMultiplier(0, this.damageMultipliers), 0.0001f);
        }

        [Test]
        public void GetDamageMultiplier_Tier3_Returns2()
        {
            Assert.AreEqual(2.0f, this.service.GetDamageMultiplier(3, this.damageMultipliers), 0.0001f);
        }

        [Test]
        public void GetDamageMultiplier_OutOfRange_ClampsToSafe()
        {
            Assert.AreEqual(2.0f, this.service.GetDamageMultiplier(10, this.damageMultipliers), 0.0001f);
        }

        [Test]
        public void GetDamageMultiplier_NegativeIndex_ClampsToZero()
        {
            Assert.AreEqual(1.0f, this.service.GetDamageMultiplier(-1, this.damageMultipliers), 0.0001f);
        }

        [Test]
        public void GetDamageMultiplier_NullArray_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetDamageMultiplier(1, null), 0.0001f);
        }

        [Test]
        public void GetExperienceMultiplier_Tier2_Returns1_6()
        {
            Assert.AreEqual(1.6f, this.service.GetExperienceMultiplier(2, this.expMultipliers), 0.0001f);
        }

        [Test]
        public void GetTierLabel_Tier1_ReturnsGood()
        {
            Assert.AreEqual("Good", this.service.GetTierLabel(1, this.labels));
        }

        [Test]
        public void GetTierLabel_NullArray_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, this.service.GetTierLabel(1, null));
        }
    }
}
