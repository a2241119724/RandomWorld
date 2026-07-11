namespace LAB2D
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
