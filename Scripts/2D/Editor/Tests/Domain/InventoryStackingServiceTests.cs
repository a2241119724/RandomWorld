namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryStackingServiceTests
    {
        private InventoryStackingService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new InventoryStackingService();
        }

        [Test]
        public void GetAvailableCapacity_EmptyCell_ReturnsFullCapacity()
        {
            int result = this.service.GetAvailableCapacity(100, 0, 0);
            Assert.AreEqual(100, result);
        }

        [Test]
        public void GetAvailableCapacity_PartiallyUsed_ReturnsRemaining()
        {
            int result = this.service.GetAvailableCapacity(100, 30, 10);
            Assert.AreEqual(60, result);
        }

        [Test]
        public void GetAvailableCapacity_Full_ReturnsZero()
        {
            int result = this.service.GetAvailableCapacity(100, 90, 10);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetAvailableCapacity_Overfilled_ReturnsZero()
        {
            int result = this.service.GetAvailableCapacity(100, 80, 30);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetPlaceCount_TakeAll_ReturnsAll()
        {
            int result = this.service.GetPlaceCount(5, 10);
            Assert.AreEqual(5, result);
        }

        [Test]
        public void GetPlaceCount_NotEnough_ReturnsAvailable()
        {
            int result = this.service.GetPlaceCount(20, 10);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void GetPlaceCount_ZeroRemaining_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetPlaceCount(0, 100));
        }

        [Test]
        public void GetPlaceCount_ZeroCapacity_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetPlaceCount(10, 0));
        }

        [Test]
        public void GetPlaceCount_NegativeRemaining_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetPlaceCount(-5, 100));
        }

        [Test]
        public void CanPlaceAll_Fits_ReturnsTrue()
        {
            Assert.IsTrue(this.service.CanPlaceAll(5, 10));
        }

        [Test]
        public void CanPlaceAll_ExactlyFits_ReturnsTrue()
        {
            Assert.IsTrue(this.service.CanPlaceAll(10, 10));
        }

        [Test]
        public void CanPlaceAll_TooMany_ReturnsFalse()
        {
            Assert.IsFalse(this.service.CanPlaceAll(15, 10));
        }

        [Test]
        public void CanPlaceAll_ZeroRemaining_ReturnsFalse()
        {
            Assert.IsFalse(this.service.CanPlaceAll(0, 100));
        }

        [Test]
        public void CanPlaceAll_NegativeRemaining_ReturnsFalse()
        {
            Assert.IsFalse(this.service.CanPlaceAll(-3, 100));
        }
    }
}
