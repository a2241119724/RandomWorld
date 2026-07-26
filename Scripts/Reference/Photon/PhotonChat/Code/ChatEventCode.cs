// ----------------------------------------------------------------------------------------------------------------------
// <summary>Photon Chat Api 使客户端能够连接到聊天服务器并与其他客户端通信。</summary>
// <remarks>ChatClient 是此 api 的主类。</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------

namespace Photon.Chat
{
    /// <summary>
    /// 封装 Photon Chat 事件中内部使用的常量。您通常不需要直接使用它们。
    /// </summary>
    public class ChatEventCode
    {
        /// <summary>(0) 在公共频道中发布的消息的事件代码。</summary>
        public const byte ChatMessages = 0;
        /// <summary>(1) 未使用。</summary>
        public const byte Users = 1;// 用户列表或用户列表的变更列表
        /// <summary>(2) 在私有频道中发布的消息的事件代码。</summary>
        public const byte PrivateMessage = 2;
        /// <summary>(3) 未使用。</summary>
        public const byte FriendsList = 3;
        /// <summary>(4) 状态更新的事件代码。</summary>
        public const byte StatusUpdate = 4;
        /// <summary>(5) 订阅确认的事件代码。</summary>
        public const byte Subscribe = 5;
        /// <summary>(6) 取消订阅确认的事件代码。</summary>
        public const byte Unsubscribe = 6;
        /// <summary>(7) 属性更新的事件代码。</summary>
        public const byte PropertiesChanged = 7;

        /// <summary>(8) 新用户订阅启用了 <see cref="ChatChannel.PublishSubscribers"/> 的频道的事件代码。</summary>
        public const byte UserSubscribed = 8;
        /// <summary>(9) 用户从启用了 <see cref="ChatChannel.PublishSubscribers"/> 的频道取消订阅时的事件代码。</summary>
        public const byte UserUnsubscribed = 9;
        /// <summary>(10) 服务器向客户端发送错误时的事件代码。</summary>
        /// <remarks> 目前仅由 Chat WebHooks 使用。</remarks>
        public const byte ErrorInfo = 10;
    }
}
