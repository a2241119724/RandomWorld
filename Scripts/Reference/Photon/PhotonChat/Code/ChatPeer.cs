// ----------------------------------------------------------------------------------------------------------------------
// <summary>Photon Chat Api 使客户端能够连接到聊天服务器并与其他客户端通信。</summary>
// <remarks>ChatClient 是此 api 的主类。</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------

#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif

namespace Photon.Chat
{
    using ExitGames.Client.Photon;
    using System;
    using System.Collections.Generic;

#if SUPPORTED_UNITY || NETFX_CORE
#endif


    /// <summary>
    /// 提供 Photon Chat 服务器的基本操作。此内部类由公共 ChatClient 使用。
    /// </summary>
    public class ChatPeer : PhotonPeer
    {
        /// <summary>Photon Cloud 的名称服务器主机名。不带端口和任何前缀。</summary>
        public string NameServerHost = "ns.photonengine.io";

        /// <summary>每种协议的名称服务器端口（UDP 端口与 TCP 等不同）。</summary>
        private static readonly Dictionary<ConnectionProtocol, int> ProtocolToNameServerPort = new Dictionary<ConnectionProtocol, int>() { { ConnectionProtocol.Udp, 5058 }, { ConnectionProtocol.Tcp, 4533 }, { ConnectionProtocol.WebSocket, 9093 }, { ConnectionProtocol.WebSocketSecure, 19093 } }; //, { ConnectionProtocol.RHttp, 6063 } };

        /// <summary>Photon Cloud 的名称服务器地址（基于当前协议）。您可以使用默认值，通常不需要设置此值。</summary>
        public string NameServerAddress { get { return this.GetNameServerAddress(); } }

        virtual internal bool IsProtocolSecure { get { return this.UsedProtocol == ConnectionProtocol.WebSocketSecure; } }

        /// <summary> Chat Peer 构造函数。</summary>
        /// <param name="listener">Chat 监听器实现。</param>
        /// <param name="protocol">Peer 要使用的协议。</param>
        public ChatPeer(IPhotonPeerListener listener, ConnectionProtocol protocol) : base(listener, protocol)
        {
            this.ConfigUnitySockets();
        }



        // 根据平台设置要使用的 Socket 实现
        [System.Diagnostics.Conditional("SUPPORTED_UNITY")]
        private void ConfigUnitySockets()
        {
            Type websocketType = null;
#if (UNITY_XBOXONE || UNITY_GAMECORE) && !UNITY_EDITOR
            websocketType = Type.GetType("ExitGames.Client.Photon.SocketNativeSource, Assembly-CSharp", false);
            if (websocketType == null)
            {
                websocketType = Type.GetType("ExitGames.Client.Photon.SocketNativeSource, Assembly-CSharp-firstpass", false);
            }
            if (websocketType == null)
            {
                websocketType = Type.GetType("ExitGames.Client.Photon.SocketNativeSource, PhotonRealtime", false);
            }
            if (websocketType != null)
            {
                this.SocketImplementationConfig[ConnectionProtocol.Udp] = websocketType;    // on Xbox, the native socket plugin supports UDP as well
            }
#else
            // 为了支持 Unity 中的 WebGL 导出，我们查找并分配 SocketWebTcp 类（如果它在项目中的话）。
            // 或者 SocketWebTcp 类可能在 Photon3Unity3D.dll 中
            websocketType = Type.GetType("ExitGames.Client.Photon.SocketWebTcp, PhotonWebSocket", false);
            if (websocketType == null)
            {
                websocketType = Type.GetType("ExitGames.Client.Photon.SocketWebTcp, Assembly-CSharp-firstpass", false);
            }
            if (websocketType == null)
            {
                websocketType = Type.GetType("ExitGames.Client.Photon.SocketWebTcp, Assembly-CSharp", false);
            }
#endif

            if (websocketType != null)
            {
                this.SocketImplementationConfig[ConnectionProtocol.WebSocket] = websocketType;
                this.SocketImplementationConfig[ConnectionProtocol.WebSocketSecure] = websocketType;
            }

#if NET_4_6 && (UNITY_EDITOR || !ENABLE_IL2CPP) && !NETFX_CORE
            this.SocketImplementationConfig[ConnectionProtocol.Udp] = typeof(SocketUdpAsync);
            this.SocketImplementationConfig[ConnectionProtocol.Tcp] = typeof(SocketTcpAsync);
#endif
        }

