namespace LAB2D.AI.Dialogue.RAG
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    /// 游戏知识条目
    /// </summary>
    [Serializable]
    public class GameKnowledgeEntry
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string id;

        /// <summary>
        /// 标题
        /// </summary>
        public string title;

        /// <summary>
        /// 内容
        /// </summary>
        public string content;

        /// <summary>
        /// 关键词列表
        /// </summary>
        public List<string> keywords = new List<string>();

        /// <summary>
        /// 分类
        /// </summary>
        public string category;

        /// <summary>
        /// 格式化为 Prompt 文本
        /// </summary>
        public string ToPromptText()
        {
            if (!string.IsNullOrEmpty(this.title))
            {
                return this.title + "：" + this.content;
            }

            return this.content;
        }

        /// <summary>
        /// 从 Resources/GameKnowledge/ 加载知识条目（Markdown 文件）
        /// </summary>
        public static List<GameKnowledgeEntry> LoadFromResources()
        {
            List<GameKnowledgeEntry> entries = new List<GameKnowledgeEntry>();
            TextAsset[] assets = Resources.LoadAll<TextAsset>("GameKnowledge");

            if (assets == null)
            {
                return entries;
            }

            foreach (TextAsset asset in assets)
            {
                GameKnowledgeEntry entry = ParseMarkdown(asset.text, asset.name);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private static GameKnowledgeEntry ParseMarkdown(string text, string fileName)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            GameKnowledgeEntry entry = new GameKnowledgeEntry();
            entry.id = fileName;

            // 解析 YAML front-matter
            Match frontMatterMatch = Regex.Match(
                text, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);

            string body = text;
            if (frontMatterMatch.Success)
            {
                string frontMatter = frontMatterMatch.Groups[1].Value;
                body = text.Substring(frontMatterMatch.Length);

                // 解析 title
                Match titleMatch = Regex.Match(frontMatter, @"title:\s*(.+)");
                if (titleMatch.Success)
                {
                    entry.title = titleMatch.Groups[1].Value.Trim();
                }

                // 解析 keywords
                Match keywordsMatch = Regex.Match(frontMatter, @"keywords:\s*\[(.+?)\]");
                if (keywordsMatch.Success)
                {
                    string[] parts = keywordsMatch.Groups[1].Value.Split(',');
                    foreach (string part in parts)
                    {
                        string kw = part.Trim().Trim('"', '\'');
                        if (!string.IsNullOrEmpty(kw))
                        {
                            entry.keywords.Add(kw);
                        }
                    }
                }

                // 解析 category
                Match categoryMatch = Regex.Match(frontMatter, @"category:\s*(.+)");
                if (categoryMatch.Success)
                {
                    entry.category = categoryMatch.Groups[1].Value.Trim();
                }
                else
                {
                    entry.category = "general";
                }
            }
            else
            {
                // 无 front-matter，使用文件名作为标题
                entry.title = fileName;
                entry.category = "general";
            }

            entry.content = body.Trim();
            return entry;
        }
    }
}
