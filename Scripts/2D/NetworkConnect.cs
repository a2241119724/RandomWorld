namespace LAB2D
{
    using Photon.Pun;
    using Photon.Realtime;

    /// <summary>
    /// 将官网注册的AppId复制到设置中
    /// Photon View观察某些物体.
    /// </summary>
    public class NetworkConnect : MonoBehaviourPunCallbacks, ILobbyCallbacks
    {
        /// <summary>
        /// 单例.
        /// </summary>
        public static NetworkConnect Instance { get; private set; }

        /// <summary>
        /// 是否是联网的.
        /// </summary>
        public bool IsOnline { get; private set; } = true;

        public void Awake()
        {
            Instance = this;
            PhotonNetwork.AutomaticallySyncScene = true;

            // this.IsOnline = false;
        }

        public void Start()
        {
            // 使用Photon/PhotonUnityNetworking/Resources/PhotonServerSettings连接服务器
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// 是否连接服务器.
        /// </summary>
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            LogManager.Instance.Log("已连接服务器", LogManager.LogLevel.Info);

            // 设置当前大厅类型为sqlLobby
            TypedLobby typedLobby = new ("myLobby", LobbyType.SqlLobby);

            // 只有加入到大厅才可以获取房间列表
            PhotonNetwork.JoinLobby(typedLobby);
        }

        /// <summary>
        /// 连接大厅时调用.
        /// </summary>
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
            LogManager.Instance.Log("进入大厅", LogManager.LogLevel.Info);
        }

        /// <summary>
        /// 加入房间成功.
        /// </summary>
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            LogManager.Instance.Log("加入房间成功", LogManager.LogLevel.Info);

            // 同步地图数据
            SyncDataTool.SyncDataReqWrapper(TileMap.Instance.PhotonView);
            SyncDataTool.SyncDataReqWrapper(ResourceMap.Instance.PhotonView);
            SyncDataTool.SyncDataReqWrapper(BuildMap.Instance.PhotonView);
            SyncDataTool.SyncDataReqWrapper(GatherMap.Instance.PhotonView);
            SyncDataTool.SyncDataReqWrapper(ItemMap.Instance.PhotonView);
        }

        /// <summary>
        /// 离开大厅时调用.
        /// </summary>
        public override void OnLeftLobby()
        {
            base.OnLeftLobby();
            LogManager.Instance.Log("退出大厅", LogManager.LogLevel.Info);
        }

        /// <summary>
        /// 离开房间时调用.
        /// </summary>
        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            LogManager.Instance.Log("离开房间", LogManager.LogLevel.Info);
        }

        /// <summary>
        /// 玩家进入房间时调用.
        /// </summary>
        /// <param name="newPlayer">加入的玩家信息.</param>
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            LogManager.Instance.Log("新玩家加入", LogManager.LogLevel.Info);
            GlobalInit.Instance.ShowTip("新玩家加入");
        }

        /// <summary>
        /// 当创建房间失败时调用.
        /// </summary>
        /// <param name="returnCode">返回的状态码.</param>
        /// <param name="message">返回的信息.</param>
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            LogManager.Instance.Log("创建房间失败!!!", LogManager.LogLevel.Error);
            GlobalInit.Instance.ShowTip("创建房间失败");
            PanelController.Instance.Close();
            PanelController.Instance.Show(JoinMenuPanel.Instance);
        }

        /// <summary>
        /// 当连接关闭时调用.
        /// </summary>
        /// <param name="cause">关闭原因.</param>
        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            LogManager.Instance.Log("断开连接!!!", LogManager.LogLevel.Error);
            this.IsOnline = false;
        }
    }
}
