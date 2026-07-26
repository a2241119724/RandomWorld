namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryGridTests
    {
        private InventoryGrid grid;

        [SetUp]
        public void SetUp()
        {
            this.grid = new InventoryGrid(10, 7, 100);
        }

        [Test]
        public void AddCell_CreatesCellAtPosition()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            InventoryCell cell = this.grid.AddCell(pos);
            Assert.IsNotNull(cell);
            Assert.IsTrue(this.grid.HasCell(pos));
            Assert.AreEqual(1, this.grid.CellCount);
        }

        [Test]
        public void AddItem_ToEmptyCell_Succeeds()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            Assert.IsTrue(this.grid.AddItem(pos, 5, 30));
            Assert.AreEqual(30, this.grid.GetCell(pos).Count);
            Assert.AreEqual(5, this.grid.GetCell(pos).ItemId);
        }

        [Test]
        public void AddItem_SameId_Stacks()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            this.grid.AddItem(pos, 5, 30);
            Assert.IsTrue(this.grid.AddItem(pos, 5, 20));
            Assert.AreEqual(50, this.grid.GetCell(pos).Count);
        }

        [Test]
        public void AddItem_DifferentId_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            this.grid.AddItem(pos, 5, 30);
            Assert.IsFalse(this.grid.AddItem(pos, 7, 10));
            Assert.AreEqual(5, this.grid.GetCell(pos).ItemId);
            Assert.AreEqual(30, this.grid.GetCell(pos).Count);
        }

        [Test]
        public void AddItem_ExceedsCapacity_ReturnsFalse()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos, 50);
            Assert.IsTrue(this.grid.AddItem(pos, 1, 30));
            Assert.IsFalse(this.grid.AddItem(pos, 1, 30));
            Assert.AreEqual(30, this.grid.GetCell(pos).Count);
        }

        [Test]
        public void TakeItem_RemovesCorrectAmount()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            this.grid.AddItem(pos, 5, 50);
            int taken = this.grid.TakeItem(pos, 20);
            Assert.AreEqual(20, taken);
            Assert.AreEqual(30, this.grid.GetCell(pos).Count);
        }

        [Test]
        public void TakeItem_MoreThanAvailable_TakesAll()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            this.grid.AddItem(pos, 5, 50);
            int taken = this.grid.TakeItem(pos, 100);
            Assert.AreEqual(50, taken);
            Assert.IsTrue(this.grid.GetCell(pos).IsEmpty);
        }

        [Test]
        public void GetTotalCount_AcrossMultipleCells()
        {
            GameGridPosition p1 = new GameGridPosition(0, 0);
            GameGridPosition p2 = new GameGridPosition(1, 0);
            this.grid.AddCell(p1);
            this.grid.AddCell(p2);
            this.grid.AddItem(p1, 5, 30);
            this.grid.AddItem(p2, 5, 20);
            Assert.AreEqual(50, this.grid.GetTotalCount(5));
        }

        [Test]
        public void GetTotalCount_NoMatchingItem_ReturnsZero()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            this.grid.AddItem(pos, 5, 30);
            Assert.AreEqual(0, this.grid.GetTotalCount(99));
        }

        [Test]
        public void GetPositionsById_ReturnsAllPositions()
        {
            GameGridPosition p1 = new GameGridPosition(0, 0);
            GameGridPosition p2 = new GameGridPosition(1, 0);
            this.grid.AddCell(p1);
            this.grid.AddCell(p2);
            this.grid.AddItem(p1, 5, 10);
            this.grid.AddItem(p2, 5, 20);
            Assert.AreEqual(2, this.grid.GetPositionsById(5).Count);
        }

        [Test]
        public void FindBestPositionForItem_ExistingStack_PrefersStack()
        {
            GameGridPosition p1 = new GameGridPosition(0, 0);
            GameGridPosition p2 = new GameGridPosition(1, 0);
            this.grid.AddCell(p1);
            this.grid.AddCell(p2);
            this.grid.AddItem(p1, 5, 10);
            GameGridPosition best = this.grid.FindBestPositionForItem(5, 20);
            Assert.AreEqual(p1, best);
        }

        [Test]
        public void FindBestPositionForItem_NewItem_UsesEmptyCell()
        {
            GameGridPosition p1 = new GameGridPosition(0, 0);
            GameGridPosition p2 = new GameGridPosition(1, 0);
            this.grid.AddCell(p1);
            this.grid.AddCell(p2);
            this.grid.AddItem(p1, 5, 100);
            GameGridPosition best = this.grid.FindBestPositionForItem(7, 20);
            Assert.AreEqual(p2, best);
        }

        [Test]
        public void RemoveCell_ClearsPosition()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            this.grid.AddCell(pos);
            this.grid.AddItem(pos, 5, 30);
            Assert.IsTrue(this.grid.RemoveCell(pos));
            Assert.IsNull(this.grid.GetCell(pos));
            Assert.AreEqual(0, this.grid.CellCount);
            Assert.AreEqual(0, this.grid.GetTotalCount(5));
        }

        [Test]
        public void AddCell_DuplicatePosition_ReturnsExisting()
        {
            GameGridPosition pos = new GameGridPosition(0, 0);
            InventoryCell c1 = this.grid.AddCell(pos);
            InventoryCell c2 = this.grid.AddCell(pos);
            Assert.AreSame(c1, c2);
            Assert.AreEqual(1, this.grid.CellCount);
        }
    }
}
