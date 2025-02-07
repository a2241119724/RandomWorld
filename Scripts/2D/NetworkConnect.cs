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
            //    Debug.LogError("player Instantiate Error!!!");
            //    return;
            //}
            //player.name = "Player";
            //// ���ò㼶
            //player.layer = LayerMask.NameToLayer("Player");
        }

        public override void OnLeftLobby()
        {
            base.OnLeftLobby();
            Debug.Log("�˳�����");
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            Debug.Log("�뿪����");
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            Debug.Log("����Ҽ���");
            //InitTip.Instance.showTip("����Ҽ���");
            //����Ҫ�����������ݸ������
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
            Debug.Log("��������ʧ��!!!");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            Debug.Log("�Ͽ�����!!!");
            IsOnline = false;
        }
    }
}
