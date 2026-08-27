namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 自发关系系统纯规则 — 亲密度累积/升级、记仇、爱慕、事件喂食、每日衰减、查询判定。
    /// </summary>
    [TestFixture]
    public class WorkerRelationshipRuleServiceTests
    {
        [Test]
        public void GetOrCreate_NewTarget_CreatesEntry()
        {
            var mind = new WorkerMindData();

            WorkerRelationEntry entry = WorkerRelationshipRuleService.GetOrCreate(mind, "Tom");

            Assert.IsNotNull(entry);
            Assert.AreEqual("Tom", entry.TargetName);
            Assert.AreEqual(1, mind.Relations.Count);
        }

        [Test]
        public void GetOrCreate_ExistingTarget_ReturnsSame()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.GetOrCreate(mind, "Tom");

            WorkerRelationEntry again = WorkerRelationshipRuleService.GetOrCreate(mind, "Tom");

            Assert.AreEqual(1, mind.Relations.Count);
            Assert.AreSame(again, mind.Relations[0]);
        }

        [Test]
        public void ModifyAffinity_AccumulatesAndUpgradesToFriendship()
        {
            var mind = new WorkerMindData();

            // 3 次 +15 → 45 ≥ 40，最后一次触发 Kind 升级
            Assert.IsFalse(WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 15f, 1));
            Assert.IsFalse(WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 15f, 2));
            Assert.IsTrue(WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 15f, 3));

            Assert.AreEqual(45f, mind.Relations[0].Affinity, 0.0001f);
            Assert.AreEqual(RelationKind.Friendship, mind.Relations[0].Kind);
            Assert.AreEqual(3, mind.Relations[0].LastInteractionDay);
        }

        [Test]
        public void ModifyAffinity_BelowThreshold_StaysNone()
        {
            var mind = new WorkerMindData();

            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 30f, 1);

            Assert.AreEqual(RelationKind.None, mind.Relations[0].Kind);
        }

        [Test]
        public void ModifyAffinity_NegativeAccumulation_DowngradesToEnmity()
        {
            var mind = new WorkerMindData();

            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", -15f, 1);
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", -15f, 2);

            Assert.AreEqual(RelationKind.Enmity, mind.Relations[0].Kind); // -30 ≤ 阈值
        }

        [Test]
        public void ModifyAffinity_CapsAtAbsCap()
        {
            var mind = new WorkerMindData();

            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 999f, 1);

            Assert.AreEqual(WorkerMindConstant.RelationAffinityAbsCap, mind.Relations[0].Affinity, 0.0001f);
        }

        [Test]
        public void AddGrudge_SetsGrudgeKind_OverridesFriendship()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 50f, 1);
            Assert.AreEqual(RelationKind.Friendship, mind.Relations[0].Kind);

            Assert.IsTrue(WorkerRelationshipRuleService.AddGrudge(mind, "Tom", 40f, 2));

            Assert.AreEqual(RelationKind.Grudge, mind.Relations[0].Kind); // 记仇优先
            Assert.Greater(mind.Relations[0].GrudgeLevel, 0f);
        }

        [Test]
        public void AddAdmiration_SetsAdmirationKind()
        {
            var mind = new WorkerMindData();

            Assert.IsTrue(WorkerRelationshipRuleService.AddAdmiration(mind, "Tom", 40f, 1));

            Assert.AreEqual(RelationKind.Admiration, mind.Relations[0].Kind);
        }

        [Test]
        public void Feed_TradeSuccess_IncreasesAffinity()
        {
            var mind = new WorkerMindData();

            WorkerRelationshipRuleService.Feed(mind, "Tom", WorkerMindConstant.EVT_TRADE_SUCCESS, 45f, 1);

            Assert.Greater(mind.Relations[0].Affinity, 0f); // 45 * 0.15 = 6.75
        }

        [Test]
        public void Feed_TradeRejected_AddsGrudge()
        {
            var mind = new WorkerMindData();

            WorkerRelationshipRuleService.Feed(mind, "Tom", WorkerMindConstant.EVT_TRADE_REJECTED, 40f, 1);

            Assert.AreEqual(RelationKind.Grudge, mind.Relations[0].Kind);
            Assert.AreEqual(WorkerMindConstant.RelationTradeRejectGrudge, mind.Relations[0].GrudgeLevel, 0.0001f);
        }

        [Test]
        public void Feed_UnknownType_NoChange()
        {
            var mind = new WorkerMindData();

            bool changed = WorkerRelationshipRuleService.Feed(mind, "Tom", "no_such_event", 40f, 1);

            Assert.IsFalse(changed);
            Assert.AreEqual(0, mind.Relations.Count);
        }

        [Test]
        public void Decay_GrudgeFadesAndEntryRemovedWhenZero()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.AddGrudge(mind, "Tom", 2f, 1); // GrudgeLevel=2

            WorkerRelationshipRuleService.Decay(mind, 2);

            Assert.AreEqual(0, mind.Relations.Count); // 归零后条目移除
        }

        [Test]
        public void Decay_AffinityReturnsToZero()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 5f, 1);

            WorkerRelationshipRuleService.Decay(mind, 2);
            WorkerRelationshipRuleService.Decay(mind, 3);
            WorkerRelationshipRuleService.Decay(mind, 4);
            WorkerRelationshipRuleService.Decay(mind, 5);
            WorkerRelationshipRuleService.Decay(mind, 6);

            Assert.AreEqual(0, mind.Relations.Count); // 5 → 4 → 3 → 2 → 1 → 0 移除
        }

        [Test]
        public void WouldHelp_Friend_True()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 40f, 1); // Friendship

            Assert.IsTrue(WorkerRelationshipRuleService.WouldHelp(mind, "Tom"));
            Assert.IsFalse(WorkerRelationshipRuleService.WouldRefuse(mind, "Tom"));
        }

        [Test]
        public void WouldRefuse_Grudge_True()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.AddGrudge(mind, "Tom", 30f, 1);

            Assert.IsTrue(WorkerRelationshipRuleService.WouldRefuse(mind, "Tom"));
            Assert.IsFalse(WorkerRelationshipRuleService.WouldHelp(mind, "Tom"));
        }

        [Test]
        public void WouldRefuse_NoRelation_False()
        {
            var mind = new WorkerMindData();

            Assert.IsFalse(WorkerRelationshipRuleService.WouldRefuse(mind, "Tom"));
            Assert.IsFalse(WorkerRelationshipRuleService.WouldHelp(mind, "Tom"));
        }

        [Test]
        public void FindGiftTarget_ReturnsFriend()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 40f, 1);

            WorkerRelationEntry gift = WorkerRelationshipRuleService.FindGiftTarget(mind);

            Assert.IsNotNull(gift);
            Assert.AreEqual("Tom", gift.TargetName);
        }

        [Test]
        public void FindGiftTarget_NoFriend_ReturnsNull()
        {
            var mind = new WorkerMindData();

            Assert.IsNull(WorkerRelationshipRuleService.FindGiftTarget(mind));
        }

        [Test]
        public void Remove_RemovesDeadRelation()
        {
            var mind = new WorkerMindData();
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Tom", 40f, 1);
            WorkerRelationshipRuleService.ModifyAffinity(mind, "Jerry", 40f, 1);

            Assert.IsTrue(WorkerRelationshipRuleService.Remove(mind, "Tom"));

            Assert.AreEqual(1, mind.Relations.Count);
            Assert.AreEqual("Jerry", mind.Relations[0].TargetName);
        }
    }
}
