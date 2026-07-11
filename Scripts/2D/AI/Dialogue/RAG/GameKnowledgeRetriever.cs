namespace LAB2D.AI.Dialogue.RAG
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 游戏知识检索器，基于关键词匹配
    /// </summary>
    public class GameKnowledgeRetriever : Singleton<GameKnowledgeRetriever>
    {
        private List<GameKnowledgeEntry> knowledgeBase;
        private Dictionary<string, List<GameKnowledgeEntry>> keywordIndex;

        public GameKnowledgeRetriever()
        {
            this.knowledgeBase = GameKnowledgeEntry.LoadFromResources();
            this.BuildKeywordIndex();
            LogManager.Instance.Log(
                "GameKnowledgeRetriever: 加载了 " + this.knowledgeBase.Count + " 条知识",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 检索相关知识条目
        /// </summary>
        /// <param name="query">玩家输入</param>
        /// <param name="npcKnowledgeTags">NPC 可用的知识标签（来自 NPCPromptProfile）</param>
        /// <param name="topK">返回前 K 条</param>
        public List<GameKnowledgeEntry> Retrieve(string query, List<string> npcKnowledgeTags = null, int topK = 3)
        {
            if (string.IsNullOrEmpty(query) || this.knowledgeBase.Count == 0)
            {
                return new List<GameKnowledgeEntry>();
            }

            // 从查询中提取关键词
            List<string> queryKeywords = this.ExtractKeywords(query);

            // 对每条知识条目打分
            var scoredEntries = new List<(GameKnowledgeEntry entry, int score)>();
            foreach (GameKnowledgeEntry entry in this.knowledgeBase)
            {
                int score = this.CalculateRelevanceScore(entry, queryKeywords, npcKnowledgeTags);
                if (score > 0)
                {
                    scoredEntries.Add((entry, score));
                }
            }

            // 按分数降序排序，取 topK
            scoredEntries.Sort((a, b) => b.score.CompareTo(a.score));

            List<GameKnowledgeEntry> results = new List<GameKnowledgeEntry>();
            for (int i = 0; i < System.Math.Min(topK, scoredEntries.Count); i++)
            {
                results.Add(scoredEntries[i].entry);
            }

            return results;
        }

        /// <summary>
        /// 重新加载知识库
        /// </summary>
        public void Reload()
        {
            this.knowledgeBase = GameKnowledgeEntry.LoadFromResources();
            this.BuildKeywordIndex();
        }

        private int CalculateRelevanceScore(
            GameKnowledgeEntry entry,
            List<string> queryKeywords,
            List<string> npcKnowledgeTags)
        {
            int score = 0;

            // 关键词匹配
            foreach (string queryKw in queryKeywords)
            {
                foreach (string entryKw in entry.keywords)
                {
                    if (entryKw.Contains(queryKw, StringComparison.OrdinalIgnoreCase)
                        || queryKw.Contains(entryKw, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 5;
                    }
                }

                // 标题匹配
                if (entry.title != null && entry.title.Contains(queryKw, StringComparison.OrdinalIgnoreCase))
                {
                    score += 3;
                }

                // 内容匹配
                if (entry.content != null && entry.content.Contains(queryKw, StringComparison.OrdinalIgnoreCase))
                {
                    score += 1;
                }
            }

            // NPC 知识标签匹配
            if (npcKnowledgeTags != null && entry.category != null)
            {
                foreach (string tag in npcKnowledgeTags)
                {
                    if (entry.category.Equals(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 10;
                        break;
                    }
                }
            }

            return score;
        }

        private List<string> ExtractKeywords(string text)
        {
            // 简单分词：按空格、标点分割，去重
            char[] separators = { ' ', ',', '，', '。', '.', '?', '？', '!', '！', '、', '\n', '\r', '\t' };
            string[] parts = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length >= 1)
                {
                    keywords.Add(trimmed);
                }
            }

            return keywords.ToList();
        }

        private void BuildKeywordIndex()
        {
            this.keywordIndex = new Dictionary<string, List<GameKnowledgeEntry>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (GameKnowledgeEntry entry in this.knowledgeBase)
            {
                foreach (string keyword in entry.keywords)
                {
                    if (!this.keywordIndex.TryGetValue(keyword, out List<GameKnowledgeEntry> list))
                    {
                        list = new List<GameKnowledgeEntry>();
                        this.keywordIndex[keyword] = list;
                    }

                    list.Add(entry);
                }
            }
        }
    }
}
