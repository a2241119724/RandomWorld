namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 心智层信念演化纯规则 — 事件类型→四维信念增量、强度缩放、clamp 到 [0,100]。
    /// </summary>
    [TestFixture]
    public class WorkerBeliefRuleServiceTests
    {
        [Test]
        public void GetDelta_PlayerHelp_BoostsPlayerTrust()
        {
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_PLAYER_HELP, 100f);
            Assert.AreEqual(8f, d.TrustInPlayer, 0.0001f);
            Assert.AreEqual(3f, d.TrustInWorld, 0.0001f);
            Assert.AreEqual(2f, d.SenseOfBelonging, 0.0001f);
            Assert.AreEqual(0f, d.SelfEsteem, 0.0001f);
        }

        [Test]
        public void GetDelta_PlayerAttack_DamagesPlayerTrust()
        {
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_PLAYER_ATTACK, 100f);
            Assert.AreEqual(-10f, d.TrustInPlayer, 0.0001f);
            Assert.AreEqual(-5f, d.TrustInWorld, 0.0001f);
        }

        [Test]
        public void GetDelta_PlayerKill_HeaviestPenalty()
        {
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_PLAYER_KILL, 100f);
            Assert.AreEqual(-20f, d.TrustInPlayer, 0.0001f);
            Assert.AreEqual(-12f, d.TrustInWorld, 0.0001f);
            Assert.AreEqual(-5f, d.SelfEsteem, 0.0001f);
        }

        [Test]
        public void GetDelta_ScalesByIntensity()
        {
            // 强度减半 → 增量减半（-20 * 0.5 = -10）
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_PLAYER_KILL, 50f);
            Assert.AreEqual(-10f, d.TrustInPlayer, 0.0001f);
        }

        [Test]
        public void GetDelta_ClampsIntensity()
        {
            // 强度超 100 → 按 100 计算
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_PLAYER_KILL, 200f);
            Assert.AreEqual(-20f, d.TrustInPlayer, 0.0001f);
        }

        [Test]
        public void GetDelta_GroundSleep_DentsSelfEsteem()
        {
            // 地面睡眠（WorkerSleepTask.Finish 无床分支的生产者已接线）
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_GROUND_SLEEP, 100f);
            Assert.AreEqual(-2f, d.SelfEsteem, 0.0001f);
            Assert.AreEqual(0f, d.TrustInWorld, 0.0001f);
            Assert.AreEqual(0f, d.TrustInPlayer, 0.0001f);
            Assert.AreEqual(0f, d.SenseOfBelonging, 0.0001f);
        }

        [Test]
        public void GetDelta_FoundItem_BoostsSelfEsteem()
        {
            // 拾获小确幸（WorkerMindService.RecordFoundItem 每日节流，生产者已接线）
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_FOUND_ITEM, 100f);
            Assert.AreEqual(2f, d.SelfEsteem, 0.0001f);
            Assert.AreEqual(0f, d.TrustInWorld, 0.0001f);
            Assert.AreEqual(0f, d.TrustInPlayer, 0.0001f);
            Assert.AreEqual(0f, d.SenseOfBelonging, 0.0001f);
        }

        [Test]
        public void GetDelta_UnknownType_Zero()
        {
            BeliefDelta d = WorkerBeliefRuleService.GetDelta("no_such_event", 100f);
            Assert.IsTrue(d.IsZero);
        }

        [Test]
        public void GetDelta_IntensityZero_Zero()
        {
            BeliefDelta d = WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_PLAYER_KILL, 0f);
            Assert.IsTrue(d.IsZero);
        }

        [Test]
        public void Apply_AddsAndClampsUpper()
        {
            var mind = new WorkerMindData();
            mind.TrustInPlayer = 95f;
            WorkerBeliefRuleService.Apply(mind, new BeliefDelta { TrustInPlayer = 20f });
            Assert.AreEqual(100f, mind.TrustInPlayer, 0.0001f); // 95+20 → clamp 100
        }

        [Test]
        public void Apply_AddsAndClampsLower()
        {
            var mind = new WorkerMindData();
            mind.TrustInWorld = 5f;
            WorkerBeliefRuleService.Apply(mind, new BeliefDelta { TrustInWorld = -10f });
            Assert.AreEqual(0f, mind.TrustInWorld, 0.0001f); // 5-10 → clamp 0
        }

        [Test]
        public void Apply_ZeroDelta_NoChange()
        {
            var mind = new WorkerMindData();
            float before = mind.TrustInPlayer;
            WorkerBeliefRuleService.Apply(mind, BeliefDelta.Zero);
            Assert.AreEqual(before, mind.TrustInPlayer, 0.0001f);
        }

        [Test]
        public void Apply_EventDelta_ModifiesOnlyRelevantDimensions()
        {
            var mind = new WorkerMindData();
            float worldBefore = mind.TrustInWorld;
            float playerBefore = mind.TrustInPlayer;
            float esteemBefore = mind.SelfEsteem;
            float belongBefore = mind.SenseOfBelonging;

            // 交易成功只动 TrustInWorld/SenseOfBelonging
            WorkerBeliefRuleService.Apply(mind, WorkerBeliefRuleService.GetDelta(WorkerMindConstant.EVT_TRADE_SUCCESS, 100f));

            Assert.AreEqual(worldBefore + 2f, mind.TrustInWorld, 0.0001f);
            Assert.AreEqual(playerBefore, mind.TrustInPlayer, 0.0001f);
            Assert.AreEqual(esteemBefore, mind.SelfEsteem, 0.0001f);
            Assert.AreEqual(belongBefore + 1f, mind.SenseOfBelonging, 0.0001f);
        }
    }
}
