namespace LAB2D.Domain.Worker
{
    using System.Collections.Generic;

    /// <summary>
    /// Worker 事件记忆纯规则服务 — 记忆的写入、逐日衰减与遗忘剔除。
    /// 零 Unity 依赖，可单测。数据本体在 WorkerData.Mind.Memories。
    /// </summary>
    public static class WorkerMemoryRuleService
    {
        /// <summary>
        /// 写入一条事件记忆。超过 MemoryCap 时丢弃最旧（Day 最小，同日取最早插入）。
        /// </summary>
        /// <param name="mind">目标 Mind 容器。</param>
        /// <param name="day">当前游戏日。</param>
        /// <param name="typeKey">事件类型键（WorkerMindConstant.EVT_*）。</param>
        /// <param name="valence">事件正负向。</param>
        /// <param name="targetName">相关目标："PLAYER" 哨兵或 Worker 稳定名；空 = 无目标。</param>
        /// <param name="intensity">事件强度 0-100。</param>
        /// <returns>是否成功写入。</returns>
        public static bool AddMemory(
            WorkerMindData mind, int day, string typeKey,
            MemoryValence valence, string targetName, float intensity)
        {
            if (mind == null || string.IsNullOrEmpty(typeKey))
            {
                return false;
            }

            mind.Memories.Add(new WorkerMemoryEntry
            {
                Day = day,
                TypeKey = typeKey,
                Valence = valence,
                TargetName = targetName,
                Intensity = ClampIntensity(intensity),
                Weight = 1f,
            });

            while (mind.Memories.Count > mind.MemoryCap)
            {
                int oldestIndex = FindOldestIndex(mind.Memories);
                if (oldestIndex < 0)
                {
                    break;
                }

                mind.Memories.RemoveAt(oldestIndex);
            }

            return true;
        }

        /// <summary>
        /// 游戏日切换时逐日衰减记忆权重，并剔除低于剪枝阈值的记忆（遗忘）。
        /// 由 WorkerMindManager 在检测到游戏日变化时驱动（每游戏日一次）。
        /// </summary>
        /// <returns>被遗忘（剔除）的记忆条数。</returns>
        public static int TickDay(WorkerMindData mind, int currentDay)
        {
            if (mind == null || mind.Memories.Count == 0)
            {
                return 0;
            }

            int removed = 0;
            for (int i = mind.Memories.Count - 1; i >= 0; i--)
            {
                WorkerMemoryEntry m = mind.Memories[i];
                m.Weight -= WorkerMindConstant.MemoryForgetRatePerDay;
                if (m.Weight < WorkerMindConstant.MemoryWeightPrune)
                {
                    mind.Memories.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// 查找最近一条记忆（可指定类型过滤）。供信念/关系层引用，未命中返回 null。
        /// </summary>
        public static WorkerMemoryEntry FindRecent(WorkerMindData mind, string typeKey = null)
        {
            if (mind == null || mind.Memories.Count == 0)
            {
                return null;
            }

            for (int i = mind.Memories.Count - 1; i >= 0; i--)
            {
                WorkerMemoryEntry m = mind.Memories[i];
                if (typeKey != null && m.TypeKey != typeKey)
                {
                    continue;
                }

                return m;
            }

            return null;
        }

        /// <summary>查找索引最早插入且 Day 最小的一条记忆。</summary>
        private static int FindOldestIndex(List<WorkerMemoryEntry> memories)
        {
            int oldestIndex = -1;
            int oldestDay = int.MaxValue;
            for (int i = 0; i < memories.Count; i++)
            {
                if (memories[i].Day < oldestDay)
                {
                    oldestDay = memories[i].Day;
                    oldestIndex = i;
                }
            }

            return oldestIndex;
        }

        private static float ClampIntensity(float intensity)
        {
            if (intensity < 0f) return 0f;
            if (intensity > 100f) return 100f;
            return intensity;
        }
    }
}
