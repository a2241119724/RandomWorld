using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Photon.Realtime;

namespace LAB2D
{
    /// <summary>
    /// 将官网注册的AppId复制到设置中
    /// Photon View观察某些物体
    /// </summary>
    public class NetworkConnect : MonoBehaviourPunCallbacks
    {
        public static NetworkConnect Instance { get; private set; }
        public bool IsOnline { get; private set; } = true;

        void Awake()
        {
            Instance = this;
            PhotonNetwork.AutomaticallySyncScene = true;
            IsOnline = false;
        }

        void Start()
        {
            // 使用Photon/PhotonUnityNetworking/Resources/PhotonServerSettings连接服务器
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// 是否连接服务器
        /// </summary>
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            // 设置当前大厅类型为sqlLobby
            TypedLobby typedLobby = new TypedLobby("myLobby", LobbyType.SqlLobby);
            // 只有加入到大厅才可以获取房间列表
            PhotonNetwork.JoinLobby(typedLobby);
        }

        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
        }

        /// <summary>
        /// 在加入房间成功
        /// </summary>
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            //GameObject player = PhotonNetwork.Instantiate(Constant.PREFAB + _player.name, Vector3.zero, Quaternion.identity);
            //if (player == null)
            //{
            //    LogManager.Instance.log("加入房间成功", LogManager.LogLevel.Info);
            //    return;
            //}
            //player.name = "Player";
            //// 设置层级
            //player.layer = LayerMask.NameToLayer("Player");
        }

        public override void OnLeftLobby()
        {
            base.OnLeftLobby();
            LogManager.Instance.log("退出大厅", LogManager.LogLevel.Info);
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            LogManager.Instance.log("离开房间", LogManager.LogLevel.Info);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            LogManager.Instance.log("新玩家加入", LogManager.LogLevel.Info);
            //InitTip.Instance.showTip("新玩家加入");
            //仅需要房主传递数据给新玩家
            if (IsOnline && PhotonNetwork.IsMasterClient)
            {
                if (TileMap.Instance != null)
                {
                    TileMap.Instance.initData();
                }
                //if (EnemyManager.Instance != null)
                //{
                //    EnemyManager.Instance.initData();
                //}
            }
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            LogManager.Instance.log("创建房间失败!!!", LogManager.LogLevel.Error);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            LogManager.Instance.log("断开连接!!!", LogManager.LogLevel.Error);
            IsOnline = false;
        }
    }
}
