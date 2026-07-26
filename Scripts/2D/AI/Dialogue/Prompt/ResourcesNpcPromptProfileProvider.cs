namespace LAB2D.AI.Dialogue.Prompt
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Loads NPC prompt profiles from Resources/SO/AI and supplies runtime defaults.
    /// </summary>
    public sealed class ResourcesNpcPromptProfileProvider : INPCPromptProfileProvider
    {
        private readonly Dictionary<string, NPCPromptProfile> profileCache;

        public ResourcesNpcPromptProfileProvider()
        {
            this.profileCache = new Dictionary<string, NPCPromptProfile>();
            this.LoadProfiles();
            this.EnsureDefaultWorkerProfile();
        }

        public NPCPromptProfile GetProfile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            this.profileCache.TryGetValue(name, out NPCPromptProfile profile);
            return profile;
        }

        public IEnumerable<NPCPromptProfile> GetAllProfiles()
        {
            return this.profileCache.Values;
        }

        private void LoadProfiles()
        {
            NPCPromptProfile[] profiles = Resources.LoadAll<NPCPromptProfile>("SO/AI");
            if (profiles == null)
            {
                return;
            }

            foreach (NPCPromptProfile profile in profiles)
            {
                if (profile != null)
                {
                    this.profileCache[profile.name] = profile;
                }
            }
        }

        private void EnsureDefaultWorkerProfile()
        {
            if (this.profileCache.ContainsKey("Worker"))
            {
                return;
            }

            NPCPromptProfile defaultProfile = ScriptableObject.CreateInstance<NPCPromptProfile>();
            defaultProfile.name = "Worker";
            defaultProfile.npcName = "工人";
            defaultProfile.npcRole = "村民";
            defaultProfile.personalityDescription = "勤劳的工人";
            defaultProfile.speakingStyle = "说话简洁";
            this.profileCache["Worker"] = defaultProfile;
        }
    }
}
