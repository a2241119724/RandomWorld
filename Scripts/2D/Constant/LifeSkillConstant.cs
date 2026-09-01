namespace LAB2D.Constant
{
    /// <summary>
    /// 生活技能系统公共常量：单次任务经验、升级阈值、效率倍率、展示文案。
    /// 数值调整直接影响 Worker 效率曲线，请配合 Play Mode 验证。
    /// </summary>
    public static class LifeSkillConstant
    {
        /// <summary>完成一次伐木（资源采集）任务获得的经验。</summary>
        public const float XpPerFelling = 2f;

        /// <summary>完成一次采矿（地形挖掘）任务获得的经验。</summary>
        public const float XpPerMining = 2f;

        /// <summary>完成一次农耕（种植收获）任务获得的经验。</summary>
        public const float XpPerFarming = 1f;

        /// <summary>等级上限（0 级起步，练到 3 级满级）。</summary>
        public const int MaxLevel = 3;

        /// <summary>升到 1 级所需累计经验。</summary>
        public const float XpToLevel1 = 10f;

        /// <summary>升到 2 级所需累计经验。</summary>
        public const float XpToLevel2 = 30f;

        /// <summary>升到 3 级所需累计经验。</summary>
        public const float XpToLevel3 = 80f;

        /// <summary>0 级效率倍率（无加成）。</summary>
        public const float MultiplierLevel0 = 1.0f;

        /// <summary>1 级效率倍率。</summary>
        public const float MultiplierLevel1 = 1.15f;

        /// <summary>2 级效率倍率。</summary>
        public const float MultiplierLevel2 = 1.3f;

        /// <summary>3 级效率倍率。</summary>
        public const float MultiplierLevel3 = 1.5f;

        /// <summary>伐木技能中文名。</summary>
        public const string FellingName = "伐木";

        /// <summary>采矿技能中文名。</summary>
        public const string MiningName = "采矿";

        /// <summary>农耕技能中文名。</summary>
        public const string FarmingName = "农耕";
    }
}
