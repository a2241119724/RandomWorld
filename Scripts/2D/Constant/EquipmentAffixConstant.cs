namespace LAB2D.Constant
{
    /// <summary>
    /// 装备词条公共常量：各词条数值区间、按稀有度的词条条数、显示名。
    /// 词条最终数值 = 区间内随机值 × 稀有度属性倍率（复用 EquipmentLootRuleService.GetStatMultiplier）。
    /// </summary>
    public static class EquipmentAffixConstant
    {
        // ============================================================
        // 词条数值区间（基础值，未乘稀有度倍率）
        // ============================================================

        /// <summary>物理攻击词条下限。</summary>
        public const float FlatAtnMin = 3f;

        /// <summary>物理攻击词条上限。</summary>
        public const float FlatAtnMax = 8f;

        /// <summary>魔法攻击词条下限。</summary>
        public const float FlatIntMin = 3f;

        /// <summary>魔法攻击词条上限。</summary>
        public const float FlatIntMax = 8f;

        /// <summary>生命上限词条下限。</summary>
        public const float MaxHpMin = 10f;

        /// <summary>生命上限词条上限。</summary>
        public const float MaxHpMax = 30f;

        /// <summary>吸血词条下限（比例）。</summary>
        public const float LifestealMin = 0.03f;

        /// <summary>吸血词条上限（比例）。</summary>
        public const float LifestealMax = 0.08f;

        /// <summary>反伤词条下限（比例）。</summary>
        public const float ReflectMin = 0.05f;

        /// <summary>反伤词条上限（比例）。</summary>
        public const float ReflectMax = 0.15f;

        // ============================================================
        // 按稀有度的词条条数（区间含端点，Roll 时随机取整）
        // ============================================================

        /// <summary>普通装备词条条数。</summary>
        public const int CommonCountMin = 1;
        public const int CommonCountMax = 1;

        /// <summary>不凡装备词条条数。</summary>
        public const int UncommonCountMin = 1;
        public const int UncommonCountMax = 2;

        /// <summary>稀有装备词条条数。</summary>
        public const int RareCountMin = 2;
        public const int RareCountMax = 2;

        /// <summary>史诗装备词条条数。</summary>
        public const int EpicCountMin = 2;
        public const int EpicCountMax = 2;

        /// <summary>传说装备词条条数。</summary>
        public const int LegendaryCountMin = 2;
        public const int LegendaryCountMax = 3;

        /// <summary>神话装备词条条数。</summary>
        public const int MythicCountMin = 3;
        public const int MythicCountMax = 3;
    }
}
