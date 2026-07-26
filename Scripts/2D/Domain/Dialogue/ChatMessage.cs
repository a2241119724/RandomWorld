namespace LAB2D.Domain.Dialogue
{
    using System;

    /// <summary>
    /// 聊天消息 — 简单的 role+content 数据载体。
    /// 从 LAB2D.AI.Dialogue.LLM 迁移至 Domain 层以消除反向依赖。
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>角色 (system/user/assistant)。</summary>
        public string role;

        /// <summary>消息内容。</summary>
        public string content;

        public ChatMessage()
        {
        }

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}
