using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Photon.Realtime;

namespace LAB2D
{
    /// <summary>
    /// ������ע���AppId���Ƶ�������
    /// Photon View�۲�ĳЩ����
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
            // ʹ��Photon/PhotonUnityNetworking/Resources/PhotonServerSettings���ӷ�����
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// �Ƿ����ӷ�����
        /// </summary>
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            // ���õ�ǰ��������ΪsqlLobby
            TypedLobby typedLobby = new TypedLobby("myLobby", LobbyType.SqlLobby);
            // ֻ�м��뵽�����ſ��Ի�ȡ�����б�
            PhotonNetwork.JoinLobby(typedLobby);
        }

        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
        }

        /// <summary>
        /// �ڼ��뷿��ɹ�
        /// </summary>
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            //GameObject player = PhotonNetwork.Instantiate(Constant.PREFAB + _player.name, Vector3.zero, Quaternion.identity);
            //if (player == null)
            //{
            //    LogManager.Instance.log("���뷿��ɹ�", LogManager.LogLevel.Info);
            //    return;
            //}
            //player.name = "Player";
            //// ���ò㼶
            //player.layer = LayerMask.NameToLayer("Player");
        }

        public override void OnLeftLobby()
        {
            base.OnLeftLobby();
            LogManager.Instance.log("�˳�����", LogManager.LogLevel.Info);
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            LogManager.Instance.log("�뿪����", LogManager.LogLevel.Info);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            LogManager.Instance.log("����Ҽ���", LogManager.LogLevel.Info);
            //InitTip.Instance.showTip("����Ҽ���");
            //����Ҫ�����������ݸ������
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
            LogManager.Instance.log("��������ʧ��!!!", LogManager.LogLevel.Error);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            LogManager.Instance.log("�Ͽ�����!!!", LogManager.LogLevel.Error);
            IsOnline = false;
        }
    }
}
