namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 漫游发现物品纯规则 — 池均匀随机、数量范围、边界钳制、确定性。
    /// </summary>
    [TestFixture]
    public class WanderDiscoveryRuleServiceTests
    {
        [Test]
        public void TryRoll_EmptyPool_ReturnsFalse()
        {
            Assert.IsFalse(WanderDiscoveryRuleService.TryRoll(null, 0.5f, 0.5f, out _, out _));
            Assert.IsFalse(WanderDiscoveryRuleService.TryRoll(new int[0], 0.5f, 0.5f, out _, out _));
        }

        [Test]
        public void TryRoll_SingleItem_AlwaysSelected()
        {
            for (float roll = 0f; roll <= 1.0001f; roll += 0.25f)
            {
                Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(new[] { 200001 }, roll, 0.5f, out int itemId, out _));
                Assert.AreEqual(200001, itemId);
            }
        }

        [Test]
        public void TryRoll_TwoItems_SplitsByRoll()
        {
            // 均匀分桶：roll < 0.5 落第一桶，>= 0.5 落第二桶
            int[] pool = { 200001, 200002 };

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0f, 0f, out int first, out _));
            Assert.AreEqual(200001, first);

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0.49f, 0f, out first, out _));
            Assert.AreEqual(200001, first);

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0.5f, 0f, out int second, out _));
            Assert.AreEqual(200002, second);

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 1f, 0f, out second, out _));
            Assert.AreEqual(200002, second);
        }

        [Test]
        public void TryRoll_CountStaysInRange()
        {
            int[] pool = { 200001 };

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0f, 0f, out _, out int min));
            Assert.AreEqual(WanderDiscoveryRuleService.MinCount, min);

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0f, 0.999f, out _, out int max));
            Assert.AreEqual(WanderDiscoveryRuleService.MaxCount, max);

            // roll == 1.0 边界钳制到上限，不越界
            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0f, 1f, out _, out max));
            Assert.AreEqual(WanderDiscoveryRuleService.MaxCount, max);
        }

        [Test]
        public void TryRoll_RollOutOfRange_Clamped()
        {
            int[] pool = { 200001, 200002 };

            // 负 roll / 超 1 roll 不炸不越界
            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, -0.5f, -1f, out int itemId, out int count));
            Assert.AreEqual(200001, itemId);
            Assert.AreEqual(WanderDiscoveryRuleService.MinCount, count);

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 1.5f, 2f, out itemId, out count));
            Assert.AreEqual(200002, itemId);
            Assert.AreEqual(WanderDiscoveryRuleService.MaxCount, count);
        }

        [Test]
        public void TryRoll_Deterministic_SameInputSameOutput()
        {
            int[] pool = { 200001, 200002, 200003 };

            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0.34f, 0.7f, out int id1, out int count1));
            Assert.IsTrue(WanderDiscoveryRuleService.TryRoll(pool, 0.34f, 0.7f, out int id2, out int count2));

            Assert.AreEqual(id1, id2);
            Assert.AreEqual(count1, count2);
        }
    }
}
