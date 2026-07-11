namespace LAB2D.Enum
{
    /// <summary>
    /// 装备稀有度等级枚举。
    /// 映射到已有的 BackpackItemQualityEnum 品质系统，为装备掉落提供独立的稀有度语义。
    /// 稀有度从低到高排列，越高的稀有度属性倍率越大、掉落概率越低。
    /// 使用场景：装备掉落判定、属性加权生成、对比弹窗颜色、装备面板展示、Editor 菜单。
    /// </summary>
    public enum EquipmentRarityType
    {
        /// <summary>
        /// 普通（映射 Gray/White），基础属性倍率 1.0x，掉落权重 50%
        /// </summary>
        Common,

        /// <summary>
        /// 不凡（映射 Green），属性倍率 1.3x，掉落权重 25%
        /// </summary>
        Uncommon,

        /// <summary>
        /// 稀有（映射 Blue），属性倍率 1.6x，掉落权重 15%
        /// </summary>
        Rare,

        /// <summary>
        /// 史诗（映射 Purple），属性倍率 2.0x，掉落权重 7%
        /// </summary>
        Epic,

        /// <summary>
        /// 传说（映射 Orange），属性倍率 2.5x，极值属性+1条，掉落权重 2.5%
        /// </summary>
        Legendary,

        /// <summary>
        /// 神话（映射 Red），属性倍率 3.2x，极值属性+2条，掉落权重 0.5%
        /// </summary>
        Mythic,
    }
}
