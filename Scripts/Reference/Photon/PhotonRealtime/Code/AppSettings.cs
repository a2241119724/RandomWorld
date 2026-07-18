// -----------------------------------------------------------------------
// <copyright file="AppSettings.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>Photon 应用程序和要连接到的服务器的设置。</summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

#if UNITY_2017_4_OR_NEWER
#define SUPPORTED_UNITY
#endif

namespace Photon.Realtime
{
    using ExitGames.Client.Photon;
    using System;

#if SUPPORTED_UNITY || NETFX_CORE
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
    public class AppSettings
    {
        /// <summary>Realtime 或 PUN 的 AppId。</summary>
        public string AppIdRealtime;

        /// <summary>Photon Fusion 的 AppId。</summary>
        public string AppIdFusion;

        /// <summary>Photon Chat 的 AppId。</summary>
        public string AppIdChat;

        /// <summary>Photon Voice 的 AppId。</summary>
        public string AppIdVoice;

        /// <summary>AppVersion 可用于标识构建版本，并将 AppId 分隔为不同的"虚拟 AppId"（对于匹配很重要）。</summary>
        public string AppVersion;


        /// <summary>如果为 false，应用将尝试连接到主服务器（已过时但有时仍然需要）。</summary>
        /// <remarks>如果为 true，Server 指向 NameServer（或为 null，使用默认值），否则它指向 MasterServer。</remarks>
        public bool UseNameServer = true;

        /// <summary>可以设置为 Photon Cloud 的任何区域名称，以直接连接到该区域。</summary>
        /// <remarks>如果 IsNullOrEmpty() 且 UseNameServer == true，则使用 BestRegion。否则，使用服务器</remarks>
        public string FixedRegion;

        /// <summary>在连接之前设置先前的 BestRegionSummary 值。</summary>
        /// <remarks>
        /// 这是客户端连接到"最佳区域"时使用的值。</br>
        /// 如果此值为 null 或空，则会对所有区域进行 ping 操作。在连接时提供先前的摘要，
        /// 可以加速最佳区域选择，并使先前选择的区域具有"粘性"。</br>
        ///
        /// Unity 客户端应将 BestRegionSummary 存储在 PlayerPrefs 中。
        /// 您可以通过实现 <see cref="IConnectionCallbacks.OnConnectedToMaster"/> 来存储新结果。
        /// 如果 <see cref="LoadBalancingClient.SummaryToCache"/> 不为 null，则存储此字符串。
        /// 为避免多次存储该值，您可以将 SummaryToCache 设置为 null。
        /// </remarks>
#if SUPPORTED_UNITY
        [NonSerialized]
#endif
        public string BestRegionSummaryFromStorage;

        /// <summary>要连接到的服务器地址（主机名或 IP）。</summary>
        public string Server;

        /// <summary>如果不为 null，则设置要连接到的第一个 Photon 服务器的端口（该服务器将根据需要"转发"客户端）。</summary>
        public int Port;

        /// <summary>代理服务器的地址（主机名或 IP 和端口）。</summary>
        public string ProxyServer;

        /// <summary>要使用的网络层协议。</summary>
        public ConnectionProtocol Protocol = ConnectionProtocol.Udp;

        /// <summary>在连接到名称服务器失败时，启用回退到另一个协议。</summary>
        /// <remarks>参见：LoadBalancingClient.EnableProtocolFallback。</remarks>
        public bool EnableProtocolFallback = true;

        /// <summary>定义如何进行身份验证。在每个系统上，通过一次或通过 WSS 安全连接进行。</summary>
        public AuthModeOption AuthMode = AuthModeOption.Auth;

        /// <summary>如果为 true，客户端将请求当前可用的大厅列表。</summary>
        public bool EnableLobbyStatistics;

        /// <summary>网络库的日志级别。</summary>
        public DebugLevel NetworkLogging = DebugLevel.ERROR;

        /// <summary>如果为 true，Server 字段包含 Master Server 地址（如果有地址的话）。</summary>
        public bool IsMasterServerAddress
        {
            get { return !this.UseNameServer; }
        }

