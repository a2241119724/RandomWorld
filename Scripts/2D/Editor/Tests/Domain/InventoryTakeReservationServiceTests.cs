namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Inventory;
    using NUnit.Framework;

    [TestFixture]
    public class InventoryTakeReservationServiceTests
    {
        private readonly InventoryTakeReservationService service = new InventoryTakeReservationService();

        [Test]
        public void GetTargetTakeCount_RequiredGreater_ReturnsRequired()
        {
            Assert.AreEqual(10, this.service.GetTargetTakeCount(10, 5));
        }

        [Test]
        public void GetTargetTakeCount_MaxCarryGreater_ReturnsMaxCarry()
        {
            Assert.AreEqual(8, this.service.GetTargetTakeCount(3, 8));
        }

        [Test]
        public void GetTargetTakeCount_Equal_ReturnsValue()
        {
            Assert.AreEqual(5, this.service.GetTargetTakeCount(5, 5));
        }

        [Test]
        public void GetAvailableTakeCount_Plenty_ReturnsAvailable()
        {
            Assert.AreEqual(80, this.service.GetAvailableTakeCount(100, 20));
        }

        [Test]
        public void GetAvailableTakeCount_AllReserved_Returns0()
        {
            Assert.AreEqual(0, this.service.GetAvailableTakeCount(50, 50));
        }

        [Test]
        public void GetAvailableTakeCount_OverReserved_Returns0()
        {
            Assert.AreEqual(0, this.service.GetAvailableTakeCount(20, 30));
        }
    }
}
