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
    using System.Collections.Generic;
    using System.Text;

#if SUPPORTED_UNITY || NETFX_CORE
#endif


    /// <summary>
    /// Photon Chat 中的通信频道，由 ChatClient 更新并以只读方式提供。
    /// </summary>
    /// <remarks>
    /// 包含要使用和显示的消息和发送者。
    /// 通过这些方式访问：
    ///     ChatClient.PublicChannels
    ///     ChatClient.PrivateChannels
    /// </remarks>
    public class ChatChannel
    {
        /// <summary>频道名称（用于订阅和取消订阅）。</summary>
        public readonly string Name;

        /// <summary>按时间顺序的消息发送者。Senders 和 Messages 通过索引相互引用。Senders[x] 是 Messages[x] 的发送者。</summary>
        public readonly List<string> Senders = new List<string>();

        /// <summary>按时间顺序的消息。Senders 和 Messages 通过索引相互引用。Senders[x] 是 Messages[x] 的发送者。</summary>
        public readonly List<object> Messages = new List<object>();

        /// <summary>如果大于 0，此频道将限制本地缓存的消息数量。</summary>
        public int MessageLimit;

        /// <summary>唯一频道 ID。</summary>
        public int ChannelID;

        /// <summary>这是私有的 1 对 1 频道吗？</summary>
        public bool IsPrivate { get; protected internal set; }

        /// <summary>此客户端为此频道缓冲/已知的消息计数。</summary>
        public int MessageCount { get { return this.Messages.Count; } }

        /// <summary>
        /// 最后收到的消息的 ID。
        /// </summary>
        public int LastMsgId { get; protected set; }

        private Dictionary<object, object> properties;

        /// <summary>此频道是否跟踪其订阅者列表。</summary>
        public bool PublishSubscribers { get; protected set; }

        /// <summary>频道订阅者的最大数量。0 表示无限。</summary>
        public int MaxSubscribers { get; protected set; }

        /// <summary>已订阅的用户。</summary>
        public readonly HashSet<string> Subscribers = new HashSet<string>();

        /// <summary>内部用于创建新频道。这不会在服务器上创建频道！请使用 ChatClient.Subscribe。</summary>
        public ChatChannel(string name)
        {
            this.Name = name;
        }

        /// <summary>内部用于向此频道添加消息。</summary>
        public void Add(string sender, object message, int msgId)
        {
            this.Senders.Add(sender);
            this.Messages.Add(message);
            this.LastMsgId = msgId;
            this.TruncateMessages();
        }

        /// <summary>内部用于向此频道添加消息。</summary>
        public void Add(string[] senders, object[] messages, int lastMsgId)
        {
            this.Senders.AddRange(senders);
            this.Messages.AddRange(messages);
            this.LastMsgId = lastMsgId;
            this.TruncateMessages();
        }

        /// <summary>将此频道中本地缓存的消息数量减少到 MessageLimit（如果已设置）。</summary>
        public void TruncateMessages()
        {
            if (this.MessageLimit <= 0 || this.Messages.Count <= this.MessageLimit)
            {
                return;
            }

            int excessCount = this.Messages.Count - this.MessageLimit;
            this.Senders.RemoveRange(0, excessCount);
            this.Messages.RemoveRange(0, excessCount);
        }

        /// <summary>清除当前存储的消息本地缓存。这会释放内存，但不会影响服务器。</summary>
        public void ClearMessages()
        {
            this.Senders.Clear();
            this.Messages.Clear();
        }

        /// <summary>提供此频道中所有消息的字符串表示。</summary>
        /// <returns>所有已知消息，格式为"Sender: Message"，逐行显示。</returns>
        public string ToStringMessages()
        {
            StringBuilder txt = new StringBuilder();
            for (int i = 0; i < this.Messages.Count; i++)
            {
                txt.AppendLine(string.Format("{0}: {1}", this.Senders[i], this.Messages[i]));
            }
            return txt.ToString();
        }

        internal void ReadChannelProperties(Dictionary<object, object> newProperties)
        {
            if (newProperties != null && newProperties.Count > 0)
            {
                if (this.properties == null)
                {
                    this.properties = new Dictionary<object, object>(newProperties.Count);
                }
                foreach (var pair in newProperties)
                {
                    if (pair.Value == null)
                    {
                        this.properties.Remove(pair.Key);
                    }
                    else
                    {
                        this.properties[pair.Key] = pair.Value;
                    }
                }
                object temp;
                if (this.properties.TryGetValue(ChannelWellKnownProperties.PublishSubscribers, out temp))
                {
                    this.PublishSubscribers = (bool)temp;
                }
                if (this.properties.TryGetValue(ChannelWellKnownProperties.MaxSubscribers, out temp))
                {
                    this.MaxSubscribers = (int)temp;
                }
            }
        }

        internal void AddSubscribers(string[] users)
        {
            if (users == null)
            {
                return;
            }
            for (int i = 0; i < users.Length; i++)
            {
                this.Subscribers.Add(users[i]);
            }
        }

#if CHAT_EXTENDED
        internal void ReadUserProperties(string userId, Dictionary<object, object> changedProperties)
        {
            throw new System.NotImplementedException();
        }
        
        internal bool TryGetChannelProperty<T>(object propertyKey, out T propertyValue)
        {
            propertyValue = default(T);
            object temp;
            if (properties != null && properties.TryGetValue(propertyKey, out temp) && temp is T)
            {
                propertyValue = (T)temp;
                return true;
            }
            return false;
        }

        public bool TryGetCustomChannelProperty<T>(string propertyKey, out T propertyValue)
        {
            return this.TryGetChannelProperty(propertyKey, out propertyValue);
        }
#endif
    }
}