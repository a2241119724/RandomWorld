// ----------------------------------------------------------------------------------------------------------------------
// <summary>Photon Chat Api 使客户端能够连接到聊天服务器并与其他客户端通信。</summary>
// <remarks>ChatClient 是此 api 的主类。</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------

namespace Photon.Chat
{
    /// <summary>包含 SetOnlineStatus 常用的状态值。您可以定义自己的值。</summary>
    /// <remarks>
    /// 当"在线"（值为 2 及以上）时，状态消息将发送给好友列表中的任何人。
    ///
    /// 按照以下规则定义自定义在线状态值：
    /// 0：表示"离线"。当您未连接时将使用此值。在此状态下，没有状态消息。
    /// 1：表示"隐身"，对好友显示为"离线"。他们看到状态 0，没有消息，但您可以聊天。
    /// 2：以及任何更高的值将被视为"在线"。可以设置状态。
    /// </remarks>
    public static class ChatUserStatus
    {
        /// <summary>(0) 离线。</summary>
        public const int Offline = 0;
        /// <summary>(1) 对所有人隐身。不发送消息。</summary>
        public const int Invisible = 1;
        /// <summary>(2) 在线且可用。</summary>
        public const int Online = 2;
        /// <summary>(3) 在线但不可用。</summary>
        public const int Away = 3;
        /// <summary>(4) 请勿打扰。</summary>
        public const int DND = 4;
        /// <summary>(5) 正在寻找游戏/组队。当您想被邀请或进行匹配时可以使用。</summary>
        public const int LFG = 5;
        /// <summary>(6) 当在房间中、正在游戏时可以使用。</summary>
        public const int Playing = 6;
    }
}
