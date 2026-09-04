namespace LAB2D.Constant
{
    using LAB2D.Enum;
    using UnityEngine;

    /// <summary>
    /// 装备光束特效常量。
    /// 统一管理光束尺寸、透明度、动画参数和渲染层级。
    /// 所有光束相关子模块通过本类获取固定值。
    /// </summary>
    public static class EquipmentBeamConstant
    {
        // ============================================================
        // 光束尺寸（世界单位）：宽度和高度
        // ============================================================

        public const float CommonBeamWidth = 0.45f;
        public const float CommonBeamHeight = 3.0f;

        public const float UncommonBeamWidth = 0.51f;
        public const float UncommonBeamHeight = 3.6f;

        public const float RareBeamWidth = 0.57f;
        public const float RareBeamHeight = 4.0f;

        public const float EpicBeamWidth = 0.63f;
        public const float EpicBeamHeight = 5.0f;

        public const float LegendaryBeamWidth = 0.69f;
        public const float LegendaryBeamHeight = 6.0f;

        public const float MythicBeamWidth = 0.75f;
        public const float MythicBeamHeight = 7.0f;

        // ============================================================
        // 光束透明度
        // ============================================================

        public const float CommonBeamAlpha = 0.50f;
        public const float UncommonBeamAlpha = 0.54f;
        public const float RareBeamAlpha = 0.58f;
        public const float EpicBeamAlpha = 0.62f;
        public const float LegendaryBeamAlpha = 0.66f;
        public const float MythicBeamAlpha = 0.70f;

        // ============================================================
        // 动画参数（Custom/BeamGradient shader 顶点脉冲驱动）
        // ============================================================

        /// <summary>主光束脉冲速度</summary>
        public const float PulseSpeed = 2.0f;

        /// <summary>主光束脉冲幅度（普通稀有度）</summary>
        public const float PulseAmplitudeNormal = 0.08f;

        /// <summary>主光束脉冲幅度（Epic+）</summary>
        public const float PulseAmplitudeGlow = 0.15f;

        // ============================================================
        // 渲染
        // ============================================================

        /// <summary>光束 GameObject 容器名</summary>
        public const string BeamContainerName = "EquipmentBeam";

        /// <summary>光束对象名前缀</summary>
        public const string BeamObjectPrefix = "EquipmentBeam";

        // ============================================================
        // 工具方法
        // ============================================================

        public static float GetBeamWidth(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return CommonBeamWidth;
                case EquipmentRarityType.Uncommon:  return UncommonBeamWidth;
                case EquipmentRarityType.Rare:      return RareBeamWidth;
                case EquipmentRarityType.Epic:      return EpicBeamWidth;
                case EquipmentRarityType.Legendary: return LegendaryBeamWidth;
                case EquipmentRarityType.Mythic:    return MythicBeamWidth;
                default:                            return CommonBeamWidth;
            }
        }

        public static float GetBeamHeight(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return CommonBeamHeight;
                case EquipmentRarityType.Uncommon:  return UncommonBeamHeight;
                case EquipmentRarityType.Rare:      return RareBeamHeight;
                case EquipmentRarityType.Epic:      return EpicBeamHeight;
                case EquipmentRarityType.Legendary: return LegendaryBeamHeight;
                case EquipmentRarityType.Mythic:    return MythicBeamHeight;
                default:                            return CommonBeamHeight;
            }
        }

        public static float GetBeamAlpha(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return CommonBeamAlpha;
                case EquipmentRarityType.Uncommon:  return UncommonBeamAlpha;
                case EquipmentRarityType.Rare:      return RareBeamAlpha;
                case EquipmentRarityType.Epic:      return EpicBeamAlpha;
                case EquipmentRarityType.Legendary: return LegendaryBeamAlpha;
                case EquipmentRarityType.Mythic:    return MythicBeamAlpha;
                default:                            return CommonBeamAlpha;
            }
        }

    }
}
