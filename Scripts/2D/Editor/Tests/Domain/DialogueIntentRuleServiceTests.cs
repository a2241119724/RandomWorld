namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>对话预设意图确定性结算规则测试（M3 包2.4）。</summary>
    [TestFixture]
    public class DialogueIntentRuleServiceTests
    {
        private static DialogueIntentInput MakeInput(
            float favor = 50f, float resentment = 0f, float stressRatio = 0.2f,
            int npcRealm = 2, int playerRealm = 0, int usedToday = 0, bool coinsEnough = true)
        {
            return new DialogueIntentInput
            {
                Favorability = favor,
                Resentment = resentment,
                NpcStressRatio = stressRatio,
                NpcRealmIndex = npcRealm,
                PlayerRealmIndex = playerRealm,
                UsedCountToday = usedToday,
                PlayerCoinsEnough = coinsEnough,
                NpcName = "张三",
            };
        }

        [Test]
        public void Evaluate_NullInput_NotAvailable()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(DialogueIntentKind.Gift, null);
            Assert.IsFalse(result.Available);
            Assert.AreEqual("invalid_input", result.OutcomeKey);
        }

        [Test]
        public void Evaluate_DailyCapReached_NotAvailable()
        {
            Assert.IsFalse(DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.TeachSeek, MakeInput(usedToday: DialogueIntentRuleService.TeachDailyCap)).Available);
            Assert.IsFalse(DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Comfort, MakeInput(usedToday: DialogueIntentRuleService.ComfortDailyCap)).Available);
            Assert.IsFalse(DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Apology, MakeInput(usedToday: DialogueIntentRuleService.ApologyDailyCap)).Available);
            Assert.IsFalse(DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Gift, MakeInput(usedToday: DialogueIntentRuleService.GiftDailyCap)).Available);
        }

        [Test]
        public void Evaluate_GiftNoCoins_NotAvailable()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Gift, MakeInput(coinsEnough: false));
            Assert.IsFalse(result.Available);
            Assert.AreEqual("no_coins", result.OutcomeKey);
            Assert.AreEqual(0, result.CoinCost);
        }

        [Test]
        public void EvaluateTeach_NpcRealmNotHigher_Refused()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.TeachSeek, MakeInput(favor: 90f, npcRealm: 1, playerRealm: 1));
            Assert.IsTrue(result.Available);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("teach_refused_realm", result.OutcomeKey);
            Assert.AreEqual(0f, result.PlayerQiGain, 0.0001f);
            Assert.AreEqual(0f, result.FavorDelta, 0.0001f);
        }

        [Test]
        public void EvaluateTeach_FavorBelowThreshold_Refused()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.TeachSeek,
                MakeInput(favor: DialogueIntentRuleService.TeachFavorabilityThreshold - 0.1f, npcRealm: 3, playerRealm: 1));
            Assert.IsTrue(result.Available);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("teach_refused_favor", result.OutcomeKey);
            Assert.AreEqual(0f, result.PlayerQiGain, 0.0001f);
        }

        [Test]
        public void EvaluateTeach_RealmHigherAndFavorEnough_QiGainWithFavorBonus()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.TeachSeek, MakeInput(favor: 80f, npcRealm: 2, playerRealm: 0));
            Assert.IsTrue(result.Available);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("taught", result.OutcomeKey);
            float expectedQi = DialogueIntentRuleService.TeachQiBase
                + 80f * DialogueIntentRuleService.TeachQiPerFavor;
            Assert.AreEqual(expectedQi, result.PlayerQiGain, 0.0001f);
            Assert.AreEqual(3f, result.FavorDelta, 0.0001f);
            Assert.AreEqual(5f, result.GratitudeDelta, 0.0001f);
            Assert.AreEqual(DialogueIntentRuleService.GetEventKey(DialogueIntentKind.TeachSeek), result.EventKey);
        }

        [Test]
        public void EvaluateComfort_StressAboveThreshold_FullEffect()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Comfort,
                MakeInput(stressRatio: DialogueIntentRuleService.ComfortHighStressRatio + 0.01f));
            Assert.IsTrue(result.Available);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("comforted_high", result.OutcomeKey);
            Assert.AreEqual(-15f, result.StressDelta, 0.0001f);
            Assert.AreEqual(5f, result.MoraleDelta, 0.0001f);
            Assert.AreEqual(3f, result.FavorDelta, 0.0001f);
            Assert.AreEqual(3f, result.TrustDelta, 0.0001f);
        }

        [Test]
        public void EvaluateComfort_StressAtOrBelowThreshold_WeakEffect()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Comfort, MakeInput(stressRatio: DialogueIntentRuleService.ComfortHighStressRatio));
            Assert.IsTrue(result.Available);
            Assert.AreEqual("comforted_low", result.OutcomeKey);
            Assert.AreEqual(-5f, result.StressDelta, 0.0001f);
            Assert.AreEqual(0f, result.MoraleDelta, 0.0001f);
            Assert.AreEqual(1f, result.FavorDelta, 0.0001f);
        }

        [Test]
        public void EvaluateApology_WithResentment_FullEffect()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Apology, MakeInput(favor: 80f, resentment: 20f));
            Assert.IsTrue(result.Available);
            Assert.AreEqual("apology_full_resent", result.OutcomeKey);
            // 怨恨化解 = -(8 + 好感*0.06)
            Assert.AreEqual(-(8f + 80f * 0.06f), result.ResentDelta, 0.0001f);
            Assert.AreEqual(4f, result.FavorDelta, 0.0001f);
            Assert.AreEqual(5f, result.TrustDelta, 0.0001f);
        }

        [Test]
        public void EvaluateApology_FavorBelowLineNoResentment_FullEffect()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Apology,
                MakeInput(favor: DialogueIntentRuleService.ApologyLowFavorLine - 1f, resentment: 0f));
            Assert.IsTrue(result.Available);
            Assert.AreEqual("apology_full_favor", result.OutcomeKey);
            Assert.Less(result.ResentDelta, 0f);
            Assert.AreEqual(4f, result.FavorDelta, 0.0001f);
        }

        [Test]
        public void EvaluateApology_NoCause_LightEffect()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Apology, MakeInput(favor: 70f, resentment: 0f));
            Assert.IsTrue(result.Available);
            Assert.AreEqual("apology_light", result.OutcomeKey);
            Assert.AreEqual(0f, result.ResentDelta, 0.0001f);
            Assert.AreEqual(1f, result.FavorDelta, 0.0001f);
            Assert.IsTrue(string.IsNullOrEmpty(result.EventKey));
        }

        [Test]
        public void EvaluateGift_SuccessChargesCoinsAndGratitude()
        {
            DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                DialogueIntentKind.Gift, MakeInput());
            Assert.IsTrue(result.Available);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("gift_done", result.OutcomeKey);
            Assert.AreEqual(DialogueIntentRuleService.GiftCoinCost, result.CoinCost);
            Assert.AreEqual(8f, result.FavorDelta, 0.0001f);
            Assert.AreEqual(10f, result.GratitudeDelta, 0.0001f);
            Assert.AreEqual(DialogueIntentRuleService.GetEventKey(DialogueIntentKind.Gift), result.EventKey);
        }

        [Test]
        public void Evaluate_SuccessBranches_CarryDisplayAndActionText()
        {
            // 全部分支都必须带玩家气泡短句与 LLM 消息（措辞增强管线依赖）
            DialogueIntentKind[] kinds =
            {
                DialogueIntentKind.TeachSeek,
                DialogueIntentKind.Comfort,
                DialogueIntentKind.Apology,
                DialogueIntentKind.Gift,
            };
            foreach (DialogueIntentKind kind in kinds)
            {
                DialogueIntentResult result = DialogueIntentRuleService.Evaluate(
                    kind, MakeInput(favor: 80f, npcRealm: 3, playerRealm: 0));
                Assert.IsTrue(result.Available, kind.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(result.PlayerActionText), kind.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(result.PlayerDisplayText), kind.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(result.FallbackReply), kind.ToString());
            }
        }

        [Test]
        public void GetDailyCap_AllKinds_MatchConstants()
        {
            Assert.AreEqual(DialogueIntentRuleService.TeachDailyCap,
                DialogueIntentRuleService.GetDailyCap(DialogueIntentKind.TeachSeek));
            Assert.AreEqual(DialogueIntentRuleService.ComfortDailyCap,
                DialogueIntentRuleService.GetDailyCap(DialogueIntentKind.Comfort));
            Assert.AreEqual(DialogueIntentRuleService.ApologyDailyCap,
                DialogueIntentRuleService.GetDailyCap(DialogueIntentKind.Apology));
            Assert.AreEqual(DialogueIntentRuleService.GiftDailyCap,
                DialogueIntentRuleService.GetDailyCap(DialogueIntentKind.Gift));
            Assert.AreEqual(4, DialogueIntentRuleService.AllKinds.Length);
        }
    }
}
