namespace LAB2D.Editor.Tests.Tool
{
    using LAB2D.Render;
    using NUnit.Framework;

    /// <summary>
    /// YSortAlgorithm.AssignOrders 纯函数单测：
    /// 按底端 y 降序分配唯一 sortingOrder（y 大→order 小→先绘制被覆盖；y 小→order 大→后绘制盖住上方）。
    /// </summary>
    [TestFixture]
    public class YSortOrderTests
    {
        [Test]
        public void AssignOrders_DescendingInput_GetsAscendingOrders()
        {
            // 已是降序：底端 y 大的索引 0 先画（order 0）
            int[] orders = YSortAlgorithm.AssignOrders(new float[] { 10f, 5f, 1f });
            Assert.AreEqual(new[] { 0, 1, 2 }, orders);
        }

        [Test]
        public void AssignOrders_AscendingInput_GetsReversedOrders()
        {
            // 升序输入：底端 y 最小的索引 0 后画（order 最大）
            int[] orders = YSortAlgorithm.AssignOrders(new float[] { 1f, 5f, 10f });
            Assert.AreEqual(new[] { 2, 1, 0 }, orders);
        }

        [Test]
        public void AssignOrders_EqualBottomY_IsStableAndUnique()
        {
            // 相同底端 y：保持原索引顺序（稳定），且 order 唯一不重复
            int[] orders = YSortAlgorithm.AssignOrders(new float[] { 5f, 5f, 3f });
            Assert.AreEqual(new[] { 0, 1, 2 }, orders);
        }

        [Test]
        public void AssignOrders_Empty_ReturnsEmpty()
        {
            int[] orders = YSortAlgorithm.AssignOrders(new float[] { });
            Assert.AreEqual(0, orders.Length);
        }

        [Test]
        public void AssignOrders_SingleElement_ReturnsZero()
        {
            int[] orders = YSortAlgorithm.AssignOrders(new float[] { 7f });
            Assert.AreEqual(new[] { 0 }, orders);
        }
    }
}
