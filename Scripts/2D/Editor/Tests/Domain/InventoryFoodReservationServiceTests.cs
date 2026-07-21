namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryFoodReservationServiceTests
    {
        private InventoryFoodReservationService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new InventoryFoodReservationService();
        }

        [Test]
        public void GetNeededFoodCount_ExactDivision_ReturnsExact()
        {
            int result = this.service.GetNeededFoodCount(100f, 10f);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void GetNeededFoodCount_NeedCeiling_ReturnsRoundedUp()
        {
            int result = this.service.GetNeededFoodCount(95f, 10f);
            Assert.AreEqual(10, result, "95/10=9.5 应向上取整为 10");
        }

        [Test]
        public void GetNeededFoodCount_SmallNeed_ReturnsOne()
        {
            int result = this.service.GetNeededFoodCount(5f, 10f);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void GetNeededFoodCount_ZeroHungry_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetNeededFoodCount(0f, 10f));
        }

        [Test]
        public void GetNeededFoodCount_NegativeHungry_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetNeededFoodCount(-10f, 10f));
        }

        [Test]
        public void GetNeededFoodCount_ZeroRestorePerItem_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetNeededFoodCount(100f, 0f));
            Assert.AreEqual(0, this.service.GetNeededFoodCount(100f, -5f));
        }

        [Test]
        public void GetPreTakeCount_EnoughAvailable_ReturnsNeed()
        {
            Assert.AreEqual(10, this.service.GetPreTakeCount(50, 10));
        }

        [Test]
        public void GetPreTakeCount_NotEnoughAvailable_ReturnsAvailable()
        {
            Assert.AreEqual(5, this.service.GetPreTakeCount(5, 10));
        }

        [Test]
        public void GetPreTakeCount_ExactMatch_ReturnsEither()
        {
            Assert.AreEqual(10, this.service.GetPreTakeCount(10, 10));
        }

        [Test]
        public void GetPreTakeCount_ZeroAvailable_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetPreTakeCount(0, 10));
        }

        [Test]
        public void GetPreTakeCount_ZeroNeed_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetPreTakeCount(50, 0));
        }

        [Test]
        public void GetPreTakeCount_BothZero_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetPreTakeCount(0, 0));
        }
    }
}
