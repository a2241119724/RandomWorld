// ----------------------------------------------------------------------------------------------------------------------
// <summary>Photon Chat Api 使客户端能够连接到聊天服务器并与其他客户端通信。</summary>
// <remarks>ChatClient 是此 api 的主类。</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------


namespace Photon.Chat
{
    using ExitGames.Client.Photon;

    /// <summary>
    /// Chat 客户端回调接口。包含用于通知您的应用程序有关更新的回调方法。
    /// 必须在构造函数中提供给新的 ChatClient
    /// </summary>
    public interface IChatClientListener
    {
        /// <summary>
        /// 库的所有调试输出都将通过此方法报告。打印它或将其放入
        /// 缓冲区以在屏幕上使用。
        /// </summary>
        /// <param name="level">消息的 DebugLevel（严重性）。</param>
        /// <param name="message">调试文本。打印到 System.Console 或屏幕。</param>
        void DebugReturn(DebugLevel level, string message);

        /// <summary>
        /// 发生了断开连接。
        /// </summary>
        void OnDisconnected();

        /// <summary>
        /// 客户端现在已连接。
        /// </summary>
        /// <remarks>
        /// 客户端必须先连接，然后才能发送其状态、订阅频道和发送任何消息。
        /// </remarks>
        void OnConnected();

        /// <summary>ChatClient 的状态已更改。通常，OnConnected 和 OnDisconnected 是需要响应的回调。</summary>
        /// <param name="state">新状态。</param>
        void OnChatStateChange(ChatState state);

        /// <summary>
        /// 通知应用程序客户端从服务器收到了新消息
        /// 发送者数量等于 'messages' 中的消息数量。编号为 '0' 的发送者对应编号为
        /// '0' 的消息，编号为 '1' 的发送者对应编号为 '1' 的消息，依此类推
        /// </summary>
        /// <param name="channelName">消息来源频道</param>
        /// <param name="senders">发送消息的用户列表</param>
        /// <param name="messages">消息本身列表</param>
        void OnGetMessages(string channelName, string[] senders, object[] messages);

        /// <summary>
        /// 通知客户端有关私密消息
        /// </summary>
        /// <param name="sender">发送此消息的用户</param>
        /// <param name="message">消息本身</param>
        /// <param name="channelName">私密消息的 channelName（您自己发送的消息会按目标用户名添加到频道中）</param>
        void OnPrivateMessage(string sender, object message, string channelName);

        /// <summary>
        /// Subscribe 操作的结果。返回每个请求的频道名称的订阅结果。
        /// </summary>
        /// <remarks>
        /// 如果在 Subscribe 操作中发送了多个频道，OnSubscribed 可能会被调用多次，每次调用包含发送数组的一部分或在 "channels" 参数中包含单个频道。
        /// 调用顺序和 "channels" 参数中频道的顺序可能与 Subscribe 操作的 "channels" 参数中频道的顺序不同。
        /// </remarks>
        /// <param name="channels">频道名称数组。</param>
        /// <param name="results">每个频道的订阅结果。</param>
        void OnSubscribed(string[] channels, bool[] results);

        /// <summary>
        /// Unsubscribe 操作的结果。如果频道现在已取消订阅，则返回频道名称。
        /// </summary>
        /// 如果在 Unsubscribe 操作中发送了多个频道，OnUnsubscribed 可能会被调用多次，每次调用包含发送数组的一部分或在 "channels" 参数中包含单个频道。
        /// 调用顺序和 "channels" 参数中频道的顺序可能与 Unsubscribe 操作的 "channels" 参数中频道的顺序不同。
        /// <param name="channels">不再订阅的频道名称数组。</param>
        void OnUnsubscribed(string[] channels);

        /// <summary>
        /// 其他用户的新状态（您会收到好友列表中设置的用户的更新）。
        /// </summary>
        /// <param name="user">用户名称。</param>
        /// <param name="status">该用户的新状态。</param>
        /// <param name="gotMessage">如果状态包含应在本地缓存的消息则为 True。False：此状态更新不包含消息（保留您已有的消息）。</param>
        /// <param name="message">用户设置的消息。</param>
        void OnStatusUpdate(string user, int status, bool gotMessage, object message);

        /// <summary>
        /// 用户已订阅公共聊天频道
        /// </summary>
        /// <param name="channel">聊天频道名称</param>
        /// <param name="user">订阅的用户的 UserId</param>
        void OnUserSubscribed(string channel, string user);

        /// <summary>
        /// 用户已取消订阅公共聊天频道
        /// </summary>
        /// <param name="channel">聊天频道名称</param>
        /// <param name="user">取消订阅的用户的 UserId</param>
        void OnUserUnsubscribed(string channel, string user);


#if CHAT_EXTENDED
        
        /// <summary>
        /// 公共频道的属性已更改
        /// </summary>
        /// <param name="channel">属性已更改的频道名称</param>
        /// <param name="senderUserId">更改属性的用户的 UserID</param>
        /// <param name="properties">已更改的属性</param>
        void OnChannelPropertiesChanged(string channel, string senderUserId, Dictionary<object, object> properties);

        /// <summary>
        /// 公共频道中用户的属性已更改
        /// </summary>
        /// <param name="channel">属性已更改的频道名称</param>
        /// <param name="targetUserId">属性已更改的用户的 UserID</param>
        /// <param name="senderUserId">更改属性的用户的 UserID</param>
        /// <param name="properties">已更改的属性</param>
        void OnUserPropertiesChanged(string channel, string targetUserId, string senderUserId, Dictionary<object, object> properties);

        /// <summary>
        /// 服务器使用错误事件来让客户端了解某些问题。
        /// </summary>
        /// <remarks>
        /// 目前仅在 Chat WebHooks 中使用。
        /// </remarks>
        /// <param name="channel">收到此错误信息的频道名称</param>
        /// <param name="error">错误信息的文本消息</param>
        /// <param name="data">可选的错误数据</param>
        void OnErrorInfo(string channel, string error, object data);
        
#endif


#if SDK_V4
        /// <summary>
        /// 收到广播消息
        /// </summary>
        /// <param name="channel">聊天频道名称</param>
        /// <param name="message">消息数据</param>
        void OnReceiveBroadcastMessage(string channel, byte[] message);
#endif

    }
}