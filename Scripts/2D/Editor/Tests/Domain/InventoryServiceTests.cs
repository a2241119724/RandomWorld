namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;
    using System.Collections.Generic;

    /// <summary>
    /// InventoryService 单元测试 — 验证纯 C# 库存服务的所有数据操作。
    /// 可在 EditMode 下运行，无需 Unity 场景。
    /// </summary>
    [TestFixture]
    public class InventoryServiceTests
    {
        private InventoryService service;
        private List<IGameEvent> publishedEvents;

        [SetUp]
        public void SetUp()
        {
            this.publishedEvents = new List<IGameEvent>();
            this.service = new InventoryService(
                managerName: "TestInventory",
                gridWidth: 100,
                gridHeight: 100,
                cellCapacity: 1000,
                itemTypeResolver: TestItemTypeResolver,
                eventPublisher: (e) => this.publishedEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            this.service = null;
            this.publishedEvents = null;
        }

        private static int TestItemTypeResolver(int itemId)
        {
            // 简单映射: 1-9 → Food(1), 10-19 → Material(2), 20-29 → Seed(3)
            if (itemId >= 1 && itemId <= 9) return 1;
            if (itemId >= 10 && itemId <= 19) return 2;
            if (itemId >= 20 && itemId <= 29) return 3;
            return 0; // Null
        }

        // ---- 格子管理测试 ----

        [Test]
        public void EnsureCell_CreatesCell()
        {
            GameGridPosition pos = new GameGridPosition(5, 5);
            InventoryCell cell = this.service.EnsureCell(pos);

            Assert.IsNotNull(cell);
            Assert.IsTrue(cell.IsEmpty);
            Assert.AreEqual(1000, cell.Capacity);
            Assert.AreEqual(1, this.service.CellCount);
        }

        [Test]
        public void EnsureCell_SamePosition_ReturnsExistingCell()
        {
            GameGridPosition pos = new GameGridPosition(5, 5);
            InventoryCell cell1 = this.service.EnsureCell(pos);
            InventoryCell cell2 = this.service.EnsureCell(pos);

            Assert.AreSame(cell1, cell2);
            Assert.AreEqual(1, this.service.CellCount);
        }

        [Test]
        public void HasCell_ReturnsTrueForExistingCell()
        {
            GameGridPosition pos = new GameGridPosition(3, 3);
            this.service.EnsureCell(pos);

            Assert.IsTrue(this.service.HasCell(pos));
            Assert.IsFalse(this.service.HasCell(new GameGridPosition(99, 99)));
        }

        // ---- 物品添加测试 ----

        [Test]
        public void AddItem_ToEmptyCell_Succeeds()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);

            bool result = this.service.AddItem(pos, 10, 5);

            Assert.IsTrue(result);
            InventoryCell cell = this.service.GetCell(pos);
            Assert.AreEqual(10, cell.ItemId);
            Assert.AreEqual(5, cell.Count);

            // 验证事件
            Assert.AreEqual(1, this.publishedEvents.Count);
            InventoryGridChangedEvent evt = this.publishedEvents[0] as InventoryGridChangedEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual(InventoryChangeType.Added, evt.ChangeType);
            Assert.AreEqual(10, evt.ItemId);
            Assert.AreEqual(5, evt.Count);
        }

        [Test]
        public void AddItem_SameId_Stacks()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);
            this.publishedEvents.Clear();

            bool result = this.service.AddItem(pos, 10, 3);

            Assert.IsTrue(result);
            InventoryCell cell = this.service.GetCell(pos);
            Assert.AreEqual(10, cell.ItemId);
            Assert.AreEqual(8, cell.Count);

            Assert.AreEqual(1, this.publishedEvents.Count);
            InventoryGridChangedEvent evt = this.publishedEvents[0] as InventoryGridChangedEvent;
            Assert.AreEqual(InventoryChangeType.CountChanged, evt.ChangeType);
        }

        [Test]
        public void AddItem_DifferentId_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);

            bool result = this.service.AddItem(pos, 20, 3);

            Assert.IsFalse(result);
            InventoryCell cell = this.service.GetCell(pos);
            Assert.AreEqual(10, cell.ItemId); // 未改变
            Assert.AreEqual(5, cell.Count);   // 未改变
        }

        [Test]
        public void AddItem_ExceedsCapacity_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos, 10); // 容量=10
            this.service.AddItem(pos, 10, 8);

            bool result = this.service.AddItem(pos, 10, 5); // 超出容量

            Assert.IsFalse(result);
            Assert.AreEqual(8, this.service.GetCell(pos).Count);
        }

        [Test]
        public void AddItem_ZeroCount_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);

            bool result = this.service.AddItem(pos, 10, 0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, this.publishedEvents.Count);
        }

        [Test]
        public void AddItem_NegativeCount_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);

            bool result = this.service.AddItem(pos, 10, -3);

            Assert.IsFalse(result);
        }

        [Test]
        public void AddItem_AutoCreatesCell()
        {
            GameGridPosition pos = new GameGridPosition(10, 10);

            // 不先调用 EnsureCell，直接 AddItem
            bool result = this.service.AddItem(pos, 10, 5);

            Assert.IsTrue(result);
            Assert.IsTrue(this.service.HasCell(pos));
            Assert.AreEqual(5, this.service.GetCell(pos).Count);
        }

        // ---- 物品取出测试 ----

        [Test]
        public void TakeItem_RemovesCorrectAmount()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 10);
            this.publishedEvents.Clear();

            int taken = this.service.TakeItem(pos, 3);

            Assert.AreEqual(3, taken);
            Assert.AreEqual(7, this.service.GetCell(pos).Count);
            Assert.AreEqual(1, this.publishedEvents.Count);
            Assert.AreEqual(InventoryChangeType.CountChanged,
                ((InventoryGridChangedEvent)this.publishedEvents[0]).ChangeType);
        }

        [Test]
        public void TakeItem_MoreThanAvailable_TakesAll()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);
            this.publishedEvents.Clear();

            int taken = this.service.TakeItem(pos, 10);

            Assert.AreEqual(5, taken);
            Assert.IsTrue(this.service.GetCell(pos).IsEmpty);
            Assert.AreEqual(InventoryChangeType.Cleared,
                ((InventoryGridChangedEvent)this.publishedEvents[0]).ChangeType);
        }

        [Test]
        public void TakeItem_FromEmptyCell_ReturnsZero()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);

            int taken = this.service.TakeItem(pos, 5);

            Assert.AreEqual(0, taken);
            Assert.AreEqual(0, this.publishedEvents.Count);
        }

        [Test]
        public void TakeItem_NonExistentCell_ReturnsZero()
        {
            int taken = this.service.TakeItem(new GameGridPosition(99, 99), 5);
            Assert.AreEqual(0, taken);
        }

        // ---- 清空格子测试 ----

        [Test]
        public void ClearCell_RemovesAllItems()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);
            this.publishedEvents.Clear();

            this.service.ClearCell(pos);

            Assert.IsTrue(this.service.GetCell(pos).IsEmpty);
            Assert.AreEqual(1, this.publishedEvents.Count);
            InventoryGridChangedEvent evt = this.publishedEvents[0] as InventoryGridChangedEvent;
            Assert.AreEqual(InventoryChangeType.Cleared, evt.ChangeType);
        }

        [Test]
        public void ClearCell_EmptyCell_DoesNotPublishEvent()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);

            this.service.ClearCell(pos);

            Assert.AreEqual(0, this.publishedEvents.Count);
        }

        // ---- 查询测试 ----

        [Test]
        public void GetTotalCount_SumsAcrossCells()
        {
            this.service.EnsureCell(new GameGridPosition(0, 0));
            this.service.EnsureCell(new GameGridPosition(1, 1));
            this.service.EnsureCell(new GameGridPosition(2, 2));
            this.service.AddItem(new GameGridPosition(0, 0), 10, 5);
            this.service.AddItem(new GameGridPosition(1, 1), 10, 3);
            this.service.AddItem(new GameGridPosition(2, 2), 20, 7);

            Assert.AreEqual(8, this.service.GetTotalCount(10));
            Assert.AreEqual(7, this.service.GetTotalCount(20));
            Assert.AreEqual(0, this.service.GetTotalCount(99));
        }

        [Test]
        public void GetPositionsById_ReturnsCorrectPositions()
        {
            this.service.EnsureCell(new GameGridPosition(0, 0));
            this.service.EnsureCell(new GameGridPosition(5, 5));
            this.service.EnsureCell(new GameGridPosition(10, 10));
            this.service.AddItem(new GameGridPosition(0, 0), 10, 1);
            this.service.AddItem(new GameGridPosition(5, 5), 10, 1);
            this.service.AddItem(new GameGridPosition(10, 10), 20, 1);

            var positions = this.service.GetPositionsById(10);
            Assert.AreEqual(2, positions.Count);
        }

        [Test]
        public void GetPositionsByType_ReturnsCorrectPositions()
        {
            // itemId=1 → type=1 (Food)
            this.service.EnsureCell(new GameGridPosition(0, 0));
            this.service.EnsureCell(new GameGridPosition(1, 1));
            this.service.EnsureCell(new GameGridPosition(2, 2));
            this.service.AddItem(new GameGridPosition(0, 0), 1, 1);  // Food
            this.service.AddItem(new GameGridPosition(1, 1), 2, 1);  // Food
            this.service.AddItem(new GameGridPosition(2, 2), 10, 1); // Material

            var foodPositions = this.service.GetPositionsByType(1);
            Assert.AreEqual(2, foodPositions.Count);

            var materialPositions = this.service.GetPositionsByType(2);
            Assert.AreEqual(1, materialPositions.Count);
        }

        [Test]
        public void FindBestPositionForItem_PrefersExistingStack()
        {
            this.service.EnsureCell(new GameGridPosition(0, 0));
            this.service.EnsureCell(new GameGridPosition(1, 1));
            this.service.AddItem(new GameGridPosition(1, 1), 10, 5);

            GameGridPosition best = this.service.FindBestPositionForItem(10, 3);

            // 应优先选择已有 itemId=10 的格子
            Assert.AreEqual(new GameGridPosition(1, 1), best);
        }

        [Test]
        public void FindBestPositionForItem_FallsBackToEmptyCell()
        {
            this.service.EnsureCell(new GameGridPosition(0, 0));

            GameGridPosition best = this.service.FindBestPositionForItem(10, 3);

            // 无同 ID 的格子，应选择空格子
            Assert.AreEqual(new GameGridPosition(0, 0), best);
        }

        [Test]
        public void FindBestPositionForItem_ReturnsDefaultWhenFull()
        {
            // 不创建任何格子
            GameGridPosition best = this.service.FindBestPositionForItem(10, 3);

            Assert.AreEqual(default(GameGridPosition), best);
        }

        // ---- 容量查询测试 ----

        [Test]
        public void GetAvailableCapacity_EmptyCell_ReturnsFullCapacity()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos, 100);

            int capacity = this.service.GetAvailableCapacity(pos);

            Assert.AreEqual(100, capacity);
        }

        [Test]
        public void GetAvailableCapacity_WithReservedCount_ReturnsReducedCapacity()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos, 100);

            int capacity = this.service.GetAvailableCapacity(pos, reservedCount: 30);

            Assert.AreEqual(70, capacity);
        }

        [Test]
        public void GetAvailableCapacity_FullCell_ReturnsZero()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos, 10);
            this.service.AddItem(pos, 10, 10);

            int capacity = this.service.GetAvailableCapacity(pos);

            Assert.AreEqual(0, capacity);
        }

        [Test]
        public void CanPlaceItem_SameId_ReturnsTrue()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos, 100);
            this.service.AddItem(pos, 10, 20);

            Assert.IsTrue(this.service.CanPlaceItem(pos, 10, 30));
            Assert.IsFalse(this.service.CanPlaceItem(pos, 10, 100)); // 超出容量
        }

        [Test]
        public void CanPlaceItem_DifferentId_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos, 100);
            this.service.AddItem(pos, 10, 20);

            Assert.IsFalse(this.service.CanPlaceItem(pos, 20, 30));
        }

        // ---- GetResourceInfo 测试 ----

        [Test]
        public void GetResourceInfo_ReturnsCorrectData()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);

            ResourceInfo info = this.service.GetResourceInfo(pos);

            Assert.IsNotNull(info);
            Assert.AreEqual(10, info.Id);
            Assert.AreEqual(5, info.Count);
        }

        [Test]
        public void GetResourceInfo_EmptyCell_ReturnsEmptyResource()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);

            ResourceInfo info = this.service.GetResourceInfo(pos);

            Assert.IsNotNull(info);
            Assert.AreEqual(-1, info.Id);
            Assert.AreEqual(0, info.Count);
        }

        [Test]
        public void GetResourceInfo_NonExistentCell_ReturnsEmptyResource()
        {
            ResourceInfo info = this.service.GetResourceInfo(new GameGridPosition(99, 99));

            Assert.IsNotNull(info);
            Assert.AreEqual(-1, info.Id);
        }

        // ---- ManagerName 测试 ----

        [Test]
        public void ManagerName_IsSetCorrectly()
        {
            Assert.AreEqual("TestInventory", this.service.ManagerName);
        }

        [Test]
        public void PublishedEvent_ContainsManagerName()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);

            InventoryGridChangedEvent evt = this.publishedEvents[0] as InventoryGridChangedEvent;
            Assert.AreEqual("TestInventory", evt.ManagerName);
        }

        // ---- TransferItem 测试 ----

        [Test]
        public void TransferItem_ChangesItemId()
        {
            GameGridPosition pos = new GameGridPosition(1, 1);
            this.service.EnsureCell(pos);
            this.service.AddItem(pos, 10, 5);
            this.publishedEvents.Clear();

            this.service.TransferItem(pos, oldItemId: 10, newItemId: 20);

            InventoryCell cell = this.service.GetCell(pos);
            Assert.AreEqual(20, cell.ItemId);
            Assert.AreEqual(5, cell.Count);
            Assert.AreEqual(1, this.publishedEvents.Count);
        }
    }
}
