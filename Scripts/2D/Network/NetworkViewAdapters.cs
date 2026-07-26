namespace LAB2D.Network
{
    /// <summary>
    /// 在线模式网络视图适配器 — 包装 PhotonView 以匹配 INetworkView 接口。
    /// </summary>
    public sealed class PunNetworkViewAdapter : INetworkView
    {
        private readonly Photon.Pun.PhotonView photonView;

        public PunNetworkViewAdapter(Photon.Pun.PhotonView photonView)
        {
            this.photonView = photonView;
        }

        public bool IsMine
        {
            get { return this.photonView != null && this.photonView.IsMine; }
        }

        public string OwnerName
        {
            get { return this.photonView != null ? this.photonView.Owner.NickName : string.Empty; }
        }

        public bool IsOnline
        {
            get { return Photon.Pun.PhotonNetwork.IsConnected; }
        }

        public bool IsMasterClient
        {
            get { return Photon.Pun.PhotonNetwork.IsMasterClient; }
        }

        public void RPC(string methodName, object target, params object[] args)
        {
            if (this.photonView == null || !this.IsOnline)
            {
                return;
            }

            this.photonView.RPC(methodName, (Photon.Pun.RpcTarget)target, args);
        }
    }

    /// <summary>
    /// 离线模式网络视图 — 所有 RPC 调用为空操作，IsMine/IsMasterClient 始终为 true。
    /// </summary>
    public sealed class OfflineNetworkView : INetworkView
    {
        public static readonly OfflineNetworkView Instance = new OfflineNetworkView();

        public bool IsMine { get { return true; } }

        public string OwnerName { get { return "LocalPlayer"; } }

        public bool IsOnline { get { return false; } }

        public bool IsMasterClient { get { return true; } }

        public void RPC(string methodName, object target, params object[] args)
        {
        }
    }
}
