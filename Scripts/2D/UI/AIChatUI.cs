using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class AIChatUI : MonoBehaviour
    {
        public static AIChatUI Instance { get; private set; }
        public Text input;
        public Transform content;
        
        private bool isWorking = false;

        private void Awake()
        {
            Instance = this;
            input = Tool.GetComponentInChildren<Text>(gameObject, "Message");
            content = Tool.GetComponentInChildren<Transform>(gameObject, "Content");
        }

        public void send()
        {
            if(isWorking) return;
            isWorking = true;
            _ = chat(input.text);
        }

        /// <summary>
        /// 使用ollama对话
        /// </summary>
        /// <returns></returns>
        public async Task chat(string question)
        {
            GameObject g = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("RightChatItem"), content,false);
            Tool.GetComponentInChildren<Text>(g, "Text").text = question;
            string url = "http://127.0.0.1:11434/api/chat";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (request == null) return;
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
            string text = "";
            try
            {
                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(body, 0, body.Length);
                }
                HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
                if (response == null) return;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
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
                        } while (!chatData.done && !reader.EndOfStream);
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.log("AIChatUI请求失败: " + e.Message, LogManager.LogLevel.Error);
                text = "请求失败";
            }
            finally
            {
                g = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("LeftChatItem"), content, false);
                g.transform.SetParent(content);
                Tool.GetComponentInChildren<Text>(g, "Text").text = text;
                isWorking = false;
            }
        }

        [Serializable]
        class ChatData
        {
            public string model;
            public string created_at;
            public Message message;
            public bool done;

            [Serializable]
            public class Message
            {
                public string role;
                public string content;
            }
        }
    }
}
