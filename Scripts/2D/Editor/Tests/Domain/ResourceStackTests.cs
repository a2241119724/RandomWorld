namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class ResourceStackTests
    {
        [Test]
        public void Constructor_SetsFields()
        {
            ResourceStack stack = new ResourceStack(5, 30, 100);
            Assert.AreEqual(5, stack.ItemId);
            Assert.AreEqual(30, stack.Count);
            Assert.AreEqual(100, stack.Capacity);
        }

        [Test]
        public void Empty_ReturnsEmptyStack()
        {
            ResourceStack stack = ResourceStack.Empty(50);
            Assert.IsTrue(stack.IsEmpty);
            Assert.AreEqual(-1, stack.ItemId);
            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(50, stack.Capacity);
        }

        [Test]
        public void IsFull_AtCapacity_ReturnsTrue()
        {
            Assert.IsTrue(new ResourceStack(1, 100, 100).IsFull);
            Assert.IsTrue(new ResourceStack(1, 150, 100).IsFull);
        }

        [Test]
        public void AvailableSpace_CalculatesCorrectly()
        {
            ResourceStack stack = new ResourceStack(1, 30, 100);
            Assert.AreEqual(70, stack.AvailableSpace);
        }

        [Test]
        public void CanMerge_SameIdAndFits_ReturnsTrue()
        {
            ResourceStack a = new ResourceStack(5, 30, 100);
            ResourceStack b = new ResourceStack(5, 20, 100);
            Assert.IsTrue(a.CanMerge(b));
        }

        [Test]
        public void CanMerge_DifferentId_ReturnsFalse()
        {
            ResourceStack a = new ResourceStack(5, 30, 100);
            ResourceStack b = new ResourceStack(7, 20, 100);
            Assert.IsFalse(a.CanMerge(b));
        }

        [Test]
        public void CanMerge_ExceedsCapacity_ReturnsFalse()
        {
            ResourceStack a = new ResourceStack(5, 90, 100);
            ResourceStack b = new ResourceStack(5, 20, 100);
            Assert.IsFalse(a.CanMerge(b));
        }

        [Test]
        public void CanMerge_IntoEmpty_ReturnsTrue()
        {
            ResourceStack empty = ResourceStack.Empty(100);
            ResourceStack b = new ResourceStack(5, 20, 100);
            Assert.IsTrue(empty.CanMerge(b));
        }

        [Test]
        public void Merge_SameId_CombinesCorrectly()
        {
            ResourceStack a = new ResourceStack(5, 30, 100);
            ResourceStack b = new ResourceStack(5, 20, 100);
            ResourceStack result = a.Merge(b);
            Assert.AreEqual(5, result.ItemId);
            Assert.AreEqual(50, result.Count);
        }

        [Test]
        public void Merge_IntoEmpty_TakesOtherId()
        {
            ResourceStack empty = ResourceStack.Empty(100);
            ResourceStack b = new ResourceStack(5, 20, 100);
            ResourceStack result = empty.Merge(b);
            Assert.AreEqual(5, result.ItemId);
            Assert.AreEqual(20, result.Count);
        }

        [Test]
        public void Merge_Incompatible_ReturnsUnchanged()
        {
            ResourceStack a = new ResourceStack(5, 30, 100);
            ResourceStack b = new ResourceStack(7, 20, 100);
            ResourceStack result = a.Merge(b);
            Assert.AreEqual(5, result.ItemId);
            Assert.AreEqual(30, result.Count);
        }

        [Test]
        public void Take_NormalAmount_ReturnsRemainderAndTaken()
        {
            ResourceStack a = new ResourceStack(5, 50, 100);
            ResourceStack taken;
            ResourceStack remainder = a.Take(20, out taken);
            Assert.AreEqual(5, taken.ItemId);
            Assert.AreEqual(20, taken.Count);
            Assert.AreEqual(5, remainder.ItemId);
            Assert.AreEqual(30, remainder.Count);
        }

        [Test]
        public void Take_All_EmptiesStack()
        {
            ResourceStack a = new ResourceStack(5, 50, 100);
            ResourceStack taken;
            ResourceStack remainder = a.Take(50, out taken);
            Assert.AreEqual(50, taken.Count);
            Assert.IsTrue(remainder.IsEmpty);
        }

        [Test]
        public void Take_MoreThanAvailable_TakesAll()
        {
            ResourceStack a = new ResourceStack(5, 50, 100);
            ResourceStack taken;
            ResourceStack remainder = a.Take(100, out taken);
            Assert.AreEqual(50, taken.Count);
            Assert.IsTrue(remainder.IsEmpty);
        }

        [Test]
        public void Take_Zero_NoChange()
        {
            ResourceStack a = new ResourceStack(5, 50, 100);
            ResourceStack taken;
            ResourceStack remainder = a.Take(0, out taken);
            Assert.AreEqual(5, remainder.ItemId);
            Assert.AreEqual(50, remainder.Count);
            Assert.IsTrue(taken.IsEmpty);
        }

        [Test]
        public void WithCount_UpdatesCount()
        {
            ResourceStack a = new ResourceStack(5, 50, 100);
            ResourceStack result = a.WithCount(25);
            Assert.AreEqual(5, result.ItemId);
            Assert.AreEqual(25, result.Count);
        }

        [Test]
        public void WithCount_ZeroOrNegative_ReturnsEmpty()
        {
            ResourceStack a = new ResourceStack(5, 50, 100);
            Assert.IsTrue(a.WithCount(0).IsEmpty);
            Assert.IsTrue(a.WithCount(-5).IsEmpty);
        }
    }
}
