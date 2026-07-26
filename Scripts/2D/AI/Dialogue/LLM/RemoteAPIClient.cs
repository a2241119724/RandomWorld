namespace LAB2D.AI.Dialogue.LLM
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;

    public class RemoteAPIClient : ILLMClient
    {
        private readonly string apiBaseUrl;
        private readonly string apiKey;
        private readonly string modelName;
        private readonly int timeoutSeconds;
        private UnityWebRequest currentRequest;

        public RemoteAPIClient(
            string apiBaseUrl,
            string apiKey,
            string modelName,
            int timeoutSeconds = LLMClientConfig.TIMEOUT_SECONDS)
        {
            this.apiBaseUrl = apiBaseUrl?.TrimEnd('/') ?? LLMClientConfig.DEFAULT_REMOTE_API_BASE_URL;
            if (this.apiBaseUrl.EndsWith("/v1"))
            {
                this.apiBaseUrl = this.apiBaseUrl.Substring(0, this.apiBaseUrl.Length - 3);
            }
            this.apiKey = apiKey ?? string.Empty;
            this.modelName = modelName ?? LLMClientConfig.DEFAULT_REMOTE_MODEL;
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<string> ChatAsync(List<ChatMessage> messages, LLMGenerationOptions options)
        {
            string url = this.apiBaseUrl + LLMClientConfig.CHAT_COMPLETIONS_PATH;
            string json = BuildRequestJson(messages, options, stream: false);

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
                    AWorkerTask.LogProvider(
                        "RemoteAPIClient.ChatAsync 失败: " + request.error,
                        LogManager.LogLevelEnum.Error);
                    return string.Empty;
                }

                string responseText = request.downloadHandler.text;
                RemoteChatCompletionResponse response =
                    JsonUtility.FromJson<RemoteChatCompletionResponse>(responseText);
                if (response?.choices != null && response.choices.Length > 0
                    && response.choices[0].message != null)
                {
                    return response.choices[0].message.content ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    "RemoteAPIClient.ChatAsync 异常: " + e.Message,
                    LogManager.LogLevelEnum.Error);
                return string.Empty;
            }
            finally
            {
                this.currentRequest = null;
            }
        }

        public async Task ChatStreamAsync(
            List<ChatMessage> messages,
            LLMGenerationOptions options,
            Action<string> onToken,
            Action onComplete,
            Action<string> onError)
        {
            string url = this.apiBaseUrl + LLMClientConfig.CHAT_COMPLETIONS_PATH;
            string json = BuildRequestJson(messages, options, stream: true);
            AWorkerTask.LogProvider(
                "[RemoteAPI ChatStreamAsync] 请求参数: " + json,
                LogManager.LogLevelEnum.Info);

            using UnityWebRequest request = CreatePostRequest(url, json);
            this.currentRequest = request;

            var handler = new RemoteSSEDownloadHandler(onToken, onComplete, onError);
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
                            AWorkerTask.LogProvider(
                                "RemoteAPIClient.ChatStreamAsync 失败: " + errorMsg,
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
                AWorkerTask.LogProvider(
                    "RemoteAPIClient.ChatStreamAsync 异常: " + e.Message,
                    LogManager.LogLevelEnum.Error);
                onError?.Invoke(e.Message);
            }
            finally
            {
                this.currentRequest = null;
            }
        }

        public void Cancel()
        {
            if (this.currentRequest != null)
            {
                this.currentRequest.Abort();
                this.currentRequest = null;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            string url = this.apiBaseUrl + "/v1/models";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 5;
            if (!string.IsNullOrEmpty(this.apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + this.apiKey);
            }

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

        private string BuildRequestJson(
            List<ChatMessage> messages, LLMGenerationOptions options, bool stream)
        {
            var sb = new StringBuilder();
            sb.Append("{\"model\":\"");
            sb.Append(EscapeJson(this.modelName));
            sb.Append("\",\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"role\":\"");
                sb.Append(EscapeJson(messages[i].role));
                sb.Append("\",\"content\":\"");
                sb.Append(EscapeJson(messages[i].content));
                sb.Append("\"}");
            }

            sb.Append("],\"temperature\":");
            sb.Append(options.temperature.ToString("F4"));
            sb.Append(",\"max_tokens\":");
            sb.Append(options.maxTokens);
            sb.Append(",\"top_p\":");
            sb.Append(options.topP.ToString("F4"));
            if (options.repeatPenalty > 0)
            {
                sb.Append(",\"repeat_penalty\":");
                sb.Append(options.repeatPenalty.ToString("F4"));
            }

            sb.Append(",\"stream\":");
            sb.Append(stream ? "true" : "false");
            if (options.deepThinking)
            {
                sb.Append(",\"thinking\":{\"type\":\"enabled\"}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static string EscapeJson(string text)
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

        private UnityWebRequest CreatePostRequest(string url, string json)
        {
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(this.apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + this.apiKey);
            }

            request.timeout = this.timeoutSeconds;
            return request;
        }

#pragma warning disable SA1307
        [Serializable]
        private class RemoteChatCompletionResponse
        {
            public RemoteChoice[] choices;
        }

        [Serializable]
        private class RemoteChoice
        {
            public RemoteMessageData message;
            public RemoteMessageData delta;
        }

        [Serializable]
        private class RemoteMessageData
        {
            public string content;
        }
#pragma warning restore SA1307

        private class RemoteSSEDownloadHandler : DownloadHandlerScript
        {
            private readonly Action<string> onToken;
            private readonly Action onComplete;
            private readonly Action<string> onError;
            private readonly byte[] buffer = new byte[65536];
            private int bufferPos;

            public RemoteSSEDownloadHandler(
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
                    RemoteChatCompletionResponse chunk =
                        JsonUtility.FromJson<RemoteChatCompletionResponse>(data);
                    if (chunk?.choices != null && chunk.choices.Length > 0)
                    {
                        RemoteMessageData delta = chunk.choices[0].delta;
                        if (delta != null && !string.IsNullOrEmpty(delta.content))
                        {
                            this.onToken?.Invoke(delta.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    AWorkerTask.LogProvider(
                        "RemoteSSE 解析失败: " + e.Message + " data: " + data,
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
