namespace LAB2D.AI.Dialogue.Core
{
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.AI.Dialogue.Prompt;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 对话会话，保存一次 NPC 对话的临时状态
    /// </summary>
    [Serializable]
    public class DialogueSession
    {
        /// <summary>
        /// NPC 唯一 ID
        /// </summary>
        public string npcId;

        /// <summary>
        /// NPC 配置
        /// </summary>
        public NPCPromptProfile profile;

        /// <summary>
        /// 生成参数
        /// </summary>
        public LLMGenerationOptions options;

        /// <summary>
        /// 累积中回复（流式输出时拼接）
        /// </summary>
        public StringBuilder accumulatedResponse = new StringBuilder();

        /// <summary>
        /// 完整对话历史
        /// </summary>
        public List<ChatMessage> fullHistory = new List<ChatMessage>();

        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool isActive = true;

        /// <summary>
        /// 构造
        /// </summary>
        public DialogueSession(string npcId, NPCPromptProfile profile)
        {
            this.npcId = npcId;
            this.profile = profile;
            this.options = new LLMGenerationOptions
            {
                deepThinking = ModelSourceSettings.DeepThinkingEnabled,
            };
        }

        /// <summary>
        /// 获取完整回复文本
        /// </summary>
        public string GetFullResponse()
        {
            return this.accumulatedResponse.ToString();
        }

        /// <summary>
        /// 添加玩家消息
        /// </summary>
        public void AddPlayerMessage(string text)
        {
            this.fullHistory.Add(new ChatMessage("user", text));
        }

        /// <summary>
        /// 添加 NPC 回复
        /// </summary>
        public void AddNPCMessage(string text)
        {
            this.fullHistory.Add(new ChatMessage("assistant", text));
        }
    }
}
