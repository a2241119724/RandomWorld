namespace LAB2D.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 轻量级服务定位器，作为从 Singleton 到依赖注入的过渡方案。
    /// 不依赖反射、不自动装配 — 所有注册由 GlobalInit 在启动时显式完成。
    ///
    /// 用法:
    ///   // 注册 (GlobalInit.Awake)
    ///   ServiceLocator.Register&lt;LogManager&gt;(LogManager.Instance);
    ///
    ///   // 获取 (构造函数注入回退)
    ///   IGameTime time = injectedTime ?? ServiceLocator.Get&lt;IGameTime&gt;();
    ///
    ///   // 测试
    ///   ServiceLocator.Register&lt;IGameTime&gt;(new FakeGameTime());
    ///   // ... 运行测试 ...
    ///   ServiceLocator.Reset();
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// 注册一个服务实例。
        /// 如果该类型已注册，则替换旧实例。
        /// </summary>
        /// <typeparam name="T">服务类型（通常是接口）。</typeparam>
        /// <param name="instance">服务实例。</param>
        public static void Register<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), $"ServiceLocator.Register<{typeof(T).Name}>() 收到 null 实例。");
            }

            services[typeof(T)] = instance;
        }

        /// <summary>
        /// 获取已注册的服务实例。
        /// 如果未找到，抛出 KeyNotFoundException。
        /// </summary>
        /// <typeparam name="T">服务类型。</typeparam>
        /// <returns>服务实例。</returns>
        public static T Get<T>()
        {
            if (services.TryGetValue(typeof(T), out object instance))
            {
                return (T)instance;
            }

            throw new KeyNotFoundException(
                $"ServiceLocator 中未注册类型 {typeof(T).Name}。" +
                "请确保在 GlobalInit.Awake() 中调用了 ServiceLocator.Register<" + typeof(T).Name + ">(...)。");
        }

        /// <summary>
        /// 尝试获取已注册的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型。</typeparam>
        /// <param name="service">输出服务实例，如果未注册则为 default。</param>
        /// <returns>如果找到服务则返回 true。</returns>
        public static bool TryGet<T>(out T service)
        {
            if (services.TryGetValue(typeof(T), out object instance))
            {
                service = (T)instance;
                return true;
            }

            service = default;
            return false;
        }

        /// <summary>
        /// 检查指定类型是否已注册。
        /// </summary>
        public static bool IsRegistered<T>()
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 获取所有注册的实现指定接口的服务。
        /// 用于批量初始化/更新（如 IInitializable, ITickable）。
        /// </summary>
        /// <typeparam name="T">接口类型。</typeparam>
        /// <returns>所有匹配的服务实例。</returns>
        public static List<T> GetAll<T>()
        {
            List<T> result = new List<T>();
            foreach (var kvp in services)
            {
                if (kvp.Value is T typed)
                {
                    result.Add(typed);
                }
            }

            return result;
        }

        /// <summary>
        /// 移除指定类型的注册。
        /// </summary>
        public static void Unregister<T>()
        {
            services.Remove(typeof(T));
        }

        /// <summary>
        /// 清除所有注册（仅用于测试或场景卸载）。
        /// 生产代码中请勿调用此方法。
        /// </summary>
        public static void Reset()
        {
            services.Clear();
        }

        /// <summary>
        /// 获取已注册服务类型的数量，用于调试。
        /// </summary>
        public static int Count => services.Count;
    }
}