        /// <summary>如果为 true，客户端应从名称服务器获取区域列表并找到延迟最低的区域。</summary>
        /// <remarks>参见在线文档中的"Best Region"。</remarks>
        public bool IsBestRegion
        {
            get { return this.UseNameServer && string.IsNullOrEmpty(this.FixedRegion); }
        }

        /// <summary>如果为 true，则应使用 Photon Cloud 的默认名称服务器地址。</summary>
        public bool IsDefaultNameServer
        {
            get { return this.UseNameServer && string.IsNullOrEmpty(this.Server); }
        }

        /// <summary>如果为 true，将使用协议的默认端口。</summary>
        public bool IsDefaultPort
        {
            get { return this.Port <= 0; }
        }

        /// <summary>ToString 但包含更多详细信息。</summary>
        public string ToStringFull()
        {
            return string.Format(
                                 "appId {0}{1}{2}{3}" +
                                 "use ns: {4}, reg: {5}, {9}, " +
                                 "{6}{7}{8}" +
                                 "auth: {10}",
                                 String.IsNullOrEmpty(this.AppIdRealtime) ? string.Empty : "Realtime/PUN: " + this.HideAppId(this.AppIdRealtime) + ", ",
                                 String.IsNullOrEmpty(this.AppIdFusion) ? string.Empty : "Fusion: " + this.HideAppId(this.AppIdFusion) + ", ",
                                 String.IsNullOrEmpty(this.AppIdChat) ? string.Empty : "Chat: " + this.HideAppId(this.AppIdChat) + ", ",
                                 String.IsNullOrEmpty(this.AppIdVoice) ? string.Empty : "Voice: " + this.HideAppId(this.AppIdVoice) + ", ",
                                 String.IsNullOrEmpty(this.AppVersion) ? string.Empty : "AppVersion: " + this.AppVersion + ", ",
                                 "UseNameServer: " + this.UseNameServer + ", ",
                                 "Fixed Region: " + this.FixedRegion + ", ",
                                 //this.BestRegionSummaryFromStorage,
                                 String.IsNullOrEmpty(this.Server) ? string.Empty : "Server: " + this.Server + ", ",
                                 this.IsDefaultPort ? string.Empty : "Port: " + this.Port + ", ",
                                 String.IsNullOrEmpty(ProxyServer) ? string.Empty : "Proxy: " + this.ProxyServer + ", ",
                                 this.Protocol,
                                 this.AuthMode
                                //this.EnableLobbyStatistics,
                                //this.NetworkLogging,
                                );
        }


        /// <summary>通过尝试创建 Guid 来检查字符串是否为 Guid。</summary>
        /// <param name="val">要检查的潜在 guid。</param>
        /// <returns>如果 new Guid(val) 没有失败，则返回 True。</returns>
        public static bool IsAppId(string val)
        {
            try
            {
                new Guid(val);
            }
            catch
            {
                return false;
            }

            return true;
        }


        private string HideAppId(string appId)
        {
            return string.IsNullOrEmpty(appId) || appId.Length < 8
                       ? appId
                       : string.Concat(appId.Substring(0, 8), "***");
        }

        public AppSettings CopyTo(AppSettings d)
        {
            d.AppIdRealtime = this.AppIdRealtime;
            d.AppIdFusion = this.AppIdFusion;
            d.AppIdChat = this.AppIdChat;
            d.AppIdVoice = this.AppIdVoice;
            d.AppVersion = this.AppVersion;
            d.UseNameServer = this.UseNameServer;
            d.FixedRegion = this.FixedRegion;
            d.BestRegionSummaryFromStorage = this.BestRegionSummaryFromStorage;
            d.Server = this.Server;
            d.Port = this.Port;
            d.ProxyServer = this.ProxyServer;
            d.Protocol = this.Protocol;
            d.AuthMode = this.AuthMode;
            d.EnableLobbyStatistics = this.EnableLobbyStatistics;
            d.NetworkLogging = this.NetworkLogging;
            d.EnableProtocolFallback = this.EnableProtocolFallback;
            return d;
        }
    }
}
