namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.AI.Dialogue.Core;
    using LAB2D.AI.Dialogue.LLM;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// AI聊天UI
    /// </summary>
    public class AIChatUI : MonoBehaviour
    {
        private Text input;
        private Transform content;
        private ILLMClient llmClient;
        private bool isWorking = false;

        /// <summary>
        /// 单例
        /// </summary>
        public static AIChatUI Instance { get; private set; }

        /// <summary>
        /// 发送聊天记录
        /// </summary>
        public void Send()
        {
            if (this.isWorking)
            {
                return;
            }

            this.isWorking = true;
            _ = this.Chat(this.input.text);
        }

        /// <summary>
        /// 使用内置 GGUF 模型对话
        /// </summary>
        /// <param name="question">问题</param>
        /// <returns>回答</returns>
        public async Task Chat(string question)
        {
            GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.RIGHT_CHAT_ITEM, this.content, false);
            LAB2D.Tool.Tool.GetComponentInChildren<Text>(g, "Text").text = question;
            string text = string.Empty;
            try
            {
                var messages = new List<ChatMessage>
                {
                    new ChatMessage("system", "你是游戏内置的中文助手，回答要简洁、自然，并且只输出最终回答。"),
                    new ChatMessage("user", question),
                };

                text = await this.llmClient.ChatAsync(messages, new LLMGenerationOptions { stream = false });
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = "模型服务未响应";
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.Log("AIChatUI请求失败: " + e, LogManager.LogLevelEnum.Error);
                text = "请求失败";
            }
            finally
            {
                g = ResourceManager.Instance.Instantiate(PrefabConstant.LEFT_CHAT_ITEM, this.content, false);
                g.transform.SetParent(this.content);
                LAB2D.Tool.Tool.GetComponentInChildren<Text>(g, "Text").text = text;
                this.isWorking = false;
            }
        }

        public void Awake()
        {
            Instance = this;
            this.llmClient = DialogueManager.Instance.GetLLMClient();
            this.input = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Message");
            this.content = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
        }
    }
}
