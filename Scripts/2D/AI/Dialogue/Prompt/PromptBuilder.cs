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

            // 1. 系统消息
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
            sb.Append("请将以下对话压缩为一段简短摘要，只保留关键信息：");

            foreach (ChatMessage msg in dialogueToSummarize)
            {
                string prefix = msg.role == "user" ? "玩家" : "NPC";
                sb.Append(prefix + "：" + msg.content + "。");
            }

            return new List<ChatMessage>
            {
                new ChatMessage("system", "你是对话摘要助手，只输出摘要内容，不超过3句话。"),
                new ChatMessage("user", sb.ToString()),
            };
        }

        private string BuildSystemPrompt(
            NPCPromptProfile profile,
            GameStateContext gameContext,
            List<GameKnowledgeEntry> ragResults)
        {
            string npcName = profile != null ? profile.npcName : "NPC";
            string npcRole = profile != null ? profile.npcRole : "村民";
            string npcLocation = profile != null ? profile.npcLocation : "未知";
            string personality = profile != null ? profile.personalityDescription : "友善";
            string speakingStyle = profile != null ? profile.speakingStyle : "简洁";
            string maxSentences = profile != null ? profile.maxSentences.ToString() : "3";

            var sb = new StringBuilder();

            sb.Append("你是名叫");
            sb.Append(npcName);
            sb.Append("的NPC，职业是");
            sb.Append(npcRole);
            sb.Append("，位于");
            sb.Append(npcLocation);
            sb.Append("。你的性格");
            sb.Append(personality);
            sb.Append("。");

            if (profile != null && !string.IsNullOrEmpty(profile.backgroundStory))
            {
                sb.Append("你的背景：");
                sb.Append(profile.backgroundStory);
                sb.Append("。");
            }

            sb.Append("你的说话风格：");
            sb.Append(speakingStyle);
            sb.Append("。");

            sb.Append("回答不超过");
            sb.Append(maxSentences);
            sb.Append("句话。");

            if (gameContext != null)
            {
                sb.Append(gameContext.ToPromptText());
            }

            if (ragResults != null && ragResults.Count > 0)
            {
                foreach (GameKnowledgeEntry entry in ragResults)
                {
                    sb.Append(entry.ToPromptText());
                }
            }

            return sb.ToString();
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
