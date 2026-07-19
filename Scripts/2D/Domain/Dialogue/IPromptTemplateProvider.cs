using System.Collections.Generic;

namespace LAB2D.Domain.Dialogue
{
    /// <summary>
    /// Provides prompt template rendering without binding dialogue rules to Unity Resources.
    /// </summary>
    public interface IPromptTemplateProvider
    {
        string FillTemplate(string templateName, Dictionary<string, string> replacements);
    }
}
