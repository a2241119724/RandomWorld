namespace LAB2D.AI.Dialogue.Memory
{
    using LAB2D;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 对话记忆管理器，管理短期和长期记忆（ASingletonSaveData）。
    /// </summary>
    public class DialogueMemoryManager : ASingletonSaveData<DialogueMemoryManager>
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
            AWorkerTask.LogProvider(
                "DialogueMemoryManager: NPC " + npcId + " 触发长期记忆压缩",
                LogManager.LogLevelEnum.Info);
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            DialogueMemoryManagerData data = new DialogueMemoryManagerData();

            // 保存短期记忆
            foreach (KeyValuePair<string, ShortTermMemory> kv in this.shortTermMemories)
            {
                List<ChatMessage> messages = kv.Value.GetAllMessages();
                if (messages.Count > 0)
                {
                    data.ShortTermEntries.Add(new ShortTermMemoryEntry
                    {
                        NpcId = kv.Key,
                        Messages = messages,
                    });
                }
            }

            // 保存长期记忆
            foreach (KeyValuePair<string, List<LongTermMemorySummary>> kv in this.longTermSummaries)
            {
                foreach (LongTermMemorySummary summary in kv.Value)
                {
                    data.LongTermEntries.Add(new LongTermMemoryEntry
                    {
                        NpcId = kv.Key,
                        SummaryText = summary.summaryText,
                        KeyTopics = summary.keyTopics ?? new List<string>(),
                        CreatedAt = summary.createdAt,
                        ExchangeCountCovered = summary.exchangeCountCovered,
                    });
                }
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            DialogueMemoryManagerData data = DataTool.LoadDataByBinary<DialogueMemoryManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                return;
            }

            // 恢复短期记忆
            this.shortTermMemories.Clear();
            if (data.ShortTermEntries != null)
            {
                foreach (ShortTermMemoryEntry entry in data.ShortTermEntries)
                {
                    if (entry.Messages == null || entry.Messages.Count == 0)
                    {
                        continue;
                    }

                    ShortTermMemory stm = new ShortTermMemory();
                    foreach (ChatMessage msg in entry.Messages)
                    {
                        if (msg != null && !string.IsNullOrEmpty(msg.role))
                        {
                            if (msg.role == "user")
                            {
                                // 需要成对添加 — 先收集 user，等待 assistant
                                string playerText = msg.content;
                                // 简单方案：按顺序重放（每两个消息为一轮）
                            }
                        }
                    }

                    // 按轮次重建（每两个消息为 user + assistant 一轮）
                    List<ChatMessage> msgs = entry.Messages;
                    for (int i = 0; i + 1 < msgs.Count; i += 2)
                    {
                        ChatMessage userMsg = msgs[i];
                        ChatMessage assistantMsg = msgs[i + 1];
                        if (userMsg != null && assistantMsg != null
                            && userMsg.role == "user" && assistantMsg.role == "assistant")
                        {
                            stm.AddExchange(userMsg.content, assistantMsg.content);
                        }
                    }

                    this.shortTermMemories[entry.NpcId] = stm;
                }
            }

            // 恢复长期记忆
            this.longTermSummaries.Clear();
            if (data.LongTermEntries != null)
            {
                foreach (LongTermMemoryEntry entry in data.LongTermEntries)
                {
                    if (!this.longTermSummaries.TryGetValue(entry.NpcId, out List<LongTermMemorySummary> list))
                    {
                        list = new List<LongTermMemorySummary>();
                        this.longTermSummaries[entry.NpcId] = list;
                    }

                    list.Add(new LongTermMemorySummary
                    {
                        npcId = entry.NpcId,
                        summaryText = entry.SummaryText,
                        keyTopics = entry.KeyTopics ?? new List<string>(),
                        createdAt = entry.CreatedAt,
                        exchangeCountCovered = entry.ExchangeCountCovered,
                    });
                }
            }
        }

        [Serializable]
        public class DialogueMemoryManagerData
        {
            public List<ShortTermMemoryEntry> ShortTermEntries = new List<ShortTermMemoryEntry>();
            public List<LongTermMemoryEntry> LongTermEntries = new List<LongTermMemoryEntry>();
        }

        [Serializable]
        public class ShortTermMemoryEntry
        {
            public string NpcId;
            public List<ChatMessage> Messages = new List<ChatMessage>();
        }

        [Serializable]
        public class LongTermMemoryEntry
        {
            public string NpcId;
            public string SummaryText;
            public List<string> KeyTopics = new List<string>();
            public string CreatedAt;
            public int ExchangeCountCovered;
        }
    }
}
