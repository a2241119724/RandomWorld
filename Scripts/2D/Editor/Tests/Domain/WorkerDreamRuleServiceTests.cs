namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;
    using System.Collections.Generic;

    /// <summary>
    /// Worker 执念纯规则 — 诞生/消退/追求/目标映射。
    /// </summary>
    [TestFixture]
    public class WorkerDreamRuleServiceTests
    {
        [Test]
        public void TryBirth_NoActiveAndUnderChance_CreatesDream()
        {
            var mind = new WorkerMindData();
            Assert.IsTrue(WorkerDreamRuleService.TryBirth(mind, 3, 0.05f));

            Assert.IsFalse(mind.ActiveDream.IsEmpty);
            Assert.AreNotEqual(WorkerDreamType.None, mind.ActiveDream.Type);
            Assert.AreEqual(WorkerDreamRuleService.InitialPassion, mind.ActiveDream.Passion, 0.0001f);
            Assert.AreEqual(3, mind.ActiveDream.BornDay);
        }

        [Test]
        public void TryBirth_OverChance_NoDream()
        {
            var mind = new WorkerMindData();
            Assert.IsFalse(WorkerDreamRuleService.TryBirth(mind, 3, 0.5f));
            Assert.IsTrue(mind.ActiveDream.IsEmpty);
        }

        [Test]
        public void TryBirth_AlreadyHasDream_NoBirth()
        {
            var mind = new WorkerMindData();
            mind.ActiveDream = new WorkerDream { Type = WorkerDreamType.BecomeRich, Passion = 50f };

            Assert.IsFalse(WorkerDreamRuleService.TryBirth(mind, 3, 0.05f));
            Assert.AreEqual(WorkerDreamType.BecomeRich, mind.ActiveDream.Type);
        }

        [Test]
        public void TryBirth_HistoryFull_NoBirth()
        {
            var mind = new WorkerMindData();
            for (int i = 0; i < WorkerMindConstant.DreamHistoryCap; i++)
            {
                mind.DreamHistory.Add(WorkerDream.None);
            }

            Assert.IsFalse(WorkerDreamRuleService.TryBirth(mind, 3, 0.05f));
            Assert.IsTrue(mind.ActiveDream.IsEmpty);
        }

        [Test]
        public void IsPursuable_PassionAboveThreshold_True()
        {
            var dream = new WorkerDream { Type = WorkerDreamType.BecomeRich, Passion = 50f };
            Assert.IsTrue(WorkerDreamRuleService.IsPursuable(dream));
        }

        [Test]
        public void IsPursuable_PassionBelowThreshold_False()
        {
            var dream = new WorkerDream { Type = WorkerDreamType.BecomeRich, Passion = 30f };
            Assert.IsFalse(WorkerDreamRuleService.IsPursuable(dream));
        }

        [Test]
        public void IsPursuable_Empty_NoDream()
        {
            Assert.IsFalse(WorkerDreamRuleService.IsPursuable(WorkerDream.None));
        }

        [Test]
        public void Pursue_ReinforcesPassion_AndUpdatesDay()
        {
            var mind = new WorkerMindData();
            mind.ActiveDream = new WorkerDream { Type = WorkerDreamType.BecomeRich, Passion = 60f };

            WorkerDreamRuleService.Pursue(mind, 5);

            Assert.AreEqual(62f, mind.ActiveDream.Passion, 0.0001f);
            Assert.AreEqual(5, mind.ActiveDream.LastPursuedDay);
        }

        [Test]
        public void Pursue_NoActiveDream_Noop()
        {
            var mind = new WorkerMindData();
            WorkerDreamRuleService.Pursue(mind, 5);
            Assert.IsTrue(mind.ActiveDream.IsEmpty);
        }

        [Test]
        public void Decay_ReducesPassion()
        {
            var mind = new WorkerMindData();
            mind.ActiveDream = new WorkerDream { Type = WorkerDreamType.BecomeRich, Passion = 50f };

            WorkerDreamRuleService.Decay(mind, 1);

            Assert.AreEqual(50f - WorkerMindConstant.DreamPassionDecayPerDay, mind.ActiveDream.Passion, 0.0001f);
        }

        [Test]
        public void Decay_ExhaustsAndArchives()
        {
            var mind = new WorkerMindData();
            mind.ActiveDream = new WorkerDream { Type = WorkerDreamType.BecomeRich, Passion = 2f };

            WorkerDreamRuleService.Decay(mind, 1);

            Assert.IsTrue(mind.ActiveDream.IsEmpty);
            Assert.AreEqual(1, mind.DreamHistory.Count);
            Assert.AreEqual(WorkerDreamType.BecomeRich, mind.DreamHistory[0].Type);
        }

        [Test]
        public void MapToGoal_BecomeRich_EarnMoney()
        {
            var dream = new WorkerDream { Type = WorkerDreamType.BecomeRich };
            WorkerGoal g = WorkerDreamRuleService.MapToGoal(dream, null);
            Assert.AreEqual(WorkerGoalType.EarnMoney, g.Type);
        }

        [Test]
        public void MapToGoal_BuildBigHome_BuildStructure()
        {
            var dream = new WorkerDream { Type = WorkerDreamType.BuildBigHome };
            var materials = new Dictionary<int, int> { { 0, 10 } };
            WorkerGoal g = WorkerDreamRuleService.MapToGoal(dream, materials);
            Assert.AreEqual(WorkerGoalType.BuildStructure, g.Type);
            Assert.IsTrue(g.HasMaterialNeeds);
        }

        [Test]
        public void MapToGoal_MasterCraft_CraftEquipment()
        {
            var dream = new WorkerDream { Type = WorkerDreamType.MasterCraft };
            var materials = new Dictionary<int, int> { { 0, 10 } };
            WorkerGoal g = WorkerDreamRuleService.MapToGoal(dream, materials);
            Assert.AreEqual(WorkerGoalType.CraftEquipment, g.Type);
        }

        [Test]
        public void GetDescription_KnownType_NotEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(WorkerDreamRuleService.GetDescription(WorkerDreamType.WanderWorld)));
        }
    }
}
