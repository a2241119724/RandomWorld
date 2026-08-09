namespace LAB2D.Domain.Common
{
    using LAB2D.Core;
    using LAB2D.UnityAdapter;

    /// <summary>
    /// 安全的 IGameLogger 工厂。
    /// 通过 TryGet 获取 ServiceLocator 中注册的实例；
    /// 若尚未注册（如初始化早期），降级为 UnityLogger（直接封装 Debug.Log）。
    /// 解决 Singleton 构造 / OnEnable 在 GlobalInit.RegisterSafeServices() 之前被触发时的崩溃问题。
    /// </summary>
    public static class GameLoggerFactory
    {
        private static IGameLogger fallbackInstance;

        /// <summary>
        /// 安全获取 IGameLogger 实例。优先返回已注册的服务，未注册时返回 UnityLogger 降级。
        /// </summary>
        public static IGameLogger Get()
        {
            if (ServiceLocator.TryGet<IGameLogger>(out var logger))
            {
                return logger;
            }

            return fallbackInstance ?? (fallbackInstance = new UnityLogger());
        }
    }
}
