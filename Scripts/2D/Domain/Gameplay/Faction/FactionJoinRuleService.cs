namespace LAB2D.Domain.Gameplay.Faction
{
    using System.Collections.Generic;

    /// <summary>
    /// 入伙候选阵营快照（纯数据，由 FactionManager 每日评估时采集）。
    /// </summary>
    public struct FactionCandidateSnapshot
    {
        /// <summary>阵营 ID。</summary>
        public int FactionId;

        /// <summary>当前成员数。</summary>
        public int MemberCount;

        /// <summary>阵营金库。</summary>
        public int TreasuryGold;

        /// <summary>核心等级。</summary>
        public int CoreLevel;

        /// <summary>核心是否已放置。</summary>
        public bool CorePlaced;

        /// <summary>候选人与该阵营成员的平均长期亲密度（-100..100）。</summary>
        public float AvgAffinity;

        /// <summary>候选人与该阵营成员的平均好感度（0..100）。</summary>
        public float AvgFavor;

        /// <summary>候选人家到阵营核心的距离（格）。</summary>
        public float DistanceHomeToCore;
    }

    /// <summary>
    /// 入伙决策者人格快照（纯数据）。
    /// </summary>
    public struct JoinPersonalitySnapshot
    {
        /// <summary>事业心（高野心者不愿寄人篱下）。</summary>
        public float Ambition;

        /// <summary>社交（合群者更愿入伙）。</summary>
        public float Sociality;

        /// <summary>贪婪（金库充裕度加分对贪婪者更有吸引力——当前未单独加权，预留）。</summary>
        public float Greed;

        /// <summary>心情。</summary>
        public float Mood;
    }

    /// <summary>
    /// 阵营入伙规则（纯函数，确定性打分无随机）— 玩家可从性格与关系稳定预测
    /// 某个村民会投奔哪个阵营。亲密度/好感权重最高，距离每 10 格 -2，
    /// 高野心者扣分（想自己当领袖）；总分过 55 才入伙，取最高分阵营。
    /// </summary>
    public static class FactionJoinRuleService
    {
        /// <summary>入伙总分门槛。</summary>
        public const float JoinScoreThreshold = 55f;

        /// <summary>单阵营成员上限。</summary>
        public const int MaxMembers = 20;

        /// <summary>家到核心每 10 格的距离惩罚。</summary>
        public const float DistancePenaltyPer10Cells = 2f;

        /// <summary>金库充裕加分的进位单位（每 200 金 +1，封顶 +10）。</summary>
        public const float TreasuryBonusPerGold = 200f;

        /// <summary>金库充裕加分封顶。</summary>
        public const float TreasuryBonusCap = 10f;

        /// <summary>野心过 50 后每点的寄人篱下惩罚系数。</summary>
        public const float AmbitionIndependenceScale = 0.2f;

        /// <summary>
        /// 计算候选人对某阵营的入伙分。满员返回 MinValue（直接排除）。
        /// </summary>
        public static float ComputeJoinScore(in JoinPersonalitySnapshot personality, in FactionCandidateSnapshot candidate)
        {
            if (candidate.MemberCount >= MaxMembers)
            {
                return float.MinValue;
            }

            float treasuryBonus = candidate.TreasuryGold / TreasuryBonusPerGold;
            if (treasuryBonus > TreasuryBonusCap)
            {
                treasuryBonus = TreasuryBonusCap;
            }

            float score = 30f
                + (personality.Sociality * 0.10f)
                + (candidate.AvgAffinity * 0.30f)
                + (candidate.AvgFavor * 0.20f)
                + (candidate.CoreLevel * 3f)
                + treasuryBonus
                - ((candidate.DistanceHomeToCore / 10f) * DistancePenaltyPer10Cells);

            if (personality.Ambition > 50f)
            {
                score -= (personality.Ambition - 50f) * AmbitionIndependenceScale;
            }

            return score;
        }

        /// <summary>
        /// 入伙决策：在候选阵营中取最高分，过门槛才加入。
        /// </summary>
        /// <returns>是否加入；bestFactionId 输出目标阵营 ID（不加入时为 0）。</returns>
        public static bool DecideJoin(
            in JoinPersonalitySnapshot personality,
            IReadOnlyList<FactionCandidateSnapshot> candidates,
            out int bestFactionId,
            out float bestScore)
        {
            bestFactionId = 0;
            bestScore = float.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                FactionCandidateSnapshot candidate = candidates[i];
                float score = ComputeJoinScore(in personality, in candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFactionId = candidate.FactionId;
                }
            }

            if (bestFactionId == 0 || bestScore < JoinScoreThreshold)
            {
                bestFactionId = 0;
                return false;
            }

            return true;
        }
    }
}
