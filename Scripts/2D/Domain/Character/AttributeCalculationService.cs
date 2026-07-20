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
            BattleStats? weaponStats,
            IReadOnlyList<BattleStats> equipmentStats)
        {
            float ratio = 1.0f;
            if (isPlayer)
            {
                ratio += level * 0.1f;
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

            return result;
        }
    }
}
