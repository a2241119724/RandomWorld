namespace LAB2D.AI.Dialogue.LLM
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
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
        private readonly string modelPath;
        private readonly int timeoutSeconds;
        private UnityWebRequest currentRequest;
        private bool modelFileStateLogged;
        private string lastAvailabilityError = string.Empty;

        public LlamaServerClient(
            string serverUrl = LLMClientConfig.DEFAULT_SERVER_URL,
            string modelName = LLMClientConfig.DEFAULT_MODEL,
            int timeoutSeconds = LLMClientConfig.TIMEOUT_SECONDS,
            string modelPath = null)
        {
            this.serverUrl = serverUrl;
            this.modelName = modelName;
            this.modelPath = string.IsNullOrEmpty(modelPath)
                ? LLMClientConfig.DefaultModelPath
                : modelPath;
            this.timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// 当前客户端期望使用的本地 GGUF 模型路径.
        /// </summary>
        public string ModelPath => this.modelPath;

        /// <inheritdoc/>
        public async Task<string> ChatAsync(List<ChatMessage> messages, LLMGenerationOptions options)
        {
            if (!await this.EnsureServerAvailableAsync())
            {
                if (!string.IsNullOrEmpty(this.lastAvailabilityError))
                {
                    LogManager.Instance.Log(this.lastAvailabilityError, LogManager.LogLevelEnum.Error);
                }

                return string.Empty;
            }

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
            if (!await this.EnsureServerAvailableAsync())
            {
                string error = string.IsNullOrEmpty(this.lastAvailabilityError)
                    ? "内置 llama-server 未启动，无法请求本地模型"
                    : this.lastAvailabilityError;
                LogManager.Instance.Log(error, LogManager.LogLevelEnum.Error);
                onError?.Invoke(error);
                return;
            }

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
            return await this.EnsureServerAvailableAsync();
        }

        private async Task<bool> ProbeServerAsync(int timeoutSeconds)
        {
            string url = this.serverUrl + "/v1/models";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = timeoutSeconds;

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

        private async Task<bool> EnsureServerAvailableAsync()
        {
            this.LogModelFileStateOnce();

            // 快速失败：模型文件不存在时跳过网络探测，避免 2 秒超时等待
            if (!string.IsNullOrEmpty(this.modelPath) && !File.Exists(this.modelPath))
            {
                this.lastAvailabilityError = "未找到内置模型文件: " + this.modelPath;
                return false;
            }

            if (await this.ProbeServerAsync(2))
            {
                this.lastAvailabilityError = string.Empty;
                return true;
            }

            if (!LocalLlamaServerProcess.TryStart(this.modelPath, this.serverUrl, this.modelName, out string error))
            {
                this.lastAvailabilityError = error;
                LogManager.Instance.Log(this.lastAvailabilityError, LogManager.LogLevelEnum.Error);
                return false;
            }

            int intervalMs = LLMClientConfig.SERVER_START_PROBE_INTERVAL_MS;
            DateTime deadline = DateTime.UtcNow.AddSeconds(LLMClientConfig.SERVER_START_TIMEOUT_SECONDS);
            while (DateTime.UtcNow < deadline)
            {
                if (await this.ProbeServerAsync(2))
                {
                    this.lastAvailabilityError = string.Empty;
                    LogManager.Instance.Log("内置 llama-server 已就绪", LogManager.LogLevelEnum.Info);
                    return true;
                }

                if (LocalLlamaServerProcess.StartedProcessHasExited)
                {
                    this.lastAvailabilityError = LocalLlamaServerProcess.GetExitError();
                    LogManager.Instance.Log(this.lastAvailabilityError, LogManager.LogLevelEnum.Error);
                    return false;
                }

                await Task.Delay(intervalMs);
            }

            this.lastAvailabilityError = "内置 llama-server 启动超时: " + this.serverUrl;
            LogManager.Instance.Log(this.lastAvailabilityError, LogManager.LogLevelEnum.Error);
            return false;
        }

        private void LogModelFileStateOnce()
        {
            if (this.modelFileStateLogged)
            {
                return;
            }

            this.modelFileStateLogged = true;
            if (string.IsNullOrEmpty(this.modelPath))
            {
                return;
            }

            bool modelFileExists = File.Exists(this.modelPath);
            LogManager.Instance.Log(
                modelFileExists
                    ? "LlamaServerClient 使用内置模型: " + this.modelPath
                    : "LlamaServerClient 未找到内置模型文件: " + this.modelPath,
                modelFileExists
                    ? LogManager.LogLevelEnum.Info
                    : LogManager.LogLevelEnum.Warning);
        }

        private static class LocalLlamaServerProcess
        {
            private static readonly object SyncRoot = new object();
            private static Process serverProcess;
            private static bool quitHookRegistered;

            public static bool StartedProcessHasExited
            {
                get
                {
                    try
                    {
                        return serverProcess != null && serverProcess.HasExited;
                    }
                    catch
                    {
                        return true;
                    }
                }
            }

            public static string GetExitError()
            {
                try
                {
                    if (serverProcess == null)
                    {
                        return "内置 llama-server 进程不存在";
                    }

                    return "内置 llama-server 进程已退出，退出码: "
                        + serverProcess.ExitCode
                        + "。请检查可执行文件依赖、模型格式和启动参数";
                }
                catch (Exception exception)
                {
                    return "内置 llama-server 进程已退出，读取退出信息失败: " + exception.Message;
                }
            }

            public static bool TryStart(
                string modelPath,
                string serverUrl,
                string modelAlias,
                out string error)
            {
                error = string.Empty;

#if !UNITY_STANDALONE && !UNITY_EDITOR
                error = "当前平台不支持通过进程启动内置 llama-server";
                return false;
#else
                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    error = "未找到内置模型文件: " + modelPath;
                    return false;
                }

                string executablePath = ResolveExecutablePath();
                if (string.IsNullOrEmpty(executablePath))
                {
                    error = "未找到内置 llama-server。请将 llama-server.exe 放到 StreamingAssets/AI/llama-server.exe";
                    return false;
                }

                lock (SyncRoot)
                {
                    if (IsProcessRunning())
                    {
                        return true;
                    }

                    if (!TryGetHostAndPort(serverUrl, out string host, out int port))
                    {
                        host = "127.0.0.1";
                        port = 8080;
                    }

                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = executablePath,
                            Arguments = BuildArguments(modelPath, host, port, modelAlias),
                            WorkingDirectory = Path.GetDirectoryName(executablePath),
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };

                        serverProcess = Process.Start(startInfo);
                        if (serverProcess == null)
                        {
                            error = "启动内置 llama-server 失败: Process.Start 返回空进程";
                            return false;
                        }

                        RegisterQuitHook();
                        LogManager.Instance.Log(
                            "已启动内置 llama-server: " + executablePath,
                            LogManager.LogLevelEnum.Info);
                        return true;
                    }
                    catch (Exception exception)
                    {
                        error = "启动内置 llama-server 失败: " + exception.Message;
                        serverProcess = null;
                        return false;
                    }
                }
#endif
            }

            private static bool IsProcessRunning()
            {
                try
                {
                    return serverProcess != null && !serverProcess.HasExited;
                }
                catch
                {
                    serverProcess = null;
                    return false;
                }
            }

            private static void RegisterQuitHook()
            {
                if (quitHookRegistered)
                {
                    return;
                }

                Application.quitting += Stop;
                quitHookRegistered = true;
            }

            private static void Stop()
            {
                lock (SyncRoot)
                {
                    try
                    {
                        if (serverProcess != null && !serverProcess.HasExited)
                        {
                            serverProcess.Kill();
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        serverProcess = null;
                    }
                }
            }

            private static string ResolveExecutablePath()
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                string executableFile = LLMClientConfig.SERVER_EXECUTABLE_WINDOWS;
#else
                string executableFile = LLMClientConfig.SERVER_EXECUTABLE_UNIX;
#endif
                string root = Application.streamingAssetsPath;
                string[] candidates =
                {
                    Path.Combine(root, LLMClientConfig.SERVER_DIRECTORY, executableFile),
                    Path.Combine(root, executableFile),
                    Path.Combine(root, LLMClientConfig.LEGACY_SERVER_DIRECTORY, executableFile),
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }

                return string.Empty;
            }

            private static string BuildArguments(string modelPath, string host, int port, string modelAlias)
            {
                return "-m " + QuoteArgument(modelPath)
                    + " --host " + QuoteArgument(host)
                    + " --port " + port
                    + " -c " + LLMClientConfig.DEFAULT_CONTEXT_SIZE
                    + " --alias " + QuoteArgument(modelAlias);
            }

            private static bool TryGetHostAndPort(string serverUrl, out string host, out int port)
            {
                host = string.Empty;
                port = 0;
                if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri uri))
                {
                    return false;
                }

                host = uri.Host;
                port = uri.Port;
                return !string.IsNullOrEmpty(host) && port > 0;
            }

            private static string QuoteArgument(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return "\"\"";
                }

                return "\"" + value.Replace("\"", "\\\"") + "\"";
            }
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
