namespace LAB2D
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Centralized event bus for decoupled communication between game systems.
    /// Pure C# — no UnityEngine dependencies. Managers publish events here;
    /// presentation/adapters subscribe to receive them.
    ///
    /// Usage:
    ///   // Subscribe
    ///   EventBus.Instance.Subscribe&lt;CharacterDamagedEvent&gt;(OnCharacterDamaged);
    ///   // Publish
    ///   EventBus.Instance.Publish(new CharacterDamagedEvent { ... });
    ///   // Unsubscribe (call in OnDestroy/Disable)
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
        /// Singleton instance. Create via new EventBus() for testing.
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
        /// Replace the singleton instance (for testing).
        /// </summary>
        public static void SetInstance(EventBus newInstance)
        {
            instance = newInstance;
        }

        /// <summary>
        /// Subscribe to events of type T.
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
        /// Unsubscribe from events of type T.
        /// Safe to call even if never subscribed.
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
        /// Publish an event to all subscribers of type T.
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
        /// Clear all subscriptions (for scene unload or testing).
        /// </summary>
        public void Clear()
        {
            this.subscribers.Clear();
        }

        /// <summary>
        /// Get count of subscribers for a given event type.
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
