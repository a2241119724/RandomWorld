namespace LAB2D.AI.Dialogue.Prompt
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides Unity NPC prompt profiles to presentation/application dialogue code.
    /// </summary>
    public interface INPCPromptProfileProvider
    {
        NPCPromptProfile GetProfile(string name);

        IEnumerable<NPCPromptProfile> GetAllProfiles();
    }
}
