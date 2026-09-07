namespace LAB2D.Domain.Gameplay.Faction
{
    using System;

    /// <summary>
    /// 阵营间关系规则（纯函数）— 关系值区间/事件增减/日衰减/互殴日限。
    /// 关系值 [-100,100]：低于宣战线（FactionWarRuleService.WarThreshold）可能开战，
    /// 事件驱动升降（跨阵营互殴恶化/掠夺恶化），无互动日缓慢向 0 回归。
    /// 零 Unity 依赖，可单测。
    /// </summary>
    public static class FactionRelationRuleService
    {
        /// <summary>关系值下限。</summary>
        public const float RelationMin = -100f;

        /// <summary>关系值上限。</summary>
        public const float RelationMax = 100f;

        /// <summary>跨阵营互殴单次关系减量。</summary>
        public const float CrossAttackDelta = -3f;

        /// <summary>互殴每日每对生效次数上限（3 次 × -3 = 每日封顶 -9）。</summary>
        public const int CrossAttackDailyCapHits = 3;

        /// <summary>无互动日关系向 0 回归量（战争疲劳外的自然淡化）。</summary>
        public const float DailyDecay = 0.2f;

        /// <summary>钳制关系到合法区间。</summary>
        public static float ClampRelation(float relation)
        {
            return Math.Max(RelationMin, Math.Min(RelationMax, relation));
        }

        /// <summary>应用一次事件增减（钳制到区间）。</summary>
        public static float ApplyEvent(float relation, float delta)
        {
            return ClampRelation(relation + delta);
        }

        /// <summary>
        /// 无互动日衰减：向 0 回归 DailyDecay（不跨越 0）。
        /// 交战对不调用本函数（战争态走 FactionWarRuleService.WarDailyRelationRecovery 回升）。
        /// </summary>
        public static float ComputeDecay(float relation)
        {
            if (relation > 0f)
            {
                return Math.Max(0f, relation - DailyDecay);
            }

            if (relation < 0f)
            {
                return Math.Min(0f, relation + DailyDecay);
            }

            return 0f;
        }

        /// <summary>有序对键（小 ID 在前，关系矩阵/战争对/账本共用）。</summary>
        public static (int A, int B) GetPairKey(int factionIdA, int factionIdB)
        {
            return factionIdA <= factionIdB ? (factionIdA, factionIdB) : (factionIdB, factionIdA);
        }

        /// <summary>互殴账本判定：当日已生效次数是否仍在封顶内。</summary>
        public static bool CanApplyCrossAttack(int hitsToday)
        {
            return hitsToday < CrossAttackDailyCapHits;
        }
    }
}
