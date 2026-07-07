namespace LAB2D
{
    /// <summary>
    /// Engine-agnostic prompt profile used by prompt assembly rules.
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
