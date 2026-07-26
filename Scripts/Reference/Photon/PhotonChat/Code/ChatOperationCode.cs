// ----------------------------------------------------------------------------------------------------------------------
// <summary>Photon Chat Api 使客户端能够连接到聊天服务器并与其他客户端通信。</summary>
// <remarks>ChatClient 是此 api 的主类。</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------

namespace Photon.Chat
{
    /// <summary>
    /// 封装 Photon Chat 中内部使用的操作代码。您通常不需要直接使用它们。
    /// </summary>
    public class ChatOperationCode
    {
        /// <summary>(230) 身份验证操作。</summary>
        public const byte Authenticate = 230;

        /// <summary>(0) 订阅聊天频道的操作。</summary>
        public const byte Subscribe = 0;
        /// <summary>(1) 取消订阅聊天频道的操作。</summary>
        public const byte Unsubscribe = 1;
        /// <summary>(2) 在聊天频道中发布消息的操作。</summary>
        public const byte Publish = 2;
        /// <summary>(3) 向其他用户发送私密消息的操作。</summary>
        public const byte SendPrivate = 3;

        /// <summary>(4) 尚未使用。</summary>
        public const byte ChannelHistory = 4;

        /// <summary>(5) 设置您（客户端）的状态。</summary>
        public const byte UpdateStatus = 5;
        /// <summary>(6) 将好友添加到应更新其状态的好友列表中。</summary>
        public const byte AddFriends = 6;
        /// <summary>(7) 从应更新其状态的好友列表中移除好友。</summary>
        public const byte RemoveFriends = 7;
        /// <summary>(8) 设置公共聊天频道或公共聊天频道中用户的属性的操作。</summary>
        public const byte SetProperties = 8;
    }
}