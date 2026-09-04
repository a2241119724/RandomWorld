namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character.Growth;
    using NUnit.Framework;

    [TestFixture]
    public class GrowthDataTests
    {
        [Test]
        public void Ensure_NullReference_CreatesInstance()
        {
            GrowthData growth = null;
            GrowthData.Ensure(ref growth);
            Assert.IsNotNull(growth);
        }

        [Test]
        public void Ensure_NullCollections_BackfillsAll()
        {
            GrowthData growth = new GrowthData();
            GrowthData.Ensure(ref growth);
            Assert.IsNotNull(growth.LingGenElements);
            Assert.IsNotNull(growth.LearnedGongFaIds);
            Assert.IsNotNull(growth.AwakenedPowerIds);
            Assert.IsTrue(string.IsNullOrEmpty(growth.ActiveNeiGongId));
            Assert.AreEqual(0, growth.RealmIndex);
        }

        [Test]
        public void Ensure_ExistingData_Preserved()
        {
            GrowthData growth = new GrowthData();
            // 先建集合模拟读档后已有数据（构造函数无字段初始化器，直接 Add 会 NullRef）
            growth.LingGenElements = new System.Collections.Generic.List<int> { 3 };
            growth.LearnedGongFaIds = new System.Collections.Generic.List<string> { "gongfa_a" };
            growth.Qi = 42f;

            GrowthData.Ensure(ref growth);

            Assert.AreEqual(1, growth.LingGenElements.Count);
            Assert.AreEqual(1, growth.LearnedGongFaIds.Count);
            Assert.AreEqual(42f, growth.Qi, 0.0001f);
        }

        [Test]
        public void Ensure_Idempotent_CollectionReferencesStable()
        {
            GrowthData growth = new GrowthData();
            GrowthData.Ensure(ref growth);
            var lingGens = growth.LingGenElements;
            var gongFas = growth.LearnedGongFaIds;

            GrowthData.Ensure(ref growth);

            Assert.AreSame(lingGens, growth.LingGenElements);
            Assert.AreSame(gongFas, growth.LearnedGongFaIds);
        }
    }
}
