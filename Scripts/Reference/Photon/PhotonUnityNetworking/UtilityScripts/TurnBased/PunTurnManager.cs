// ----------------------------------------------------------------------------
// <copyright file="PunTurnManager.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//  基于回合的游戏管理器，使用PUN
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// Pun基于回合的游戏管理器。
    /// 为玩家之间的典型回合流程和逻辑提供接口(IPunTurnManagerCallbacks)
    /// 为Player、Room和RoomInfo提供扩展，以实现回合制所需的专用API
    /// </summary>
	public class PunTurnManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {

        /// <summary>
        /// 外部定义，用于更好的垃圾回收管理，在ProcessEvent中使用。
        /// </summary>
        Player sender;

        /// <summary>
        /// 封装对房间"回合"自定义属性的访问。
        /// </summary>
        /// <value>回合索引</value>
        public int Turn
        {
            get { return PhotonNetwork.CurrentRoom.GetTurn(); }
            private set
            {

                _isOverCallProcessed = false;

                PhotonNetwork.CurrentRoom.SetTurn(value, true);
            }
        }


        /// <summary>
        /// 回合的持续时间（秒）。
        /// </summary>
        public float TurnDuration = 20f;

        /// <summary>
        /// 获取当前回合已过去的时间（秒）
        /// </summary>
        /// <value>回合中的已过时间。</value>
        public float ElapsedTimeInTurn
        {
            get { return ((float)(PhotonNetwork.ServerTimestamp - PhotonNetwork.CurrentRoom.GetTurnStart())) / 1000.0f; }
        }


        /// <summary>
        /// 获取当前回合的剩余秒数。范围从0到TurnDuration
        /// </summary>
        /// <value>当前回合的剩余秒数</value>
        public float RemainingSecondsInTurn
        {
            get { return Mathf.Max(0f, this.TurnDuration - this.ElapsedTimeInTurn); }
        }


        /// <summary>
        /// 获取一个值，指示回合是否所有人已完成。
        /// </summary>
        /// <value><c>true</c> 如果此回合所有人已完成；否则，<c>false</c>。</value>
        public bool IsCompletedByAll
        {
            get { return PhotonNetwork.CurrentRoom != null && Turn > 0 && this.finishedPlayers.Count == PhotonNetwork.CurrentRoom.PlayerCount; }
        }

        /// <summary>
        /// 获取一个值，指示当前回合是否已被我完成。
        /// </summary>
        /// <value><c>true</c> 如果当前回合已被我完成；否则，<c>false</c>。</value>
        public bool IsFinishedByMe
        {
            get { return this.finishedPlayers.Contains(PhotonNetwork.LocalPlayer); }
        }

        /// <summary>
        /// 获取一个值，指示当前回合是否已结束。即ElapsedTimeinTurn大于或等于TurnDuration
        /// </summary>
        /// <value><c>true</c> 如果当前回合已结束；否则，<c>false</c>。</value>
        public bool IsOver
        {
            get { return this.RemainingSecondsInTurn <= 0f; }
        }

        /// <summary>
        /// 回合管理器监听器。将此设置为你自己的脚本实例以捕获回调
        /// </summary>
        public IPunTurnManagerCallbacks TurnManagerListener;


        /// <summary>
        /// 已完成的玩家集合。
        /// </summary>
        private readonly HashSet<Player> finishedPlayers = new HashSet<Player>();

        /// <summary>
        /// 回合管理器事件偏移事件消息字节。内部用于在房间自定义属性中定义数据
        /// </summary>
        public const byte TurnManagerEventOffset = 0;

        /// <summary>
        /// 移动事件消息字节。内部用于在房间自定义属性中保存数据
        /// </summary>
        public const byte EvMove = 1 + TurnManagerEventOffset;

        /// <summary>
        /// 最终移动事件消息字节。内部用于在房间自定义属性中保存数据
        /// </summary>
        public const byte EvFinalMove = 2 + TurnManagerEventOffset;

        // 跟踪消息调用
        private bool _isOverCallProcessed = false;

        #region MonoBehaviour CallBack


        void Start() { }

        void Update()
        {
            if (Turn > 0 && this.IsOver && !_isOverCallProcessed)
            {
                _isOverCallProcessed = true;
                this.TurnManagerListener.OnTurnTimeEnds(this.Turn);
            }

        }

        #endregion


        /// <summary>
        /// 告知TurnManager开始新回合。
        /// </summary>
        public void BeginTurn()
        {
            Turn = this.Turn + 1; // 注意：这将设置房间中的一个属性，其他玩家也可以访问到。
        }


        /// <summary>
        /// 调用以发送一个动作。也可以选择一并完成回合。
        /// move对象可以是任何东西。但请尝试优化，只发送定义回合动作所需的最少信息集。
        /// </summary>
        /// <param name="move"></param>
        /// <param name="finished"></param>
        public void SendMove(object move, bool finished)
        {
            if (IsFinishedByMe)
            {
                UnityEngine.Debug.LogWarning("Can't SendMove. Turn is finished by this player.");
                return;
            }

            // 除了实际的动作，我们还必须发送此动作属于哪个回合
            Hashtable moveHt = new Hashtable();
            moveHt.Add("turn", Turn);
            moveHt.Add("move", move);

            byte evCode = (finished) ? EvFinalMove : EvMove;
            PhotonNetwork.RaiseEvent(evCode, moveHt, new RaiseEventOptions() { CachingOption = EventCaching.AddToRoomCache }, SendOptions.SendReliable);
            if (finished)
            {
                PhotonNetwork.LocalPlayer.SetFinishedTurn(Turn);
            }

            // 服务器默认不会将事件发送回发起者。要获取事件，在本地调用它
            // （注意：由于我们在本地执行此操作，事件的顺序可能会被打乱）
            ProcessOnEvent(evCode, moveHt, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        /// <summary>
        /// 获取玩家是否完成了当前回合。
        /// </summary>
        /// <returns><c>true</c>，如果玩家完成了当前回合，否则<c>false</c>。</returns>
        /// <param name="player">要检查的玩家</param>
        public bool GetPlayerFinishedTurn(Player player)
        {
            if (player != null && this.finishedPlayers != null && this.finishedPlayers.Contains(player))
            {
                return true;
            }

            return false;
        }

        #region Callbacks

        // 内部调用
        void ProcessOnEvent(byte eventCode, object content, int senderId)
        {
            if (senderId == -1)
            {
                return;
            }

            sender = PhotonNetwork.CurrentRoom.GetPlayer(senderId);

            switch (eventCode)
            {
                case EvMove:
                    {
                        Hashtable evTable = content as Hashtable;
                        int turn = (int)evTable["turn"];
                        object move = evTable["move"];
                        this.TurnManagerListener.OnPlayerMove(sender, turn, move);

                        break;
                    }
                case EvFinalMove:
                    {
                        Hashtable evTable = content as Hashtable;
                        int turn = (int)evTable["turn"];
                        object move = evTable["move"];

                        if (turn == this.Turn)
                        {
                            this.finishedPlayers.Add(sender);

                            this.TurnManagerListener.OnPlayerFinished(sender, turn, move);

                        }

                        if (IsCompletedByAll)
                        {
                            this.TurnManagerListener.OnTurnCompleted(this.Turn);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// 由PhotonNetwork.OnEventCall注册调用
        /// </summary>
			/// <param name="photonEvent">Photon事件。</param>
			public void OnEvent(EventData photonEvent)
        {
            this.ProcessOnEvent(photonEvent.Code, photonEvent.CustomData, photonEvent.Sender);
        }

        /// <summary>
        /// 由PhotonNetwork调用
        /// </summary>
        /// <param name="propertiesThatChanged">已更改的属性。</param>
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {

            //   Debug.Log("OnRoomPropertiesUpdate: "+propertiesThatChanged.ToStringFull());

            if (propertiesThatChanged.ContainsKey("Turn"))
            {
                _isOverCallProcessed = false;
                this.finishedPlayers.Clear();
                this.TurnManagerListener.OnTurnBegins(this.Turn);
            }
        }

        #endregion
    }


    public interface IPunTurnManagerCallbacks
    {
        /// <summary>
        /// 回合开始事件时调用。
        /// </summary>
        /// <param name="turn">回合索引</param>
        void OnTurnBegins(int turn);

        /// <summary>
        /// 回合完成时调用（所有玩家完成）
        /// </summary>
        /// <param name="turn">回合索引</param>
        void OnTurnCompleted(int turn);

        /// <summary>
        /// 玩家移动时调用（但未完成回合）
        /// </summary>
        /// <param name="player">玩家引用</param>
        /// <param name="turn">回合索引</param>
        /// <param name="move">移动对象数据</param>
        void OnPlayerMove(Player player, int turn, object move);

        /// <summary>
        /// 当玩家完成回合时调用（包含该玩家的动作/移动）
        /// </summary>
        /// <param name="player">玩家引用</param>
        /// <param name="turn">回合索引</param>
        /// <param name="move">移动对象数据</param>
        void OnPlayerFinished(Player player, int turn, object move);


        /// <summary>
        /// 当回合因时间限制而完成时调用（回合超时）
        /// </summary>
        /// <param name="turn">回合索引</param>
        void OnTurnTimeEnds(int turn);
    }


    public static class TurnExtensions
    {
        /// <summary>
        /// 当前正在进行的回合编号
        /// </summary>
        public static readonly string TurnPropKey = "Turn";

        /// <summary>
        /// 当前正在进行的回合的开始（服务器）时间（用于计算结束）
        /// </summary>
        public static readonly string TurnStartPropKey = "TStart";

        /// <summary>
        /// Actor的已完成回合（后跟数字）
        /// </summary>
        public static readonly string FinishedTurnPropKey = "FToA";

        /// <summary>
        /// 设置回合。
        /// </summary>
        /// <param name="room">房间引用</param>
        /// <param name="turn">回合索引</param>
        /// <param name="setStartTime">如果设置为<c>true</c>则设置开始时间。</param>
        public static void SetTurn(this Room room, int turn, bool setStartTime = false)
        {
            if (room == null || room.CustomProperties == null)
            {
                return;
            }

            Hashtable turnProps = new Hashtable();
            turnProps[TurnPropKey] = turn;
            if (setStartTime)
            {
                turnProps[TurnStartPropKey] = PhotonNetwork.ServerTimestamp;
            }

            room.SetCustomProperties(turnProps);
        }

        /// <summary>
        /// 从RoomInfo获取当前回合
        /// </summary>
        /// <returns>回合索引</returns>
        /// <param name="room">RoomInfo引用</param>
        public static int GetTurn(this RoomInfo room)
        {
            if (room == null || room.CustomProperties == null || !room.CustomProperties.ContainsKey(TurnPropKey))
            {
                return 0;
            }

            return (int)room.CustomProperties[TurnPropKey];
        }


        /// <summary>
        /// 返回回合开始的时间。这可用于计算已进行了多长时间。
        /// </summary>
        /// <returns>回合开始时间。</returns>
        /// <param name="room">房间。</param>
        public static int GetTurnStart(this RoomInfo room)
        {
            if (room == null || room.CustomProperties == null || !room.CustomProperties.ContainsKey(TurnStartPropKey))
            {
                return 0;
            }

            return (int)room.CustomProperties[TurnStartPropKey];
        }

        /// <summary>
        /// 获取玩家的已完成回合（从房间属性中）
        /// </summary>
        /// <returns>已完成的回合索引</returns>
        /// <param name="player">玩家引用</param>
        public static int GetFinishedTurn(this Player player)
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null || room.CustomProperties == null || !room.CustomProperties.ContainsKey(TurnPropKey))
            {
                return 0;
            }

            string propKey = FinishedTurnPropKey + player.ActorNumber;
            return (int)room.CustomProperties[propKey];
        }

        /// <summary>
        /// 设置玩家的已完成回合（在房间属性中）
        /// </summary>
        /// <param name="player">玩家引用</param>
        /// <param name="turn">回合索引</param>
        public static void SetFinishedTurn(this Player player, int turn)
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null || room.CustomProperties == null)
            {
                return;
            }

            string propKey = FinishedTurnPropKey + player.ActorNumber;
            Hashtable finishedTurnProp = new Hashtable();
            finishedTurnProp[propKey] = turn;

            room.SetCustomProperties(finishedTurnProp);
        }
    }
}