        /// <summary>如果不为零，则在连接时用作名称服务器端口。独立于协议（因此最好与之匹配）。由 ChatClient.ConnectUsingSettings 设置。</summary>
        /// <remarks>当使用协议回退时，此值会被重置。</remarks>
        public ushort NameServerPortOverride;

        /// <summary>
        /// 基于设置的协议（this.UsedProtocol）获取 NameServer 地址（带前缀和端口）。
        /// </summary>
        /// <returns>NameServer 地址（带前缀和端口）。</returns>
        private string GetNameServerAddress()
        {
            var protocolPort = 0;
            ProtocolToNameServerPort.TryGetValue(this.TransportProtocol, out protocolPort);

            if (this.NameServerPortOverride != 0)
            {
                this.Listener.DebugReturn(DebugLevel.INFO, string.Format("Using NameServerPortInAppSettings as port for Name Server: {0}", this.NameServerPortOverride));
                protocolPort = this.NameServerPortOverride;
            }

            switch (this.TransportProtocol)
            {
                case ConnectionProtocol.Udp:
                case ConnectionProtocol.Tcp:
                    return string.Format("{0}:{1}", NameServerHost, protocolPort);
                case ConnectionProtocol.WebSocket:
                    return string.Format("ws://{0}:{1}", NameServerHost, protocolPort);
                case ConnectionProtocol.WebSocketSecure:
                    return string.Format("wss://{0}:{1}", NameServerHost, protocolPort);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> 连接到名称服务器。</summary>
        /// <returns>连接尝试是否可以发送。</returns>
        public bool Connect()
        {
            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.Listener.DebugReturn(DebugLevel.INFO, "Connecting to nameserver " + this.NameServerAddress);
            }

            return this.Connect(this.NameServerAddress, "NameServer");
        }

        /// <summary> 在名称服务器上进行身份验证。</summary>
        /// <returns>身份验证操作请求是否可以发送。</returns>
        public bool AuthenticateOnNameServer(string appId, string appVersion, string region, AuthenticationValues authValues)
        {
            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.Listener.DebugReturn(DebugLevel.INFO, "OpAuthenticate()");
            }

            var opParameters = new Dictionary<byte, object>();

            opParameters[ParameterCode.AppVersion] = appVersion;
            opParameters[ParameterCode.ApplicationId] = appId;
            opParameters[ParameterCode.Region] = region;

            if (authValues != null)
            {
                if (!string.IsNullOrEmpty(authValues.UserId))
                {
                    opParameters[ParameterCode.UserId] = authValues.UserId;
                }

                if (authValues.AuthType != CustomAuthenticationType.None)
                {
                    opParameters[ParameterCode.ClientAuthenticationType] = (byte)authValues.AuthType;
                    if (authValues.Token != null)
                    {
                        opParameters[ParameterCode.Secret] = authValues.Token;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(authValues.AuthGetParameters))
                        {
                            opParameters[ParameterCode.ClientAuthenticationParams] = authValues.AuthGetParameters;
                        }
                        if (authValues.AuthPostData != null)
                        {
                            opParameters[ParameterCode.ClientAuthenticationData] = authValues.AuthPostData;
                        }
                    }
                }
            }

