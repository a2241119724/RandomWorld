namespace LAB2D.Network
{
    /// <summary>
    /// 网络视图抽象接口 — 解耦 Character 与 PhotonView 的直接依赖。
    /// 提供在线/离线/测试三种实现：PunNetworkViewAdapter、NullNetworkView、MockNetworkView。
    /// </summary>
    public interface INetworkView
    {
        /// <summary>是否为本地玩家拥有。</summary>
        bool IsMine { get; }

        /// <summary>拥有者名称（用于 HUD 显示）。</summary>
        string OwnerName { get; }

        /// <summary>在线模式是否已连接。</summary>
        bool IsOnline { get; }

        /// <summary>是否为 MasterClient。离线模式下始终为 true。</summary>
        bool IsMasterClient { get; }

        /// <summary>
        /// 调用 RPC 方法。
        /// 离线模式下为空操作。
        /// </summary>
        /// <param name="methodName">Photon [PunRPC] 方法名。</param>
        /// <param name="target">RPC 目标。</param>
        /// <param name="args">RPC 参数。</param>
        void RPC(string methodName, object target, params object[] args);
    }
}
