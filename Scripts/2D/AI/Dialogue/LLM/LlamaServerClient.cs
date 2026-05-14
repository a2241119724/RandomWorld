namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// llama-server 客户端，通过 OpenAI 兼容 API 调用本地 GGUF 模型
    /// </summary>
    public class LlamaServerClient : ILLMClient
    {
        private readonly string serverUrl;
        private readonly string modelName;
        private readonly int timeoutSeconds;
        private UnityWebRequest currentRequest;

        public LlamaServerClient(
            string serverUrl = LLMClientConfig.DEFAULT_SERVER_URL,
            string modelName = LLMClientConfig.DEFAULT_MODEL,
            int timeoutSeconds = LLMClientConfig.TIMEOUT_SECONDS)
        {
            this.serverUrl = serverUrl;
            this.modelName = modelName;
            this.timeoutSeconds = timeoutSeconds;
        }

        /// <inheritdoc/>
        public async Task<string> ChatAsync(List<ChatMessage> messages, LLMGenerationOptions options)
        {
            string json = BuildRequestJson(messages, options, stream: false);
            string url = this.serverUrl + LLMClientConfig.CHAT_COMPLETIONS_PATH;
            LogManager.Instance.Log(
                "[LLM ChatAsync] 请求参数: " + json,
                LogManager.LogLevelEnum.Info);

            using UnityWebRequest request = CreatePostRequest(url, json);
            this.currentRequest = request;

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                operation.completed += _ => { tcs.TrySetResult(true); };
                await tcs.Task;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogManager.Instance.Log(
                        "LlamaServerClient.ChatAsync 失败: " + request.error,
                        LogManager.LogLevelEnum.Error);
                    return string.Empty;
                }

                string responseText = request.downloadHandler.text;
                ChatCompletionResponse response = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
                if (response?.choices != null && response.choices.Length > 0
                    && response.choices[0].message != null)
                {
                    return response.choices[0].message.content ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    "LlamaServerClient.ChatAsync 异常: " + e.Message,
                    LogManager.LogLevelEnum.Error);
                return string.Empty;
            }
            finally
            {
                this.currentRequest = null;
            }
        }

        /// <inheritdoc/>
        public async Task ChatStreamAsync(
            List<ChatMessage> messages,
            LLMGenerationOptions options,
            Action<string> onToken,
            Action onComplete,
            Action<string> onError)
        {
            string json = BuildRequestJson(messages, options, stream: true);
            string url = this.serverUrl + LLMClientConfig.CHAT_COMPLETIONS_PATH;
            LogManager.Instance.Log(
                "[LLM ChatStreamAsync] 请求参数: " + json,
                LogManager.LogLevelEnum.Debug);

            using UnityWebRequest request = CreatePostRequest(url, json);
            this.currentRequest = request;

            var handler = new SSEDownloadHandler(onToken, onComplete, onError);
            request.downloadHandler = handler;

            var tcs = new TaskCompletionSource<bool>();

            try
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                operation.completed += _ =>
                {
                    try
                    {
                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            string errorMsg = request.error ?? "未知网络错误";
                            LogManager.Instance.Log(
                                "LlamaServerClient.ChatStreamAsync 失败: " + errorMsg,
                                LogManager.LogLevelEnum.Error);

                            onError?.Invoke(errorMsg);
                        }
                        else
                        {
                            handler.FlushRemaining();
                            handler.NotifyComplete();
                        }
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                    }
                };

                await tcs.Task;
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    "LlamaServerClient.ChatStreamAsync 异常: " + e.Message,
                    LogManager.LogLevelEnum.Error);
                onError?.Invoke(e.Message);
            }
            finally
            {
                this.currentRequest = null;
            }
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            if (this.currentRequest != null)
            {
                this.currentRequest.Abort();
                this.currentRequest = null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsAvailableAsync()
        {
            string url = this.serverUrl + "/v1/models";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 5;

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                operation.completed += _ => { tcs.TrySetResult(true); };
                await tcs.Task;
                return request.result == UnityWebRequest.Result.Success;
            }
            catch
            {
                return false;
            }
        }

        private UnityWebRequest CreatePostRequest(string url, string json)
        {
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = this.timeoutSeconds;
            return request;
        }

        private string BuildRequestJson(
            List<ChatMessage> messages, LLMGenerationOptions options, bool stream)
        {
            var wrapper = new ChatCompletionRequest
            {
                model = this.modelName,
                messages = ConvertMessages(messages),
                temperature = options.temperature,
                max_tokens = options.maxTokens,
                top_p = options.topP,
                stream = stream,
            };

            if (options.repeatPenalty > 0)
            {
                wrapper.repeat_penalty = options.repeatPenalty;
            }

            return JsonUtility.ToJson(wrapper);
        }

        private ChatMessageForJson[] ConvertMessages(List<ChatMessage> messages)
        {
            var result = new ChatMessageForJson[messages.Count];
            for (int i = 0; i < messages.Count; i++)
            {
                result[i] = new ChatMessageForJson
                {
                    role = messages[i].role,
                    content = messages[i].content,
                };
            }

            return result;
        }

        private static string EscapeJsonString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace("\t", "\\t");
        }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        [Serializable]
        private class ChatCompletionRequest
        {
            public string model;
            public ChatMessageForJson[] messages;
            public float temperature;
            public int max_tokens;
            public float top_p;
            public float repeat_penalty;
            public bool stream;
        }

        [Serializable]
        private class ChatMessageForJson
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatCompletionResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public MessageData message;
            public MessageData delta;
            public string finish_reason;
        }

        [Serializable]
        private class MessageData
        {
            public string content;
        }
