namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class WorkerTaskAssignmentServiceTests
    {
        private WorkerTaskAssignmentService<int> service;
        private WorkerAgentSnapshot idleWorker;
        private WorkerAgentSnapshot busyWorker;
        private WorkerAgentSnapshot pausedWorker;

        [SetUp]
        public void SetUp()
        {
            this.service = new WorkerTaskAssignmentService<int>();
            this.idleWorker = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 80f, 100f, 80f, 100f);
            this.busyWorker = new WorkerAgentSnapshot(1, new GameVector2(0, 0), false, false, 80f, 100f, 80f, 100f);
            this.pausedWorker = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, true, 80f, 100f, 80f, 100f);
        }

        [Test]
        public void SelectTask_IdleWorkerAndAssignableTask_ReturnsTask()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, () => true),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsTrue(result.HasTask);
            Assert.AreEqual(42, result.Task);
        }

        [Test]
        public void SelectTask_BusyWorker_ReturnsNone()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, () => true),
            };
            var result = this.service.SelectTask(this.busyWorker, tasks);
            Assert.IsFalse(result.HasTask);
        }

        [Test]
        public void SelectTask_PausedWorker_ReturnsNone()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, () => true),
            };
            var result = this.service.SelectTask(this.pausedWorker, tasks);
            Assert.IsFalse(result.HasTask);
        }

        [Test]
        public void SelectTask_NullWorker_ReturnsNone()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, () => true),
            };
            var result = this.service.SelectTask(null, tasks);
            Assert.IsFalse(result.HasTask);
        }

        [Test]
        public void SelectTask_EmptyTaskList_ReturnsNone()
        {
            var result = this.service.SelectTask(this.idleWorker, new List<WorkerTaskSnapshot<int>>());
            Assert.IsFalse(result.HasTask);
        }

        [Test]
        public void SelectTask_AlreadyRunningTask_Skipped()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), true, () => true),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsFalse(result.HasTask);
        }

        [Test]
        public void SelectTask_TaskFailsCanAssign_Skipped()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, () => false),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsFalse(result.HasTask);
        }

        [Test]
        public void SelectTask_MultipleTasks_SelectsNearest()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(10, 1, 0, new GameVector2(10, 0), false, () => true),
                new WorkerTaskSnapshot<int>(20, 2, 0, new GameVector2(3, 4), false, () => true),
                new WorkerTaskSnapshot<int>(30, 3, 0, new GameVector2(7, 1), false, () => true),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsTrue(result.HasTask);
            Assert.AreEqual(20, result.Task);
        }

        [Test]
        public void SelectTask_NearestBlocked_FallsBackToNext()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(10, 1, 0, new GameVector2(1, 0), false, () => false),
                new WorkerTaskSnapshot<int>(20, 2, 0, new GameVector2(10, 0), false, () => true),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsTrue(result.HasTask);
            Assert.AreEqual(20, result.Task);
        }

        [Test]
        public void SelectTask_ReturnsCorrectPriority()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 2, new GameVector2(3, 4), false, () => true),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.AreEqual(2, result.Priority);
        }

        [Test]
        public void SelectTask_NullTaskInList_Skipped()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                null,
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, () => true),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsTrue(result.HasTask);
            Assert.AreEqual(42, result.Task);
        }

        [Test]
        public void SelectTask_CanAssignNull_Allowed()
        {
            var tasks = new List<WorkerTaskSnapshot<int>>
            {
                new WorkerTaskSnapshot<int>(42, 1, 0, new GameVector2(3, 4), false, null),
            };
            var result = this.service.SelectTask(this.idleWorker, tasks);
            Assert.IsTrue(result.HasTask);
        }
    }
}
