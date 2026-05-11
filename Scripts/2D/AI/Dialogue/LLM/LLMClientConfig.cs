namespace LAB2D
{
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
        public const string DEFAULT_MODEL = "local-npc-model";

        /// <summary>
        /// 请求超时（秒）
        /// </summary>
        public const int TIMEOUT_SECONDS = 120;
    }
}
