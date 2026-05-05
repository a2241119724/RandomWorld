namespace LAB2D
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
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
        /// 使用ollama对话
        /// </summary>
        /// <param name="question">问题</param>
        /// <returns>回答</returns>
        public async Task Chat(string question)
        {
            GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.RIGHT_CHAT_ITEM, this.content, false);
            Tool.GetComponentInChildren<Text>(g, "Text").text = question;
            string url = "http://127.0.0.1:11434/api/chat";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (request == null)
            {
                return;
            }

            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 1000;
            string jsonData = @"{
                ""model"": ""deepseek-r1:1.5b"",
                ""messages"": [
                    {
                        ""role"": ""user"", 
                        ""content"": """ + question + @"""
                    }
                ]
            }";
            byte[] body = Encoding.UTF8.GetBytes(jsonData);
            string text = string.Empty;
            try
            {
                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(body, 0, body.Length);
                }

                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response == null)
                {
                    return;
                }

                using (Stream stream = response.GetResponseStream())
                {
                    using StreamReader reader = new (stream, Encoding.UTF8);
                    ChatData chatData;
                    bool isStart = false;
                    do
                    {
                        chatData = JsonUtility.FromJson<ChatData>(await reader.ReadLineAsync());
                        if (isStart && !chatData.message.content.Equals("\n\n"))
                        {
                            text += chatData.message.content;
                        }

                        if (chatData.message.content.Equals("</think>"))
                        {
                            isStart = true;
                        }
                    }
                    while (!chatData.done && !reader.EndOfStream);
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
                Tool.GetComponentInChildren<Text>(g, "Text").text = text;
                this.isWorking = false;
            }
        }

        public void Awake()
        {
            Instance = this;
            this.input = Tool.GetComponentInChildren<Text>(this.gameObject, "Message");
            this.content = Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
        }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        /// <summary>
        /// 聊天数据
        /// </summary>
        [Serializable]
        public class ChatData
        {
            /// <summary>
            /// 模型
            /// </summary>
            public string model;

            /// <summary>
            /// 创建
            /// </summary>
            public string created_at;

            /// <summary>
            /// 信息
            /// </summary>
            public Message message;

            /// <summary>
            /// 结束
            /// </summary>
            public bool done;

            /// <summary>
            /// 消息
            /// </summary>
            [Serializable]
            public class Message
            {
                /// <summary>
                /// 角色
                /// </summary>
                public string role;

                /// <summary>
                /// 内容
                /// </summary>
                public string content;
            }
        }
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
    }
}
