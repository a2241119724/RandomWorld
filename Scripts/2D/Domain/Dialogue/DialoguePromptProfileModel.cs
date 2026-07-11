namespace LAB2D
{
    /// <summary>
    /// 与引擎无关的提示词配置，供提示词组装规则使用。
    /// </summary>
    public sealed class DialoguePromptProfileModel
    {
        public string NpcName { get; set; }

        public string NpcRole { get; set; }

        public string NpcLocation { get; set; }

        public string PersonalityDescription { get; set; }

        public string BackgroundStory { get; set; }

        public string SpeakingStyle { get; set; }

        public int MaxSentences { get; set; }

        public static DialoguePromptProfileModel CreateDefault()
        {
            return new DialoguePromptProfileModel
            {
                NpcName = "NPC",
                NpcRole = "Villager",
                NpcLocation = "Unknown",
                PersonalityDescription = "Friendly",
                BackgroundStory = string.Empty,
                SpeakingStyle = "Concise",
                MaxSentences = 3,
            };
        }
    }
}
