namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 可初始化服务接口 — 用于启动阶段的批量初始化。
    /// 实现类在 ServiceLocator 中注册后，由 GlobalInit 统一调用 Initialize()。
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// 执行初始化逻辑。在 GlobalInit.Start() 中按注册顺序调用。
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// 可 Tick 服务接口 — 用于 Update 循环的批量驱动。
    /// 实现类在 ServiceLocator 中注册后，由 GlobalInit 每帧调用 Tick()。
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// 每帧更新。在 GlobalInit.Update() 中按注册顺序调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒）。</param>
        void Tick(float deltaTime);
    }
}
