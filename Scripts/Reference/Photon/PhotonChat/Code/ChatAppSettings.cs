// -----------------------------------------------------------------------
// <copyright file="ChatAppSettings.cs" company="Exit Games GmbH">
//   Chat API for Photon - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>Photon Chat 应用程序设置以及要连接到的服务器。</summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif


namespace Photon.Chat
{
    using System;
    using ExitGames.Client.Photon;
#if SUPPORTED_UNITY
    using UnityEngine.Serialization;
#endif

    /// <summary>
    /// Photon 应用程序和要连接到的服务器的设置。
    /// </summary>
    /// <remarks>
    /// 这是可序列化的，用于 Unity，因此可以包含在 ScriptableObject 实例中。
    /// </remarks>
#if !NETFX_CORE || SUPPORTED_UNITY
    [Serializable]
#endif
    public class ChatAppSettings
    {
        /// <summary>Chat Api 的 AppId。</summary>
#if SUPPORTED_UNITY
        [FormerlySerializedAs("AppId")]
#endif
        public string AppIdChat;

        /// <summary>AppVersion 可用于标识构建，并将 AppId 分隔为不同的"虚拟 AppId"（对于用户互相找到彼此很重要）。</summary>
        public string AppVersion;

        /// <summary>可以设置为 Photon Cloud 的任何区域名称，以直接连接到该区域。</summary>
        public string FixedRegion;

        /// <summary>要连接到的服务器地址（主机名或 IP）。</summary>
        public string Server;

        /// <summary>如果不为 null，则设置要连接到的第一个 Photon 服务器的端口（该服务器将根据需要"转发"客户端）。</summary>
        public ushort Port;

        /// <summary>要使用的网络层协议。</summary>
        public ConnectionProtocol Protocol = ConnectionProtocol.Udp;

        /// <summary>在网络库连接到名称服务器失败时，启用回退到另一个协议。</summary>
        /// <remarks>参见：LoadBalancingClient.EnableProtocolFallback。</remarks>
        public bool EnableProtocolFallback = true;

        /// <summary>网络库的日志级别。</summary>
        public DebugLevel NetworkLogging = DebugLevel.ERROR;

        /// <summary>如果为 true，则应使用 Photon Cloud 的默认名称服务器地址。</summary>
        public bool IsDefaultNameServer { get { return string.IsNullOrEmpty(this.Server); } }


        /// <summary>可用于不立即破坏兼容性。</summary>
        [Obsolete("Use AppIdChat instead.")]
        public string AppId
        {
            get { return this.AppIdChat; }
            set { this.AppIdChat = value; }
        }
    }
}