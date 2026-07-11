namespace LAB2D.AI.Dialogue.Prompt
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Prompt 模板加载器，从 Resources/AI/Prompts/ 加载 TextAsset
    /// </summary>
    public class PromptTemplateLoader
    {
        private readonly Dictionary<string, string> templates = new Dictionary<string, string>();

        /// <summary>
        /// 构造，加载所有模板
        /// </summary>
        public PromptTemplateLoader()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>("AI/Prompts");
            foreach (TextAsset asset in assets)
            {
                this.templates[asset.name] = asset.text;
            }
        }

        /// <summary>
        /// 获取模板原始文本
        /// </summary>
        public string GetTemplate(string name)
        {
            this.templates.TryGetValue(name, out string template);
            return template ?? string.Empty;
        }

        /// <summary>
        /// 填充模板中的占位符 {{KEY}}
        /// </summary>
        public string FillTemplate(string templateName, Dictionary<string, string> replacements)
        {
            string template = this.GetTemplate(templateName);
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            foreach (KeyValuePair<string, string> kv in replacements)
            {
                template = template.Replace("{{" + kv.Key + "}}", kv.Value);
            }

            return template;
        }
    }
}
