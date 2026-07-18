// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectAndJoinRandom.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  用于调用ConnectUsingSettings并轻松进入PUN房间的简单组件。
// </summary>
// <remarks>
//  如果AutoConnect为false，自定义Inspector会提供一个在PlayMode中连接的按钮。
//  </remarks>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>用于调用ConnectUsingSettings并轻松进入PUN房间的简单组件。</summary>
    /// <remarks>如果AutoConnect为false，自定义Inspector会提供一个在PlayMode中连接的按钮。</remarks>
    public class ConnectAndJoinRandom : MonoBehaviourPunCallbacks
    {
        /// <summary>自动连接？如果为false，你可以稍后将此设置为true，或在你自己的脚本中调用ConnectUsingSettings。</summary>
        public bool AutoConnect = true;

        /// <summary>用作PhotonNetwork.GameVersion。</summary>
        public byte Version = 1;

        /// <summary>房间允许的最大玩家数。一旦满员，下一个尝试加入的连接将创建一个新房间。</summary>
        [Tooltip("The max number of players allowed in room. Once full, a new room will be created by the next connection attemping to join.")]
        public byte MaxPlayers = 4;

        public int playerTTL = -1;

        public void Start()
        {
            if (this.AutoConnect)
            {
                this.ConnectNow();
            }
        }

        public void ConnectNow()
        {
            Debug.Log("ConnectAndJoinRandom.ConnectNow() will now call: PhotonNetwork.ConnectUsingSettings().");


            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = this.Version + "." + SceneManagerHelper.ActiveSceneBuildIndex;

        }


        // 下面，我们实现了Photon Realtime API的一些回调。
        // 作为MonoBehaviourPunCallbacks意味着我们可以重写这里所需的少数方法。


        public override void OnConnectedToMaster()
        {
            Debug.Log("OnConnectedToMaster() was called by PUN. This client is now connected to Master Server in region [" + PhotonNetwork.CloudRegion +
                "] and can join a room. Calling: PhotonNetwork.JoinRandomRoom();");
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("OnJoinedLobby(). This client is now connected to Relay in region [" + PhotonNetwork.CloudRegion + "]. This script now calls: PhotonNetwork.JoinRandomRoom();");
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available in region [" + PhotonNetwork.CloudRegion + "], so we create one. Calling: PhotonNetwork.CreateRoom(null, new RoomOptions() {maxPlayers = 4}, null);");

            RoomOptions roomOptions = new RoomOptions() { MaxPlayers = this.MaxPlayers };
            if (playerTTL >= 0)
                roomOptions.PlayerTtl = playerTTL;

            PhotonNetwork.CreateRoom(null, roomOptions, null);
        }

        // 以下方法的实现是为了提供一些上下文。根据需要重新实现它们。
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log("OnDisconnected(" + cause + ")");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room in region [" + PhotonNetwork.CloudRegion + "]. Game is now running.");
        }
    }
}