namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using NUnit.Framework;

    [TestFixture]
    public class EventBusTests
    {
        /// <summary>测试用事件类型 B，验证不同类型隔离。</summary>
        private sealed class TestEventB : IGameEvent
        {
        }

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
            bool otherCalled = false;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { damageCalled = true; });
            EventBus.Instance.Subscribe<TestEventB>(e => { otherCalled = true; });
            EventBus.Instance.Publish(new CharacterDamagedEvent());
            Assert.IsTrue(damageCalled, "匹配的事件应被调用");
            Assert.IsFalse(otherCalled, "不匹配的事件不应被调用");
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

        // ---- PublishInternal（非泛型转发路径）----
        // 2026-09-06 回归防护：CreateDelegate 直接绑 Publish<T> 到 Action<EventBus, IGameEvent>
        // 因参数类型不完全一致抛 ArgumentException（启动即炸），此处锁死非泛型转发路径。

        [Test]
        public void PublishInternal_WithSubscriber_HandlerCalled()
        {
            CharacterDamagedEvent received = null;
            CharacterDamagedEvent sent = new CharacterDamagedEvent { TargetId = 42L };
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { received = e; });
            EventBus.Instance.PublishInternal(sent);
            Assert.AreSame(sent, received, "订阅者应收到同一事件实例");
            Assert.AreEqual(42L, received.TargetId, "事件字段应完整传递");
        }

        [Test]
        public void PublishInternal_WithoutSubscriber_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                EventBus.Instance.PublishInternal(new CharacterDamagedEvent());
            }, "无订阅者时发布不应抛异常");
        }

        [Test]
        public void PublishInternal_CacheHitOnSecondPublish_StillDelivers()
        {
            int callCount = 0;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { callCount++; });
            EventBus.Instance.PublishInternal(new CharacterDamagedEvent());
            EventBus.Instance.PublishInternal(new CharacterDamagedEvent()); // 第二次走分发器缓存
            Assert.AreEqual(2, callCount, "缓存命中的分发器应继续正确投递");
        }

        [Test]
        public void PublishInternal_DifferentEventTypes_OnlyMatchingCalled()
        {
            bool damageCalled = false;
            bool otherCalled = false;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(e => { damageCalled = true; });
            EventBus.Instance.Subscribe<TestEventB>(e => { otherCalled = true; });
            EventBus.Instance.PublishInternal(new CharacterDamagedEvent());
            Assert.IsTrue(damageCalled, "匹配的事件应被调用");
            Assert.IsFalse(otherCalled, "不匹配的事件不应被调用");
        }
    }
}
