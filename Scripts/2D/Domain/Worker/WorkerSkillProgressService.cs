namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// 工人技能熟练度进度倍率的纯算术。
    /// 熟练度范围 [0, 100]，练习增长，熟练工种干得更快。
    /// </summary>
    public static class WorkerSkillProgressService
    {
        /// <summary>每次完成核心工作类任务增长的熟练度（0-100 封顶）。</summary>
        public const float SkillGainPerCompletion = 0.8f;

        /// <summary>每点熟练度带来的进度倍率加成（100 点 = +40%）。</summary>
        public const float MultiplierPerPoint = 0.004f;

        /// <summary>
        /// 根据熟练度计算任务进度倍率。
        /// </summary>
        /// <param name="proficiency">熟练度（0-100，超出自动夹取）。</param>
        /// <returns>进度倍率，1 表示无加成。</returns>
        public static float GetMultiplier(float proficiency)
        {
            float p = System.Math.Max(0f, System.Math.Min(100f, proficiency));
            return 1.0f + (p * MultiplierPerPoint);
        }
    }
}
