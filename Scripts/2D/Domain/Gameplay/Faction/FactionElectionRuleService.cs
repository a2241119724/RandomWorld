namespace LAB2D.Domain.Gameplay.Faction
{
    using System.Collections.Generic;

    /// <summary>选举候选人快照（Manager 侧从 WorkerFaction/WorkerData 组装）。</summary>
    public struct ElectionCandidateSnapshot
    {
        /// <summary>候选人名（Worker 名）。</summary>
        public string Name;

        /// <summary>野心（候选资格门槛 + 倾向分项）。</summary>
        public float Ambition;

        /// <summary>社交（倾向分项 — 会来事者得票）。</summary>
        public float Sociality;

        /// <summary>阵营累计贡献（候选排序 + 平局裁决）。</summary>
        public float Contribution;

        /// <summary>是否现任领袖（连任加成）。</summary>
        public bool IsIncumbent;

        /// <summary>当日拉票加成（0~3，拉一次 +1；Manager 侧按拉票次数填）。</summary>
        public float CampaignBonus;
    }

    /// <summary>选举投票人快照（全员投票，含候选人自投）。</summary>
    public struct ElectionVoterSnapshot
    {
        /// <summary>投票人名。</summary>
        public string Name;

        /// <summary>票重（职位票重 + 社交因子，ComputeVoteWeight 计算）。</summary>
        public float VoteWeight;

        /// <summary>对各候选人的亲密度（名字 → Affinity，无记录按 0）。</summary>
        public Dictionary<string, float> AffinityByCandidate;
    }

    /// <summary>
    /// 阵营领袖选举规则（纯函数）— 每 5 游戏日一届：候选人按贡献取前 4，
    /// 全员加权投票（职位票重 × 亲密度/社交/野心倾向 + 现任连任/拉票加成），
    /// Tally 确定性输出胜者。零随机，同输入同结果，可单测锁定。
    /// </summary>
    public static class FactionElectionRuleService
    {
        /// <summary>选举周期（游戏日）。</summary>
        public const int ElectionIntervalDays = 5;

        /// <summary>最少成员数（少于 2 人不开选，现任留任）。</summary>
        public const int MinMemberCount = 2;

        /// <summary>候选席位上限（贡献前 4）。</summary>
        public const int MaxCandidates = 4;

        /// <summary>候选资格最低野心。</summary>
        public const float MinCandidateAmbition = 45f;

        /// <summary>现任连任加成分。</summary>
        public const float IncumbentBonus = 5f;

        /// <summary>单次拉票加成分（上限 CampaignBonusCap）。</summary>
        public const float CampaignBonusPerRally = 1f;

        /// <summary>拉票加成上限分。</summary>
        public const float CampaignBonusCap = 3f;

        /// <summary>是否到达选举日（>= 防跳日错过；未初始化 0 表示立即选）。</summary>
        public static bool IsElectionDue(int dayIndex, int nextElectionDay)
        {
            return dayIndex >= nextElectionDay;
        }

        /// <summary>下一届选举日（本届结算后顺排一个周期）。</summary>
        public static int GetNextElectionDay(int dayIndex)
        {
            return dayIndex + ElectionIntervalDays;
        }

        /// <summary>
        /// 票重 = 职位票重（FactionRankRuleService：长老 2.0/精英 1.5）
        /// + 社交因子（Sociality/200，最高 +0.5）。
        /// </summary>
        public static float ComputeVoteWeight(FactionRank rank, float sociality)
        {
            float social = sociality > 0f ? sociality / 200f : 0f;
            if (social > 0.5f)
            {
                social = 0.5f;
            }

            return FactionRankRuleService.GetVoteWeight(rank) + social;
        }

        /// <summary>
        /// 组装候选名单：Ambition≥45 资格线 → 贡献降序 → 取前 4。
        /// 输入应为全成员快照（含现任）；返回顺序即平局裁决优先序。
        /// </summary>
        public static List<ElectionCandidateSnapshot> BuildCandidates(
            List<ElectionCandidateSnapshot> members)
        {
            var qualified = new List<ElectionCandidateSnapshot>();
            foreach (ElectionCandidateSnapshot member in members)
            {
                if (member.Ambition >= MinCandidateAmbition)
                {
                    qualified.Add(member);
                }
            }

            qualified.Sort((a, b) =>
            {
                int byContribution = b.Contribution.CompareTo(a.Contribution);
                return byContribution != 0 ? byContribution : string.CompareOrdinal(a.Name, b.Name);
            });

            if (qualified.Count > MaxCandidates)
            {
                qualified.RemoveRange(MaxCandidates, qualified.Count - MaxCandidates);
            }

            return qualified;
        }

        /// <summary>
        /// 单票倾向分（确定性）：亲密度 ×0.3 + 候选人社交 ×0.3 + 野心 ×0.1
        /// + 现任连任 +5 + 拉票加成。亲密度可负（交恶者倒戈）。
        /// </summary>
        public static float ComputeVoteScore(
            in ElectionVoterSnapshot voter, in ElectionCandidateSnapshot candidate)
        {
            float affinity = 0f;
            if (voter.AffinityByCandidate != null
                && voter.AffinityByCandidate.TryGetValue(candidate.Name, out float value))
            {
                affinity = value;
            }

            return (affinity * 0.3f)
                + (candidate.Sociality * 0.3f)
                + (candidate.Ambition * 0.1f)
                + (candidate.IsIncumbent ? IncumbentBonus : 0f)
                + candidate.CampaignBonus;
        }

        /// <summary>
        /// 开票：每个投票人把加权票投给自分最高候选（平局取名单靠前），
        /// 累计最高者当选；候选累计平局按名单序（贡献序）裁决。
        /// </summary>
        /// <returns>当选者名；候选为空/票全零返回 null（现任留任）。</returns>
        public static string Tally(
            List<ElectionCandidateSnapshot> candidates,
            List<ElectionVoterSnapshot> voters)
        {
            if (candidates == null || candidates.Count == 0
                || voters == null || voters.Count == 0)
            {
                return null;
            }

            var tally = new Dictionary<string, float>(candidates.Count);
            foreach (ElectionCandidateSnapshot candidate in candidates)
            {
                tally[candidate.Name] = 0f;
            }

            foreach (ElectionVoterSnapshot voter in voters)
            {
                int bestIndex = -1;
                float bestScore = float.MinValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    ElectionCandidateSnapshot candidate = candidates[i];
                    float score = ComputeVoteScore(in voter, in candidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    tally[candidates[bestIndex].Name] += voter.VoteWeight;
                }
            }

            string winner = null;
            float winnerVotes = -1f;
            foreach (ElectionCandidateSnapshot candidate in candidates)
            {
                float votes = tally[candidate.Name];
                if (votes > winnerVotes)
                {
                    winnerVotes = votes;
                    winner = candidate.Name;
                }
            }

            return winner;
        }
    }
}
