namespace LAB2D
{
    using System;

    /// <summary>
    /// LLM 生成参数
    /// </summary>
    [Serializable]
    public class LLMGenerationOptions
    {
        /// <summary>
        /// 温度
        /// </summary>
        public float temperature = 0.65f;

        /// <summary>
        /// 最大 token 数
        /// </summary>
        public int maxTokens = 128;

        /// <summary>
        /// TopP 采样
        /// </summary>
        public float topP = 0.85f;

        /// <summary>
        /// 重复惩罚
        /// </summary>
        public float repeatPenalty = 1.18f;

        /// <summary>
        /// 是否流式输出
        /// </summary>
        public bool stream = true;
    }
}
