namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerTaskProgressServiceTests
    {
        private WorkerTaskProgressService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new WorkerTaskProgressService();
        }

        [Test]
        public void ApplyTiredCost_NormalWork_ReducesTired()
        {
            float result = this.service.ApplyTiredCost(50f, 1f, 2f);
            Assert.AreEqual(48f, result, 0.0001f);
        }

        [Test]
        public void ApplyTiredCost_ZeroDeltaTime_NoChange()
        {
            float result = this.service.ApplyTiredCost(50f, 0f, 2f);
            Assert.AreEqual(50f, result, 0.0001f);
        }

        [Test]
        public void ApplyTiredCost_ClampsToZero()
        {
            float result = this.service.ApplyTiredCost(1f, 1f, 10f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void AdvanceProgress_NormalSpeed_AdvancesCorrectly()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(0f, 10f, 1f, 2f);
            Assert.AreEqual(2f, result.CurrentProgress, 0.0001f);
            Assert.IsFalse(result.Completed);
        }

        [Test]
        public void AdvanceProgress_ExceedsMax_CompletesAndResets()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(9f, 10f, 1f, 2f);
            Assert.IsTrue(result.Completed);
            Assert.AreEqual(0f, result.CurrentProgress, 0.0001f);
        }

        [Test]
        public void AdvanceProgress_ExactlyAtMax_Completes()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(10f, 10f, 1f, 1f);
            Assert.IsTrue(result.Completed);
        }

        [Test]
        public void AdvanceProgress_ZeroMultiplier_NoProgress()
        {
            WorkerTaskProgressResult result = this.service.AdvanceProgress(0f, 10f, 5f, 0f);
            Assert.AreEqual(0f, result.CurrentProgress, 0.0001f);
            Assert.IsFalse(result.Completed);
        }

        [Test]
        public void GetProgressRatio_HalfProgress_ReturnsHalf()
        {
            float ratio = this.service.GetProgressRatio(5f, 10f);
            Assert.AreEqual(0.5f, ratio, 0.0001f);
        }

        [Test]
        public void GetProgressRatio_ZeroMax_ReturnsOne()
        {
            float ratio = this.service.GetProgressRatio(5f, 0f);
            Assert.AreEqual(1f, ratio, 0.0001f);
        }

        [Test]
        public void GetProgressRatio_Complete_ReturnsOne()
        {
            float ratio = this.service.GetProgressRatio(10f, 10f);
            Assert.AreEqual(1f, ratio, 0.0001f);
        }

        [Test]
        public void GetProgressRatio_OverComplete_ExceedsOne()
        {
            float ratio = this.service.GetProgressRatio(15f, 10f);
            Assert.AreEqual(1.5f, ratio, 0.0001f);
        }
    }
}
