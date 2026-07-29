namespace LAB2D.Tool
{
    using LAB2D;
    using Photon.Pun;

    /// <summary>
    /// 同步数据工具
    /// </summary>
    public class SyncDataTool
    {
        /// <summary>
        /// 同步数据请求包装
        /// </summary>
        /// <param name="photonView">同步数据</param>
        /// <param name="methodName">rpc方法名</param>
        public static void SyncDataReqWrapper(PhotonView photonView, string methodName = "SyncDataReq")
        {
            if (Core.GameServices.NetworkIsMasterClientProvider())
            {
                return;
            }

            photonView.RPC(methodName, RpcTarget.MasterClient, DataTool.ToByteArray(PhotonNetwork.LocalPlayer.ActorNumber));
        }

        /// <summary>
        /// 同步数据响应包装
        /// </summary>
        /// <typeparam name="T">传输数据类型</typeparam>
        /// <param name="photonView">同步数据</param>
        /// <param name="playerId">请求玩家ID</param>
        /// <param name="data">响应数据</param>
        /// <param name="methodName">rpc方法名</param>
        public static void SyncDataRespWrapper<T>(PhotonView photonView, byte[] playerId, T data, string methodName = "SyncDataResp")
        {
            if (!Core.GameServices.NetworkIsMasterClientProvider())
            {
                return;
            }

            photonView.RPC(methodName, PhotonNetwork.LocalPlayer.Get(DataTool.FromByteArray<int>(playerId)), DataTool.ToByteArray(data));
        }

        /// <summary>
        /// 同步数据响应包装, 重载
        /// </summary>
        /// <typeparam name="T">传输数据类型</typeparam>
        /// <param name="photonView">同步数据</param>
        /// <param name="rpcTarget">发送的目标</param>
        /// <param name="data">响应数据</param>
        /// <param name="methodName">rpc方法名</param>
        public static void SyncDataRespWrapper<T>(PhotonView photonView, RpcTarget rpcTarget, T data, string methodName = "SyncDataResp")
        {
            if (!Core.GameServices.NetworkIsMasterClientProvider())
            {
                return;
            }

            photonView.RPC(methodName, rpcTarget, DataTool.ToByteArray(data));
        }
    }
}
