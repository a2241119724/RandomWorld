// ----------------------------------------------------------------------------
// <copyright file="ConnectionHandler.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//   If the game logic does not call Service() for whatever reason, this keeps the connection.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------


#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif


namespace Photon.Realtime
{
    using System;
    using System.Diagnostics;
    using SupportClass = ExitGames.Client.Photon.SupportClass;

#if SUPPORTED_UNITY
    using UnityEngine;
#endif


#if SUPPORTED_UNITY
    public class ConnectionHandler : MonoBehaviour
#else
    public class ConnectionHandler
#endif
    {
        /// <summary>
        /// 用于记录信息和统计数据的 Photon 客户端。
        /// </summary>
        public LoadBalancingClient Client { get; set; }

        /// <summary>选项，让回退线程在 KeepAliveInBackground 时间后调用 Disconnect。默认值：false。</summary>
        /// <remarks>
        /// 如果设置为 true，当客户端未调用 SendOutgoingCommands / Service 时，线程将定期断开客户端连接。
        /// 这可能由于应用处于后台（且未获得大量 CPU 时间）或加载资源时发生。
        ///
        /// 如果为 false，则必须经过常规的超时时间才能最终使客户端超时。
        /// </remarks>
        public bool DisconnectAfterKeepAlive = false;

        /// <summary>定义回退线程应保持连接多长时间，之后可能像往常一样超时。</summary>
        /// <remarks>我们希望客户端在应用处于后台时保持连接（且不调用 Update / Service）。客户端不应在后台无限期保持连接，因此在经过一定毫秒数后，回退线程应停止保持连接。</remarks>
        public int KeepAliveInBackground = 60000;

        /// <summary>统计回退线程调用 SendAcksOnly 的次数，这纯粹用于监控游戏逻辑是否按预期调用了 SendOutgoingCommands。</summary>
        public int CountSendAcksOnly { get; private set; }

        /// <summary>如果回退线程正在运行，则为 True。将调用客户端的 SendAcksOnly() 方法来保持连接。</summary>
        public bool FallbackThreadRunning
        {
            get { return this.fallbackThreadId < 255; }
        }

        /// <summary>即使加载了新场景，也保持 ConnectionHandler。</summary>
        public bool ApplyDontDestroyOnLoad = true;

        /// <summary>表示应用正在关闭。在 OnApplicationQuit() 中设置。</summary>
        [NonSerialized]
        public static bool AppQuits;


        private byte fallbackThreadId = 255;
        private bool didSendAcks;
        private readonly Stopwatch backgroundStopwatch = new Stopwatch();


#if SUPPORTED_UNITY

#if UNITY_2019_4_OR_NEWER

        /// <summary>
        /// 为域重载重置静态变量
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticReset()
        {
            AppQuits = false;
        }

#endif


        /// <summary>Unity 在应用程序关闭时调用。UnityEngine 还将调用 OnDisable，它会断开连接。</summary>
        protected void OnApplicationQuit()
        {
            AppQuits = true;
        }


        /// <summary></summary>
        protected virtual void Awake()
        {
            if (this.ApplyDontDestroyOnLoad)
            {
                DontDestroyOnLoad(this.gameObject);
            }
        }

        /// <summary>Unity 在应用程序关闭时调用。如果之前调用了 OnApplicationQuit()，则断开连接。</summary>
        protected virtual void OnDisable()
        {
            this.StopFallbackSendAckThread();

            if (AppQuits)
            {
                if (this.Client != null && this.Client.IsConnected)
                {
                    this.Client.Disconnect();
                    this.Client.LoadBalancingPeer.StopThread();
                }

                SupportClass.StopAllBackgroundCalls();
            }
        }

#endif


        public void StartFallbackSendAckThread()
        {
#if !UNITY_WEBGL
            if (this.FallbackThreadRunning)
            {
                return;
            }

#if UNITY_SWITCH
            this.fallbackThreadId = SupportClass.StartBackgroundCalls(this.RealtimeFallbackThread, 50);  // as workaround, we don't name the Thread.
#else
            this.fallbackThreadId = SupportClass.StartBackgroundCalls(this.RealtimeFallbackThread, 50, "RealtimeFallbackThread");
#endif
#endif
        }

        public void StopFallbackSendAckThread()
        {
#if !UNITY_WEBGL
            if (!this.FallbackThreadRunning)
            {
                return;
            }

            SupportClass.StopBackgroundCalls(this.fallbackThreadId);
            this.fallbackThreadId = 255;
#endif
        }


        /// <summary>一个独立于 Update() 调用运行的线程。在加载或在后台时保持连接在线。参见 <see cref="KeepAliveInBackground"/>。</summary>
        public bool RealtimeFallbackThread()
        {
            if (this.Client != null)
            {
                if (!this.Client.IsConnected)
                {
                    this.didSendAcks = false;
                    return true;
                }

                if (this.Client.LoadBalancingPeer.ConnectionTime - this.Client.LoadBalancingPeer.LastSendOutgoingTime > 100)
                {
                    if (!this.didSendAcks)
                    {
                        backgroundStopwatch.Reset();
                        backgroundStopwatch.Start();
                    }

                    // 检查客户端是否应在后台若干秒后断开连接
                    if (backgroundStopwatch.ElapsedMilliseconds > this.KeepAliveInBackground)
                    {
                        if (this.DisconnectAfterKeepAlive)
                        {
                            this.Client.Disconnect();
                        }
                        return true;
                    }


                    this.didSendAcks = true;
                    this.CountSendAcksOnly++;
                    this.Client.LoadBalancingPeer.SendAcksOnly();
                }
                else
                {
                    this.didSendAcks = false;
                }
            }

            return true;
        }
    }
}