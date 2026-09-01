namespace LAB2D.Domain.Character
{
    using System.Collections.Generic;

    /// <summary>
    /// 属性计算服务 — 纯 C# 规则，将基础属性、等级、武器、装备合并为最终战斗属性。
    /// 不依赖 UnityEngine，不访问 MonoBehaviour 或 ScriptableObject。
    /// </summary>
    public sealed class AttributeCalculationService
    {
        public BattleStats ComputeFinalStats(
            BattleStats baseStats,
            int level,
            bool isPlayer,
            bool isWorker = false,
            BattleStats? weaponStats = null,
            IReadOnlyList<BattleStats> equipmentStats = null,
            IReadOnlyList<BattleStats> growthSources = null)
        {
            float ratio = 1.0f;
            if (isPlayer)
            {
                ratio += level * 0.1f; // 玩家每级 +10%
            }
            else if (isWorker)
            {
                ratio += level * 0.05f; // Worker 每级 +5%（减半加成）
            }

            BattleStats result = baseStats * ratio;

            if (weaponStats.HasValue)
            {
                result += weaponStats.Value;
            }

            if (equipmentStats != null)
            {
                foreach (BattleStats equipment in equipmentStats)
                {
                    result += equipment;
                }
            }

            // 成长源（词条/内功/灵根/境界等）— 纯加法，与装备同层；
            // 8 维之外的维度（上限/回蓝/吸血等）由 GrowthSourceResult.Special 单独承载，不经过此管线
            if (growthSources != null)
            {
                foreach (BattleStats growth in growthSources)
                {
                    result += growth;
                }
            }

            return result;
        }
    }
}
