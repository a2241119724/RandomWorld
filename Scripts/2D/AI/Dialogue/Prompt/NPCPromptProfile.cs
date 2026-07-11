namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// NPC Prompt 配置
    /// </summary>
    [CreateAssetMenu(menuName = "SO/AI/NPCPromptProfile", order = 0)]
    public class NPCPromptProfile : ScriptableObject
    {
        /// <summary>
        /// NPC 名称
        /// </summary>
        public string npcName = "无名NPC";

        /// <summary>
        /// NPC 职业/角色
        /// </summary>
        public string npcRole = "村民";

        /// <summary>
        /// NPC 所在地点
        /// </summary>
        public string npcLocation = "边境小镇";

        /// <summary>
        /// 性格描述
        /// </summary>
        [TextArea(3, 10)]
        public string personalityDescription = "友善、乐于助人";

        /// <summary>
        /// 背景故事
        /// </summary>
        [TextArea(3, 10)]
        public string backgroundStory = string.Empty;

        /// <summary>
        /// 说话风格
        /// </summary>
        [TextArea(2, 5)]
        public string speakingStyle = "说话简洁、直率";

        /// <summary>
        /// 初始好感度 0-100
        /// </summary>
        [Range(0, 100)]
        public float initialFavorability = 50;

        /// <summary>
        /// 默认心情
        /// </summary>
        public string defaultMood = "中立";

        /// <summary>
        /// RAG 知识标签（用于检索）
        /// </summary>
        public List<string> knowledgeTags = new List<string>();

        /// <summary>
        /// 回答最大长度（句子数）
        /// </summary>
        [Range(1, 5)]
        public int maxSentences = 3;

        /// <summary>
        /// 启用时，每次 LLM 请求只发送系统提示词和当前玩家输入。
        /// </summary>
        public bool sendOnlyCurrentMessage = true;
    }
}
