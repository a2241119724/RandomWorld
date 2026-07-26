namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryStackingServiceTests
    {
        private readonly InventoryStackingService service = new InventoryStackingService();

        [Test]
        public void GetAvailableCapacity_Plenty_ReturnsSpace()
        {
            Assert.AreEqual(80, this.service.GetAvailableCapacity(100, 10, 10));
        }

        [Test]
        public void GetAvailableCapacity_Full_Returns0()
        {
            Assert.AreEqual(0, this.service.GetAvailableCapacity(100, 90, 10));
        }

        [Test]
        public void GetAvailableCapacity_Overflow_Returns0()
        {
            Assert.AreEqual(0, this.service.GetAvailableCapacity(50, 30, 30));
        }

        [Test]
        public void GetPlaceCount_LessThanCapacity_ReturnsRemaining()
        {
            Assert.AreEqual(30, this.service.GetPlaceCount(30, 50));
        }

        [Test]
        public void GetPlaceCount_MoreThanCapacity_ReturnsCapacity()
        {
            Assert.AreEqual(50, this.service.GetPlaceCount(100, 50));
        }

        [Test]
        public void GetPlaceCount_ZeroRemaining_Returns0()
        {
            Assert.AreEqual(0, this.service.GetPlaceCount(0, 50));
        }

        [Test]
        public void GetPlaceCount_ZeroCapacity_Returns0()
        {
            Assert.AreEqual(0, this.service.GetPlaceCount(30, 0));
        }

        [Test]
        public void CanPlaceAll_Fits_ReturnsTrue()
        {
            Assert.IsTrue(this.service.CanPlaceAll(30, 50));
        }

        [Test]
        public void CanPlaceAll_TooMany_ReturnsFalse()
        {
            Assert.IsFalse(this.service.CanPlaceAll(60, 50));
        }

        [Test]
        public void CanPlaceAll_ZeroItems_ReturnsFalse()
        {
            Assert.IsFalse(this.service.CanPlaceAll(0, 50));
        }
    }
}
