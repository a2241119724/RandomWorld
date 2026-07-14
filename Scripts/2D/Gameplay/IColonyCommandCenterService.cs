namespace LAB2D.Gameplay
{
    using System;

    /// <summary>
    /// 殖民地指挥中心服务接口 — 解耦 Singleton 直接引用。
    /// 实现：ColonyCommandCenterManager。
    /// 消费者通过 ServiceLocator.Get&lt;IColonyCommandCenterService&gt;() 获取。
    /// </summary>
    public interface IColonyCommandCenterService
    {
        /// <summary>指挥报告变化事件。</summary>
        event Action<ColonyCommandCenterReport> OnCommandReportChanged;

        /// <summary>立即刷新并返回指挥报告。</summary>
        ColonyCommandCenterReport Refresh(bool allowTip);
    }
}
