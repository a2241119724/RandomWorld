namespace LAB2D.AI.Dialogue.Memory
{
    using LAB2D;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 对话记忆管理器，管理短期和长期记忆
    /// </summary>
    public class DialogueMemoryManager : Singleton<DialogueMemoryManager>
    {
        /// <summary>
        /// 触发长期记忆压缩的轮数阈值
        /// </summary>
        private const int COMPRESSION_THRESHOLD = 8;

        private readonly Dictionary<string, ShortTermMemory> shortTermMemories = new Dictionary<string, ShortTermMemory>();
        private readonly Dictionary<string, List<LongTermMemorySummary>> longTermSummaries = new Dictionary<string, List<LongTermMemorySummary>>();

        /// <summary>
        /// 记录一轮对话
        /// </summary>
        public void RecordExchange(string npcId, string playerText, string npcResponse)
        {
            ShortTermMemory stm = this.GetOrCreateShortTerm(npcId);
            stm.AddExchange(playerText, npcResponse);

            // 检查是否需要触发长期记忆压缩
            if (stm.ExchangeCount >= COMPRESSION_THRESHOLD && stm.ExchangeCount % COMPRESSION_THRESHOLD == 0)
            {
                this.RequestCompression(npcId);
            }
        }

        /// <summary>
        /// 获取最近对话历史
        /// </summary>
        public List<ChatMessage> GetRecentHistory(string npcId, int maxExchanges = 6)
        {
            return this.GetOrCreateShortTerm(npcId).GetRecentMessages(maxExchanges * 2);
        }

        /// <summary>
        /// 获取长期记忆
        /// </summary>
        public List<LongTermMemorySummary> GetLongTermMemories(string npcId)
        {
            this.longTermSummaries.TryGetValue(npcId, out List<LongTermMemorySummary> summaries);
            return summaries ?? new List<LongTermMemorySummary>();
        }

        /// <summary>
        /// 获取所有短期消息（用于压缩）
        /// </summary>
        public List<ChatMessage> GetAllMessagesForCompression(string npcId)
        {
            return this.GetOrCreateShortTerm(npcId).GetAllMessages();
        }

        /// <summary>
        /// 存储长期记忆摘要
        /// </summary>
        public void AddLongTermSummary(string npcId, string summaryText, List<string> keyTopics, int exchangeCount)
        {
            if (!this.longTermSummaries.ContainsKey(npcId))
            {
                this.longTermSummaries[npcId] = new List<LongTermMemorySummary>();
            }

            this.longTermSummaries[npcId].Add(new LongTermMemorySummary
            {
                npcId = npcId,
                summaryText = summaryText,
                keyTopics = keyTopics ?? new List<string>(),
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                exchangeCountCovered = exchangeCount,
            });
        }

        /// <summary>
        /// 获取短期记忆
        /// </summary>
        public ShortTermMemory GetShortTermMemory(string npcId)
        {
            return this.GetOrCreateShortTerm(npcId);
        }

        /// <summary>
        /// 清除某个 NPC 的所有记忆
        /// </summary>
        public void ClearMemory(string npcId)
        {
            this.shortTermMemories.Remove(npcId);
            this.longTermSummaries.Remove(npcId);
        }

        private ShortTermMemory GetOrCreateShortTerm(string npcId)
        {
            if (!this.shortTermMemories.TryGetValue(npcId, out ShortTermMemory stm))
            {
                stm = new ShortTermMemory();
                this.shortTermMemories[npcId] = stm;
            }

            return stm;
        }

        private void RequestCompression(string npcId)
        {
            // 将在 DialogueManager 中异步处理
            // 触发事件或由 DialogueManager 轮询
            LogManager.Instance.Log(
                "DialogueMemoryManager: NPC " + npcId + " 触发长期记忆压缩",
                LogManager.LogLevelEnum.Info);
        }
    }
}
