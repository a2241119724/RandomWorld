namespace LAB2D.Domain.Gameplay.Alchemy
{
    using LAB2D.Domain.Character;

    /// <summary>丹药效果类型。</summary>
    public enum PillEffectType
    {
        /// <summary>聚气：立即获得灵气（EffectValue = 灵气值）。</summary>
        GainQi,

        /// <summary>治伤：按最大生命百分比恢复 HP（EffectValue = 0~1 比例）。</summary>
        HealHp,

        /// <summary>归元：按最大灵力百分比恢复 MP（EffectValue = 0~1 比例）。</summary>
        RestoreMp,

        /// <summary>渡劫辅助：下次突破灵气需求按比例减免（EffectValue = 减免比例，饮用时生效不入档）。</summary>
        BreakthroughAid,

        /// <summary>洗髓：永久战斗属性加成（走 PermanentBonus 管线，EffectValue 无效）。</summary>
        PermanentStats,
    }

    /// <summary>丹药品质档（决定效果倍率）。</summary>
    public enum PillQuality
    {
        /// <summary>凡品（效果 ×1.0）。</summary>
        Common = 0,

        /// <summary>上品（效果 ×1.5）。</summary>
        Superior = 1,

        /// <summary>极品（效果 ×2.0）。</summary>
        Premium = 2,
    }

    /// <summary>
    /// 丹方定义 — 静态库条目，不可变。
    /// 成本用灵气（与突破共用货币），本版不引入药材物品（后续接炼丹炉建筑/采集时再扩展输入槽）。
    /// </summary>
    public class PillDef
    {
        /// <summary>丹方 Id（库内唯一，存档/检索键）。</summary>
        public string Id;

        /// <summary>丹药名。</summary>
        public string Name;

        /// <summary>丹方描述。</summary>
        public string Description;

        /// <summary>效果类型。</summary>
        public PillEffectType Effect;

        /// <summary>效果基准值（语义随 Effect；品质倍率乘在此值上）。</summary>
        public float EffectValue;

        /// <summary>洗髓类永久加成基准（PermanentStats 专用）。</summary>
        public BattleStats PermanentBonus;

        /// <summary>炼制消耗灵气。</summary>
        public float QiCost;

        /// <summary>炼制所需最低境界 Index（RealmLibrary 序）。</summary>
        public int RequiredRealmIndex;
    }

    /// <summary>炼丹结算结果。</summary>
    public struct PillCraftResult
    {
        /// <summary>是否炼成（失败时其余字段为默认值）。</summary>
        public bool Success;

        /// <summary>炼出的丹方。</summary>
        public PillDef Pill;

        /// <summary>品质档。</summary>
        public PillQuality Quality;

        /// <summary>效果数值（已乘品质倍率；语义随 Pill.Effect）。</summary>
        public float EffectValue;

        /// <summary>永久加成（PermanentStats 专用，已乘品质倍率）。</summary>
        public BattleStats PermanentBonus;
    }
}
