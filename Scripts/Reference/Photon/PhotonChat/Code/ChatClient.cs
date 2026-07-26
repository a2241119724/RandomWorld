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
    using System;
    using System.Collections.Generic;
    using ExitGames.Client.Photon;

#if SUPPORTED_UNITY || NETFX_CORE
    using SupportClass = ExitGames.Client.Photon.SupportClass;
#endif


    /// <summary>Photon Chat API 的中心类，用于连接、处理频道和消息。</summary>
    /// <remarks>
    /// 此类必须使用 IChatClientListener 实例来实例化以获取回调。
    /// 通过定期调用 Service 将其集成到您的游戏循环中。如果目标平台支持 Threads/Tasks，
    /// 设置 UseBackgroundWorkerForSending = true，让 ChatClient 通过独立线程
    /// 发送来保持连接。
    ///
    /// 使用设置为 Photon Chat 应用程序的 AppId 调用 Connect。注意：Connect 涵盖此客户端
    /// 与服务器之间的多条消息。一个简短的工作流将把您连接到聊天服务器。
    ///
    /// 每个 ChatClient 代表聊天中的一个用户（在 Connect 中设置）。每个用户会自动订阅一个
    /// 用于接收私密消息的频道，并且可以私密地向任何其他用户发送消息。
    /// 在您在任何非私有频道中发布消息之前，必须先订阅该频道。
    ///
    /// PublicChannels 是已订阅频道的列表，包含消息和发送者。
    /// PrivateChannels 包含所有传入和发送的私密消息。
    /// </remarks>
    public class ChatClient : IPhotonPeerListener
    {
        const int FriendRequestListMax = 1024;

        /// <summary> 当 <see cref="ChatChannel.PublishSubscribers"/> 启用时，<see cref="ChatChannel.MaxSubscribers"/> 的默认最大可能值</summary>
        public const int DefaultMaxSubscribers = 100;

        private const byte HttpForwardWebFlag = 0x01;

        /// <summary>在连接到名称服务器失败时，启用回退到另一个协议。</summary>
        /// <remarks>
        /// 当第一次连接到名称服务器失败时，客户端将选择替代的
        /// 网络协议并重试连接。
        ///
        /// 回退将使用 ProtocolToNameServerPort 定义的默认名称服务器端口。
        ///
        /// TCP 的回退是 UDP。所有其他协议回退到 TCP。
        /// </remarks>
        public bool EnableProtocolFallback { get; set; }

        /// <summary>最后连接的名称服务器的地址。</summary>
        public string NameServerAddress { get; private set; }

        /// <summary>从 NameServer 分配的实际聊天服务器的地址。仅公开用于只读。</summary>
        public string FrontendAddress { get; private set; }

        /// <summary>用于连接的区域。目前所有聊天都在 EU 区域进行。对整个游戏只使用一个区域可能是有意义的。</summary>
        private string chatRegion = "EU";

        /// <summary>只能在连接前设置！默认为"EU"。</summary>
        public string ChatRegion
        {
            get { return this.chatRegion; }
            set { this.chatRegion = value; }
        }

        /// <summary>ChatClient 的当前状态。也可使用 CanChat。</summary>
        public ChatState State { get; private set; }

        /// <summary> 断开连接的原因。在 <see cref="IChatClientListener.OnDisconnected"/> 中检查此项。</summary>
        public ChatDisconnectCause DisconnectedCause { get; private set; }
        /// <summary>
        /// 检查此客户端是否准备好发送消息。
        /// </summary>
        public bool CanChat
        {
            get { return this.State == ChatState.ConnectedToFrontEnd && this.HasPeer; }
        }
        /// <summary>
        /// 检查此客户端是否准备好在公共频道中发布消息。
        /// </summary>
        /// <param name="channelName">要检查的频道。</param>
        /// <returns>此客户端是否准备好在具有指定 channelName 的公共频道中发布消息。</returns>
        public bool CanChatInChannel(string channelName)
        {
            return this.CanChat && this.PublicChannels.ContainsKey(channelName) && !this.PublicChannelsUnsubscribing.Contains(channelName);
        }

        private bool HasPeer
        {
            get { return this.chatPeer != null; }
        }

        /// <summary>您的客户端版本。新版本还会创建一个新的"虚拟应用"，以将玩家与旧客户端版本分开。</summary>
        public string AppVersion { get; private set; }

        /// <summary>从 Photon Cloud 分配的 AppID。</summary>
        public string AppId { get; private set; }


        /// <summary>只能在连接前设置！</summary>
        public AuthenticationValues AuthValues { get; set; }

        /// <summary>用户/人员的唯一 ID，存储在 AuthValues.UserId 中。在连接前设置它。</summary>
        /// <remarks>
        /// 此值包装 AuthValues.UserId。
        /// 它不是昵称，我们假定具有相同 userID 的用户是同一个人。</remarks>
        public string UserId
        {
            get
            {
                return (this.AuthValues != null) ? this.AuthValues.UserId : null;
            }
            private set
            {
                if (this.AuthValues == null)
                {
                    this.AuthValues = new AuthenticationValues();
                }
                this.AuthValues.UserId = value;
            }
        }

        /// <summary>如果大于 0，新频道将限制其本地缓存的消息数量。</summary>
        /// <remarks>
        /// 这对于限制聊天所使用的内存量很有用。
        /// 您可以为每个频道设置 MessageLimit，但此值会应用于新的频道。
        ///
        /// 注意：
        /// 更改此值不会影响已经使用中的 ChatChannel！
        /// </remarks>
        public int MessageLimit;

        /// <summary>限制来自私有频道历史记录的消息数量。</summary>
        /// <remarks>
        /// 这在重新连接时应用于所有私有频道，因为没有显式重新加入私有频道。<br/>
        /// 默认值为 -1，这会获取服务器设置的最大值以内的可用消息。<br/>
        /// 值为 0 将获取零条消息。<br/>
        /// 服务器的消息限制可能更低。如果是这样，服务器的值将覆盖此值。<br/>
        /// </remarks>
        public int PrivateChatHistoryLength = -1;

        /// <summary> 此客户端订阅的公共频道。</summary>
        public readonly Dictionary<string, ChatChannel> PublicChannels;
        /// <summary> 此客户端已交换消息的私有频道。</summary>
        public readonly Dictionary<string, ChatChannel> PrivateChannels;

        // 处于取消订阅过程中的频道
        // 项目将在成功取消订阅或订阅后被移除（后者在尝试从不存在的频道取消订阅后需要）
        private readonly HashSet<string> PublicChannelsUnsubscribing;

        private readonly IChatClientListener listener = null;
        /// <summary> 此客户端使用的 Chat Peer。</summary>
        public ChatPeer chatPeer = null;
        private const string ChatAppName = "chat";
        private bool didAuthenticate;

        private int? statusToSetWhenConnected;
        private object messageToSetWhenConnected;

        private int msDeltaForServiceCalls = 50;
        private int msTimestampOfLastServiceCall;

        /// <summary>定义后台线程是否将调用 SendOutgoingCommands，而您的代码调用 Service 来分发接收到的消息。</summary>
        /// <remarks>
        /// 使用后台线程调用 SendOutgoingCommands 的好处是：
        ///
        /// 即使您的游戏逻辑被暂停，后台线程也会保持与服务器的连接。
        /// 在较低级别上，确认和 ping 将防止服务器端超时，例如当 Unity 加载资源时。
        ///
        /// 您的游戏逻辑仍然必须定期调用 Service，否则传入的消息不会被分发。
        /// 由于这通常会触发 UI 更新，因此从主/UI 线程调用 Service 更容易。
        /// </remarks>
        public bool UseBackgroundWorkerForSending { get; set; }

        /// <summary>公开所使用的 PhotonPeer 的 TransportProtocol。可在未连接时设置。</summary>
        public ConnectionProtocol TransportProtocol
        {
            get { return this.chatPeer.TransportProtocol; }
            set
            {
                if (this.chatPeer == null || this.chatPeer.PeerState != PeerStateValue.Disconnected)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "Can't set TransportProtocol. Disconnect first! " + ((this.chatPeer != null) ? "PeerState: " + this.chatPeer.PeerState : "The chatPeer is null."));
                    return;
                }
                this.chatPeer.TransportProtocol = value;
            }
        }

        /// <summary>定义每个 ConnectionProtocol 要使用的 IPhotonSocket 类。</summary>
        /// <remarks>
        /// 多个平台有特殊的 Socket 实现和略有不同的 API。
        /// 为了适应这种情况，可以切换网络协议的 Socket 实现。
        /// 默认情况下，UDP 和 TCP 已分配了 Socket 实现。
        ///
        /// 您只需要在创建 PhotonPeer 之后、连接之前设置一次 SocketImplementationConfig。
        /// 如果您切换 TransportProtocol，则会使用正确的实现。
        /// </remarks>
        public Dictionary<ConnectionProtocol, Type> SocketImplementationConfig
        {
            get { return this.chatPeer.SocketImplementationConfig; }
        }

        /// <summary>
        /// Chat 客户端构造函数。
        /// </summary>
        /// <param name="listener">聊天监听器实现。</param>
        /// <param name="protocol">此客户端要使用的连接协议。默认值为 <see cref="ConnectionProtocol.Udp"/>。</param>
        public ChatClient(IChatClientListener listener, ConnectionProtocol protocol = ConnectionProtocol.Udp)
        {
            this.listener = listener;
            this.State = ChatState.Uninitialized;

            this.chatPeer = new ChatPeer(this, protocol);
            this.chatPeer.SerializationProtocolType = SerializationProtocol.GpBinaryV18;

            this.PublicChannels = new Dictionary<string, ChatChannel>();
            this.PrivateChannels = new Dictionary<string, ChatChannel>();

            this.PublicChannelsUnsubscribing = new HashSet<string>();
        }


        public bool ConnectUsingSettings(ChatAppSettings appSettings)
        {
            if (appSettings == null)
            {
                this.listener.DebugReturn(DebugLevel.ERROR, "ConnectUsingSettings failed. The appSettings can't be null.'");
                return false;
            }

            if (!string.IsNullOrEmpty(appSettings.FixedRegion))
            {
                this.ChatRegion = appSettings.FixedRegion;
            }

            this.DebugOut = appSettings.NetworkLogging;

            this.TransportProtocol = appSettings.Protocol;
            this.EnableProtocolFallback = appSettings.EnableProtocolFallback;

            if (!appSettings.IsDefaultNameServer)
            {
                this.chatPeer.NameServerHost = appSettings.Server;
                this.chatPeer.NameServerPortOverride = appSettings.Port;
            }

            return this.Connect(appSettings.AppIdChat, appSettings.AppVersion, this.AuthValues);
        }

        /// <summary>
        /// 将此客户端连接到 Photon Chat Cloud 服务，该服务还将对用户进行身份验证（并设置 UserId）。
        /// </summary>
        /// <param name="appId">从 <a href="https://dashboard.photonengine.com">Dashboard</a> 获取您的 Photon Chat AppId。</param>
        /// <param name="appVersion">您编造的任何版本字符串。用于分隔用户和客户端的不同变体，这些变体可能不兼容。</param>
        /// <param name="authValues">用于身份验证的值。如果您之前设置了 UserId，可以将此参数留为 null。如果您设置了 authValues，它们将覆盖之前设置的任何 UserId。</param>
        /// <returns></returns>
        public bool Connect(string appId, string appVersion, AuthenticationValues authValues)
        {
            this.chatPeer.TimePingInterval = 3000;
            this.DisconnectedCause = ChatDisconnectCause.None;

            if (authValues != null)
            {
                this.AuthValues = authValues;
            }

            this.AppId = appId;
            this.AppVersion = appVersion;
            this.didAuthenticate = false;
            this.chatPeer.QuickResendAttempts = 2;
            this.chatPeer.SentCountAllowance = 7;

            // 清理所有频道
            this.PublicChannels.Clear();
            this.PrivateChannels.Clear();
            this.PublicChannelsUnsubscribing.Clear();

#if UNITY_WEBGL
            if (this.TransportProtocol == ConnectionProtocol.Tcp || this.TransportProtocol == ConnectionProtocol.Udp)
            {
                this.listener.DebugReturn(DebugLevel.WARNING, "WebGL requires WebSockets. Switching TransportProtocol to WebSocketSecure.");
                this.TransportProtocol = ConnectionProtocol.WebSocketSecure;
            }
#endif

            this.NameServerAddress = this.chatPeer.NameServerAddress;

            bool isConnecting = this.chatPeer.Connect();
            if (isConnecting)
            {
                this.State = ChatState.ConnectingToNameServer;
            }

            if (this.UseBackgroundWorkerForSending)
            {
#if UNITY_SWITCH
                SupportClass.StartBackgroundCalls(this.SendOutgoingInBackground, this.msDeltaForServiceCalls);  // as workaround, we don't name the Thread.
#else
                SupportClass.StartBackgroundCalls(this.SendOutgoingInBackground, this.msDeltaForServiceCalls, "ChatClient Service Thread");
#endif
            }

            return isConnecting;
        }

        /// <summary>
        /// 将此客户端连接到 Photon Chat Cloud 服务，该服务还将对用户进行身份验证（并设置 UserId）。
        /// 这也将在连接后设置在线状态。默认情况下，它将用户状态设置为 <see cref="ChatUserStatus.Online"/>。
        /// 有关更多信息，请参见 <see cref="SetOnlineStatus(int,object)"/>。
        /// </summary>
        /// <param name="appId">从 <a href="https://dashboard.photonengine.com">Dashboard</a> 获取您的 Photon Chat AppId。</param>
        /// <param name="appVersion">您编造的任何版本字符串。用于分隔用户和客户端的不同变体，这些变体可能不兼容。</param>
        /// <param name="authValues">用于身份验证的值。如果您之前设置了 UserId，可以将此参数留为 null。如果您设置了 authValues，它们将覆盖之前设置的任何 UserId。</param>
        /// <param name="status">连接时要设置的用户状态。预定义状态在 <see cref="ChatUserStatus"/> 类中。其他值可以随意使用。</param>
        /// <param name="message">可选状态。同时设置您的朋友可以获取的状态消息。</param>
        /// <returns>连接尝试是否可以发送。</returns>
        public bool ConnectAndSetStatus(string appId, string appVersion, AuthenticationValues authValues,
            int status = ChatUserStatus.Online, object message = null)
        {
            statusToSetWhenConnected = status;
            messageToSetWhenConnected = message;
            return Connect(appId, appVersion, authValues);
        }

        /// <summary>
        /// 必须定期调用以保持客户端和服务器之间的连接活动状态，并处理传入的消息。
        /// </summary>
        /// <remarks>
        /// 此方法使用私有变量 msDeltaForServiceCalls 自动限制其执行的工作量。
        /// 该值在连接时较低，当聊天服务器连接就绪时乘以 4。
        /// </remarks>
        public void Service()
        {
            // 分发直到每条已收到的消息都被分发
            while (this.HasPeer && this.chatPeer.DispatchIncomingCommands())
            {
            }

            // 如果没有用于发送的后台线程，Service() 也将按间隔执行发送操作
            if (!this.UseBackgroundWorkerForSending)
            {
                if (Environment.TickCount - this.msTimestampOfLastServiceCall > this.msDeltaForServiceCalls || this.msTimestampOfLastServiceCall == 0)
                {
                    this.msTimestampOfLastServiceCall = Environment.TickCount;

                    while (this.HasPeer && this.chatPeer.SendOutgoingCommands())
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 由单独的线程调用，只要 Peer 已连接，就会发送其传出命令。
        /// </summary>
        /// <returns>只要客户端未断开连接，就返回 True。</returns>
        private bool SendOutgoingInBackground()
        {
            while (this.HasPeer && this.chatPeer.SendOutgoingCommands())
            {
            }

            return this.State != ChatState.Disconnected;
        }

        /// <summary> 已过时：最好使用 UseBackgroundWorkerForSending 和 Service()。</summary>
        [Obsolete("Better use UseBackgroundWorkerForSending and Service().")]
        public void SendAcksOnly()
        {
            if (this.HasPeer) this.chatPeer.SendAcksOnly();
        }


        /// <summary>
        /// 通过发送"断开连接命令"从聊天服务器断开连接，这可以防止服务器端超时。
        /// </summary>
        public void Disconnect(ChatDisconnectCause cause = ChatDisconnectCause.DisconnectByClientLogic)
        {
            if (this.HasPeer && this.chatPeer.PeerState != PeerStateValue.Disconnected)
            {
                this.State = ChatState.Disconnecting;
                this.DisconnectedCause = cause;
                this.chatPeer.Disconnect();
            }
        }

        /// <summary>
        /// 本地关闭与聊天服务器的连接。这会重置本地状态，但服务器将不得不超时此 Peer。
        /// </summary>
        public void StopThread()
        {
            if (this.HasPeer)
            {
                this.chatPeer.StopThread();
            }
        }

        /// <summary>发送按名称订阅频道列表的操作。</summary>
        /// <param name="channels">要订阅的频道列表。避免 null 或空值。</param>
        /// <returns>操作是否可以发送（例如：如果未连接到聊天服务器则失败）。</returns>
        public bool Subscribe(string[] channels)
        {
            return this.Subscribe(channels, 0);
        }

        /// <summary>
        /// 发送按名称订阅频道列表的操作，并可能检索我们在取消订阅期间未收到的消息。
        /// </summary>
        /// <param name="channels">要订阅的频道列表。避免 null 或空值。</param>
        /// <param name="lastMsgIds">每个频道最后收到的消息的 ID。在重新订阅时有用，以仅接收我们错过的消息。</param>
        /// <returns>操作是否可以发送（例如：如果未连接到聊天服务器则失败）。</returns>
        public bool Subscribe(string[] channels, int[] lastMsgIds)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "Subscribe called while not connected to front end server.");
                }
                return false;
            }

            if (channels == null || channels.Length == 0)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "Subscribe can't be called for empty or null channels-list.");
                }
                return false;
            }

            for (int i = 0; i < channels.Length; i++)
            {
                if (string.IsNullOrEmpty(channels[i]))
                {
                    if (this.DebugOut >= DebugLevel.ERROR)
                    {
                        this.listener.DebugReturn(DebugLevel.ERROR, string.Format("Subscribe can't be called with a null or empty channel name at index {0}.", i));
                    }
                    return false;
                }
            }

            if (lastMsgIds == null || lastMsgIds.Length != channels.Length)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "Subscribe can't be called when \"lastMsgIds\" array is null or does not have the same length as \"channels\" array.");
                }
                return false;
            }

            Dictionary<byte, object> opParameters = new Dictionary<byte, object>
            {
                { ChatParameterCode.Channels, channels },
                { ChatParameterCode.MsgIds,  lastMsgIds},
                { ChatParameterCode.HistoryLength, -1 } // 服务器将决定向客户端发送多少消息
            };

            return this.chatPeer.SendOperation(ChatOperationCode.Subscribe, opParameters, SendOptions.SendReliable);
        }

        /// <summary>
        /// 发送操作以将客户端订阅到频道，可选从缓存中获取一定数量的消息。
        /// </summary>
        /// <remarks>
        /// 订阅的频道会将新消息转发给此用户。使用 PublishMessage 来实现。
        /// 消息缓存有限，但如果需要，可以帮助进入正在进行的对话。
        /// </remarks>
        /// <param name="channels">要订阅的频道列表。避免 null 或空值。</param>
        /// <param name="messagesFromHistory">0：无历史记录。1 及以上：历史记录中的消息数量。-1：所有可用的历史记录。</param>
        /// <returns>操作是否可以发送（例如：如果未连接到聊天服务器则失败）。</returns>
        public bool Subscribe(string[] channels, int messagesFromHistory)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "Subscribe called while not connected to front end server.");
                }
                return false;
            }

            if (channels == null || channels.Length == 0)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "Subscribe can't be called for empty or null channels-list.");
                }
                return false;
            }

            return this.SendChannelOperation(channels, (byte)ChatOperationCode.Subscribe, messagesFromHistory);
        }

        /// <summary>从频道列表中取消订阅，这将停止从这些频道接收消息。</summary>
        /// <remarks>
        /// 一旦服务器发送了对该请求的响应，客户端将从 PublicChannels 字典中移除这些频道。
        ///
        /// 请求将被发送到服务器，当服务器实际移除频道订阅时，将调用 IChatClientListener.OnUnsubscribed。
        ///
        /// 如果您包含 null 或空频道名称，取消订阅将失败。
        /// </remarks>
        /// <param name="channels">要取消订阅的频道名称。</param>
        /// <returns>如果未连接到聊天服务器，则返回 False。</returns>
        public bool Unsubscribe(string[] channels)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "Unsubscribe called while not connected to front end server.");
                }
                return false;
            }

            if (channels == null || channels.Length == 0)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "Unsubscribe can't be called for empty or null channels-list.");
                }
                return false;
            }

            foreach (string ch in channels)
            {
                this.PublicChannelsUnsubscribing.Add(ch);
            }
            return this.SendChannelOperation(channels, ChatOperationCode.Unsubscribe, 0);
        }

        /// <summary>向此客户端已订阅的公共频道发送消息。</summary>
        /// <remarks>
        /// 在向频道发布消息之前，必须先订阅它。
        /// 该频道中的每个人都会收到该消息。
        /// </remarks>
        /// <param name="channelName">要发布到的频道名称。</param>
        /// <param name="message">您的消息（字符串或任何可序列化的数据）。</param>
        /// <param name="forwardAsWebhook">可选，公共消息可以作为 webhooks 转发。配置 Chat 应用的 webhooks 以使用此功能。</param>
        /// <returns>如果客户端尚未准备好发送消息，则返回 False。</returns>
        public bool PublishMessage(string channelName, object message, bool forwardAsWebhook = false)
        {
            return this.publishMessage(channelName, message, true, forwardAsWebhook);
        }

        internal bool PublishMessageUnreliable(string channelName, object message, bool forwardAsWebhook = false)
        {
            return this.publishMessage(channelName, message, false, forwardAsWebhook);
        }

        private bool publishMessage(string channelName, object message, bool reliable, bool forwardAsWebhook = false)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "PublishMessage called while not connected to front end server.");
                }
                return false;
            }

            if (string.IsNullOrEmpty(channelName) || message == null)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "PublishMessage parameters must be non-null and not empty.");
                }
                return false;
            }

            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                {
                    { (byte)ChatParameterCode.Channel, channelName },
                    { (byte)ChatParameterCode.Message, message }
                };
            if (forwardAsWebhook)
            {
                parameters.Add(ChatParameterCode.WebFlags, (byte)0x1);
            }

            return this.chatPeer.SendOperation(ChatOperationCode.Publish, parameters, new SendOptions() { Reliability = reliable });
        }

        /// <summary>
        /// 向单个目标用户发送私密消息。在接收客户端上调用 OnPrivateMessage。
        /// </summary>
        /// <param name="target">要发送此消息的用户的用户名。</param>
        /// <param name="message">您要发送的消息。可以是简单的字符串或任何可序列化的内容。</param>
        /// <param name="forwardAsWebhook">可选，私密消息可以作为 webhooks 转发。配置 Chat 应用的 webhooks 以使用此功能。</param>
        /// <returns>如果此客户端可以向服务器发送消息，则返回 True。</returns>
        public bool SendPrivateMessage(string target, object message, bool forwardAsWebhook = false)
        {
            return this.SendPrivateMessage(target, message, false, forwardAsWebhook);
        }

        /// <summary>
        /// 向单个目标用户发送私密消息。在接收客户端上调用 OnPrivateMessage。
        /// </summary>
        /// <param name="target">要发送此消息的用户的用户名。</param>
        /// <param name="message">您要发送的消息。可以是简单的字符串或任何可序列化的内容。</param>
        /// <param name="encrypt">可选，私密消息可以加密。加密不是端到端的，因为服务器会解密消息。</param>
        /// <param name="forwardAsWebhook">可选，私密消息可以作为 webhooks 转发。配置 Chat 应用的 webhooks 以使用此功能。</param>
        /// <returns>如果此客户端可以向服务器发送消息，则返回 True。</returns>
        public bool SendPrivateMessage(string target, object message, bool encrypt, bool forwardAsWebhook)
        {
            return this.sendPrivateMessage(target, message, encrypt, true, forwardAsWebhook);
        }

        internal bool SendPrivateMessageUnreliable(string target, object message, bool encrypt, bool forwardAsWebhook = false)
        {
            return this.sendPrivateMessage(target, message, encrypt, false, forwardAsWebhook);
        }

        private bool sendPrivateMessage(string target, object message, bool encrypt, bool reliable, bool forwardAsWebhook = false)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "SendPrivateMessage called while not connected to front end server.");
                }
                return false;
            }

            if (string.IsNullOrEmpty(target) || message == null)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "SendPrivateMessage parameters must be non-null and not empty.");
                }
                return false;
            }

            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                {
                    { ChatParameterCode.UserId, target },
                    { ChatParameterCode.Message, message }
                };
            if (forwardAsWebhook)
            {
                parameters.Add(ChatParameterCode.WebFlags, (byte)0x1);
            }

            return this.chatPeer.SendOperation(ChatOperationCode.SendPrivate, parameters, new SendOptions() { Reliability = reliable, Encrypt = encrypt });
        }

        /// <summary>设置用户的状态（预定义或自定义）以及可选的消息。</summary>
        /// <remarks>
        /// 预定义的状态值可以在 ChatUserStatus 类中找到。
        /// 状态 ChatUserStatus.Invisible 将使您对所有人离线，并且不发送任何消息。
        ///
        /// 您可以在状态整数值中设置自定义值。除了预配置的值之外，
        /// 所有状态都将被视为可见和在线。否则，没有人会看到自定义状态。
        ///
        /// 消息对象可以是 Photon 可以序列化的任何内容，包括（但不限于）
        /// Hashtable、object[] 和 string。此值由您自己的约定定义。
        /// </remarks>
        /// <param name="status">预定义状态在 ChatUserStatus 类中。其他值可以随意使用。</param>
        /// <param name="message">可选的字符串消息或 null。</param>
        /// <param name="skipMessage">如果为 true，则忽略该消息。它可以为 null，但不会替换任何当前消息。</param>
        /// <returns>如果操作在服务器上被调用，则返回 True。</returns>
        private bool SetOnlineStatus(int status, object message, bool skipMessage)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "SetOnlineStatus called while not connected to front end server.");
                }
                return false;
            }

            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                {
                    { ChatParameterCode.Status, status },
                };

            if (skipMessage)
            {
                parameters[ChatParameterCode.SkipMessage] = true;
            }
            else
            {
                parameters[ChatParameterCode.Message] = message;
            }

            return this.chatPeer.SendOperation(ChatOperationCode.UpdateStatus, parameters, SendOptions.SendReliable);
        }

        /// <summary>设置用户的状态而不更改您的状态消息。</summary>
        /// <remarks>
        /// 预定义的状态值可以在 ChatUserStatus 类中找到。
        /// 状态 ChatUserStatus.Invisible 将使您对所有人离线，并且不发送任何消息。
        ///
        /// 您可以在状态整数值中设置自定义值。除了预配置的值之外，
        /// 所有状态都将被视为可见和在线。否则，没有人会看到自定义状态。
        ///
        /// 此重载不会更改已设置的消息。
        /// </remarks>
        /// <param name="status">预定义状态在 ChatUserStatus 类中。其他值可以随意使用。</param>
        /// <returns>如果操作在服务器上被调用，则返回 True。</returns>
        public bool SetOnlineStatus(int status)
        {
            return this.SetOnlineStatus(status, null, true);
        }

        /// <summary>设置用户的状态而不更改您的状态消息。</summary>
        /// <remarks>
        /// 预定义的状态值可以在 ChatUserStatus 类中找到。
        /// 状态 ChatUserStatus.Invisible 将使您对所有人离线，并且不发送任何消息。
        ///
        /// 您可以在状态整数值中设置自定义值。除了预配置的值之外，
        /// 所有状态都将被视为可见和在线。否则，没有人会看到自定义状态。
        ///
        /// 消息对象可以是 Photon 可以序列化的任何内容，包括（但不限于）
        /// Hashtable、object[] 和 string。此值由您自己的约定定义。
        /// </remarks>
        /// <param name="status">预定义状态在 ChatUserStatus 类中。其他值可以随意使用。</param>
        /// <param name="message">同时设置您的朋友可以获取的状态消息。</param>
        /// <returns>如果操作在服务器上被调用，则返回 True。</returns>
        public bool SetOnlineStatus(int status, object message)
        {
            return this.SetOnlineStatus(status, message, false);
        }

        /// <summary>
        /// 将好友添加到聊天服务器上的列表中，该服务器将为您发送这些好友的状态更新。
        /// </summary>
        /// <remarks>
        /// AddFriends 和 RemoveFriends 使客户端能够在 Photon Chat 服务器中
        /// 处理他们的好友列表。将用户添加到您的好友列表中可以让您访问
        /// 他们当前的在线状态（以及您的客户端在其中设置的任何信息）。
        ///
        /// 每个用户可以设置一个在线状态，由整数和任意
        /// （可序列化）对象组成。该对象可以是 null、Hashtable、object[] 或
        /// Photon 可以序列化的任何其他内容。
        ///
        /// 状态会自动发布给好友（任何使用 AddFriends 设置了您用户 ID 的人）。
        ///
        /// Photon 在聊天客户端断开连接时会刷新好友列表，因此每次都必须
        /// 重新设置。如果您的社区 API 已经可以访问在线状态，
        /// 您可以在 AddFriends 中过滤并设置在线好友。
        ///
        /// 实际的好友关系不是持久的，必须存储在 Photon 之外。
        /// </remarks>
        /// <param name="friends">好友 userId 的数组。</param>
        /// <returns>操作是否可以发送。</returns>
        public bool AddFriends(string[] friends)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "AddFriends called while not connected to front end server.");
                }
                return false;
            }

            if (friends == null || friends.Length == 0)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "AddFriends can't be called for empty or null list.");
                }
                return false;
            }
            if (friends.Length > FriendRequestListMax)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "AddFriends max list size exceeded: " + friends.Length + " > " + FriendRequestListMax);
                }
                return false;
            }

            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                {
                    { ChatParameterCode.Friends, friends },
                };

            return this.chatPeer.SendOperation(ChatOperationCode.AddFriends, parameters, SendOptions.SendReliable);
        }

        /// <summary>
        /// 从聊天服务器的列表中移除提供的条目，并停止其状态更新。
        /// </summary>
        /// <remarks>
        /// Photon 在聊天客户端断开连接时会刷新好友列表。除非您想
        /// 移除单个条目，否则不需要使用 RemoveFriends。
        ///
        /// AddFriends 和 RemoveFriends 使客户端能够在 Photon Chat 服务器中
        /// 处理他们的好友列表。将用户添加到您的好友列表中可以让您访问
        /// 他们当前的在线状态（以及您的客户端在其中设置的任何信息）。
        ///
        /// 每个用户可以设置一个在线状态，由整数和任意
        /// （可序列化）对象组成。该对象可以是 null、Hashtable、object[] 或
        /// Photon 可以序列化的任何其他内容。
        ///
        /// 状态会自动发布给好友（任何使用 AddFriends 设置了您用户 ID 的人）。
        ///
        /// Photon 在聊天客户端断开连接时会刷新好友列表，因此每次都必须
        /// 重新设置。如果您的社区 API 已经可以访问在线状态，
        /// 您可以在 AddFriends 中过滤并设置在线好友。
        ///
        /// 实际的好友关系不是持久的，必须存储在 Photon 之外。
        ///
        /// AddFriends 和 RemoveFriends 使客户端能够在 Photon Chat 服务器中
        /// 处理他们的好友列表。将用户添加到您的好友列表中可以让您访问
        /// 他们当前的在线状态（以及您的客户端在其中设置的任何信息）。
        ///
        /// 每个用户可以设置一个在线状态，由整数和任意
        /// （可序列化）对象组成。该对象可以是 null、Hashtable、object[] 或
        /// Photon 可以序列化的任何其他内容。
        ///
        /// 状态会自动发布给好友（任何使用 AddFriends 设置了您用户 ID 的人）。
        ///
        ///
        /// 实际的好友关系不是持久的，必须存储在 Photon 之外。
        /// </remarks>
        /// <param name="friends">好友 userId 的数组。</param>
        /// <returns>操作是否可以发送。</returns>
        public bool RemoveFriends(string[] friends)
        {
            if (!this.CanChat)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "RemoveFriends called while not connected to front end server.");
                }
                return false;
            }

            if (friends == null || friends.Length == 0)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "RemoveFriends can't be called for empty or null list.");
                }
                return false;
            }
            if (friends.Length > FriendRequestListMax)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "RemoveFriends max list size exceeded: " + friends.Length + " > " + FriendRequestListMax);
                }
                return false;
            }

            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                {
                    { ChatParameterCode.Friends, friends },
                };

            return this.chatPeer.SendOperation(ChatOperationCode.RemoveFriends, parameters, SendOptions.SendReliable);
        }

        /// <summary>
        /// 获取此客户端与另一个用户之间聊天的（本地使用的）频道名称。
        /// </summary>
        /// <param name="userName">远程用户的名称或 UserId。</param>
        /// <returns>私有频道的（本地使用的）频道名称。</returns>
        /// <remarks>不要订阅此频道。
        /// 私有频道不需要显式订阅。
        /// 主要用于调试目的。</remarks>
        public string GetPrivateChannelNameByUser(string userName)
        {
            return string.Format("{0}:{1}", this.UserId, userName);
        }

        /// <summary>
        /// 按名称简化访问私有或公共频道。
        /// </summary>
        /// <param name="channelName">要获取的频道名称。对于私有频道，频道名称由两个用户的名称组成。</param>
        /// <param name="isPrivate">定义您期望的是私有频道还是公共频道。</param>
        /// <param name="channel">输出参数为您提供找到的频道（如果有的话）。</param>
        /// <returns>如果找到了频道，则返回 True。</returns>
        /// <remarks>公共频道仅在订阅后才存在。
        /// 私有频道仅在至少与目标用户私下交换了一条消息后才存在。</remarks>
        public bool TryGetChannel(string channelName, bool isPrivate, out ChatChannel channel)
        {
            if (!isPrivate)
            {
                return this.PublicChannels.TryGetValue(channelName, out channel);
            }
            else
            {
                return this.PrivateChannels.TryGetValue(channelName, out channel);
            }
        }

        /// <summary>
        /// 按名称简化访问所有频道。先检查公共频道，再检查私有频道。
        /// </summary>
        /// <param name="channelName">要获取的频道名称。</param>
        /// <param name="channel">输出参数为您提供找到的频道（如果有的话）。</param>
        /// <returns>如果找到了频道，则返回 True。</returns>
        /// <remarks>公共频道仅在订阅后才存在。
        /// 私有频道仅在至少与目标用户私下交换了一条消息后才存在。</remarks>
        public bool TryGetChannel(string channelName, out ChatChannel channel)
        {
            bool found = false;
            found = this.PublicChannels.TryGetValue(channelName, out channel);
            if (found) return true;

            found = this.PrivateChannels.TryGetValue(channelName, out channel);
            return found;
        }

        /// <summary>
        /// 按目标用户简化访问私有频道。
        /// </summary>
        /// <param name="userId">私有频道中目标用户的 UserId。</param>
        /// <param name="channel">输出参数为您提供找到的频道（如果有的话）。</param>
        /// <returns>如果找到了频道，则返回 True。</returns>
        public bool TryGetPrivateChannelByUser(string userId, out ChatChannel channel)
        {
            channel = null;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }
            string channelName = this.GetPrivateChannelNameByUser(userId);
            return this.TryGetChannel(channelName, true, out channel);
        }

        /// <summary>
        /// 设置库提供的调试输出的级别（和数量）。
        /// </summary>
        /// <remarks>
        /// 这会影响对 IChatClientListener.DebugReturn 的回调。
        /// 默认级别：Error。
        /// </remarks>
        public DebugLevel DebugOut
        {
            set { this.chatPeer.DebugOut = value; }
            get { return this.chatPeer.DebugOut; }
        }

        #region Private methods area

        #region IPhotonPeerListener implementation

        void IPhotonPeerListener.DebugReturn(DebugLevel level, string message)
        {
            this.listener.DebugReturn(level, message);
        }

        void IPhotonPeerListener.OnEvent(EventData eventData)
        {
            switch (eventData.Code)
            {
                case ChatEventCode.ChatMessages:
                    this.HandleChatMessagesEvent(eventData);
                    break;
                case ChatEventCode.PrivateMessage:
                    this.HandlePrivateMessageEvent(eventData);
                    break;
                case ChatEventCode.StatusUpdate:
                    this.HandleStatusUpdate(eventData);
                    break;
                case ChatEventCode.Subscribe:
                    this.HandleSubscribeEvent(eventData);
                    break;
                case ChatEventCode.Unsubscribe:
                    this.HandleUnsubscribeEvent(eventData);
                    break;
                case ChatEventCode.UserSubscribed:
                    this.HandleUserSubscribedEvent(eventData);
                    break;
                case ChatEventCode.UserUnsubscribed:
                    this.HandleUserUnsubscribedEvent(eventData);
                    break;
#if CHAT_EXTENDED
                case ChatEventCode.PropertiesChanged:
                    this.HandlePropertiesChanged(eventData);
                    break;
                case ChatEventCode.ErrorInfo:
                    this.HandleErrorInfoEvent(eventData);
                    break;
#endif
            }
        }

        void IPhotonPeerListener.OnOperationResponse(OperationResponse operationResponse)
        {
            switch (operationResponse.OperationCode)
            {
                case (byte)ChatOperationCode.Authenticate:
                    this.HandleAuthResponse(operationResponse);
                    break;

                // 以下操作通常不返回有用的数据，且没有错误。
                case (byte)ChatOperationCode.Subscribe:
                case (byte)ChatOperationCode.Unsubscribe:
                case (byte)ChatOperationCode.Publish:
                case (byte)ChatOperationCode.SendPrivate:
                default:
                    if ((operationResponse.ReturnCode != 0) && (this.DebugOut >= DebugLevel.ERROR))
                    {
                        if (operationResponse.ReturnCode == -2)
                        {
                            this.listener.DebugReturn(DebugLevel.ERROR, string.Format("Chat Operation {0} unknown on server. Check your AppId and make sure it's for a Chat application.", operationResponse.OperationCode));
                        }
                        else
                        {
                            this.listener.DebugReturn(DebugLevel.ERROR, string.Format("Chat Operation {0} failed (Code: {1}). Debug Message: {2}", operationResponse.OperationCode, operationResponse.ReturnCode, operationResponse.DebugMessage));
                        }
                    }
                    break;
            }
        }

        void IPhotonPeerListener.OnStatusChanged(StatusCode statusCode)
        {
            switch (statusCode)
            {
                case StatusCode.Connect:
                    if (!this.chatPeer.IsProtocolSecure)
                    {
                        if (!this.chatPeer.EstablishEncryption())
                        {
                            if (this.DebugOut >= DebugLevel.ERROR)
                            {
                                this.listener.DebugReturn(DebugLevel.ERROR, "Error establishing encryption");
                            }
                        }
                    }
                    else
                    {
                        this.TryAuthenticateOnNameServer();
                    }

                    if (this.State == ChatState.ConnectingToNameServer)
                    {
                        this.State = ChatState.ConnectedToNameServer;
                        this.listener.OnChatStateChange(this.State);
                    }
                    else if (this.State == ChatState.ConnectingToFrontEnd)
                    {
                        if (!this.AuthenticateOnFrontEnd())
                        {
                            if (this.DebugOut >= DebugLevel.ERROR)
                            {
                                this.listener.DebugReturn(DebugLevel.ERROR, string.Format("Error authenticating on frontend! Check log output, AuthValues and if you're connected. State: {0}", this.State));
                            }
                        }
                    }
                    break;
                case StatusCode.EncryptionEstablished:
                    // 一旦加密可用，客户端应该发送一个（安全的）身份验证。它包括 AppId（用于在 Photon Cloud 上标识您的应用）
                    this.TryAuthenticateOnNameServer();
                    break;
                case StatusCode.Disconnect:
                    switch (this.State)
                    {
                        case ChatState.ConnectWithFallbackProtocol:
                            this.EnableProtocolFallback = false;        // 客户端只做一次回退
                            this.chatPeer.NameServerPortOverride = 0;   // 仅重置 Peer 中的值（因为我们更改了协议，端口也必须更改）
                            this.chatPeer.TransportProtocol = (this.chatPeer.TransportProtocol == ConnectionProtocol.Tcp) ? ConnectionProtocol.Udp : ConnectionProtocol.Tcp;
                            this.Connect(this.AppId, this.AppVersion, null);

                            // 客户端现在必须返回，而不是 break，以避免对断开连接调用进行进一步处理
                            return;

                        case ChatState.Authenticated:
                            this.ConnectToFrontEnd();
                            // 客户端在身份验证后从名称服务器断开连接
                            // 以切换到前端
                            return;
                        case ChatState.Disconnecting:
                            // 预期的断开连接
                            break;
                        default:
                            // 意外的断开连接，我们记录警告和堆栈跟踪
                            string stacktrace = string.Empty;
#if DEBUG && !NETFX_CORE
                            stacktrace = new System.Diagnostics.StackTrace(true).ToString();
#endif
                            this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Got a unexpected Disconnect in ChatState: {0}. Server: {1} Trace: {2}", this.State, this.chatPeer.ServerAddress, stacktrace));
                            break;
                    }
                    if (this.AuthValues != null)
                    {
                        this.AuthValues.Token = null; // 离开服务器时，使密钥无效（但不使身份验证值无效）
                    }
                    this.State = ChatState.Disconnected;
                    this.listener.OnChatStateChange(ChatState.Disconnected);
                    this.listener.OnDisconnected();
                    break;
                case StatusCode.DisconnectByServerUserLimit:
                    this.listener.DebugReturn(DebugLevel.ERROR, "This connection was rejected due to the apps CCU limit.");
                    this.Disconnect(ChatDisconnectCause.MaxCcuReached);
                    break;
                case StatusCode.ExceptionOnConnect:
                case StatusCode.SecurityExceptionOnConnect:
                case StatusCode.EncryptionFailedToEstablish:
                    this.DisconnectedCause = ChatDisconnectCause.ExceptionOnConnect;

                    // 如果启用，客户端可以尝试使用另一种网络协议连接，以检查该协议是否能连接
                    if (this.EnableProtocolFallback && this.State == ChatState.ConnectingToNameServer)
                    {
                        this.State = ChatState.ConnectWithFallbackProtocol;
                    }
                    else
                    {
                        this.Disconnect(ChatDisconnectCause.ExceptionOnConnect);
                    }

                    break;
                case StatusCode.Exception:
                case StatusCode.ExceptionOnReceive:
                    this.Disconnect(ChatDisconnectCause.Exception);
                    break;
                case StatusCode.DisconnectByServerTimeout:
                    this.Disconnect(ChatDisconnectCause.ServerTimeout);
                    break;
                case StatusCode.DisconnectByServerLogic:
                    this.Disconnect(ChatDisconnectCause.DisconnectByServerLogic);
                    break;
                case StatusCode.DisconnectByServerReasonUnknown:
                    this.Disconnect(ChatDisconnectCause.DisconnectByServerReasonUnknown);
                    break;
                case StatusCode.TimeoutDisconnect:
                    this.DisconnectedCause = ChatDisconnectCause.ClientTimeout;

                    // 如果启用，客户端可以尝试使用另一种网络协议连接，以检查该协议是否能连接
                    if (this.EnableProtocolFallback && this.State == ChatState.ConnectingToNameServer)
                    {
                        this.State = ChatState.ConnectWithFallbackProtocol;
                    }
                    else
                    {
                        this.Disconnect(ChatDisconnectCause.ClientTimeout);
                    }
                    break;
            }
        }

