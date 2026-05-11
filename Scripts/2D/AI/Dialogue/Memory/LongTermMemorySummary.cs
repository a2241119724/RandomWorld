namespace LAB2D
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 长期记忆摘要
    /// </summary>
    [Serializable]
    public class LongTermMemorySummary
    {
        /// <summary>
        /// 对应的 NPC ID
        /// </summary>
        public string npcId;

        /// <summary>
        /// 摘要文本
        /// </summary>
        public string summaryText;

        /// <summary>
        /// 关键话题
        /// </summary>
        public List<string> keyTopics = new List<string>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public string createdAt;

        /// <summary>
        /// 覆盖的对话轮数
        /// </summary>
        public int exchangeCountCovered;

        /// <summary>
        /// 格式化为 Prompt 文本
        /// </summary>
        public string ToPromptString()
        {
            return "过去记忆：" + this.summaryText;
        }
    }
}
