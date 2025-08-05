namespace LAB2D
{
    using Photon.Pun;
    using Photon.Realtime;

    /// <summary>
    /// 将官网注册的AppId复制到设置中
    /// Photon View观察某些物体.
    /// </summary>
    public class NetworkConnect : MonoBehaviourPunCallbacks
    {
        /// <summary>
        /// 单例.
        /// </summary>
        public static NetworkConnect Instance { get; private set; }

        /// <summary>
        /// 是否是联网的.
        /// </summary>
        public bool IsOnline { get; private set; } = true;

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

            // GameObject player = PhotonNetwork.Instantiate(Constant.PREFAB + _player.name, Vector3.zero, Quaternion.identity);
            // if (player == null)
            // {
            //     LogManager.Instance.log("加入房间成功", LogManager.LogLevel.Info);
            //     return;
            // }
            // player.name = "Player";
            // // 设置层级
            // player.layer = LayerMask.NameToLayer("Player");
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

            // InitTip.Instance.showTip("新玩家加入");
            // 仅需要房主传递数据给新玩家
            if (this.IsOnline && PhotonNetwork.IsMasterClient)
            {
                if (TileMap.Instance != null)
                {
                    TileMap.Instance.InitData();
                }

                // if (EnemyManager.Instance != null)
                // {
                //     EnemyManager.Instance.initData();
                // }
            }
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
    }
}
