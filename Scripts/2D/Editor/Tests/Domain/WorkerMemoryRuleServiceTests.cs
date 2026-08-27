namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 心智层事件记忆纯规则 — 写入/容量淘汰/逐日衰减/遗忘剔除/最近查找。
    /// </summary>
    [TestFixture]
    public class WorkerMemoryRuleServiceTests
    {
        [Test]
        public void AddMemory_AddsEntryWithDefaults()
        {
            var mind = new WorkerMindData();
            bool ok = WorkerMemoryRuleService.AddMemory(mind, 3, "evt_test", MemoryValence.Positive, "PLAYER", 60f);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, mind.Memories.Count);
            WorkerMemoryEntry e = mind.Memories[0];
            Assert.AreEqual(3, e.Day);
            Assert.AreEqual("evt_test", e.TypeKey);
            Assert.AreEqual(MemoryValence.Positive, e.Valence);
            Assert.AreEqual("PLAYER", e.TargetName);
            Assert.AreEqual(60f, e.Intensity, 0.0001f);
            Assert.AreEqual(1f, e.Weight, 0.0001f);
        }

        [Test]
        public void AddMemory_ClampsIntensity()
        {
            var mind = new WorkerMindData();
            WorkerMemoryRuleService.AddMemory(mind, 1, "a", MemoryValence.Neutral, null, 150f);
            WorkerMemoryRuleService.AddMemory(mind, 1, "b", MemoryValence.Neutral, null, -5f);

            Assert.AreEqual(100f, mind.Memories[0].Intensity, 0.0001f);
            Assert.AreEqual(0f, mind.Memories[1].Intensity, 0.0001f);
        }

        [Test]
        public void AddMemory_EvictsOldestWhenOverCap()
        {
            var mind = new WorkerMindData();
            mind.MemoryCap = 2;

            WorkerMemoryRuleService.AddMemory(mind, 1, "oldest", MemoryValence.Neutral, null, 10f);
            WorkerMemoryRuleService.AddMemory(mind, 2, "mid", MemoryValence.Neutral, null, 10f);
            WorkerMemoryRuleService.AddMemory(mind, 3, "newest", MemoryValence.Neutral, null, 10f);

            Assert.AreEqual(2, mind.Memories.Count);
            // 最早（day 1）被淘汰
            foreach (WorkerMemoryEntry e in mind.Memories)
            {
                Assert.AreNotEqual("oldest", e.TypeKey);
            }
        }

        [Test]
        public void AddMemory_NullMind_ReturnsFalse()
        {
            Assert.IsFalse(WorkerMemoryRuleService.AddMemory(null, 1, "a", MemoryValence.Neutral, null, 10f));
        }

        [Test]
        public void AddMemory_EmptyTypeKey_ReturnsFalse()
        {
            var mind = new WorkerMindData();
            Assert.IsFalse(WorkerMemoryRuleService.AddMemory(mind, 1, string.Empty, MemoryValence.Neutral, null, 10f));
            Assert.AreEqual(0, mind.Memories.Count);
        }

        [Test]
        public void TickDay_DecaysWeight()
        {
            var mind = new WorkerMindData();
            WorkerMemoryRuleService.AddMemory(mind, 1, "a", MemoryValence.Neutral, null, 10f);

            int removed = WorkerMemoryRuleService.TickDay(mind, 2);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(1, mind.Memories.Count);
            Assert.AreEqual(1f - WorkerMindConstant.MemoryForgetRatePerDay, mind.Memories[0].Weight, 0.0001f);
        }

        [Test]
        public void TickDay_PrunesBelowThreshold()
        {
            var mind = new WorkerMindData();
            WorkerMemoryRuleService.AddMemory(mind, 1, "a", MemoryValence.Neutral, null, 10f);
            mind.Memories[0].Weight = 0.15f; // 仅略高于剪枝阈值 0.12

            int removed = WorkerMemoryRuleService.TickDay(mind, 2);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(0, mind.Memories.Count);
        }

        [Test]
        public void TickDay_Empty_ReturnsZero()
        {
            var mind = new WorkerMindData();
            Assert.AreEqual(0, WorkerMemoryRuleService.TickDay(mind, 2));
        }

        [Test]
        public void FindRecent_ReturnsLatest()
        {
            var mind = new WorkerMindData();
            WorkerMemoryRuleService.AddMemory(mind, 1, "early", MemoryValence.Neutral, null, 10f);
            WorkerMemoryRuleService.AddMemory(mind, 2, "late", MemoryValence.Neutral, null, 10f);

            WorkerMemoryEntry e = WorkerMemoryRuleService.FindRecent(mind);
            Assert.IsNotNull(e);
            Assert.AreEqual("late", e.TypeKey);
        }

        [Test]
        public void FindRecent_FiltersByType()
        {
            var mind = new WorkerMindData();
            WorkerMemoryRuleService.AddMemory(mind, 1, "a", MemoryValence.Neutral, null, 10f);
            WorkerMemoryRuleService.AddMemory(mind, 2, "b", MemoryValence.Neutral, null, 10f);

            WorkerMemoryEntry e = WorkerMemoryRuleService.FindRecent(mind, "a");
            Assert.IsNotNull(e);
            Assert.AreEqual("a", e.TypeKey);
        }

        [Test]
        public void FindRecent_NoMatch_ReturnsNull()
        {
            var mind = new WorkerMindData();
            WorkerMemoryRuleService.AddMemory(mind, 1, "a", MemoryValence.Neutral, null, 10f);

            Assert.IsNull(WorkerMemoryRuleService.FindRecent(mind, "missing"));
        }
    }
}
