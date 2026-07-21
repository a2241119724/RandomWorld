namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerTaskQueueTests
    {
        [Test]
        public void Constructor_CreatesEmptyQueue()
        {
            var queue = new WorkerTaskQueue<int>();
            Assert.AreEqual(0, queue.TotalCount);
            Assert.AreEqual(4, queue.PriorityCount);
        }

        [Test]
        public void Add_IncreasesCount()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(1, 0);
            Assert.AreEqual(1, queue.TotalCount);
            Assert.AreEqual(1, queue.GetCount(0));
        }

        [Test]
        public void Add_AtDifferentPriorities()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(1, 0);
            queue.Add(2, 2);
            Assert.AreEqual(2, queue.TotalCount);
            Assert.AreEqual(1, queue.GetCount(0));
            Assert.AreEqual(0, queue.GetCount(1));
            Assert.AreEqual(1, queue.GetCount(2));
        }

        [Test]
        public void Remove_ExistingTask_ReturnsTrue()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(42, 0);
            Assert.IsTrue(queue.Remove(42));
            Assert.AreEqual(0, queue.TotalCount);
        }

        [Test]
        public void Remove_NonExistingTask_ReturnsFalse()
        {
            var queue = new WorkerTaskQueue<int>();
            Assert.IsFalse(queue.Remove(99));
        }

        [Test]
        public void MarkRunning_ExistingTask_SetsFlag()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(10, 0);
            queue.MarkRunning(10);

            var tasks = queue.GetTasksAtPriority(0);
            Assert.IsTrue(tasks[10]);
        }

        [Test]
        public void MarkIdle_AfterRunning_SetsFlagFalse()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(10, 0);
            queue.MarkRunning(10);
            queue.MarkIdle(10);

            var tasks = queue.GetTasksAtPriority(0);
            Assert.IsFalse(tasks[10]);
        }

        [Test]
        public void Contains_ExistingTask_ReturnsTrue()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(5, 1);
            Assert.IsTrue(queue.Contains(5));
        }

        [Test]
        public void Contains_RemovedTask_ReturnsFalse()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(5, 1);
            queue.Remove(5);
            Assert.IsFalse(queue.Contains(5));
        }

        [Test]
        public void RemoveWhere_RemovesMatchingTasks()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(1, 0);
            queue.Add(2, 0);
            queue.Add(3, 1);
            Assert.IsTrue(queue.RemoveWhere(t => t % 2 == 0));
            Assert.AreEqual(2, queue.TotalCount);
            Assert.IsFalse(queue.Contains(2));
            Assert.IsTrue(queue.Contains(1));
            Assert.IsTrue(queue.Contains(3));
        }

        [Test]
        public void GetRunningCountByType_CountsRunning()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(100, 0); // type 0
            queue.Add(101, 0); // type 1
            queue.Add(200, 0); // type 0
            queue.MarkRunning(100);
            queue.MarkRunning(200);

            Assert.AreEqual(3, queue.TotalCount);
            Assert.AreEqual(2, queue.GetRunningCountByType(0, t => t / 100));
            Assert.AreEqual(0, queue.GetRunningCountByType(1, t => t / 100));
        }

        [Test]
        public void MultiplePriorities_Independent()
        {
            var queue = new WorkerTaskQueue<int>();
            queue.Add(1, 0);
            queue.Add(2, 3);
            Assert.IsTrue(queue.Contains(1));
            Assert.IsTrue(queue.Contains(2));
            queue.Remove(1);
            Assert.IsTrue(queue.Contains(2));
            Assert.AreEqual(1, queue.TotalCount);
        }
    }
}
