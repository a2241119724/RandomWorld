namespace LAB2D.Domain.Gameplay.Alchemy
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;

    /// <summary>
    /// 丹方静态库 — 本版 5 条：聚气/治伤/归元/渡劫/洗髓各一，门槛覆盖练气~元婴。
    /// 数值为首版投放（成本≈对应境界 QiToNext 的 1/6~1/3，回气散净收益为正赌品质），请配合 Play 实测调参。
    /// </summary>
    public static class PillLibrary
    {
        /// <summary>回气散 — 练气档聚气丹（40 灵气炼 60，品质好则翻倍，净赌品质）。</summary>
        public static readonly PillDef HuiQiSan = new PillDef
        {
            Id = "pill_huiqisan",
            Name = "回气散",
            Description = "以自身灵气温养丹炉，炼出聚气之散",
            Effect = PillEffectType.GainQi,
            EffectValue = 60f,
            QiCost = 40f,
            RequiredRealmIndex = 1,
        };

        /// <summary>培元丹 — 练气档治伤丹（恢复 40% 生命）。</summary>
        public static readonly PillDef PeiYuanDan = new PillDef
        {
            Id = "pill_peiyuandan",
            Name = "培元丹",
            Description = "固本培元，重伤亦能回天",
            Effect = PillEffectType.HealHp,
            EffectValue = 0.4f,
            QiCost = 60f,
            RequiredRealmIndex = 1,
        };

        /// <summary>凝神丹 — 筑基档归元丹（恢复全部灵力）。</summary>
        public static readonly PillDef NingShenDan = new PillDef
        {
            Id = "pill_ningshendan",
            Name = "凝神丹",
            Description = "凝神静气，灵力涓涓复涌",
            Effect = PillEffectType.RestoreMp,
            EffectValue = 1f,
            QiCost = 80f,
            RequiredRealmIndex = 2,
        };

        /// <summary>渡劫丹 — 金丹档突破辅助（下次突破需求 -15%，品质加倍率）。</summary>
        public static readonly PillDef DuJieDan = new PillDef
        {
            Id = "pill_dujiedan",
            Name = "渡劫丹",
            Description = "服之气机圆融，破境时事半功倍",
            Effect = PillEffectType.BreakthroughAid,
            EffectValue = 0.15f,
            QiCost = 600f,
            RequiredRealmIndex = 3,
        };

        /// <summary>九转金丹 — 元婴档洗髓丹（永久全维加成）。</summary>
        public static readonly PillDef JiuZhuanJinDan = new PillDef
        {
            Id = "pill_jiuzhuanjindan",
            Name = "九转金丹",
            Description = "九转功成，脱胎换骨",
            Effect = PillEffectType.PermanentStats,
            EffectValue = 0f,
            PermanentBonus = new BattleStats(8f, 8f, 8f, 8f, 0.02f, 0.02f, 0f, 0f),
            QiCost = 3600f,
            RequiredRealmIndex = 4,
        };

        /// <summary>全部丹方（门槛升序）。</summary>
        public static readonly List<PillDef> All = new List<PillDef>
        {
            HuiQiSan,
            PeiYuanDan,
            NingShenDan,
            DuJieDan,
            JiuZhuanJinDan,
        };

        /// <summary>按 Id 查丹方（未命中返回 null）。</summary>
        public static PillDef FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (PillDef pill in All)
            {
                if (pill.Id == id)
                {
                    return pill;
                }
            }

            return null;
        }
    }
}
