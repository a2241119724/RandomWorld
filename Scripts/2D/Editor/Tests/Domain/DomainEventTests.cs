namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Inventory;
    using LAB2D.Domain.Player;
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    [TestFixture]
    public class DomainEventTests
    {
        [Test]
        public void WorkerTaskQueueChangedEvent_IsIGameEvent()
        {
            var e = new WorkerTaskQueueChangedEvent { TaskInfo = "test" };
            Assert.IsInstanceOf<IGameEvent>(e);
            Assert.AreEqual("test", e.TaskInfo);
        }

        [Test]
        public void InventoryCellChangedEvent_IsIGameEvent()
        {
            var e = new InventoryCellChangedEvent
            {
                ManagerName = "InventoryManager",
                GridX = 3,
                GridY = 5,
                CellInfo = "Wood x10",
            };
            Assert.IsInstanceOf<IGameEvent>(e);
            Assert.AreEqual("InventoryManager", e.ManagerName);
            Assert.AreEqual(3, e.GridX);
            Assert.AreEqual(5, e.GridY);
            Assert.AreEqual("Wood x10", e.CellInfo);
        }

        [Test]
        public void PlayerAttackRequestedEvent_IsIGameEvent()
        {
            var e = new PlayerAttackRequestedEvent { EntityId = 42L };
            Assert.IsInstanceOf<IGameEvent>(e);
            Assert.AreEqual(42L, e.EntityId);
        }

        [Test]
        public void PlayerSkillActivatedEvent_IsIGameEvent()
        {
            var e = new PlayerSkillActivatedEvent { EntityId = 7L, SlotIndex = 2 };
            Assert.IsInstanceOf<IGameEvent>(e);
            Assert.AreEqual(7L, e.EntityId);
            Assert.AreEqual(2, e.SlotIndex);
        }

        [Test]
        public void AllNewEvents_SubscribeAndPublish_ViaEventBus()
        {
            EventBus bus = new EventBus();
            bool workerCalled = false;
            bool inventoryCalled = false;
            bool attackCalled = false;
            bool skillCalled = false;

            bus.Subscribe<WorkerTaskQueueChangedEvent>(e => workerCalled = true);
            bus.Subscribe<InventoryCellChangedEvent>(e => inventoryCalled = true);
            bus.Subscribe<PlayerAttackRequestedEvent>(e => attackCalled = true);
            bus.Subscribe<PlayerSkillActivatedEvent>(e => skillCalled = true);

            bus.Publish(new WorkerTaskQueueChangedEvent());
            bus.Publish(new InventoryCellChangedEvent());
            bus.Publish(new PlayerAttackRequestedEvent());
            bus.Publish(new PlayerSkillActivatedEvent());

            Assert.IsTrue(workerCalled, "WorkerTaskQueueChangedEvent 应被回调");
            Assert.IsTrue(inventoryCalled, "InventoryCellChangedEvent 应被回调");
            Assert.IsTrue(attackCalled, "PlayerAttackRequestedEvent 应被回调");
            Assert.IsTrue(skillCalled, "PlayerSkillActivatedEvent 应被回调");
        }
    }
}
