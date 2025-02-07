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
            //    Debug.LogError("player Instantiate Error!!!");
            //    return;
            //}
            //player.name = "Player";
            //// 设置层级
            //player.layer = LayerMask.NameToLayer("Player");
        }

        public override void OnLeftLobby()
        {
            base.OnLeftLobby();
            Debug.Log("退出大厅");
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            Debug.Log("离开房间");
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            Debug.Log("新玩家加入");
            //InitTip.Instance.showTip("新玩家加入");
            //仅需要房主传递数据给新玩家
            if (PhotonNetwork.IsMasterClient)
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
            Debug.Log("创建房间失败!!!");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            Debug.Log("断开连接!!!");
            IsOnline = false;
        }
    }
}
