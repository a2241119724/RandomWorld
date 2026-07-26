namespace LAB2D.Network
{
    /// <summary>
    /// 地图数据同步发送接口 — 解耦 Map 层与 PhotonView.RPC 的直接依赖。
    /// 在线模式使用 PunSyncSender 广播到其他客户端，离线模式为空操作。
    /// </summary>
    public interface ISyncSender
    {
        /// <summary>在线模式是否可用。</summary>
        bool IsOnline { get; }

        /// <summary>
        /// 向其他客户端广播同步数据。
        /// 在线模式下调用 photonView.RPC(methodName, RpcTarget.Others, args)。
        /// </summary>
        void Broadcast(string methodName, params object[] args);
    }

    /// <summary>
    /// Photon 同步发送器 — 包装 PhotonView.RPC 调用。
    /// </summary>
    public sealed class PunSyncSender : ISyncSender
    {
        private readonly Photon.Pun.PhotonView photonView;
        private readonly Photon.Pun.RpcTarget rpcTarget = Photon.Pun.RpcTarget.Others;

        public PunSyncSender(Photon.Pun.PhotonView photonView)
        {
            this.photonView = photonView;
        }

        public bool IsOnline
        {
            get { return Photon.Pun.PhotonNetwork.IsConnected; }
        }

        public void Broadcast(string methodName, params object[] args)
        {
            if (this.photonView != null && this.IsOnline)
            {
                this.photonView.RPC(methodName, this.rpcTarget, args);
            }
        }
    }

    /// <summary>
    /// 空同步发送器 — 离线模式下所有广播请求均为空操作。
    /// </summary>
    public sealed class NullSyncSender : ISyncSender
    {
        public static readonly NullSyncSender Instance = new NullSyncSender();

        public bool IsOnline { get { return false; } }

        public void Broadcast(string methodName, params object[] args)
        {
        }
    }
}
