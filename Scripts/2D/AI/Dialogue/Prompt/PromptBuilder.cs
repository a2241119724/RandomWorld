namespace LAB2D
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Prompt 组装器，将 NPC 配置 + 游戏状态 + 历史 + 知识组合为 LLM 输入
    /// </summary>
    public class PromptBuilder : Singleton<PromptBuilder>
    {
        private Dictionary<string, NPCPromptProfile> profileCache;
        private PromptTemplateLoader templateLoader;

        public PromptBuilder()
        {
            this.LoadProfiles();
            this.templateLoader = new PromptTemplateLoader();
        }

        /// <summary>
        /// 获取 NPC 配置
        /// </summary>
        public NPCPromptProfile GetProfile(string name)
        {
            this.profileCache.TryGetValue(name, out NPCPromptProfile profile);
            return profile;
        }

        /// <summary>
        /// 获取所有已加载的 NPC 配置
        /// </summary>
        public IEnumerable<NPCPromptProfile> GetAllProfiles()
        {
            return this.profileCache.Values;
        }

        /// <summary>
        /// 组装消息列表
        /// </summary>
        public List<ChatMessage> BuildMessages(
            NPCPromptProfile profile,
            string playerInput,
            List<ChatMessage> history,
            GameStateContext gameContext,
            List<GameKnowledgeEntry> ragResults)
        {
            List<ChatMessage> messages = new List<ChatMessage>();

            // 1. 系统消息（使用模板填充）
            string systemContent = this.BuildSystemPrompt(profile, gameContext, ragResults);
            messages.Add(new ChatMessage("system", systemContent));

            // 2. 对话历史
            if (history != null)
            {
                messages.AddRange(history);
            }

            // 3. 当前玩家输入
            messages.Add(new ChatMessage("user", playerInput));

            return messages;
        }

        /// <summary>
        /// 组装对话摘要的系统消息（用于长期记忆压缩）
        /// </summary>
        public List<ChatMessage> BuildSummaryMessages(List<ChatMessage> dialogueToSummarize)
        {
            if (dialogueToSummarize == null || dialogueToSummarize.Count == 0)
            {
                return new List<ChatMessage>();
            }

            var sb = new StringBuilder();
            foreach (ChatMessage msg in dialogueToSummarize)
            {
                string prefix = msg.role == "user" ? "玩家" : "NPC";
                sb.Append(prefix);
                sb.Append("：");
                sb.Append(msg.content);
                sb.Append("。");
            }

            string dialogueText = sb.ToString();
            string summaryPrompt = this.templateLoader.FillTemplate(
                "MemorySummaryTemplate",
                new Dictionary<string, string>
                {
                    { "DIALOGUE_CONTENT", dialogueText },
                });

            return new List<ChatMessage>
            {
                new ChatMessage("system", "你是对话摘要助手，只输出摘要，不超过3句话。"),
                new ChatMessage("user", summaryPrompt),
            };
        }

        private string BuildSystemPrompt(
            NPCPromptProfile profile,
            GameStateContext gameContext,
            List<GameKnowledgeEntry> ragResults)
        {
            // 构建背景文本（可选）
            string backgroundText = string.Empty;
            if (profile != null && !string.IsNullOrEmpty(profile.backgroundStory))
            {
                backgroundText = "背景：" + profile.backgroundStory + "。";
            }

            // 构建 RAG 知识文本
            string knowledgeText = string.Empty;
            if (ragResults != null && ragResults.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (GameKnowledgeEntry entry in ragResults)
                {
                    sb.Append(entry.ToPromptText());
                }

                knowledgeText = sb.ToString();
            }

            var replacements = new Dictionary<string, string>
            {
                { "NPC_NAME", profile != null ? profile.npcName : "NPC" },
                { "NPC_ROLE", profile != null ? profile.npcRole : "村民" },
                { "NPC_LOCATION", profile != null ? profile.npcLocation : "未知" },
                { "PERSONALITY", profile != null ? profile.personalityDescription : "友善" },
                { "BACKGROUND", backgroundText },
                { "SPEAKING_STYLE", profile != null ? profile.speakingStyle : "简洁" },
                { "MAX_SENTENCES", profile != null ? profile.maxSentences.ToString() : "3" },
                { "WORLD_INFO", gameContext != null ? gameContext.ToWorldInfo() : string.Empty },
                { "GAME_STATE", gameContext != null ? gameContext.ToPromptText() : string.Empty },
                { "KNOWLEDGE_CONTEXT", knowledgeText },
            };

            return this.templateLoader.FillTemplate("SystemPromptTemplate", replacements);
        }

        private void LoadProfiles()
        {
            this.profileCache = new Dictionary<string, NPCPromptProfile>();
            NPCPromptProfile[] profiles = Resources.LoadAll<NPCPromptProfile>("SO/AI");
            if (profiles != null)
            {
                foreach (NPCPromptProfile profile in profiles)
                {
                    this.profileCache[profile.name] = profile;
                }
            }
        }
    }
}
