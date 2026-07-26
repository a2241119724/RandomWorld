namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerTaskProgressServiceTests
    {
        private readonly WorkerTaskProgressService service = new WorkerTaskProgressService();

        [Test]
        public void ApplyTiredCost_OneSecond_ReturnsReduced()
        {
            Assert.AreEqual(95f, this.service.ApplyTiredCost(100f, 1f, 5f), 0.0001f);
        }

        [Test]
        public void ApplyTiredCost_FloorsAtZero()
        {
            Assert.AreEqual(0f, this.service.ApplyTiredCost(3f, 1f, 5f), 0.0001f);
        }

        [Test]
        public void AdvanceProgress_MidProgress_ReturnsNotCompleted()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(5f, 10f, 1f, 2f);
            Assert.AreEqual(7f, result.CurrentProgress, 0.0001f);
            Assert.IsFalse(result.Completed);
        }

        [Test]
        public void AdvanceProgress_ExceedsMax_ReturnsCompleted()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(9f, 10f, 1f, 2f);
            Assert.AreEqual(0f, result.CurrentProgress, 0.0001f);
            Assert.IsTrue(result.Completed);
        }

        [Test]
        public void AdvanceProgress_ExactMatch_ReturnsCompleted()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(8f, 10f, 1f, 2f);
            Assert.IsTrue(result.Completed);
        }

        [Test]
        public void GetProgressRatio_Half_Returns0_5()
        {
            Assert.AreEqual(0.5f, this.service.GetProgressRatio(5f, 10f), 0.0001f);
        }

        [Test]
        public void GetProgressRatio_ZeroMax_Returns1()
        {
            Assert.AreEqual(1f, this.service.GetProgressRatio(5f, 0f), 0.0001f);
        }
    }
}
