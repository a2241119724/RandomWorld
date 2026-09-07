namespace LAB2D.Domain.Gameplay.Faction
{
    using System;

    /// <summary>
    /// 阵营创始人快照（纯数据，由 FactionManager 从 WorkerData 采集）。
    /// 全部标量，供确定性规则判定，无 Unity 依赖。
    /// </summary>
    public struct FactionFounderSnapshot
    {
        /// <summary>人格·事业心（0-100）。</summary>
        public float Ambition;

        /// <summary>人格·社交（0-100）。</summary>
        public float Sociality;

        /// <summary>人格·心情（0-100）。</summary>
        public float Mood;

        /// <summary>贪婪（0-100）。</summary>
        public float Greed;

        /// <summary>钱包金币。</summary>
        public int WalletGold;

        /// <summary>是否已有阵营。</summary>
        public bool HasFaction;

        /// <summary>当前 Worker 总数。</summary>
        public int WorkerCount;

        /// <summary>既有阵营数。</summary>
        public int ExistingFactionCount;

        /// <summary>生命周期阶段索引（0=Bootstrap 1=Settled 2=Established）。</summary>
        public int LifeStageIndex;
    }

    /// <summary>
    /// 阵营创建规则（纯函数）— 性格门槛/动态名额/创始金。
    /// 阵营数量随 Worker 数动态开档（每 6 人一个阵营位）；
    /// 高事业心+高社交者先进入攒钱态（WorkerBrain 抬高钱包保留线），
    /// 攒够创始金+安全垫即可自立门户。
    /// </summary>
    public static class FactionCreationRuleService
    {
        /// <summary>创始金（从创始人钱包扣，全额入阵营金库）。</summary>
        public const int FoundingCostGold = 800;

        /// <summary>创始后钱包安全垫（须在创始金之外保留）。</summary>
        public const int WalletReserveAfterFounding = 50;

        /// <summary>创始人事业心门槛。</summary>
        public const float MinAmbition = 60f;

        /// <summary>创始人社交门槛（有人望才拉得起队伍）。</summary>
        public const float MinSociality = 50f;

        /// <summary>每多少名 Worker 开放一个阵营位。</summary>
        public const int WorkersPerFactionSlot = 6;

        /// <summary>
        /// 当前 Worker 总数对应的阵营数上限（至少 1——哪怕只有 3 人，
        /// 最有野心的那位也可能拉起一个小门派）。
        /// </summary>
        public static int GetMaxFactionCount(int workerCount)
        {
            int effective = Math.Max(0, workerCount);
            return Math.Max(1, (effective + WorkersPerFactionSlot - 1) / WorkersPerFactionSlot);
        }

        /// <summary>
        /// 是否进入攒钱态：无阵营 + Established + 事业心/社交过门槛 + 阵营名额未满。
        /// 攒钱态由 WorkerBrain 抬高钱包保留线（抑制悬赏/奢侈支出）。
        /// </summary>
        public static bool ShouldSaveForFaction(in FactionFounderSnapshot snapshot)
        {
            return !snapshot.HasFaction
                && snapshot.LifeStageIndex >= 2
                && snapshot.Ambition >= MinAmbition
                && snapshot.Sociality >= MinSociality
                && snapshot.ExistingFactionCount < GetMaxFactionCount(snapshot.WorkerCount);
        }

        /// <summary>
        /// 是否可以立即创建：攒钱态 + 钱包 ≥ 创始金 + 安全垫。
        /// </summary>
        public static bool CanFound(in FactionFounderSnapshot snapshot)
        {
            return ShouldSaveForFaction(in snapshot)
                && snapshot.WalletGold >= FoundingCostGold + WalletReserveAfterFounding;
        }
    }
}
