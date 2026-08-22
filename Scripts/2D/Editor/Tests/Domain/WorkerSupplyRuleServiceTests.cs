namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerSupplyRuleServiceTests
    {
        private readonly WorkerSupplyRuleService service = new WorkerSupplyRuleService();

        [Test]
        public void GetRecoverNeed_BelowMax_ReturnsDifference()
        {
            float need = this.service.GetRecoverNeed(20f, 100f);
            Assert.AreEqual(80f, need, 0.0001f);
        }

        [Test]
        public void GetRecoverNeed_AtMax_ReturnsZero()
        {
            float need = this.service.GetRecoverNeed(100f, 100f);
            Assert.AreEqual(0f, need, 0.0001f);
        }

        [Test]
        public void GetRecoverNeed_AboveMax_ReturnsZero()
        {
            float need = this.service.GetRecoverNeed(120f, 100f);
            Assert.AreEqual(0f, need, 0.0001f);
        }

        [Test]
        public void NeedsFood_NullSnapshot_ReturnsFalse()
        {
            Assert.IsFalse(this.service.NeedsFood(null));
        }

        [Test]
        public void NeedsFood_BelowThreshold_ReturnsTrue()
        {
            var snapshot = new WorkerAgentSnapshot(1, new LAB2D.Domain.Common.GameVector2(0, 0), true, false, 15f, 100f, 100f, 100f);
            Assert.IsTrue(this.service.NeedsFood(snapshot));
        }

        [Test]
        public void NeedsFood_AboveThreshold_ReturnsFalse()
        {
            var snapshot = new WorkerAgentSnapshot(1, new LAB2D.Domain.Common.GameVector2(0, 0), true, false, 80f, 100f, 100f, 100f);
            Assert.IsFalse(this.service.NeedsFood(snapshot));
        }

        [Test]
        public void NeedsRest_NullSnapshot_ReturnsFalse()
        {
            Assert.IsFalse(this.service.NeedsRest(null));
        }

        [Test]
        public void NeedsRest_AboveThreshold_ReturnsTrue()
        {
            var snapshot = new WorkerAgentSnapshot(1, new LAB2D.Domain.Common.GameVector2(0, 0), true, false, 100f, 100f, 90f, 100f);
            Assert.IsTrue(this.service.NeedsRest(snapshot));
        }

        [Test]
        public void NeedsRest_BelowThreshold_ReturnsFalse()
        {
            var snapshot = new WorkerAgentSnapshot(1, new LAB2D.Domain.Common.GameVector2(0, 0), true, false, 100f, 100f, 10f, 100f);
            Assert.IsFalse(this.service.NeedsRest(snapshot));
        }

        [Test]
        public void NeedsRest_AboveWarningRatioOnly_ReturnsTrue()
        {
            // 疲劳值 70 < MaxTired-20(80) 绝对阈值未达，但疲劳比例 0.7 >= 1-WarningRatio(0.65)，
            // 验证比例分支独立生效。
            var snapshot = new WorkerAgentSnapshot(1, new LAB2D.Domain.Common.GameVector2(0, 0), true, false, 100f, 100f, 70f, 100f);
            Assert.IsTrue(this.service.NeedsRest(snapshot));
        }

        [Test]
        public void GetWorkerPrimaryIssue_Critical_ReturnsCriticalWorker()
        {
            WorkerSupplyIssueType result = this.service.GetWorkerPrimaryIssue(
                WorkerConditionState.Critical, false, false, false);
            Assert.AreEqual(WorkerSupplyIssueType.CriticalWorker, result);
        }

        [Test]
        public void GetWorkerPrimaryIssue_MissingBed_ReturnsBedShortage()
        {
            WorkerSupplyIssueType result = this.service.GetWorkerPrimaryIssue(
                WorkerConditionState.Healthy, false, false, true);
            Assert.AreEqual(WorkerSupplyIssueType.BedShortage, result);
        }

        [Test]
        public void GetWorkerPrimaryIssue_Hungry_ReturnsHungryWorker()
        {
            WorkerSupplyIssueType result = this.service.GetWorkerPrimaryIssue(
                WorkerConditionState.Healthy, true, false, false);
            Assert.AreEqual(WorkerSupplyIssueType.HungryWorker, result);
        }

        [Test]
        public void GetWorkerPrimaryIssue_Tired_ReturnsTiredWorker()
        {
            WorkerSupplyIssueType result = this.service.GetWorkerPrimaryIssue(
                WorkerConditionState.Healthy, false, true, false);
            Assert.AreEqual(WorkerSupplyIssueType.TiredWorker, result);
        }

        [Test]
        public void GetWorkerPrimaryIssue_None_ReturnsNone()
        {
            WorkerSupplyIssueType result = this.service.GetWorkerPrimaryIssue(
                WorkerConditionState.Healthy, false, false, false);
            Assert.AreEqual(WorkerSupplyIssueType.None, result);
        }
    }
}
