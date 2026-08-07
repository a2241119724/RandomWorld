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

    }
}
