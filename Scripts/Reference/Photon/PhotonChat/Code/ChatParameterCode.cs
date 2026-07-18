// ----------------------------------------------------------------------------------------------------------------------
// <summary>Photon Chat Api 使客户端能够连接到聊天服务器并与其他客户端通信。</summary>
// <remarks>ChatClient 是此 api 的主类。</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------

namespace Photon.Chat
{
    /// <summary>
    /// 封装 Photon Chat 中内部使用的参数代码（用于操作和事件）。您通常不需要直接使用它们。
    /// </summary>
    public class ChatParameterCode
    {
        /// <summary>(0) 聊天频道数组。</summary>
        public const byte Channels = 0;
        /// <summary>(1) 单个聊天频道的名称。</summary>
        public const byte Channel = 1;
        /// <summary>(2) 聊天消息数组。</summary>
        public const byte Messages = 2;
        /// <summary>(3) 单条聊天消息。</summary>
        public const byte Message = 3;
        /// <summary>(4) 发送聊天消息数组的用户的名称数组。</summary>
        public const byte Senders = 4;
        /// <summary>(5) 发送聊天消息的用户的名称。</summary>
        public const byte Sender = 5;
        /// <summary>(6) 未使用。</summary>
        public const byte ChannelUserCount = 6;
        /// <summary>(225) 要向其发送（私密）消息的用户的名称。</summary><remarks>该代码在 LoadBalancing 中使用，并在此处复制。</remarks>
        public const byte UserId = 225;
        /// <summary>(8) 消息的 ID。</summary>
        public const byte MsgId = 8;
        /// <summary>(9) 未使用。</summary>
        public const byte MsgIds = 9;
        /// <summary>(221) 用于识别授权用户的密钥令牌。</summary><remarks>该代码在 LoadBalancing 中使用，并在此处复制。</remarks>
        public const byte Secret = 221;
        /// <summary>(15) 订阅操作结果参数。一个 bool[]，包含每个频道的结果。</summary>
        public const byte SubscribeResults = 15;

        /// <summary>(10) 状态</summary>
        public const byte Status = 10;
        /// <summary>(11) 好友</summary>
        public const byte Friends = 11;
        /// <summary>(12) SkipMessage 在 SetOnlineStatus 中使用，如果为 true，则不会广播该消息。</summary>
        public const byte SkipMessage = 12;

        /// <summary>(14) 要从历史记录中获取的消息数量。0：无历史记录。1 及以上：历史记录中的消息数量。-1：所有历史记录。</summary>
        public const byte HistoryLength = 14;

        public const byte DebugMessage = 17;

        /// <summary>(21) WebFlags 对象，用于从客户端更改 webhooks 的行为。</summary>
        public const byte WebFlags = 21;

        /// <summary>(22) 频道或用户的 WellKnown 或自定义属性。</summary>
        /// <remarks>
        /// 在事件 <see cref="ChatEventCode.Subscribe"/> 中始终是频道属性，
        /// 在事件 <see cref="ChatEventCode.UserSubscribed"/> 中始终是用户属性，
        /// 在事件 <see cref="ChatEventCode.PropertiesChanged"/> 中，除非 <see cref="UserId"/> 参数值不为 null，否则为频道属性
        /// </remarks>
        public const byte Properties = 22;
        /// <summary>(23) 已订阅频道的用户的 UserId 数组。</summary>
        /// <remarks>在启用 PublishSubscribers 时用于 Subscribe 事件。
        /// 不包括刚刚订阅的本地用户。
        /// 最大长度为 (<see cref="ChatChannel.MaxSubscribers"/> - 1)。</remarks>
        public const byte ChannelSubscribers = 23;
        /// <summary>(24) 从 Chat WebHooks 的 ErrorInfo 事件中发送的可选数据。</summary>
        public const byte DebugData = 24;
        /// <summary>(25) 更改属性时用于"检查并交换"（CAS）的值代码。</summary>
        public const byte ExpectedValues = 25;
        /// <summary>(26) <see cref="ChatOperationCode.SetProperties"/> 方法的广播参数代码。</summary>
        public const byte Broadcast = 26;
        /// <summary>
        /// WellKnown 和自定义用户属性。
        /// </summary>
        /// <remarks>
        /// 仅在事件 <see cref="ChatEventCode.Subscribe"/> 中使用
        /// </remarks>
        public const byte UserProperties = 28;

        /// <summary>
        /// 生成的唯一可重用房间 ID
        /// </summary>
        public const byte UniqueRoomId = 29;
    }
}