// ----------------------------------------------------------------------------
// <copyright file="PhotonHandler.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// PhotonHandler是一个运行时MonoBehaviour，用于将PUN集成到主循环中。
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Pun
{
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Profiling;


    /// <summary>
    /// 内部MonoBehaviour，允许Photon运行Update循环。
    /// </summary>
    public class PhotonHandler : ConnectionHandler, IInRoomCallbacks, IMatchmakingCallbacks
    {

        private static PhotonHandler instance;
        internal static PhotonHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PhotonHandler>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = "PhotonMono";
                        instance = obj.AddComponent<PhotonHandler>();
                    }
                }

                return instance;
            }
        }


        /// <summary>限制每个LateUpdate中创建的数据报数量。</summary>
        /// <remarks>有助于最小限度地分散消息的发送。</remarks>
        public static int MaxDatagrams = 3;

        /// <summary>表示应在下一个LateUpdate调用中发送传出消息。</summary>
        /// <remarks>最多创建MaxDatagrams个数据报来发送排队的消息。</remarks>
        public static bool SendAsap;

        /// <summary>对"下次序列化状态的时间"值进行一定毫秒数的修正。</summary>
        /// <remarks>由于LateUpdate通常每15ms调用一次，提前比延后更有利于达到SerializeRate。</remarks>
        private const int SerializeRateFrameCorrection = 8;

        protected internal int UpdateInterval; // 连续SendOutgoingCommands调用之间的时间[毫秒]

        protected internal int UpdateIntervalOnSerialize; // 连续RunViewUpdate调用之间的时间[毫秒]（发送同步数据等）

        private int nextSendTickCount;

        private int nextSendTickCountOnSerialize;

        private SupportLogger supportLoggerComponent;


        protected override void Awake()
        {
            if (instance == null || ReferenceEquals(this, instance))
            {
                instance = this;
                base.Awake();
            }
            else
            {
                Destroy(this);
            }
        }

        protected virtual void OnEnable()
        {
            if (Instance != this)
            {
                Debug.LogError("PhotonHandler is a singleton but there are multiple instances. this != Instance.");
                return;
            }

            this.Client = PhotonNetwork.NetworkingClient;

            if (PhotonNetwork.PhotonServerSettings.EnableSupportLogger)
            {
                SupportLogger supportLogger = this.gameObject.GetComponent<SupportLogger>();
                if (supportLogger == null)
                {
                    supportLogger = this.gameObject.AddComponent<SupportLogger>();
                }
                if (this.supportLoggerComponent != null)
                {
                    if (supportLogger.GetInstanceID() != this.supportLoggerComponent.GetInstanceID())
                    {
                        Debug.LogWarningFormat("Cached SupportLogger component is different from the one attached to PhotonMono GameObject");
                    }
                }
                this.supportLoggerComponent = supportLogger;
                this.supportLoggerComponent.Client = PhotonNetwork.NetworkingClient;
            }

            this.UpdateInterval = 1000 / PhotonNetwork.SendRate;
            this.UpdateIntervalOnSerialize = 1000 / PhotonNetwork.SerializationRate;

            PhotonNetwork.AddCallbackTarget(this);
            this.StartFallbackSendAckThread();  // 这在基类中没有完成
        }

        protected void Start()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, loadingMode) =>
            {
                PhotonNetwork.NewSceneLoaded();
            };
        }

        protected override void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            base.OnDisable();
        }


        /// <summary>由UnityEngine按间隔调用。受Time.timeScale影响。</summary>
        protected void FixedUpdate()
        {
#if PUN_DISPATCH_IN_FIXEDUPDATE
            this.Dispatch();
#elif PUN_DISPATCH_IN_LATEUPDATE
            // 在此处不调度
#else
            if (Time.timeScale > PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate)
            {
                this.Dispatch();
            }
#endif
        }

        /// <summary>由UnityEngine在运行正常游戏代码和物理之后按间隔调用。</summary>
        protected void LateUpdate()
        {
#if PUN_DISPATCH_IN_LATEUPDATE
            this.Dispatch();
#elif PUN_DISPATCH_IN_FIXEDUPDATE
            // 在此处不调度
#else
            // 参见MinimalTimeScaleToDispatchInFixedUpdate和FixedUpdate的说明：
            if (Time.timeScale <= PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate)
            {
                this.Dispatch();
            }
#endif

            int currentMsSinceStart = (int)(Time.realtimeSinceStartup * 1000); // 避免使用Environment.TickCount，它在长时间运行的平台上可能为负数
            if (PhotonNetwork.IsMessageQueueRunning && currentMsSinceStart > this.nextSendTickCountOnSerialize)
            {
                PhotonNetwork.RunViewUpdate();
                this.nextSendTickCountOnSerialize = currentMsSinceStart + this.UpdateIntervalOnSerialize - SerializeRateFrameCorrection;
                this.nextSendTickCount = 0; // 当同步代码运行时立即发送
            }

            currentMsSinceStart = (int)(Time.realtimeSinceStartup * 1000);
            if (SendAsap || currentMsSinceStart > this.nextSendTickCount)
            {
                SendAsap = false;
                bool doSend = true;
                int sendCounter = 0;
                while (PhotonNetwork.IsMessageQueueRunning && doSend && sendCounter < MaxDatagrams)
                {
                    // 发送所有传出命令
                    Profiler.BeginSample("SendOutgoingCommands");
                    doSend = PhotonNetwork.NetworkingClient.LoadBalancingPeer.SendOutgoingCommands();
                    sendCounter++;
                    Profiler.EndSample();
                }

                this.nextSendTickCount = currentMsSinceStart + this.UpdateInterval;
            }
        }

        /// <summary>为PUN调度传入的网络消息。在FixedUpdate或LateUpdate中调用。</summary>
        /// <remarks>
        /// 即使timeScale接近0，调度传入消息也可能是有意义的。
        /// 可以通过PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate进行配置。
        ///
        /// 如果不调度消息，PUN就不会改变状态，也不会处理更新。
        /// </remarks>
        protected void Dispatch()
        {
            if (PhotonNetwork.NetworkingClient == null)
            {
                Debug.LogError("NetworkPeer broke!");
                return;
            }

            //if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated || PhotonNetwork.NetworkClientState == ClientState.Disconnected || PhotonNetwork.OfflineMode)
            //{
            //    return;
            //}


            bool doDispatch = true;
            Exception ex = null;
            int exceptionCount = 0;
            while (PhotonNetwork.IsMessageQueueRunning && doDispatch)
            {
                // DispatchIncomingCommands()如果调度了任何命令（事件、响应或状态更改）则返回true
                Profiler.BeginSample("DispatchIncomingCommands");
                try
                {
                    doDispatch = PhotonNetwork.NetworkingClient.LoadBalancingPeer.DispatchIncomingCommands();
                }
                catch (Exception e)
                {
                    exceptionCount++;
                    if (ex == null)
                    {
                        ex = e;
                    }
                }

                Profiler.EndSample();
            }

            if (ex != null)
            {
                throw new AggregateException("Caught " + exceptionCount + " exception(s) in methods called by DispatchIncomingCommands(). Rethrowing first only (see above).", ex);
            }
        }


        public void OnCreatedRoom()
        {
            PhotonNetwork.SetLevelInPropsIfSynced(SceneManagerHelper.ActiveSceneName);
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            PhotonNetwork.LoadLevelIfSynced();
        }


        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            var views = PhotonNetwork.PhotonViewCollection;
            foreach (var view in views)
            {
                if (view.IsRoomView)
                {
                    view.OwnerActorNr = newMasterClient.ActorNumber;
                    view.ControllerActorNr = newMasterClient.ActorNumber;
                }
            }
        }

        public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> friendList) { }

        public void OnCreateRoomFailed(short returnCode, string message) { }

        public void OnJoinRoomFailed(short returnCode, string message) { }

        public void OnJoinRandomFailed(short returnCode, string message) { }

        protected List<int> reusableIntList = new List<int>();

        public void OnJoinedRoom()
        {

            if (PhotonNetwork.ViewCount == 0)
                return;

            var views = PhotonNetwork.PhotonViewCollection;

            bool amMasterClient = PhotonNetwork.IsMasterClient;
            bool amRejoiningMaster = amMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1;

            if (amRejoiningMaster)
                reusableIntList.Clear();

            // 如果这是Master重新加入，重新声明非创建者所有者的所有权
            foreach (var view in views)
            {
                int viewOwnerId = view.OwnerActorNr;
                int viewCreatorId = view.CreatorActorNr;

                // 在加入/重新加入时，将控制权分配给Master Client（对于房间对象）或所有者（对于其他对象）
                view.RebuildControllerCache();

                // 重新加入的Master应强制执行其世界视图，并覆盖在软断开期间发生的任何更改
                if (amRejoiningMaster)
                    if (viewOwnerId != viewCreatorId)
                    {
                        reusableIntList.Add(view.ViewID);
                        reusableIntList.Add(viewOwnerId);
                    }
            }

            if (amRejoiningMaster && reusableIntList.Count > 0)
            {
                PhotonNetwork.OwnershipUpdate(reusableIntList.ToArray());
            }
        }

        public void OnLeftRoom()
        {
            // 销毁生成的对象并重置场景对象
            PhotonNetwork.LocalCleanupAnythingInstantiated(true);
        }


        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            // 注意：如果master client变为非活动状态，其他人会成为master。所以不存在活动master client重新连接的情况。
            // 可能发生的情况是Master Client在任何人（包括服务器）注意到之前在本地断开连接并使用ReconnectAndRejoin。

            bool amMasterClient = PhotonNetwork.IsMasterClient;

            var views = PhotonNetwork.PhotonViewCollection;
            if (amMasterClient)
            {
                reusableIntList.Clear();
            }

            foreach (var view in views)
            {
                view.RebuildControllerCache();  // 如果有人重新加入，所有客户端都可能需要清理所有者和控制器

                // master client通知加入的玩家任何非创建者所有权
                if (amMasterClient)
                {
                    int viewOwnerId = view.OwnerActorNr;
                    if (viewOwnerId != view.CreatorActorNr)
                    {
                        reusableIntList.Add(view.ViewID);
                        reusableIntList.Add(viewOwnerId);
                    }
                }
            }

            // 更新加入的玩家有关房间中非创建者所有权的信息
            if (amMasterClient && reusableIntList.Count > 0)
            {
                PhotonNetwork.OwnershipUpdate(reusableIntList.ToArray(), newPlayer.ActorNumber);
            }

        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            var views = PhotonNetwork.PhotonViewCollection;

            int leavingPlayerId = otherPlayer.ActorNumber;
            bool isInactive = otherPlayer.IsInactive;

            // 软断开：玩家已超时断开与中继的连接，但尚未超过PlayerTTL，可能重新连接。
            // Master将接管这些对象的控制权，直到玩家硬断开或返回。
            if (isInactive)
            {
                foreach (var view in views)
                {
                    // v2.27: 从所有者检查更改为控制器检查
                    if (view.ControllerActorNr == leavingPlayerId)
                        view.ControllerActorNr = PhotonNetwork.MasterClient.ActorNumber;
                }

            }
            // 硬断开：玩家被永久移除。移除该玩家作为其创建的所有项目的所有者（除非AutoCleanUp为false）
            else
            {
                bool autocleanup = PhotonNetwork.CurrentRoom.AutoCleanUp;

                foreach (var view in views)
                {
                    // 跳过更改将被清理的项目的主机/控制器。
                    if (autocleanup && view.CreatorActorNr == leavingPlayerId)
                        continue;

                    // 属于离开玩家的任何视图，默认为null所有者（将变为master控制）。
                    if (view.OwnerActorNr == leavingPlayerId || view.ControllerActorNr == leavingPlayerId)
                    {
                        view.OwnerActorNr = 0;
                        view.ControllerActorNr = PhotonNetwork.MasterClient.ActorNumber;
                    }
                }
            }
        }
    }
}