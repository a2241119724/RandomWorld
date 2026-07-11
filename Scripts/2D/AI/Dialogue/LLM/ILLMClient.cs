namespace LAB2D.AI.Dialogue.LLM
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// LLM 客户端接口
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// 非流式对话，返回完整回复
        /// </summary>
        Task<string> ChatAsync(List<ChatMessage> messages, LLMGenerationOptions options);

        /// <summary>
        /// 流式对话，逐 token 回调
        /// </summary>
        Task ChatStreamAsync(
            List<ChatMessage> messages,
            LLMGenerationOptions options,
            Action<string> onToken,
            Action onComplete,
            Action<string> onError);

        /// <summary>
        /// 取消当前请求
        /// </summary>
        void Cancel();

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        Task<bool> IsAvailableAsync();
    }
}
