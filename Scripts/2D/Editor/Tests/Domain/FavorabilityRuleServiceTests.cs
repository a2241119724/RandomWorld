namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class FavorabilityRuleServiceTests
    {
        // ---- Clamp ----

        [Test]
        public void Clamp_InRange_ReturnsSame()
        {
            Assert.AreEqual(50f, FavorabilityRuleService.Clamp(50f), 0.0001f);
        }

        [Test]
        public void Clamp_BelowMin_Returns0()
        {
            Assert.AreEqual(0f, FavorabilityRuleService.Clamp(-5f), 0.0001f);
        }

        [Test]
        public void Clamp_AboveMax_Returns100()
        {
            Assert.AreEqual(100f, FavorabilityRuleService.Clamp(150f), 0.0001f);
        }

        // ---- 悬赏接受门控 ----

        [Test]
        public void IsWillingForPlayerBounty_BelowThreshold_ReturnsFalse()
        {
            Assert.IsFalse(FavorabilityRuleService.IsWillingForPlayerBounty(34f));
        }

        [Test]
        public void IsWillingForPlayerBounty_AtThreshold_ReturnsTrue()
        {
            Assert.IsTrue(FavorabilityRuleService.IsWillingForPlayerBounty(35f));
        }

        [Test]
        public void IsWillingForWorkerBounty_BelowThreshold_ReturnsFalse()
        {
            Assert.IsFalse(FavorabilityRuleService.IsWillingForWorkerBounty(39f));
        }

        [Test]
        public void IsWillingForWorkerBounty_AtThreshold_ReturnsTrue()
        {
            Assert.IsTrue(FavorabilityRuleService.IsWillingForWorkerBounty(40f));
        }

        // ---- 交易意愿门控 ----

        [Test]
        public void IsWillingToTrade_BelowThreshold_ReturnsFalse()
        {
            Assert.IsFalse(FavorabilityRuleService.IsWillingToTrade(29f));
        }

        [Test]
        public void IsWillingToTrade_AtThreshold_ReturnsTrue()
        {
            Assert.IsTrue(FavorabilityRuleService.IsWillingToTrade(30f));
        }

        // ---- 交易价格乘数 ----

        [Test]
        public void GetTradePriceMultiplier_Neutral_Returns1()
        {
            Assert.AreEqual(1.0f, FavorabilityRuleService.GetTradePriceMultiplier(50f), 0.0001f);
        }

        [Test]
        public void GetTradePriceMultiplier_HighFavor_ReturnsDiscount()
        {
            Assert.AreEqual(0.8f, FavorabilityRuleService.GetTradePriceMultiplier(100f), 0.0001f);
        }

        [Test]
        public void GetTradePriceMultiplier_LowFavor_ReturnsSurcharge()
        {
            Assert.AreEqual(1.2f, FavorabilityRuleService.GetTradePriceMultiplier(0f), 0.0001f);
        }

        [Test]
        public void GetTradePriceMultiplier_Extreme_IsClamped()
        {
            Assert.AreEqual(0.7f, FavorabilityRuleService.GetTradePriceMultiplier(1000f), 0.0001f);
            Assert.AreEqual(1.3f, FavorabilityRuleService.GetTradePriceMultiplier(-1000f), 0.0001f);
        }

        // ---- 态度标签 ----

        [Test]
        public void GetAttitudeLabel_Enemy_Returns敌对()
        {
            Assert.AreEqual("敌对", FavorabilityRuleService.GetAttitudeLabel(0f));
            Assert.AreEqual("敌对", FavorabilityRuleService.GetAttitudeLabel(29.9f));
        }

        [Test]
        public void GetAttitudeLabel_Alienated_Returns疏远()
        {
            Assert.AreEqual("疏远", FavorabilityRuleService.GetAttitudeLabel(30f));
            Assert.AreEqual("疏远", FavorabilityRuleService.GetAttitudeLabel(49.9f));
        }

        [Test]
        public void GetAttitudeLabel_Friendly_Returns友好()
        {
            Assert.AreEqual("友好", FavorabilityRuleService.GetAttitudeLabel(50f));
            Assert.AreEqual("友好", FavorabilityRuleService.GetAttitudeLabel(69.9f));
        }

        [Test]
        public void GetAttitudeLabel_Close_Returns亲近()
        {
            Assert.AreEqual("亲近", FavorabilityRuleService.GetAttitudeLabel(70f));
            Assert.AreEqual("亲近", FavorabilityRuleService.GetAttitudeLabel(84.9f));
        }

        [Test]
        public void GetAttitudeLabel_BestFriend_Returns挚友()
        {
            Assert.AreEqual("挚友", FavorabilityRuleService.GetAttitudeLabel(85f));
            Assert.AreEqual("挚友", FavorabilityRuleService.GetAttitudeLabel(100f));
        }

        // ---- 悬赏完成增益 ----

        [Test]
        public void GetPlayerBountyCompleteGain_LowReward_Returns4()
        {
            Assert.AreEqual(4f, FavorabilityRuleService.GetPlayerBountyCompleteGain(30f), 0.0001f);
        }

        [Test]
        public void GetPlayerBountyCompleteGain_NormalReward_Returns8()
        {
            Assert.AreEqual(8f, FavorabilityRuleService.GetPlayerBountyCompleteGain(60f), 0.0001f);
        }

        // ---- 对话每日限额 ----

        [Test]
        public void IsConversationAllowed_UnderCap_ReturnsTrue()
        {
            Assert.IsTrue(FavorabilityRuleService.IsConversationAllowed(9, 10));
        }

        [Test]
        public void IsConversationAllowed_AtCap_ReturnsFalse()
        {
            Assert.IsFalse(FavorabilityRuleService.IsConversationAllowed(10, 10));
        }

        // ---- Mood 联动 ----

        [Test]
        public void GetMoodDelta_SmallDelta_Returns0()
        {
            Assert.AreEqual(0f, FavorabilityRuleService.GetMoodDelta(4f), 0.0001f);
            Assert.AreEqual(0f, FavorabilityRuleService.GetMoodDelta(-4f), 0.0001f);
        }

        [Test]
        public void GetMoodDelta_Positive_ReturnsScaled()
        {
            Assert.AreEqual(0.5f, FavorabilityRuleService.GetMoodDelta(10f), 0.0001f);
        }

        [Test]
        public void GetMoodDelta_Negative_ReturnsNegativeScaled()
        {
            Assert.AreEqual(-0.5f, FavorabilityRuleService.GetMoodDelta(-10f), 0.0001f);
        }

        [Test]
        public void GetMoodDelta_Huge_IsClamped()
        {
            Assert.AreEqual(5f, FavorabilityRuleService.GetMoodDelta(1000f), 0.0001f);
            Assert.AreEqual(-5f, FavorabilityRuleService.GetMoodDelta(-1000f), 0.0001f);
        }
    }
}
