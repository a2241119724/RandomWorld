// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerNumbering.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  为房间中的玩家分配编号。使用房间自定义属性
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 借助房间属性在房间/游戏中实现一致的编号。通过Player.GetPlayerNumber()扩展方法访问。
    /// </summary>
    /// <remarks>
    /// 索引范围从0到最大玩家数。
    /// 索引在玩家在房间期间保持不变。
	/// 如果编号为2的玩家离开，而编号为1的玩家离开，则编号1变为空位，并将在未来分配给加入的玩家（加入时分配第一个可用的空编号）
    /// </remarks>
    public class PlayerNumbering : MonoBehaviourPunCallbacks
    {
        //TODO: Add a "numbers available" bool, to allow easy access to this?!

        #region Public Properties

        /// <summary>
        /// 实例。查询房间索引的入口点。
        /// </summary>
        public static PlayerNumbering instance;

        public static Player[] SortedPlayers;

        /// <summary>
        /// OnPlayerNumberingChanged委托。使用
        /// </summary>
        public delegate void PlayerNumberingChanged();
        /// <summary>
        /// 每当房间索引更新时调用。用于离散更新。始终优于每帧暴力调用。
        /// </summary>
        public static event PlayerNumberingChanged OnPlayerNumberingChanged;


        /// <summary>定义用于房间玩家索引跟踪的房间自定义属性名称。</summary>
        public const string RoomPlayerIndexedProp = "pNr";

        /// <summary>
        /// 此组件的GameObject在关卡加载时不被销毁的标志。
        /// </summary>
        public bool dontDestroyOnLoad = false;


        #endregion


        #region MonoBehaviours methods

        public void Awake()
        {

            if (instance != null && instance != this && instance.gameObject != null)
            {
                GameObject.DestroyImmediate(instance.gameObject);
            }

            instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(this.gameObject);
            }

            this.RefreshData();
        }

        #endregion


        #region PunBehavior Overrides

        public override void OnJoinedRoom()
        {
            this.RefreshData();
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerNumbering.RoomPlayerIndexedProp);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            this.RefreshData();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            this.RefreshData();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps != null && changedProps.ContainsKey(PlayerNumbering.RoomPlayerIndexedProp))
            {
                this.RefreshData();
            }
        }

        #endregion


        // 每个玩家都可以在房间中选择自己的玩家编号，如果所有"更早"的玩家已经选择了他们的编号


        /// <summary>
        /// 内部调用：刷新缓存数据并调用OnPlayerNumberingChanged委托。
        /// </summary>
        public void RefreshData()
        {
            if (PhotonNetwork.CurrentRoom == null)
            {
                return;
            }

            if (PhotonNetwork.LocalPlayer.GetPlayerNumber() >= 0)
            {
                SortedPlayers = PhotonNetwork.CurrentRoom.Players.Values.OrderBy((p) => p.GetPlayerNumber()).ToArray();
                if (OnPlayerNumberingChanged != null)
                {
                    OnPlayerNumberingChanged();
                }
                return;
            }


            HashSet<int> usedInts = new HashSet<int>();
            Player[] sorted = PhotonNetwork.PlayerList.OrderBy((p) => p.ActorNumber).ToArray();

            string allPlayers = "all players: ";
            foreach (Player player in sorted)
            {
                allPlayers += player.ActorNumber + "=pNr:" + player.GetPlayerNumber() + ", ";

                int number = player.GetPlayerNumber();

                // 如果这是当前用户，选择一个编号并中断
                // 否则：
                // 检查该用户是否有编号
                // 如果没有，中断！
                // 否则记住已使用的编号

                if (player.IsLocal)
                {
                    Debug.Log("PhotonNetwork.CurrentRoom.PlayerCount = " + PhotonNetwork.CurrentRoom.PlayerCount);

                    // 选择一个编号
                    for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount; i++)
                    {
                        if (!usedInts.Contains(i))
                        {
                            player.SetPlayerNumber(i);
                            break;
                        }
                    }
                    // 然后中断
                    break;
                }
                else
                {
                    if (number < 0)
                    {
                        break;
                    }
                    else
                    {
                        usedInts.Add(number);
                    }
                }
            }

            //Debug.Log(allPlayers);
            //Debug.Log(PhotonNetwork.LocalPlayer.ToStringFull() + " has PhotonNetwork.player.GetPlayerNumber(): " + PhotonNetwork.LocalPlayer.GetPlayerNumber());

            SortedPlayers = PhotonNetwork.CurrentRoom.Players.Values.OrderBy((p) => p.GetPlayerNumber()).ToArray();
            if (OnPlayerNumberingChanged != null)
            {
                OnPlayerNumberingChanged();
            }
        }
    }



    /// <summary>用于PlayerRoomIndexing和Player类的扩展方法。</summary>
    public static class PlayerNumberingExtensions
    {
        /// <summary>Player类的扩展方法，用于封装对玩家自定义属性的访问。
			/// 确保使用委托'OnPlayerNumberingChanged'来获知何时可以查询PlayerNumber。编号可能随时间变化，或在初始阶段（例如玩家创建房间时）尚未分配。
			/// </summary>
        /// <returns>房间中的持久索引。-1表示未索引</returns>
        public static int GetPlayerNumber(this Player player)
        {
            if (player == null)
            {
                return -1;
            }

            if (PhotonNetwork.OfflineMode)
            {
                return 0;
            }
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                return -1;
            }

            object value;
            if (player.CustomProperties.TryGetValue(PlayerNumbering.RoomPlayerIndexedProp, out value))
            {
                return (byte)value;
            }
            return -1;
        }

        /// <summary>
        /// 设置玩家编号。
        /// 不建议手动干预playerNumbering，但这是可能的。
        /// </summary>
        /// <param name="player">玩家。</param>
        /// <param name="playerNumber">玩家编号。</param>
        public static void SetPlayerNumber(this Player player, int playerNumber)
        {
            if (player == null)
            {
                return;
            }

            if (PhotonNetwork.OfflineMode)
            {
                return;
            }

            if (playerNumber < 0)
            {
                Debug.LogWarning("Setting invalid playerNumber: " + playerNumber + " for: " + player.ToStringFull());
            }

            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning("SetPlayerNumber was called in state: " + PhotonNetwork.NetworkClientState + ". Not IsConnectedAndReady.");
                return;
            }

            int current = player.GetPlayerNumber();
            if (current != playerNumber)
            {
                Debug.Log("PlayerNumbering: Set number " + playerNumber);
                player.SetCustomProperties(new Hashtable() { { PlayerNumbering.RoomPlayerIndexedProp, (byte)playerNumber } });
            }
        }
    }
}