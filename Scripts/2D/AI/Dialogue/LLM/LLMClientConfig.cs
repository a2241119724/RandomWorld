namespace LAB2D.AI.Dialogue.LLM
{
    using LAB2D.Constant;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// LLM 客户端配置常量
    /// </summary>
    public static class LLMClientConfig
    {
        /// <summary>
        /// 默认 llama-server 地址
        /// </summary>
        public const string DEFAULT_SERVER_URL = "http://127.0.0.1:8080";

        /// <summary>
        /// Chat Completions API 路径
        /// </summary>
        public const string CHAT_COMPLETIONS_PATH = "/v1/chat/completions";

        /// <summary>
        /// 默认模型名称
        /// </summary>
        public const string DEFAULT_MODEL = "Qwen2.5-0.5B-Instruct-Q8_0";

        /// <summary>
        /// 默认内置模型文件路径
        /// </summary>
        public const string DEFAULT_MODEL_RELATIVE_PATH = ResourceConstant.BUILTIN_LLM_MODEL_RELATIVE_PATH;

        /// <summary>
        /// 内置 llama-server 目录名
        /// </summary>
        public const string SERVER_DIRECTORY = ResourceConstant.STREAMING_AI_ROOT;

        /// <summary>
        /// 旧版内置 llama-server 目录名
        /// </summary>
        public const string LEGACY_SERVER_DIRECTORY = "llama-server";

        /// <summary>
        /// Windows 可执行文件名
        /// </summary>
        public const string SERVER_EXECUTABLE_WINDOWS = "llama-server.exe";

        /// <summary>
        /// 其他桌面平台可执行文件名
        /// </summary>
        public const string SERVER_EXECUTABLE_UNIX = "llama-server";

        /// <summary>
        /// 启动等待超时（秒）
        /// </summary>
        public const int SERVER_START_TIMEOUT_SECONDS = 90;

        /// <summary>
        /// 启动探测间隔（毫秒）
        /// </summary>
        public const int SERVER_START_PROBE_INTERVAL_MS = 500;

        /// <summary>
        /// 默认上下文长度
        /// </summary>
        public const int DEFAULT_CONTEXT_SIZE = 2048;

        /// <summary>
        /// 请求超时（秒）
        /// </summary>
        public const int TIMEOUT_SECONDS = 120;

        /// <summary>
        /// 默认内置模型绝对路径
        /// </summary>
        public static string DefaultModelPath =>
            Path.Combine(Application.streamingAssetsPath, DEFAULT_MODEL_RELATIVE_PATH);

        /// <summary>
        /// 默认远程 API 地址
        /// </summary>
        public const string DEFAULT_REMOTE_API_BASE_URL = "https://api.deepseek.com";

        /// <summary>
        /// 默认远程模型名称
        /// </summary>
        public const string DEFAULT_REMOTE_MODEL = "deepseek-v4-pro";
    }
}
