using System;
using System.Collections.Generic;
using System.Text;

namespace LAB2D.Domain.Dialogue
{
    /// <summary>
    /// 从纯文本输入和模板数据构建LLM聊天消息。
    /// </summary>
    public sealed class PromptAssemblyService
    {
        private readonly Func<string, Dictionary<string, string>, string> fillTemplate;

        public PromptAssemblyService(Func<string, Dictionary<string, string>, string> fillTemplate)
        {
            this.fillTemplate = fillTemplate;
        }

        public List<ChatMessage> BuildMessages(
            DialoguePromptProfileModel profile,
            string playerInput,
            IEnumerable<ChatMessage> history,
            string worldInfo,
            string gameStateText,
            IEnumerable<string> knowledgeTexts)
        {
            List<ChatMessage> messages = new List<ChatMessage>();
            string systemContent = this.BuildSystemPrompt(profile, worldInfo, gameStateText, knowledgeTexts);
            messages.Add(new ChatMessage("system", systemContent));

            if (history != null)
            {
                messages.AddRange(history);
            }

            messages.Add(new ChatMessage("user", playerInput));
            return messages;
        }

        public List<ChatMessage> BuildSummaryMessages(IEnumerable<ChatMessage> dialogueToSummarize)
        {
            if (dialogueToSummarize == null)
            {
                return new List<ChatMessage>();
            }

            StringBuilder dialogueBuilder = new StringBuilder();
            bool hasMessage = false;
            foreach (ChatMessage message in dialogueToSummarize)
            {
                if (message == null)
                {
                    continue;
                }

                hasMessage = true;
                string prefix = message.role == "user" ? "Player" : "NPC";
                dialogueBuilder.Append(prefix);
                dialogueBuilder.Append(": ");
                dialogueBuilder.Append(message.content);
                dialogueBuilder.AppendLine();
            }

            if (!hasMessage)
            {
                return new List<ChatMessage>();
            }

            string summaryPrompt = this.FillTemplate(
                "MemorySummaryTemplate",
                new Dictionary<string, string>
                {
                    { "DIALOGUE_CONTENT", dialogueBuilder.ToString() },
                });

            return new List<ChatMessage>
            {
                new ChatMessage("system", "Summarize the dialogue briefly. Output only the summary."),
                new ChatMessage("user", summaryPrompt),
            };
        }

        private string BuildSystemPrompt(
            DialoguePromptProfileModel profile,
            string worldInfo,
            string gameStateText,
            IEnumerable<string> knowledgeTexts)
        {
            DialoguePromptProfileModel safeProfile = profile ?? DialoguePromptProfileModel.CreateDefault();
            string backgroundText = string.IsNullOrEmpty(safeProfile.BackgroundStory)
                ? string.Empty
                : "背景：" + safeProfile.BackgroundStory;

            Dictionary<string, string> replacements = new Dictionary<string, string>
            {
                { "NPC_NAME", ValueOrDefault(safeProfile.NpcName, "NPC") },
                { "NPC_ROLE", ValueOrDefault(safeProfile.NpcRole, "Villager") },
                { "NPC_LOCATION", ValueOrDefault(safeProfile.NpcLocation, "Unknown") },
                { "PERSONALITY", ValueOrDefault(safeProfile.PersonalityDescription, "Friendly") },
                { "BACKGROUND", backgroundText },
                { "SPEAKING_STYLE", this.GetSpeakingStyleText(safeProfile.SpeakingStyle) },
                { "MAX_SENTENCES", safeProfile.MaxSentences.ToString() },
                { "WORLD_INFO", worldInfo ?? string.Empty },
                { "GAME_STATE", gameStateText ?? string.Empty },
                { "KNOWLEDGE_CONTEXT", this.BuildKnowledgeText(knowledgeTexts) },
            };

            return this.FillTemplate("SystemPromptTemplate", replacements);
        }

        private string BuildKnowledgeText(IEnumerable<string> knowledgeTexts)
        {
            if (knowledgeTexts == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (string text in knowledgeTexts)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    builder.Append(text);
                }
            }

            return builder.ToString();
        }

        private string FillTemplate(string templateName, Dictionary<string, string> replacements)
        {
            return this.fillTemplate == null
                ? string.Empty
                : this.fillTemplate(templateName, replacements);
        }

        private string GetSpeakingStyleText(string speakingStyle)
        {
            if (string.IsNullOrWhiteSpace(speakingStyle))
            {
                return "Concise";
            }

            speakingStyle = speakingStyle.Trim();
            const string ChinesePrefix = "说话";
            return speakingStyle.StartsWith(ChinesePrefix)
                ? speakingStyle.Substring(ChinesePrefix.Length).Trim()
                : speakingStyle;
        }

        private static string ValueOrDefault(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