            return this.SendOperation(ChatOperationCode.Authenticate, opParameters, new SendOptions() { Reliability = true, Encrypt = this.IsEncryptionAvailable });
        }
    }

    /// <summary>
    /// 与 Photon 一起使用的可选"自定义身份验证"服务的选项。在连接到 Photon 后由 OpAuthenticate 使用。
    /// </summary>
    public enum CustomAuthenticationType : byte
    {
        /// <summary>使用自定义身份验证服务。目前唯一实现的选项。</summary>
        Custom = 0,

        /// <summary>通过 Steam 账户对用户进行身份验证。通过 AddAuthParameter() 将 Steam 的 ticket 设置为"ticket"。</summary>
        Steam = 1,

        /// <summary>通过 Facebook 账户对用户进行身份验证。通过 AddAuthParameter() 将 Facebook 的 token 设置为"token"。</summary>
        Facebook = 2,

        /// <summary>通过 Oculus 账户和 token 对用户进行身份验证。通过 AddAuthParameter() 将 Oculus 的 userid 设置为"userid"，nonce 设置为"nonce"。</summary>
        Oculus = 3,

        /// <summary>通过 PS4 上的 PSN 账户和 token 对用户进行身份验证。通过 AddAuthParameter() 将 token 设置为"token"，env 设置为"env"，userName 设置为"userName"。</summary>
        PlayStation4 = 4,
        [Obsolete("Use PlayStation4 or PlayStation5 as needed")]
        PlayStation = 4,

        /// <summary>通过 Xbox 账户对用户进行身份验证。通过 SetAuthPostData() 传递 XSTS token。</summary>
        Xbox = 5,

        /// <summary>通过 HTC Viveport 账户对用户进行身份验证。通过 AddAuthParameter() 将 userToken 设置为"userToken"。</summary>
        Viveport = 10,

        /// <summary>通过 NSA ID 对用户进行身份验证。通过 AddAuthParameter() 将 token 设置为"token"，appversion 设置为"appversion"。appversion 是可选的。</summary>
        NintendoSwitch = 11,

        /// <summary>通过 PS5 上的 PSN 账户和 token 对用户进行身份验证。通过 AddAuthParameter() 将 token 设置为"token"，env 设置为"env"，userName 设置为"userName"。</summary>
        PlayStation5 = 12,
        [Obsolete("Use PlayStation4 or PlayStation5 as needed")]
        Playstation5 = 12,

        /// <summary>通过 Epic Online Services (EOS) 对用户进行身份验证。通过 AddAuthParameter() 将 token 设置为"token"，ownershipToken 设置为"ownershipToken"。ownershipToken 是可选的。</summary>
        Epic = 13,

        /// <summary>通过 Facebook Gaming api 对用户进行身份验证。通过 AddAuthParameter() 将 token 设置为"token"。</summary>
        FacebookGaming = 15,

        /// <summary>禁用自定义身份验证。与不为连接提供任何 AuthenticationValues 相同（更准确地说，对于 OpAuthenticate）。</summary>
        None = byte.MaxValue
    }


    /// <summary>
    /// Photon 中用户身份验证的容器。在连接之前设置 AuthValues——其他一切都会自动处理。
    /// </summary>
    /// <remarks>
    /// 在 Photon 中，用户身份验证是可选的，但在许多情况下很有用。
    /// 如果您想使用 FindFriends，每个用户的唯一 ID 非常实用。
    ///
    /// 用户身份验证基本上有三个选项：完全不验证、客户端设置一些 UserId、
    /// 或者您可以使用某种账户 Web 服务来验证用户（并在服务器端设置 UserId）。
    ///
    /// 自定义身份验证允许您通过某种登录或 token 来验证最终用户。它将那些
    /// 值发送给 Photon，Photon 将在授予访问权限或断开客户端连接之前验证它们。
    ///
    /// AuthValues 在连接时通过 OpAuthenticate 发送，因此必须在连接之前设置。
    /// 如果 AuthValues.UserId 在发送到服务器时为 null 或为空，那么 Photon Server 会分配一个 UserId！
    ///
    /// Photon Cloud Dashboard 将允许您启用此功能并为其设置重要的服务器值。
    /// https://dashboard.photonengine.com
    /// </remarks>
    public class AuthenticationValues
    {
        /// <summary>参见 AuthType。</summary>
        private CustomAuthenticationType authType = CustomAuthenticationType.None;

        /// <summary>应使用的身份验证提供程序类型。默认为 None（不使用任何身份验证）。</summary>
        /// <remarks>有几种身份验证提供程序可用，如果您构建自己的服务，可以使用 CustomAuthenticationType.Custom。</remarks>
        public CustomAuthenticationType AuthType
        {
            get { return authType; }
            set { authType = value; }
        }

        /// <summary>此字符串必须包含所使用的身份验证服务期望的任何（http get）参数。默认情况下为 username 和 token。</summary>
        /// <remarks>
        /// 映射到操作参数 216。
        /// 此处使用标准的 http get 参数，并将其传递给服务器（Photon Cloud Dashboard）中定义的服务。
        /// </remarks>
        public string AuthGetParameters { get; set; }

        /// <summary>要通过 POST 传递给身份验证服务的数据。默认值：null（不发送）。可以是 string 或 byte[]（参见 setter）。</summary>
        /// <remarks>映射到操作参数 214。</remarks>
        public object AuthPostData { get; private set; }

        /// <summary>内部<b>Photon token</b>。在初始身份验证之后，Photon 为此客户端提供一个 token，随后用作（缓存的）验证。</summary>
        /// <remarks>任何用于自定义身份验证的 token 应通过 SetAuthPostData 或 AddAuthParameter 设置。</remarks>
        public object Token { get; protected internal set; }

        /// <summary>UserId 应该是每个用户的唯一标识符。用于查找好友等。</summary>
        /// <remarks>有关如何设置和使用此值的信息，请参阅 AuthValues 的备注。</remarks>
        public string UserId { get; set; }


        /// <summary>创建空的、没有任何信息的身份验证值。</summary>
        public AuthenticationValues()
        {
        }

        /// <summary>创建关于用户的最少信息。是否经过身份验证取决于设置的 AuthType。</summary>
        /// <param name="userId">要在 Photon 中设置的 UserId。</param>
        public AuthenticationValues(string userId)
        {
            this.UserId = userId;
        }

        /// <summary>设置要通过 POST 传递给身份验证服务的数据。</summary>
        /// <remarks>AuthPostData 只是一个值。每次 SetAuthPostData 都会替换之前的值。它可以是 string、byte[] 或 dictionary。</remarks>
        /// <param name="stringData">要在 POST 请求正文中使用的字符串数据。null 或空字符串会将 AuthPostData 设置为 null。</param>
        public virtual void SetAuthPostData(string stringData)
        {
            this.AuthPostData = (string.IsNullOrEmpty(stringData)) ? null : stringData;
        }

        /// <summary>设置要通过 POST 传递给身份验证服务的数据。</summary>
        /// <remarks>AuthPostData 只是一个值。每次 SetAuthPostData 都会替换之前的值。它可以是 string、byte[] 或 dictionary。</remarks>
        /// <param name="byteData">要传递的二进制 token / 身份验证数据。</param>
        public virtual void SetAuthPostData(byte[] byteData)
        {
            this.AuthPostData = byteData;
        }

        /// <summary>设置要通过 Post 以 Json 格式（Content-Type: "application/json"）传递给身份验证服务的数据。</summary>
        /// <remarks>AuthPostData 只是一个值。每次 SetAuthPostData 都会替换之前的值。它可以是 string、byte[] 或 dictionary。</remarks>
        /// <param name="dictData">身份验证数据字典将被转换为 Json，并通过 HTTP Post 传递给 Auth Web 服务。</param>
        public virtual void SetAuthPostData(Dictionary<string, object> dictData)
        {
            this.AuthPostData = dictData;
        }

        /// <summary>向用于自定义身份验证（AuthGetParameters）的 get 参数添加键值对。</summary>
        /// <remarks>此方法会为您进行 URI 编码。</remarks>
        /// <param name="key">要设置的值的键。</param>
        /// <param name="value">与自定义身份验证相关的某个值。</param>
        public virtual void AddAuthParameter(string key, string value)
        {
            string ampersand = string.IsNullOrEmpty(this.AuthGetParameters) ? "" : "&";
            this.AuthGetParameters = string.Format("{0}{1}{2}={3}", this.AuthGetParameters, ampersand, System.Uri.EscapeDataString(key), System.Uri.EscapeDataString(value));
        }

        /// <summary>
        /// 将此对象转换为字符串。
        /// </summary>
        /// <returns>此对象的字符串表示。</returns>
        public override string ToString()
        {
            return string.Format("AuthenticationValues Type: {3} UserId: {0}, GetParameters: {1} Token available: {2}", this.UserId, this.AuthGetParameters, this.Token != null, this.AuthType);
        }

        /// <summary>
        /// 制作当前对象的副本。
        /// </summary>
        /// <param name="copy">要复制到的对象。</param>
        /// <returns>复制的对象。</returns>
        public AuthenticationValues CopyTo(AuthenticationValues copy)
        {
            copy.AuthType = this.AuthType;
            copy.AuthGetParameters = this.AuthGetParameters;
            copy.AuthPostData = this.AuthPostData;
            copy.UserId = this.UserId;
            return copy;
        }
    }


    /// <summary>常量类。操作和事件的参数代码。</summary>
    public class ParameterCode
    {
        /// <summary>(224) 您的应用程序 ID：您自己的 Photon 上的名称或 Photon Cloud 上的 GUID</summary>
        public const byte ApplicationId = 224;
        /// <summary>(221) 内部用于建立加密</summary>
        public const byte Secret = 221;
        /// <summary>(220) 您的应用程序版本</summary>
        public const byte AppVersion = 220;
        /// <summary>(217) 此键的 (byte) 值定义了客户端连接的目标自定义身份验证类型/服务。在 OpAuthenticate 中使用</summary>
        public const byte ClientAuthenticationType = 217;
        /// <summary>(216) 此键的 (string) 值提供发送到客户端连接的自定义身份验证类型/服务的参数。在 OpAuthenticate 中使用</summary>
        public const byte ClientAuthenticationParams = 216;
        /// <summary>(214) 此键的 (string 或 byte[]) 值提供发送到 Photon Dashboard 中设置的自定义身份验证服务的参数。在 OpAuthenticate 中使用</summary>
        public const byte ClientAuthenticationData = 214;
        /// <summary>(210) 用于 OpAuth 和 OpGetRegions 中的区域值。</summary>
        public const byte Region = 210;
        /// <summary>(230) 要使用的（游戏）服务器地址。</summary>
        public const byte Address = 230;
        /// <summary>(225) 用户的 ID</summary>
        public const byte UserId = 225;
    }

    /// <summary>
    /// ErrorCode 定义了与 Photon 客户端/服务器通信相关的默认代码。
    /// </summary>
    public class ErrorCode
    {
        /// <summary>(0) 始终表示"OK"，其他任何值都表示错误或特定情况。</summary>
        public const int Ok = 0;

        // 服务器 - Photon 低（较）层级：<= 0

        /// <summary>
        /// (-3) 操作尚无法执行（例如，在进行身份验证之前不能调用 OpJoin，在进入房间之前不能使用 RaiseEvent）。
        /// </summary>
        /// <remarks>
        /// 在对 Cloud 服务器调用任何操作之前，自动化客户端工作流必须完成其授权。
        /// 在 PUN 中，请等待 State 为：JoinedLobby 或 ConnectedToMaster
        /// </remarks>
        public const int OperationNotAllowedInCurrentState = -3;

        /// <summary>(-2) 您调用的操作未在您连接的服务器（应用程序）上实现。请确保您运行了合适的应用程序。</summary>
        public const int InvalidOperationCode = -2;

        /// <summary>(-1) 服务器中出现问题。尝试重现问题并联系 Exit Games。</summary>
        public const int InternalServerError = -1;

        // 服务器 - PhotonNetwork：0x7FFF 及以下
        // 逻辑级错误代码从 short.max 开始

        /// <summary>(32767) 身份验证失败。可能的原因：AppId 对 Photon 未知（在云服务中）。</summary>
        public const int InvalidAuthentication = 0x7FFF;

        /// <summary>(32766) GameId（名称）已在使用中（无法创建另一个）。请更改名称。</summary>
        public const int GameIdAlreadyExists = 0x7FFF - 1;

        /// <summary>(32765) 游戏已满。这种情况很少发生在某个玩家在您的加入完成之前加入了房间。</summary>
        public const int GameFull = 0x7FFF - 2;

        /// <summary>(32764) 游戏已关闭，无法加入。请加入另一个游戏。</summary>
        public const int GameClosed = 0x7FFF - 3;

        /// <summary>(32762) 当前未使用。</summary>
        public const int ServerFull = 0x7FFF - 5;

        /// <summary>(32761) 当前未使用。</summary>
        public const int UserBlocked = 0x7FFF - 6;

        /// <summary>(32760) 仅当存在既未关闭也未满的房间时，随机匹配才会成功。请在几秒钟后重试或创建新房间。</summary>
        public const int NoRandomMatchFound = 0x7FFF - 7;

        /// <summary>(32758) 如果房间（名称）不存在（或不再存在），加入可能会失败。这可能发生在玩家在您加入时离开的情况。</summary>
        public const int GameDoesNotExist = 0x7FFF - 9;

        /// <summary>(32757) Photon Cloud 上的授权失败，因为已达到应用订阅的并发用户（CCU）限制。</summary>
        /// <remarks>
        /// 除非您有"CCU Burst"计划，否则客户端可能在连接期间的身份验证步骤失败。
        /// 受影响的客户端无法调用操作。请注意，结束游戏并返回
        /// 主服务器的玩家将断开连接并重新连接，这意味着他们刚刚玩过游戏，在下一分钟/重新连接时会被拒绝。
        /// 这是临时措施。一旦 CCU 低于限制，玩家将能够再次连接和玩游戏。
        ///
        /// OpAuthorize 是连接工作流的一部分，但仅在 Photon Cloud 上可能发生此错误。
        /// 具有 CCU 限制许可证的自托管 Photon 服务器根本不会让客户端连接。
        /// </remarks>
        public const int MaxCcuReached = 0x7FFF - 10;

        /// <summary>(32756) Photon Cloud 上的授权失败，因为应用订阅不允许使用特定区域的服务器。</summary>
        /// <remarks>
        /// Photon Cloud 的某些订阅计划受区域限制。其他区域的服务器无法使用。
        /// 检查您的主服务器地址，并与 Photon Cloud Dashboard 的信息进行比较。
        /// https://cloud.photonengine.com/dashboard
        ///
        /// OpAuthorize 是连接工作流的一部分，但仅在 Photon Cloud 上可能发生此错误。
        /// 具有 CCU 限制许可证的自托管 Photon 服务器根本不会让客户端连接。
        /// </remarks>
        public const int InvalidRegion = 0x7FFF - 11;

        /// <summary>
        /// (32755) 用户的自定义身份验证由于设置原因（参见 Cloud Dashboard）或提供的用户数据（如用户名或 token）而失败。请检查错误消息以获取详细信息。
        /// </summary>
        public const int CustomAuthenticationFailed = 0x7FFF - 12;

        /// <summary>(32753) 身份验证票据已过期。通常，这会在后台自动刷新。请再次连接（并授权）。</summary>
        public const int AuthenticationTicketExpired = 0x7FF1;
    }

}