#if SDK_V4
        void IPhotonPeerListener.OnMessage(object msg)
        {
            string channelName = null;
            var receivedBytes = (byte[])msg;
            var channelId = BitConverter.ToInt32(receivedBytes, 0);
            var messageBytes = new byte[receivedBytes.Length - 4];
            Array.Copy(receivedBytes, 4, messageBytes, 0, receivedBytes.Length - 4);

            foreach (var channel in this.PublicChannels)
            {
                if (channel.Value.ChannelID == channelId)
                {
                    channelName = channel.Key;
                    break;
                }
            }

            if (channelName != null)
            {
                this.listener.DebugReturn(DebugLevel.ALL, string.Format("got OnMessage in channel {0}", channelName));
            }
            else
            {
                this.listener.DebugReturn(DebugLevel.WARNING, string.Format("got OnMessage in unknown channel {0}", channelId));
            }

            this.listener.OnReceiveBroadcastMessage(channelName, messageBytes);
        }
#endif

        #endregion

        private void TryAuthenticateOnNameServer()
        {
            if (!this.didAuthenticate)
            {
                this.didAuthenticate = this.chatPeer.AuthenticateOnNameServer(this.AppId, this.AppVersion, this.ChatRegion, this.AuthValues);
                if (!this.didAuthenticate)
                {
                    if (this.DebugOut >= DebugLevel.ERROR)
                    {
                        this.listener.DebugReturn(DebugLevel.ERROR, string.Format("Error calling OpAuthenticate! Did not work on NameServer. Check log output, AuthValues and if you're connected. State: {0}", this.State));
                    }
                }
            }
        }

        private bool SendChannelOperation(string[] channels, byte operation, int historyLength)
        {
            Dictionary<byte, object> opParameters = new Dictionary<byte, object> { { (byte)ChatParameterCode.Channels, channels } };

            if (historyLength != 0)
            {
                opParameters.Add((byte)ChatParameterCode.HistoryLength, historyLength);
            }

            return this.chatPeer.SendOperation(operation, opParameters, SendOptions.SendReliable);
        }

        private void HandlePrivateMessageEvent(EventData eventData)
        {
            //Console.WriteLine(SupportClass.DictionaryToString(eventData.Parameters));

            object message = (object)eventData.Parameters[(byte)ChatParameterCode.Message];
            string sender = (string)eventData.Parameters[(byte)ChatParameterCode.Sender];
            int msgId = (int)eventData.Parameters[ChatParameterCode.MsgId];

            string channelName;
            if (this.UserId != null && this.UserId.Equals(sender))
            {
                string target = (string)eventData.Parameters[(byte)ChatParameterCode.UserId];
                channelName = this.GetPrivateChannelNameByUser(target);
            }
            else
            {
                channelName = this.GetPrivateChannelNameByUser(sender);
            }

            ChatChannel channel;
            if (!this.PrivateChannels.TryGetValue(channelName, out channel))
            {
                channel = new ChatChannel(channelName);
                channel.IsPrivate = true;
                channel.MessageLimit = this.MessageLimit;
                this.PrivateChannels.Add(channel.Name, channel);
            }

            channel.Add(sender, message, msgId);
            this.listener.OnPrivateMessage(sender, message, channelName);
        }

        private void HandleChatMessagesEvent(EventData eventData)
        {
            object[] messages = (object[])eventData.Parameters[(byte)ChatParameterCode.Messages];
            string[] senders = (string[])eventData.Parameters[(byte)ChatParameterCode.Senders];
            string channelName = (string)eventData.Parameters[(byte)ChatParameterCode.Channel];
            int lastMsgId = (int)eventData.Parameters[ChatParameterCode.MsgId];

            ChatChannel channel;
            if (!this.PublicChannels.TryGetValue(channelName, out channel))
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "Channel " + channelName + " for incoming message event not found.");
                }
                return;
            }

            channel.Add(senders, messages, lastMsgId);
            this.listener.OnGetMessages(channelName, senders, messages);
        }

        private void HandleSubscribeEvent(EventData eventData)
        {
            string[] channelsInResponse = (string[])eventData.Parameters[ChatParameterCode.Channels];
            bool[] results = (bool[])eventData.Parameters[ChatParameterCode.SubscribeResults];
            for (int i = 0; i < channelsInResponse.Length; i++)
            {
                if (results[i])
                {
                    string channelName = channelsInResponse[i];
                    ChatChannel channel;
                    if (!this.PublicChannels.TryGetValue(channelName, out channel))
                    {
                        channel = new ChatChannel(channelName);
                        channel.MessageLimit = this.MessageLimit;
                        this.PublicChannels.Add(channel.Name, channel);
                    }
                    object temp;
                    if (eventData.Parameters.TryGetValue(ChatParameterCode.Properties, out temp))
                    {
                        Dictionary<object, object> channelProperties = temp as Dictionary<object, object>;
                        channel.ReadChannelProperties(channelProperties);
                    }
                    if (channel.PublishSubscribers) // 或者也许删除检查并始终添加？
                    {
                        channel.Subscribers.Add(this.UserId);
                    }
                    if (eventData.Parameters.TryGetValue(ChatParameterCode.ChannelSubscribers, out temp))
                    {
                        string[] subscribers = temp as string[];
                        channel.AddSubscribers(subscribers);
                    }
#if CHAT_EXTENDED
                    if (eventData.Parameters.TryGetValue(ChatParameterCode.UserProperties, out temp))
                    {
                        Dictionary<string, Dictionary<object, object>> userProperties = temp as Dictionary<string, Dictionary<object, object>>;
                        foreach (var pair in userProperties)
                        {
                            channel.ReadUserProperties(pair.Key, pair.Value);
                        }
                    }
#endif
                }
            }

            this.listener.OnSubscribed(channelsInResponse, results);
        }


        private void HandleUnsubscribeEvent(EventData eventData)
        {
            string[] channelsInRequest = (string[])eventData[ChatParameterCode.Channels];
            for (int i = 0; i < channelsInRequest.Length; i++)
            {
                string channelName = channelsInRequest[i];
                this.PublicChannels.Remove(channelName);
                this.PublicChannelsUnsubscribing.Remove(channelName);
            }

            this.listener.OnUnsubscribed(channelsInRequest);
        }

        private void HandleAuthResponse(OperationResponse operationResponse)
        {
            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.listener.DebugReturn(DebugLevel.INFO, operationResponse.ToStringFull() + " on: " + this.chatPeer.NameServerAddress);
            }

            if (operationResponse.ReturnCode == 0)
            {
                if (this.State == ChatState.ConnectedToNameServer)
                {
                    this.State = ChatState.Authenticated;
                    this.listener.OnChatStateChange(this.State);

                    if (operationResponse.Parameters.ContainsKey(ParameterCode.Secret))
                    {
                        if (this.AuthValues == null)
                        {
                            this.AuthValues = new AuthenticationValues();
                        }
                        this.AuthValues.Token = operationResponse[ParameterCode.Secret] as string;

                        this.FrontendAddress = (string)operationResponse[ParameterCode.Address];

                        // 我们断开连接，状态处理程序开始连接到前端
                        this.chatPeer.Disconnect();
                    }
                    else
                    {
                        if (this.DebugOut >= DebugLevel.ERROR)
                        {
                            this.listener.DebugReturn(DebugLevel.ERROR, "No secret in authentication response.");
                        }
                    }
                    if (operationResponse.Parameters.ContainsKey(ParameterCode.UserId))
                    {
                        string incomingId = operationResponse.Parameters[ParameterCode.UserId] as string;
                        if (!string.IsNullOrEmpty(incomingId))
                        {
                            this.UserId = incomingId;
                            this.listener.DebugReturn(DebugLevel.INFO, string.Format("Received your UserID from server. Updating local value to: {0}", this.UserId));
                        }
                    }
                }
                else if (this.State == ChatState.ConnectingToFrontEnd)
                {
                    this.State = ChatState.ConnectedToFrontEnd;
                    this.listener.OnChatStateChange(this.State);
                    this.listener.OnConnected();
                    if (statusToSetWhenConnected.HasValue)
                    {
                        SetOnlineStatus(statusToSetWhenConnected.Value, messageToSetWhenConnected);
                        statusToSetWhenConnected = null;
                    }
                }
            }
            else
            {
                //this.listener.DebugReturn(DebugLevel.INFO, operationResponse.ToStringFull() + " NS: " + this.NameServerAddress + " FrontEnd: " + this.frontEndAddress);

                switch (operationResponse.ReturnCode)
                {
                    case ErrorCode.InvalidAuthentication:
                        this.DisconnectedCause = ChatDisconnectCause.InvalidAuthentication;
                        break;
                    case ErrorCode.CustomAuthenticationFailed:
                        this.DisconnectedCause = ChatDisconnectCause.CustomAuthenticationFailed;
                        break;
                    case ErrorCode.InvalidRegion:
                        this.DisconnectedCause = ChatDisconnectCause.InvalidRegion;
                        break;
                    case ErrorCode.MaxCcuReached:
                        this.DisconnectedCause = ChatDisconnectCause.MaxCcuReached;
                        break;
                    case ErrorCode.OperationNotAllowedInCurrentState:
                        this.DisconnectedCause = ChatDisconnectCause.OperationNotAllowedInCurrentState;
                        break;
                    case ErrorCode.AuthenticationTicketExpired:
                        this.DisconnectedCause = ChatDisconnectCause.AuthenticationTicketExpired;
                        break;
                }

                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, string.Format("{0} ClientState: {1} ServerAddress: {2}", operationResponse.ToStringFull(), this.State, this.chatPeer.ServerAddress));
                }


                this.Disconnect(this.DisconnectedCause);
            }
        }

        private void HandleStatusUpdate(EventData eventData)
        {
            string user = (string)eventData.Parameters[ChatParameterCode.Sender];
            int status = (int)eventData.Parameters[ChatParameterCode.Status];

            object message = null;
            bool gotMessage = eventData.Parameters.ContainsKey(ChatParameterCode.Message);
            if (gotMessage)
            {
                message = eventData.Parameters[ChatParameterCode.Message];
            }

            this.listener.OnStatusUpdate(user, status, gotMessage, message);
        }

        private bool ConnectToFrontEnd()
        {
            this.State = ChatState.ConnectingToFrontEnd;

            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.listener.DebugReturn(DebugLevel.INFO, "Connecting to frontend " + this.FrontendAddress);
            }

