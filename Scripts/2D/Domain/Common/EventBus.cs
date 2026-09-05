namespace LAB2D.Domain.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 集中式事件总线，用于游戏系统之间的解耦通信。
    /// 纯C#实现 — 不依赖UnityEngine。管理器在此发布事件；
    /// 展示层/适配器订阅以接收事件。
    ///
    /// 用法:
    ///   // 订阅
    ///   EventBus.Instance.Subscribe&lt;CharacterDamagedEvent&gt;(OnCharacterDamaged);
    ///   // 发布
    ///   EventBus.Instance.Publish(new CharacterDamagedEvent { ... });
    ///   // 取消订阅（在OnDestroy/Disable中调用）
    ///   EventBus.Instance.Unsubscribe&lt;CharacterDamagedEvent&gt;(OnCharacterDamaged);
    /// </summary>
    public class EventBus
    {
        private static EventBus instance;
        private readonly Dictionary<Type, Delegate> subscribers;

        public EventBus()
        {
            this.subscribers = new Dictionary<Type, Delegate>();
        }

        /// <summary>
        /// 单例实例。测试时可通过 new EventBus() 创建。
        /// </summary>
        public static EventBus Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventBus();
                }

                return instance;
            }
        }

        /// <summary>
        /// 替换单例实例（用于测试）。
        /// </summary>
        public static void SetInstance(EventBus newInstance)
        {
            instance = newInstance;
        }

        /// <summary>
        /// 订阅类型为T的事件。
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Type eventType = typeof(T);
            if (this.subscribers.TryGetValue(eventType, out Delegate existing))
            {
                this.subscribers[eventType] = Delegate.Combine(existing, handler);
            }
            else
            {
                this.subscribers[eventType] = handler;
            }
        }

        /// <summary>
        /// 取消订阅类型为T的事件。
        /// 即使从未订阅也可以安全调用。
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Type eventType = typeof(T);
            if (this.subscribers.TryGetValue(eventType, out Delegate existing))
            {
                Delegate remaining = Delegate.Remove(existing, handler);
                if (remaining == null)
                {
                    this.subscribers.Remove(eventType);
                }
                else
                {
                    this.subscribers[eventType] = remaining;
                }
            }
        }

        /// <summary>
        /// 向类型T的所有订阅者发布事件。
        /// </summary>
        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            Type eventType = typeof(T);
            if (this.subscribers.TryGetValue(eventType, out Delegate existing))
            {
                if (existing is Action<T> typedHandler)
                {
                    typedHandler.Invoke(gameEvent);
                }
            }
        }

        /// <summary>
        /// 按运行时类型发布事件（供非泛型上下文/闭包转发调用）。
        /// 经 <see cref="PublishDispatcher{T}"/> 转发强类型 <see cref="Publish{T}"/>——
        /// DynamicInvoke 每次分配 object[] 参数组 + 反射调用栈（CharacterDamagedEvent 在
        /// 100 角色混战下每秒数十次），此处按事件类型缓存一个 Action&lt;EventBus, IGameEvent&gt;
        /// 强类型分发器，之后调用是 委托直调 + (T)e 引用转换，零反射零分配。
        /// 注意：不能对 Publish&lt;T&gt; 直接 CreateDelegate 绑 Action&lt;EventBus, IGameEvent&gt;——
        /// CLR 委托绑定要求参数类型完全一致（具体事件类型 ≠ IGameEvent），会抛
        /// ArgumentException: method arguments are incompatible（2026-09-06 启动即炸实锤）。
        /// </summary>
        /// <param name="gameEvent">事件实例。</param>
        public void PublishInternal(IGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return;
            }

            Type eventType = gameEvent.GetType();
            if (!this.subscribers.TryGetValue(eventType, out Delegate existing) || existing == null)
            {
                return;
            }

            if (!TypedPublishCache.TryGetValue(eventType, out Action<EventBus, IGameEvent> typedPublish))
            {
                // MakeGenericType 实例化分发器（cctor 构造一次闭包委托），读静态字段缓存
                typedPublish = (Action<EventBus, IGameEvent>)typeof(PublishDispatcher<>)
                    .MakeGenericType(eventType)
                    .GetField(nameof(PublishDispatcher<IGameEvent>.Invoke))
                    .GetValue(null);
                lock (TypedPublishCache)
                {
                    TypedPublishCache[eventType] = typedPublish;
                }
            }

            typedPublish(this, gameEvent);
        }

        /// <summary>事件类型 → 强类型 Publish&lt;T&gt; 分发器缓存（事件类型集合在运行期有限且恒定）。</summary>
        private static readonly Dictionary<Type, Action<EventBus, IGameEvent>> TypedPublishCache = new Dictionary<Type, Action<EventBus, IGameEvent>>();

        /// <summary>
        /// 泛型强类型分发器：让非泛型上下文拿到编译期 T，实现 PublishInternal → Publish&lt;T&gt;
        /// 的零反射转发（MakeGenericType 触发 cctor 构造一次静态闭包委托，之后直调）。
        /// (T)e 转换必然安全：PublishInternal 的 eventType 取自 gameEvent.GetType()。
        /// </summary>
        private static class PublishDispatcher<T> where T : IGameEvent
        {
            public static readonly Action<EventBus, IGameEvent> Invoke = (bus, e) => bus.Publish((T)e);
        }

        /// <summary>
        /// 清除所有订阅（用于场景卸载或测试）。
        /// </summary>
        public void Clear()
        {
            this.subscribers.Clear();
        }

        /// <summary>
        /// 获取指定事件类型的订阅者数量。
        /// </summary>
        public int GetSubscriberCount<T>() where T : IGameEvent
        {
            Type eventType = typeof(T);
            if (this.subscribers.TryGetValue(eventType, out Delegate existing) &&
                existing is Action<T> typedHandler)
            {
                return typedHandler.GetInvocationList().Length;
            }

            return 0;
        }
    }
}
