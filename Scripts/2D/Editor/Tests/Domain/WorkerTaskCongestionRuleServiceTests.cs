namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerTaskCongestionRuleServiceTests
    {
        private readonly WorkerTaskCongestionRuleService service = new WorkerTaskCongestionRuleService();

        [Test]
        public void GetCongestionLevel_Zero_ReturnsNone()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.None, this.service.GetCongestionLevel(0));
        }

        [Test]
        public void GetCongestionLevel_Negative_ReturnsNone()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.None, this.service.GetCongestionLevel(-1));
        }

        [Test]
        public void GetCongestionLevel_Small_ReturnsSmooth()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.Smooth, this.service.GetCongestionLevel(1));
            Assert.AreEqual(WorkerTaskCongestionLevel.Smooth, this.service.GetCongestionLevel(29));
        }

        [Test]
        public void GetCongestionLevel_BusyThreshold_ReturnsBusy()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.Busy, this.service.GetCongestionLevel(30));
        }

        [Test]
        public void GetCongestionLevel_CongestedThreshold_ReturnsCongested()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.Congested, this.service.GetCongestionLevel(100));
        }

        [Test]
        public void GetCongestionLevel_AboveCongested_ReturnsCongested()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.Congested, this.service.GetCongestionLevel(150));
        }

        [Test]
        public void GetCongestionLevel_CriticalThreshold_ReturnsCritical()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.Critical, this.service.GetCongestionLevel(250));
        }

        [Test]
        public void GetCongestionLevel_BeyondCritical_ReturnsCritical()
        {
            Assert.AreEqual(WorkerTaskCongestionLevel.Critical, this.service.GetCongestionLevel(500));
        }

        [Test]
        public void HasDominantTaskType_EnoughCountAndRatio_ReturnsTrue()
        {
            Assert.IsTrue(this.service.HasDominantTaskType(15, 30));
        }

        [Test]
        public void HasDominantTaskType_NotEnoughCount_ReturnsFalse()
        {
            Assert.IsFalse(this.service.HasDominantTaskType(10, 20));
        }

        [Test]
        public void HasDominantTaskType_NotEnoughRatio_ReturnsFalse()
        {
            Assert.IsFalse(this.service.HasDominantTaskType(15, 40));
        }

        [Test]
        public void HasDominantTaskType_ZeroTotal_ReturnsFalse()
        {
            Assert.IsFalse(this.service.HasDominantTaskType(5, 0));
        }

        [Test]
        public void HasDominantTaskType_NegativePrimary_ReturnsFalse()
        {
            Assert.IsFalse(this.service.HasDominantTaskType(-1, 10));
        }
    }
}