#if UNITY_WEBGL
            if (this.TransportProtocol == ConnectionProtocol.Tcp || this.TransportProtocol == ConnectionProtocol.Udp)
            {
                this.listener.DebugReturn(DebugLevel.WARNING, "WebGL requires WebSockets. Switching TransportProtocol to WebSocketSecure.");
                this.TransportProtocol = ConnectionProtocol.WebSocketSecure;
            }
#endif

            if (!this.chatPeer.Connect(this.FrontendAddress, ChatAppName))
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, string.Format("Connecting to frontend {0} failed.", this.FrontendAddress));
                }
                return false;
            }

            return true;
        }

        private bool AuthenticateOnFrontEnd()
        {
            if (this.AuthValues != null)
            {
                if (this.AuthValues.Token == null)
                {
                    if (this.DebugOut >= DebugLevel.ERROR)
                    {
                        this.listener.DebugReturn(DebugLevel.ERROR, "Can't authenticate on front end server. Secret (AuthValues.Token) is not set");
                    }
                    return false;
                }
                else
                {
                    Dictionary<byte, object> opParameters = new Dictionary<byte, object> { { (byte)ChatParameterCode.Secret, this.AuthValues.Token } };
                    if (this.PrivateChatHistoryLength > -1)
                    {
                        opParameters[(byte)ChatParameterCode.HistoryLength] = this.PrivateChatHistoryLength;
                    }

                    return this.chatPeer.SendOperation(ChatOperationCode.Authenticate, opParameters, SendOptions.SendReliable);
                }
            }
            else
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "Can't authenticate on front end server. Authentication Values are not set");
                }
                return false;
            }
        }

        private void HandleUserUnsubscribedEvent(EventData eventData)
        {
            string channelName = eventData.Parameters[ChatParameterCode.Channel] as string;
            string userId = eventData.Parameters[ChatParameterCode.UserId] as string;
            ChatChannel channel;
            if (this.PublicChannels.TryGetValue(channelName, out channel))
            {
                if (!channel.PublishSubscribers)
                {
                    if (this.DebugOut >= DebugLevel.WARNING)
                    {
                        this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\" for incoming UserUnsubscribed (\"{1}\") event does not have PublishSubscribers enabled.", channelName, userId));
                    }
                }
                if (!channel.Subscribers.Remove(userId)) // 未找到用户！
                {
                    if (this.DebugOut >= DebugLevel.WARNING)
                    {
                        this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\" does not contain unsubscribed user \"{1}\".", channelName, userId));
                    }
                }
            }
            else
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\" not found for incoming UserUnsubscribed (\"{1}\") event.", channelName, userId));
                }
            }
            this.listener.OnUserUnsubscribed(channelName, userId);
        }

        private void HandleUserSubscribedEvent(EventData eventData)
        {
            //TODO: 处理用户属性！

            string channelName = eventData.Parameters[ChatParameterCode.Channel] as string;
            string userId = eventData.Parameters[ChatParameterCode.UserId] as string;
            ChatChannel channel;
            if (this.PublicChannels.TryGetValue(channelName, out channel))
            {
                if (!channel.PublishSubscribers)
                {
                    if (this.DebugOut >= DebugLevel.WARNING)
                    {
                        this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\" for incoming UserSubscribed (\"{1}\") event does not have PublishSubscribers enabled.", channelName, userId));
                    }
                }
                if (!channel.Subscribers.Add(userId)) // 用户复活了？
                {
                    if (this.DebugOut >= DebugLevel.WARNING)
                    {
                        this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\" already contains newly subscribed user \"{1}\".", channelName, userId));
                    }
                }
                else if (channel.MaxSubscribers > 0 && channel.Subscribers.Count > channel.MaxSubscribers)
                {
                    if (this.DebugOut >= DebugLevel.WARNING)
                    {
                        this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\"'s MaxSubscribers exceeded. count={1} > MaxSubscribers={2}.", channelName, channel.Subscribers.Count, channel.MaxSubscribers));
                    }
                }
            }
            else
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel \"{0}\" not found for incoming UserSubscribed (\"{1}\") event.", channelName, userId));
                }
            }
            this.listener.OnUserSubscribed(channelName, userId);
        }

        #endregion

        /// <summary>
        /// 订阅单个频道，并可选地在创建频道时设置其 WellKnown 频道属性。
        /// </summary>
        /// <param name="channel">要订阅的频道名称</param>
        /// <param name="lastMsgId">在重新订阅时，来自此频道的最后收到消息的 ID，仅用于接收错过的消息，默认为 0</param>
        /// <param name="messagesFromHistory">要从历史记录中接收多少错过的消息，默认为 -1（可用的历史记录）。0 将获取零条消息。正值受服务器端限制的限制。</param>
        /// <param name="creationOptions">在要订阅的频道将被创建时使用的选项。</param>
        /// <returns></returns>
        public bool Subscribe(string channel, int lastMsgId = 0, int messagesFromHistory = -1, ChannelCreationOptions creationOptions = null)
        {
            if (creationOptions == null)
            {
                creationOptions = ChannelCreationOptions.Default;
            }
            int maxSubscribers = creationOptions.MaxSubscribers;
            bool publishSubscribers = creationOptions.PublishSubscribers;
            if (maxSubscribers < 0)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "Cannot set MaxSubscribers < 0.");
                }
                return false;
            }
            if (lastMsgId < 0)
            {
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.listener.DebugReturn(DebugLevel.ERROR, "lastMsgId cannot be < 0.");
                }
                return false;
            }
            if (messagesFromHistory < -1)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "messagesFromHistory < -1, setting it to -1");
                }
                messagesFromHistory = -1;
            }
            if (lastMsgId > 0 && messagesFromHistory == 0)
            {
                if (this.DebugOut >= DebugLevel.WARNING)
                {
                    this.listener.DebugReturn(DebugLevel.WARNING, "lastMsgId will be ignored because messagesFromHistory == 0");
                }
                lastMsgId = 0;
            }
            Dictionary<object, object> properties = null;
            if (publishSubscribers)
            {
                if (maxSubscribers > DefaultMaxSubscribers)
                {
                    if (this.DebugOut >= DebugLevel.ERROR)
                    {
                        this.listener.DebugReturn(DebugLevel.ERROR,
                            string.Format("Cannot set MaxSubscribers > {0} when PublishSubscribers == true.", DefaultMaxSubscribers));
                    }
                    return false;
                }
                properties = new Dictionary<object, object>();
                properties[ChannelWellKnownProperties.PublishSubscribers] = true;
            }
            if (maxSubscribers > 0)
            {
                if (properties == null)
                {
                    properties = new Dictionary<object, object>();
                }
                properties[ChannelWellKnownProperties.MaxSubscribers] = maxSubscribers;
            }
