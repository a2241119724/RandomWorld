namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 心智层玩家命令接受判定 — 纯规则单测。
    /// 覆盖 CommandAcceptanceRuleService.Evaluate 的全部优先级分支。
    /// </summary>
    [TestFixture]
    public class CommandAcceptanceRuleServiceTests
    {
        // ---- 生存硬阻断（最高优先，交给现有紧急打断/BlocksWhenHungry）----

        [Test]
        public void Evaluate_SurvivalHungry_DelaySurvival()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, curHungry: 10f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonSurvival, reason);
        }

        [Test]
        public void Evaluate_SurvivalTired_DelaySurvival()
        {
            // 疲劳 > MaxTired - 15
            string reason;
            CommandAcceptance r = Eval(out reason, curTired: 90f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonSurvival, reason);
        }

        [Test]
        public void Evaluate_SurvivalSpirit_DelaySurvival()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, curSpirit: 5f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonSurvival, reason);
        }

        [Test]
        public void Evaluate_SurvivalTakesPriorityOverCooldown()
        {
            // 同时命中生存与冷却 → 生存优先（绝不因冷却静默而让濒危 Worker 去干活）
            string reason;
            CommandAcceptance r = Eval(out reason, curHungry: 5f, delayCooldownRemaining: 30f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonSurvival, reason);
        }

        // ---- 拖延冷却（刚拒绝过 → 静默，不重复反馈）----

        [Test]
        public void Evaluate_InCooldown_DelayCooldown()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, delayCooldownRemaining: 10f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonCooldown, reason);
        }

        [Test]
        public void Evaluate_CooldownJustExpired_NotCooldown()
        {
            // 冷却恰好归零 → 不再命中冷却分支
            string reason;
            CommandAcceptance r = Eval(out reason, delayCooldownRemaining: 0f, randomValue: 0.9f);
            Assert.AreNotEqual(WorkerMindConstant.ReasonCooldown, reason);
        }

        // ---- 好感度基础门控（现有规则兼容）----

        [Test]
        public void Evaluate_FavorabilityBelow35_Refuse()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, playerFavorability: 30f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Refuse, r);
            Assert.AreEqual(WorkerMindConstant.ReasonFavorability, reason);
        }

        [Test]
        public void Evaluate_FavorabilityAt35_NotRefuse()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, playerFavorability: 35f, randomValue: 0.9f);
            Assert.AreNotEqual(CommandAcceptance.Refuse, r);
        }

        // ---- 怨恨门控 ----

        [Test]
        public void Evaluate_Resentment85_Refuse()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, resentment: 85f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Refuse, r);
            Assert.AreEqual(WorkerMindConstant.ReasonResentment, reason);
        }

        [Test]
        public void Evaluate_Resentment60AndRandomLow_Refuse()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, resentment: 70f, randomValue: 0.1f);
            Assert.AreEqual(CommandAcceptance.Refuse, r);
            Assert.AreEqual(WorkerMindConstant.ReasonResentment, reason);
        }

        [Test]
        public void Evaluate_Resentment60AndRandomHigh_NotRefuse()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, resentment: 70f, randomValue: 0.9f);
            Assert.AreNotEqual(CommandAcceptance.Refuse, r);
        }

        [Test]
        public void Evaluate_ResentmentBelow60_NotRefuse()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, resentment: 50f, randomValue: 0.0f);
            Assert.AreNotEqual(CommandAcceptance.Refuse, r);
        }

        // ---- 感恩覆盖（怨恨 60-85 带内，感恩 ≥65 覆盖为接受）----

        [Test]
        public void Evaluate_GratitudeOverridesResentmentBand()
        {
            string reason;
            CommandAcceptance r = Eval(
                out reason,
                resentment: 70f, gratitude: 70f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Accept, r);
            Assert.AreEqual(WorkerMindConstant.ReasonGratitude, reason);
        }

        // ---- 意愿度过低 → 拖延 ----

        [Test]
        public void Evaluate_WillingnessBelow25_Delay()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, willingnessToObey: 10f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonWillingness, reason);
        }

        [Test]
        public void Evaluate_WillingnessAt25_NotDelay()
        {
            // 意愿度恰好 25 → 不 <25，落到随机/接受
            string reason;
            CommandAcceptance r = Eval(out reason, willingnessToObey: 25f, randomValue: 0.9f);
            Assert.AreNotEqual(CommandAcceptance.Delay, r);
        }

        [Test]
        public void Evaluate_LowMoodHighRandom_StillAccepts()
        {
            // 心情 10 → 意愿度 50+(50-50)+(10-50)*0.3+(50-50)*0.2 = 38 ≥ 25；
            // random 0.9 不命中随机拖延分支 → 正常接受（低心情不是必然拖延）
            string reason;
            CommandAcceptance r = Eval(out reason, mood: 10f, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Accept, r);
        }

        // ---- 随机个人因素（低概率拖延）----

        [Test]
        public void Evaluate_RandomDelayChanceHit_Delay()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, randomValue: 0.05f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonRandomMood, reason);
        }

        [Test]
        public void Evaluate_LowMoodRaisesRandomDelayChance()
        {
            // 心情 10 <20：random 0.10（≥0.06 但 <0.15）→ 心情低随机拖延命中
            string reason;
            CommandAcceptance r = Eval(out reason, mood: 10f, randomValue: 0.10f);
            Assert.AreEqual(CommandAcceptance.Delay, r);
            Assert.AreEqual(WorkerMindConstant.ReasonRandomMood, reason);
        }

        [Test]
        public void Evaluate_LowMoodRandomMiss_NotDelay()
        {
            // 心情 10 但 random 0.9 → 不命中任何拖延分支（意愿度仍 38 ≥25）
            string reason;
            CommandAcceptance r = Eval(out reason, mood: 10f, randomValue: 0.9f);
            Assert.AreNotEqual(CommandAcceptance.Delay, r);
        }

        // ---- 普通健康 → 接受 ----

        [Test]
        public void Evaluate_Healthy_Accept()
        {
            string reason;
            CommandAcceptance r = Eval(out reason, randomValue: 0.9f);
            Assert.AreEqual(CommandAcceptance.Accept, r);
            Assert.AreEqual(WorkerMindConstant.ReasonAccept, reason);
        }

        // ---- WorkerMindData.Ensure 兜底（BinaryFormatter 读档不跑构造函数）----

        [Test]
        public void Ensure_NullMind_CreatesInstance()
        {
            var wd = new AWorker.WorkerData();
            wd.Mind = null;
            WorkerMindData.Ensure(wd);
            Assert.IsNotNull(wd.Mind);
            Assert.AreEqual(50f, wd.Mind.WillingnessToObey, 0.0001f);
        }

        [Test]
        public void Ensure_ExistingMind_KeepsIt()
        {
            var wd = new AWorker.WorkerData();
            WorkerMindData original = wd.Mind;
            original.WillingnessToObey = 30f;
            WorkerMindData.Ensure(wd);
            Assert.AreSame(original, wd.Mind);
            Assert.AreEqual(30f, wd.Mind.WillingnessToObey, 0.0001f);
        }

        /// <summary>健康基线参数 + 指定覆盖的便捷求值。</summary>
        private static CommandAcceptance Eval(
            out string reasonKey,
            float curHungry = 100f, float maxHungry = 100f,
            float curTired = 0f, float maxTired = 100f,
            float curSpirit = 100f,
            float playerFavorability = 50f,
            float mood = 50f, float morale = 50f,
            float resentment = 0f, float gratitude = 0f, float willingnessToObey = 50f,
            float delayCooldownRemaining = 0f,
            float randomValue = 0.9f)
        {
            return CommandAcceptanceRuleService.Evaluate(
                curHungry, maxHungry,
                curTired, maxTired,
                curSpirit,
                playerFavorability,
                mood, morale,
                resentment, gratitude, willingnessToObey,
                delayCooldownRemaining,
                randomValue,
                out reasonKey);
        }
    }
}
