namespace LAB2D.AI.Dialogue.LLM
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
        public float temperature = 0.01f;

        /// <summary>
        /// 最大 token 数
        /// </summary>
        public int maxTokens = 256;

        /// <summary>
        /// TopP 采样
        /// </summary>
        public float topP = 0.01f;

        /// <summary>
        /// 重复惩罚
        /// </summary>
        public float repeatPenalty = 1.2f;

        /// <summary>
        /// 是否流式输出
        /// </summary>
        public bool stream = true;
    }
}
