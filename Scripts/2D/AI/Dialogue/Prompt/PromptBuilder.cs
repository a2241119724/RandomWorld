namespace LAB2D.AI.Dialogue.Prompt
{
    using LAB2D;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.AI.Dialogue.RAG;
    using LAB2D.Domain.Dialogue;
    using System.Collections.Generic;

    /// <summary>
    /// Prompt 组装器，将 NPC 配置 + 游戏状态 + 历史 + 知识组合为 LLM 输入
    /// </summary>
    public class PromptBuilder : Singleton<PromptBuilder>
    {
        private readonly INPCPromptProfileProvider profileProvider;
        private readonly IPromptTemplateProvider templateProvider;
        private readonly PromptAssemblyService promptAssemblyService;

        public PromptBuilder()
            : this(new ResourcesNpcPromptProfileProvider(), new PromptTemplateLoader())
        {
        }

        public PromptBuilder(INPCPromptProfileProvider profileProvider, IPromptTemplateProvider templateProvider)
        {
            this.profileProvider = profileProvider ?? new ResourcesNpcPromptProfileProvider();
            this.templateProvider = templateProvider ?? new PromptTemplateLoader();
            this.promptAssemblyService = new PromptAssemblyService(this.templateProvider);
        }

        /// <summary>
        /// 获取 NPC 配置
        /// </summary>
        public NPCPromptProfile GetProfile(string name)
        {
            return this.profileProvider.GetProfile(name);
        }

        /// <summary>
        /// 获取所有已加载的 NPC 配置
        /// </summary>
        public IEnumerable<NPCPromptProfile> GetAllProfiles()
        {
            return this.profileProvider.GetAllProfiles();
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
            return this.promptAssemblyService.BuildMessages(
                ToModel(profile),
                playerInput,
                history,
                gameContext != null ? gameContext.ToWorldInfo() : string.Empty,
                gameContext != null ? gameContext.ToPromptText() : string.Empty,
                ToKnowledgeTexts(ragResults));

            // 1. 系统消息（使用模板填充）

            // 2. 对话历史

            // 3. 当前玩家输入
        }

        /// <summary>
        /// 组装对话摘要的系统消息（用于长期记忆压缩）
        /// </summary>
        public List<ChatMessage> BuildSummaryMessages(List<ChatMessage> dialogueToSummarize)
        {
            return this.promptAssemblyService.BuildSummaryMessages(dialogueToSummarize);
        }

        /*
        public List<ChatMessage> BuildSummaryMessagesLegacy(List<ChatMessage> dialogueToSummarize)
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

        */

        private static DialoguePromptProfileModel ToModel(NPCPromptProfile profile)
        {
            if (profile == null)
            {
                return DialoguePromptProfileModel.CreateDefault();
            }

            return new DialoguePromptProfileModel
            {
                NpcName = profile.npcName,
                NpcRole = profile.npcRole,
                NpcLocation = profile.npcLocation,
                PersonalityDescription = profile.personalityDescription,
                BackgroundStory = profile.backgroundStory,
                SpeakingStyle = profile.speakingStyle,
                MaxSentences = profile.maxSentences,
            };
        }

        private static List<string> ToKnowledgeTexts(List<GameKnowledgeEntry> ragResults)
        {
            List<string> knowledgeTexts = new List<string>();
            if (ragResults == null)
            {
                return knowledgeTexts;
            }

            foreach (GameKnowledgeEntry entry in ragResults)
            {
                if (entry != null)
                {
                    knowledgeTexts.Add(entry.ToPromptText());
                }
            }

            return knowledgeTexts;
        }

        /*
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
                { "SPEAKING_STYLE", GetSpeakingStyleText(profile) },
                { "MAX_SENTENCES", profile != null ? profile.maxSentences.ToString() : "3" },
                { "WORLD_INFO", gameContext != null ? gameContext.ToWorldInfo() : string.Empty },
                { "GAME_STATE", gameContext != null ? gameContext.ToPromptText() : string.Empty },
                { "KNOWLEDGE_CONTEXT", knowledgeText },
            };

            return this.templateLoader.FillTemplate("SystemPromptTemplate", replacements);
        }

        private static string GetSpeakingStyleText(NPCPromptProfile profile)
        {
            string speakingStyle = profile == null ? "简洁" : profile.speakingStyle;
            if (string.IsNullOrWhiteSpace(speakingStyle))
            {
                return "简洁";
            }

            speakingStyle = speakingStyle.Trim();
            return speakingStyle.StartsWith("说话")
                ? speakingStyle.Substring("说话".Length).Trim()
                : speakingStyle;
        }

        */

    }
}
