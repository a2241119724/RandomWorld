namespace LAB2D.Domain.Gameplay.Faction
{
    /// <summary>
    /// 阵营贡献规则（纯函数）— 阵营任务/捐献/兑换的数值换算与限额。
    /// 零 Unity 依赖，可单测；任务贡献是成员日常主要来源，捐献 1:1 供玩家
    /// 与富裕成员补给金库，兑换 2:1 且每日限额防刷。
    /// </summary>
    public static class FactionContributionRuleService
    {
        /// <summary>完成一个阵营任务获得的贡献。</summary>
        public const float QuestContribution = 10f;

        /// <summary>阵营任务金库付款（金库 → 执行者钱包）。</summary>
        public const int QuestRewardGold = 12;

        /// <summary>防御击杀贡献（P4 Defend 接入时按杀结算）。</summary>
        public const float DefenseKillContribution = 5f;

        /// <summary>捐献换算：1 金币 = 1 贡献。</summary>
        public const float DonationContributionPerGold = 1f;

        /// <summary>兑换换算：2 贡献 = 1 金币。</summary>
        public const float ExchangeContributionPerGold = 2f;

        /// <summary>每人每日兑换金币上限。</summary>
        public const int ExchangeDailyGoldCap = 50;

        /// <summary>
        /// 当日阵营任务生成数（确定性：2 个，每逢第 3 日 3 个）。
        /// 无随机 — 同一日所有阵营生成数量一致，便于测试与玩家预期。
        /// </summary>
        public static int GetQuestCountToday(int dayIndex)
        {
            return dayIndex % 3 == 0 ? 3 : 2;
        }

        /// <summary>捐献金币折算贡献（向下取整）。</summary>
        public static float ComputeDonationContribution(int gold)
        {
            return gold * DonationContributionPerGold;
        }

        /// <summary>兑换金币所需的贡献（向上取整，防小数贡献兑换超额）。</summary>
        public static float ComputeExchangeCost(int gold)
        {
            return gold * ExchangeContributionPerGold;
        }

        /// <summary>今日剩余可兑换金币（限额裁剪；负余额按 0 计）。</summary>
        public static int ClampExchangeToday(int goldWanted, int goldAlreadyToday)
        {
            int remaining = ExchangeDailyGoldCap - goldAlreadyToday;
            if (remaining < 0)
            {
                remaining = 0;
            }

            if (goldWanted < 0)
            {
                return 0;
            }

            return goldWanted <= remaining ? goldWanted : remaining;
        }
    }
}
