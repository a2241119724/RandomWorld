namespace LAB2D.Domain.Gameplay.Faction
{
    /// <summary>
    /// 阵营职位（按累计贡献派生，不落存档 — 贡献即职位权威）。
    /// 排序：Novice &lt; Member &lt; Elite &lt; Elder。
    /// </summary>
    public enum FactionRank
    {
        /// <summary>新手（贡献 &lt; 100）。</summary>
        Novice = 0,

        /// <summary>正式成员（贡献 ≥ 100）。</summary>
        Member = 1,

        /// <summary>精英（贡献 ≥ 400）。</summary>
        Elite = 2,

        /// <summary>长老（贡献 ≥ 1000，票重最高）。</summary>
        Elder = 3,
    }

    /// <summary>
    /// 阵营职位规则（纯函数）— 贡献阈值派生职位、投票票重（P3 选举用）、
    /// 职位中文名（UI 展示）。领袖独立于职位序列（Faction.LeaderName）。
    /// </summary>
    public static class FactionRankRuleService
    {
        /// <summary>Member 阈值。</summary>
        public const float MemberThreshold = 100f;

        /// <summary>Elite 阈值。</summary>
        public const float EliteThreshold = 400f;

        /// <summary>Elder 阈值。</summary>
        public const float ElderThreshold = 1000f;

        /// <summary>长老票重（P3 选举）。</summary>
        public const float ElderVoteWeight = 2f;

        /// <summary>精英票重（P3 选举）。</summary>
        public const float EliteVoteWeight = 1.5f;

        /// <summary>按累计贡献派生职位。</summary>
        public static FactionRank GetRank(float contribution)
        {
            if (contribution >= ElderThreshold)
            {
                return FactionRank.Elder;
            }

            if (contribution >= EliteThreshold)
            {
                return FactionRank.Elite;
            }

            if (contribution >= MemberThreshold)
            {
                return FactionRank.Member;
            }

            return FactionRank.Novice;
        }

        /// <summary>职位对应投票票重（P3 选举 Tally 输入）。</summary>
        public static float GetVoteWeight(FactionRank rank)
        {
            return rank switch
            {
                FactionRank.Elder => ElderVoteWeight,
                FactionRank.Elite => EliteVoteWeight,
                _ => 1f,
            };
        }

        /// <summary>职位中文名（HUD/气泡展示）。</summary>
        public static string GetRankName(FactionRank rank)
        {
            return rank switch
            {
                FactionRank.Elder => "长老",
                FactionRank.Elite => "精英",
                FactionRank.Member => "正式成员",
                _ => "新手",
            };
        }

        /// <summary>是否发生晋职（事件播报用）。</summary>
        public static bool IsPromotion(FactionRank before, FactionRank after)
        {
            return after > before;
        }
    }
}
