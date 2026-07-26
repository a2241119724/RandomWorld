namespace LAB2D.AI.Dialogue.Memory
{
    using LAB2D;
    using LAB2D.AI.Dialogue.LLM;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 短期记忆，FIFO 队列保留最近 N 轮对话
    /// </summary>
    public class ShortTermMemory
    {
        private readonly Queue<ChatMessage> messages = new Queue<ChatMessage>();
        private readonly int maxExchanges;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="maxExchanges">最大保留轮数（一轮 = 玩家消息 + NPC 回复）</param>
        public ShortTermMemory(int maxExchanges = 6)
        {
            this.maxExchanges = maxExchanges;
        }

        /// <summary>
        /// 记录一轮对话
        /// </summary>
        public void AddExchange(string playerText, string npcResponse)
        {
            this.messages.Enqueue(new ChatMessage("user", playerText));
            this.messages.Enqueue(new ChatMessage("assistant", npcResponse));

            while (this.messages.Count > this.maxExchanges * 2)
            {
                this.messages.Dequeue();
                this.messages.Dequeue();
            }
        }

        /// <summary>
        /// 获取最近的消息
        /// </summary>
        public List<ChatMessage> GetRecentMessages(int count)
        {
            if (count <= 0)
            {
                return new List<ChatMessage>();
            }

            int skip = this.messages.Count - count;
            if (skip <= 0)
            {
                return this.messages.ToList();
            }

            return this.messages.Skip(skip).Take(count).ToList();
        }

        /// <summary>
        /// 获取所有消息（用于压缩为长期记忆）
        /// </summary>
        public List<ChatMessage> GetAllMessages()
        {
            return this.messages.ToList();
        }

        /// <summary>
        /// 已记录的轮数
        /// </summary>
        public int ExchangeCount
        {
            get { return this.messages.Count / 2; }
        }

        /// <summary>
        /// 清空记忆
        /// </summary>
        public void Clear()
        {
            this.messages.Clear();
        }
    }
}