#pragma warning restore SA1307

        /// <summary>
        /// SSE 流式下载处理器
        /// </summary>
        private class SSEDownloadHandler : DownloadHandlerScript
        {
            private readonly Action<string> onToken;
            private readonly Action onComplete;
            private readonly Action<string> onError;
            private readonly byte[] buffer = new byte[65536];
            private int bufferPos;

            public SSEDownloadHandler(
                Action<string> onToken,
                Action onComplete,
                Action<string> onError)
            {
                this.onToken = onToken;
                this.onComplete = onComplete;
                this.onError = onError;
                this.bufferPos = 0;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength <= 0)
                {
                    return true;
                }

                if (this.bufferPos + dataLength > this.buffer.Length)
                {
                    this.FlushRemaining();
                }

                Array.Copy(data, 0, this.buffer, this.bufferPos, dataLength);
                this.bufferPos += dataLength;

                this.ProcessBuffer();
                return true;
            }

            public void FlushRemaining()
            {
                this.ProcessBuffer();
            }

            public void NotifyComplete()
            {
                this.onComplete?.Invoke();
            }

            private void ProcessBuffer()
            {
                string text = Encoding.UTF8.GetString(this.buffer, 0, this.bufferPos);
                int lastComplete = 0;
                int searchStart = 0;

                while (searchStart < text.Length)
                {
                    int eventEnd = text.IndexOf("\n\n", searchStart, StringComparison.Ordinal);
                    if (eventEnd < 0)
                    {
                        break;
                    }

                    int lineStart = lastComplete;
                    while (lineStart < eventEnd)
                    {
                        int lineEnd = text.IndexOf('\n', lineStart);
                        if (lineEnd < 0 || lineEnd > eventEnd)
                        {
                            lineEnd = eventEnd;
                        }

                        string line = text.Substring(lineStart, lineEnd - lineStart).Trim();

                        if (line.StartsWith("data: ", StringComparison.Ordinal))
                        {
                            string data = line.Substring(6);
                            this.ParseSSEData(data);
                        }

                        lineStart = lineEnd + 1;
                    }

                    searchStart = eventEnd + 2;
                    lastComplete = searchStart;
                }

                if (lastComplete > 0 && lastComplete < this.bufferPos)
                {
                    int remaining = this.bufferPos - lastComplete;
                    Array.Copy(this.buffer, lastComplete, this.buffer, 0, remaining);
                    this.bufferPos = remaining;
                }
                else if (lastComplete >= this.bufferPos)
                {
                    this.bufferPos = 0;
                }
            }

            private void ParseSSEData(string data)
            {
                if (string.IsNullOrEmpty(data) || data == "[DONE]")
                {
                    return;
                }

                try
                {
                    ChatCompletionResponse chunk = JsonUtility.FromJson<ChatCompletionResponse>(data);
                    if (chunk?.choices != null && chunk.choices.Length > 0)
                    {
                        MessageData delta = chunk.choices[0].delta;
                        if (delta != null && !string.IsNullOrEmpty(delta.content))
                        {
                            string token = delta.content;
                            this.onToken?.Invoke(token);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogManager.Instance.Log(
                        "SSE 解析失败: " + e.Message + " data: " + data,
                        LogManager.LogLevelEnum.Warning);
                }
            }

            protected override void CompleteContent()
            {
            }

            protected override string GetText()
            {
                return Encoding.UTF8.GetString(this.buffer, 0, this.bufferPos);
            }
        }
    }
}