#if CHAT_EXTENDED
            if (creationOptions.CustomProperties != null && creationOptions.CustomProperties.Count > 0)
            {
                foreach (var pair in creationOptions.CustomProperties)
                {
                    properties.Add(pair.Key, pair.Value);
                }
            }
#endif
            Dictionary<byte, object> opParameters = new Dictionary<byte, object> { { ChatParameterCode.Channels, new[] { channel } } };
            if (messagesFromHistory != 0)
            {
                opParameters.Add(ChatParameterCode.HistoryLength, messagesFromHistory);
            }
            if (lastMsgId > 0)
            {
                opParameters.Add(ChatParameterCode.MsgIds, new[] { lastMsgId });
            }
            if (properties != null && properties.Count > 0)
            {
                opParameters.Add(ChatParameterCode.Properties, properties);
            }

            return this.chatPeer.SendOperation(ChatOperationCode.Subscribe, opParameters, SendOptions.SendReliable);
        }

#if CHAT_EXTENDED

        internal bool SetChannelProperties(string channelName, Dictionary<object, object> channelProperties, Dictionary<object, object> expectedProperties = null, bool httpForward = false)
        {
            if (!this.CanChat)
            {
                this.listener.DebugReturn(DebugLevel.ERROR, "SetChannelProperties called while not connected to front end server.");
                return false;
            }

            if (string.IsNullOrEmpty(channelName) || channelProperties == null || channelProperties.Count == 0)
            {
                this.listener.DebugReturn(DebugLevel.WARNING, "SetChannelProperties parameters must be non-null and not empty.");
                return false;
            }
            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                                                  {
                                                      { ChatParameterCode.Channel, channelName },
                                                      { ChatParameterCode.Properties, channelProperties },
                                                      { ChatParameterCode.Broadcast, true }
                                                  };
            if (httpForward)
            {
                parameters.Add(ChatParameterCode.WebFlags, HttpForwardWebFlag);
            }
            if (expectedProperties != null && expectedProperties.Count > 0)
            {
                parameters.Add(ChatParameterCode.ExpectedValues, expectedProperties);
            }
            return this.chatPeer.SendOperation(ChatOperationCode.SetProperties, parameters, SendOptions.SendReliable);
        }

        public bool SetCustomChannelProperties(string channelName, Dictionary<string, object> channelProperties, Dictionary<string, object> expectedProperties = null, bool httpForward = false)
        {
            if (channelProperties != null && channelProperties.Count > 0)
            {
                Dictionary<object, object> properties = new Dictionary<object, object>(channelProperties.Count);
                foreach (var pair in channelProperties)
                {
                    properties.Add(pair.Key, pair.Value);
                }
                Dictionary<object, object> expected = null;
                if (expectedProperties != null && expectedProperties.Count > 0)
                {
                    expected = new Dictionary<object, object>(expectedProperties.Count);
                    foreach (var pair in expectedProperties)
                    {
                        expected.Add(pair.Key, pair.Value);
                    }
                }
                return this.SetChannelProperties(channelName, properties, expected, httpForward);
            }
            return this.SetChannelProperties(channelName, null);
        }

        public bool SetCustomUserProperties(string channelName, string userId, Dictionary<string, object> userProperties, Dictionary<string, object> expectedProperties = null, bool httpForward = false)
        {
            if (userProperties != null && userProperties.Count > 0)
            {
                Dictionary<object, object> properties = new Dictionary<object, object>(userProperties.Count);
                foreach (var pair in userProperties)
                {
                    properties.Add(pair.Key, pair.Value);
                }
                Dictionary<object, object> expected = null;
                if (expectedProperties != null && expectedProperties.Count > 0)
                {
                    expected = new Dictionary<object, object>(expectedProperties.Count);
                    foreach (var pair in expectedProperties)
                    {
                        expected.Add(pair.Key, pair.Value);
                    }
                }
                return this.SetUserProperties(channelName, userId, properties, expected, httpForward);
            }
            return this.SetUserProperties(channelName, userId, null);
        }

        internal bool SetUserProperties(string channelName, string userId, Dictionary<object, object> channelProperties, Dictionary<object, object> expectedProperties = null, bool httpForward = false)
        {
            if (!this.CanChat)
            {
                this.listener.DebugReturn(DebugLevel.ERROR, "SetUserProperties called while not connected to front end server.");
                return false;
            }
            if (string.IsNullOrEmpty(channelName))
            {
                this.listener.DebugReturn(DebugLevel.WARNING, "SetUserProperties \"channelName\" parameter must be non-null and not empty.");
                return false;
            }
            if (channelProperties == null || channelProperties.Count == 0)
            {
                this.listener.DebugReturn(DebugLevel.WARNING, "SetUserProperties \"channelProperties\" parameter must be non-null and not empty.");
                return false;
            }
            if (string.IsNullOrEmpty(userId))
            {
                this.listener.DebugReturn(DebugLevel.WARNING, "SetUserProperties \"userId\" parameter must be non-null and not empty.");
                return false;
            }
            Dictionary<byte, object> parameters = new Dictionary<byte, object>
                                                  {
                                                      { ChatParameterCode.Channel, channelName },
                                                      { ChatParameterCode.Properties, channelProperties },
                                                      { ChatParameterCode.UserId, userId },
                                                      { ChatParameterCode.Broadcast, true }
                                                  };
            if (httpForward)
            {
                parameters.Add(ChatParameterCode.WebFlags, HttpForwardWebFlag);
            }
            if (expectedProperties != null && expectedProperties.Count > 0)
            {
                parameters.Add(ChatParameterCode.ExpectedValues, expectedProperties);
            }
            return this.chatPeer.SendOperation(ChatOperationCode.SetProperties, parameters, SendOptions.SendReliable);
        }

        private void HandlePropertiesChanged(EventData eventData)
        {
            string channelName = eventData.Parameters[ChatParameterCode.Channel] as string;
            ChatChannel channel;
            if (!this.PublicChannels.TryGetValue(channelName, out channel))
            {
                this.listener.DebugReturn(DebugLevel.WARNING, string.Format("Channel {0} for incoming ChannelPropertiesUpdated event not found.", channelName));
                return;
            }
            string senderId = eventData.Parameters[ChatParameterCode.Sender] as string;
            Dictionary<object, object> changedProperties = eventData.Parameters[ChatParameterCode.Properties] as Dictionary<object, object>;
            object temp;
            if (eventData.Parameters.TryGetValue(ChatParameterCode.UserId, out temp))
            {
                string targetUserId = temp as string;
                channel.ReadUserProperties(targetUserId, changedProperties);
                this.listener.OnUserPropertiesChanged(channelName, targetUserId, senderId, changedProperties);
            }
            else
            {
                channel.ReadChannelProperties(changedProperties);
                this.listener.OnChannelPropertiesChanged(channelName, senderId, changedProperties);
            }
        }

        private void HandleErrorInfoEvent(EventData eventData)
        {
            string channel = eventData.Parameters[ChatParameterCode.Channel] as string;
            string msg = eventData.Parameters[ChatParameterCode.DebugMessage] as string;
            object data = eventData.Parameters[ChatParameterCode.DebugData];
            this.listener.OnErrorInfo(channel, msg, data);
        }

#endif
    }
}
