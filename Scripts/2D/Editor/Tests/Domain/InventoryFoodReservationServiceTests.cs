namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryFoodReservationServiceTests
    {
        private readonly InventoryFoodReservationService service = new InventoryFoodReservationService();

        [Test]
        public void GetNeededFoodCount_ExactMultiple_ReturnsWholeItems()
        {
            Assert.AreEqual(4, this.service.GetNeededFoodCount(20f, 5f));
        }

        [Test]
        public void GetNeededFoodCount_Partial_ReturnsCeiled()
        {
            Assert.AreEqual(5, this.service.GetNeededFoodCount(21f, 5f));
        }

        [Test]
        public void GetNeededFoodCount_ZeroHungry_Returns0()
        {
            Assert.AreEqual(0, this.service.GetNeededFoodCount(0f, 5f));
        }

        [Test]
        public void GetNeededFoodCount_ZeroRestoredPerItem_Returns0()
        {
            Assert.AreEqual(0, this.service.GetNeededFoodCount(20f, 0f));
        }

        [Test]
        public void GetNeededFoodCount_NegativeHungry_Returns0()
        {
            Assert.AreEqual(0, this.service.GetNeededFoodCount(-1f, 5f));
        }

        [Test]
        public void GetPreTakeCount_AvailableLessThanNeed_ReturnsAvailable()
        {
            Assert.AreEqual(3, this.service.GetPreTakeCount(3, 5));
        }

        [Test]
        public void GetPreTakeCount_NeedLessThanAvailable_ReturnsNeed()
        {
            Assert.AreEqual(2, this.service.GetPreTakeCount(5, 2));
        }

        [Test]
        public void GetPreTakeCount_ZeroAvailable_Returns0()
        {
            Assert.AreEqual(0, this.service.GetPreTakeCount(0, 5));
        }
    }
}
