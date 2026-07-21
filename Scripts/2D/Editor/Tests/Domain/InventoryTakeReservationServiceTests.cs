namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryTakeReservationServiceTests
    {
        private InventoryTakeReservationService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new InventoryTakeReservationService();
        }

        [Test]
        public void GetTargetTakeCount_RequiredGreaterThanCarry_ReturnsRequired()
        {
            Assert.AreEqual(20, this.service.GetTargetTakeCount(20, 10));
        }

        [Test]
        public void GetTargetTakeCount_CarryGreaterThanRequired_ReturnsCarry()
        {
            Assert.AreEqual(50, this.service.GetTargetTakeCount(20, 50));
        }

        [Test]
        public void GetTargetTakeCount_Equal_ReturnsEither()
        {
            Assert.AreEqual(10, this.service.GetTargetTakeCount(10, 10));
        }

        [Test]
        public void GetTargetTakeCount_ZeroRequired_ReturnsCarry()
        {
            Assert.AreEqual(10, this.service.GetTargetTakeCount(0, 10));
        }

        [Test]
        public void GetAvailableTakeCount_HasStock_ReturnsAvailable()
        {
            Assert.AreEqual(40, this.service.GetAvailableTakeCount(50, 10));
        }

        [Test]
        public void GetAvailableTakeCount_FullyReserved_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetAvailableTakeCount(50, 50));
        }

        [Test]
        public void GetAvailableTakeCount_OverReserved_ReturnsZero()
        {
            Assert.AreEqual(0, this.service.GetAvailableTakeCount(30, 50));
        }

        [Test]
        public void GetAvailableTakeCount_NoneReserved_ReturnsFull()
        {
            Assert.AreEqual(50, this.service.GetAvailableTakeCount(50, 0));
        }
    }
}
