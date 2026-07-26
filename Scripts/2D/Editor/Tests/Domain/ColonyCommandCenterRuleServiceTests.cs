namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class ColonyCommandCenterRuleServiceTests
    {
        private readonly ColonyCommandCenterRuleService service = new ColonyCommandCenterRuleService();

        [Test]
        public void ResolvePrimaryReason_NonEmptyReturnFirst()
        {
            var summaries = new List<WorkerTaskBlockReasonSummary>
            {
                new WorkerTaskBlockReasonSummary { Reason = WorkerTaskBlockReason.MissingMaterial, Count = 3 },
                new WorkerTaskBlockReasonSummary { Reason = WorkerTaskBlockReason.WorkerBusy, Count = 1 },
            };
            Assert.AreEqual(WorkerTaskBlockReason.MissingMaterial, this.service.ResolvePrimaryReason(summaries));
        }

        [Test]
        public void ResolvePrimaryReason_Empty_ReturnsNone()
        {
            Assert.AreEqual(WorkerTaskBlockReason.None, this.service.ResolvePrimaryReason(new List<WorkerTaskBlockReasonSummary>()));
        }

        [Test]
        public void ResolvePrimaryReason_Null_ReturnsNone()
        {
            Assert.AreEqual(WorkerTaskBlockReason.None, this.service.ResolvePrimaryReason(null));
        }

        [Test]
        public void BuildAdviceByBlockReason_NoWorker_ReturnsCreationAdvice()
        {
            string advice = this.service.BuildAdviceByBlockReason(WorkerTaskBlockReason.NoWorker);
            StringAssert.Contains("创建", advice);
        }

        [Test]
        public void BuildAdviceByBlockReason_MissingMaterial_ReturnsMaterialAdvice()
        {
            string advice = this.service.BuildAdviceByBlockReason(WorkerTaskBlockReason.MissingMaterial);
            StringAssert.Contains("材料", advice);
        }

        [Test]
        public void BuildAdviceByBlockReason_FoodUnavailable_ReturnsFoodAdvice()
        {
            string advice = this.service.BuildAdviceByBlockReason(WorkerTaskBlockReason.FoodUnavailable);
            StringAssert.Contains("食物", advice);
        }

        [Test]
        public void BuildAdviceByBlockReason_MissingBed_ReturnsBedAdvice()
        {
            string advice = this.service.BuildAdviceByBlockReason(WorkerTaskBlockReason.MissingBed);
            StringAssert.Contains("床", advice);
        }

        [Test]
        public void BuildAdviceByBlockReason_SeedUnavailable_ReturnsSeedAdvice()
        {
            string advice = this.service.BuildAdviceByBlockReason(WorkerTaskBlockReason.SeedUnavailable);
            StringAssert.Contains("种子", advice);
        }

        [Test]
        public void BuildAdviceByBlockReason_InventoryFull_ReturnsCapacityAdvice()
        {
            string advice = this.service.BuildAdviceByBlockReason(WorkerTaskBlockReason.InventoryFull);
            StringAssert.Contains("仓库", advice);
        }

        [Test]
        public void ClampRefreshInterval_Valid_ReturnsSame()
        {
            float result = this.service.ClampRefreshInterval(2f);
            Assert.GreaterOrEqual(result, 0f);
        }

        [Test]
        public void IsTaskToggleEnabled_ContextNull_ReturnsFalse()
        {
            Assert.IsFalse(this.service.IsTaskToggleEnabled(1L, WorkerTaskType.Carry, new ColonyDiagnosticContext()));
        }

        [Test]
        public void IsTaskToggleEnabled_ProviderReturnsTrue_ReturnsTrue()
        {
            var context = new ColonyDiagnosticContext
            {
                IsTaskToggleEnabled = (workerId, taskType) => true,
            };
            Assert.IsTrue(this.service.IsTaskToggleEnabled(1L, WorkerTaskType.Build, context));
        }

        [Test]
        public void ResolveAlertLevel_AssignmentCriticalWorker_ReturnsCritical()
        {
            var assignmentReport = new WorkerTaskAssignmentReport { CriticalWorkerCount = 1 };
            Assert.AreEqual(ColonyCommandAlertLevel.Critical,
                this.service.ResolveAlertLevel(assignmentReport, null, null, null));
        }

        [Test]
        public void ResolveAlertLevel_AssignmentBlocked6_ReturnsCritical()
        {
            var assignmentReport = new WorkerTaskAssignmentReport { BlockedTaskCount = 6 };
            Assert.AreEqual(ColonyCommandAlertLevel.Critical,
                this.service.ResolveAlertLevel(assignmentReport, null, null, null));
        }

        [Test]
        public void ResolveAlertLevel_CongestionCritical_ReturnsCritical()
        {
            var congestionReport = new WorkerTaskCongestionReport { Level = WorkerTaskCongestionLevel.Critical };
            Assert.AreEqual(ColonyCommandAlertLevel.Critical,
                this.service.ResolveAlertLevel(null, null, congestionReport, null));
        }

        [Test]
        public void ResolveAlertLevel_SupplyCritical_ReturnsCritical()
        {
            var supplyReport = new WorkerSupplyReport { CriticalWorkerCount = 1 };
            Assert.AreEqual(ColonyCommandAlertLevel.Critical,
                this.service.ResolveAlertLevel(null, supplyReport, null, null));
        }

        [Test]
        public void ResolveAlertLevel_Blocked2_ReturnsWarning()
        {
            var assignmentReport = new WorkerTaskAssignmentReport { BlockedTaskCount = 2 };
            Assert.AreEqual(ColonyCommandAlertLevel.Warning,
                this.service.ResolveAlertLevel(assignmentReport, null, null, null));
        }

        [Test]
        public void ResolveAlertLevel_SupplyIssue_ReturnsWarning()
        {
            var supplyReport = new WorkerSupplyReport { PrimaryIssue = WorkerSupplyIssueType.HungryWorker };
            Assert.AreEqual(ColonyCommandAlertLevel.Warning,
                this.service.ResolveAlertLevel(null, supplyReport, null, null));
        }

        [Test]
        public void ResolveAlertLevel_CongestionBusy_ReturnsWarning()
        {
            var congestionReport = new WorkerTaskCongestionReport { Level = WorkerTaskCongestionLevel.Busy };
            Assert.AreEqual(ColonyCommandAlertLevel.Warning,
                this.service.ResolveAlertLevel(null, null, congestionReport, null));
        }

        [Test]
        public void ResolveAlertLevel_WaitingTasks_ReturnsNotice()
        {
            var queueSnapshot = new WorkerTaskQueueSnapshot(1, 0, new List<WorkerTaskTypeSummary>());
            Assert.AreEqual(ColonyCommandAlertLevel.Notice,
                this.service.ResolveAlertLevel(null, null, null, queueSnapshot));
        }

        [Test]
        public void ResolveAlertLevel_AllNull_ReturnsStable()
        {
            Assert.AreEqual(ColonyCommandAlertLevel.Stable,
                this.service.ResolveAlertLevel(null, null, null, null));
        }

        [Test]
        public void ResolveAlertLevel_EmptyAssignment_ReturnsStable()
        {
            var assignmentReport = new WorkerTaskAssignmentReport();
            Assert.AreEqual(ColonyCommandAlertLevel.Stable,
                this.service.ResolveAlertLevel(assignmentReport, null, null, null));
        }

        [Test]
        public void BuildCommandAdvice_BlockedMaterial_ReturnsRelevantAdvice()
        {
            var report = new WorkerTaskAssignmentReport
            {
                PrimaryBlockReason = WorkerTaskBlockReason.MissingMaterial,
            };
            string advice = this.service.BuildCommandAdvice(report, null, null);
            StringAssert.Contains("材料", advice);
        }

        [Test]
        public void BuildCommandAdvice_SupplyHungry_ReturnsFoodAdvice()
        {
            var supplyReport = new WorkerSupplyReport
            {
                PrimaryIssue = WorkerSupplyIssueType.HungryWorker,
            };
            string advice = this.service.BuildCommandAdvice(null, supplyReport, null);
            StringAssert.Contains("食物", advice);
        }

        [Test]
        public void BuildCommandAdvice_SupplyBed_ReturnsBedAdvice()
        {
            var supplyReport = new WorkerSupplyReport
            {
                PrimaryIssue = WorkerSupplyIssueType.BedShortage,
            };
            string advice = this.service.BuildCommandAdvice(null, supplyReport, null);
            StringAssert.Contains("床", advice);
        }

        [Test]
        public void BuildCommandAdvice_SupplyCritical_ReturnsCriticalAdvice()
        {
            var supplyReport = new WorkerSupplyReport
            {
                PrimaryIssue = WorkerSupplyIssueType.CriticalWorker,
            };
            string advice = this.service.BuildCommandAdvice(null, supplyReport, null);
            StringAssert.Contains("暂停", advice);
        }

        [Test]
        public void BuildCommandAdvice_CongestionAdvice_ReturnsCongestionText()
        {
            var congestionReport = new WorkerTaskCongestionReport
            {
                AdviceText = "建议增加工人",
            };
            string advice = this.service.BuildCommandAdvice(null, null, congestionReport);
            StringAssert.Contains("增加工人", advice);
        }

        [Test]
        public void BuildCommandAdvice_NoIssues_ReturnsDefault()
        {
            string advice = this.service.BuildCommandAdvice(null, null, null);
            StringAssert.Contains("保持", advice);
        }
    }
}
