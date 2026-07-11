namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using NUnit.Framework;

    [TestFixture]
    public class EventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.SetInstance(new EventBus());
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Instance.Clear();
        }

        [Test]
        public void Publish_WithSubscriber_HandlerCalled()
        {
            bool called = false;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { called = true; });
            EventBus.Instance.Publish(new CharacterDamagedEvent());
            Assert.IsTrue(called, "订阅者应被调用");
        }

        [Test]
        public void Publish_WithoutSubscriber_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                EventBus.Instance.Publish(new CharacterDamagedEvent());
            }, "无订阅者时发布不应抛异常");
        }

        [Test]
        public void Unsubscribe_AfterSubscribe_HandlerNotCalled()
        {
            int callCount = 0;
            System.Action<CharacterDamagedEvent> handler = e => { callCount++; };
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(handler);
            EventBus.Instance.Unsubscribe<CharacterDamagedEvent>(handler);
            EventBus.Instance.Publish(new CharacterDamagedEvent());
            Assert.AreEqual(0, callCount, "取消订阅后不应被调用");
        }

        [Test]
        public void Publish_MultipleSubscribers_AllCalled()
        {
            int callCount = 0;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { callCount++; });
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { callCount++; });
            EventBus.Instance.Publish(new CharacterDamagedEvent());
            Assert.AreEqual(2, callCount, "所有订阅者都应被调用");
        }

        [Test]
        public void Publish_DifferentEventTypes_OnlyMatchingCalled()
        {
            bool damageCalled = false;
            bool moveCalled = false;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { damageCalled = true; });
            EventBus.Instance.Subscribe<PlayerMovedEvent>(e => { moveCalled = true; });
            EventBus.Instance.Publish(new CharacterDamagedEvent());
            Assert.IsTrue(damageCalled, "匹配的事件应被调用");
            Assert.IsFalse(moveCalled, "不匹配的事件不应被调用");
        }

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            int callCount = 0;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { callCount++; });
            EventBus.Instance.Clear();
            EventBus.Instance.Publish(new CharacterDamagedEvent());
            Assert.AreEqual(0, callCount, "Clear后不应有订阅者");
        }

        [Test]
        public void GetSubscriberCount_AfterSubscribe_ReturnsCorrectCount()
        {
            Assert.AreEqual(0, EventBus.Instance.GetSubscriberCount<CharacterDamagedEvent>(), "初始应为0");
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { });
            Assert.AreEqual(1, EventBus.Instance.GetSubscriberCount<CharacterDamagedEvent>());
        }
    }
}
