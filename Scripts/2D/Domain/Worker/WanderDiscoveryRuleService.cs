namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// 漫游发现物品纯规则 — 漫游路点小概率（决策层 5%）发现随机基础资源掉落。
    /// 池均匀随机 + 数量 [MinCount, MaxCount]；随机源由调用方注入（决策层传
    /// UnityEngine.Random.value），同输入同输出可单测。零 Unity 依赖。
    /// </summary>
    public static class WanderDiscoveryRuleService
    {
        /// <summary>单次发现的最小数量。</summary>
        public const int MinCount = 1;

        /// <summary>单次发现的最大数量。</summary>
        public const int MaxCount = 2;

        /// <summary>
        /// 从候选 id 池均匀随机选一个物品并 roll 数量。
        /// </summary>
        /// <param name="candidateIds">候选物品 id 池（Material 段基础资源）</param>
        /// <param name="idRoll">[0,1] id 随机源（越界钳制）</param>
        /// <param name="countRoll">[0,1] 数量随机源（越界钳制）</param>
        /// <param name="itemId">选中的物品 id</param>
        /// <param name="count">数量 [MinCount, MaxCount]</param>
        /// <returns>池空返回 false（不生成掉落）</returns>
        public static bool TryRoll(int[] candidateIds, float idRoll, float countRoll, out int itemId, out int count)
        {
            itemId = 0;
            count = 0;
            if (candidateIds == null || candidateIds.Length == 0)
            {
                return false;
            }

            // 均匀分桶：roll ∈ [0,1] 钳制后乘池长取整，边界 1.0 落最后一桶
            float clampedId = idRoll < 0f ? 0f : (idRoll > 1f ? 1f : idRoll);
            int index = (int)(clampedId * candidateIds.Length);
            if (index >= candidateIds.Length)
            {
                index = candidateIds.Length - 1;
            }

            itemId = candidateIds[index];

            float clampedCount = countRoll < 0f ? 0f : (countRoll > 1f ? 1f : countRoll);
            count = MinCount + (int)(clampedCount * (MaxCount - MinCount + 1));
            if (count > MaxCount)
            {
                count = MaxCount;
            }

            return true;
        }
    }
}
