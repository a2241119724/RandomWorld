namespace LAB2D.AI.Dialogue.LLM
{
    using System;

    /// <summary>
    /// 聊天消息
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>
        /// 角色
        /// </summary>
        public string role;

        /// <summary>
        /// 内容
        /// </summary>
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
