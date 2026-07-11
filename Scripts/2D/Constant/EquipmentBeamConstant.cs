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
        // 发光层（Epic+）参数
        // ============================================================

        /// <summary>发光层相对于主光束的宽度倍率</summary>
        public const float GlowWidthMultiplier = 1.5f;

        /// <summary>发光层相对于主光束的高度倍率</summary>
        public const float GlowHeightMultiplier = 1.2f;

        /// <summary>发光层基础透明度</summary>
        public const float GlowAlpha = 0.35f;

        // ============================================================
        // 程序化纹理参数
        // ============================================================

        /// <summary>纹理宽度（像素）</summary>
        public const int TextureWidth = 128;

        /// <summary>纹理高度（像素）</summary>
        public const int TextureHeight = 256;

        /// <summary>高斯水平衰减系数（越大边缘越锐利）</summary>
        public const float GaussianFalloff = 8.0f;

        /// <summary>Sprite 像素单位比</summary>
        public const float PixelsPerUnit = 100f;

        // ============================================================
        // 动画参数
        // ============================================================

        /// <summary>主光束脉冲速度</summary>
        public const float PulseSpeed = 2.0f;

        /// <summary>主光束脉冲幅度（普通稀有度）</summary>
        public const float PulseAmplitudeNormal = 0.08f;

        /// <summary>主光束脉冲幅度（Epic+）</summary>
        public const float PulseAmplitudeGlow = 0.15f;

        /// <summary>发光层独立脉冲速度</summary>
        public const float GlowPulseSpeed = 3.3f;

        /// <summary>发光层独立脉冲幅度</summary>
        public const float GlowPulseAmplitude = 0.12f;

        // ============================================================
        // 渲染
        // ============================================================

        /// <summary>光束 SortingOrder（高于地面 Tile，低于角色）</summary>
        public const int BeamSortingOrder = 50;

        /// <summary>光束 GameObject 容器名</summary>
        public const string BeamContainerName = "Ambitious_A011_EquipmentBeam_Root";

        /// <summary>光束对象名前缀</summary>
        public const string BeamObjectPrefix = "EquipmentBeam_";

        /// <summary>发光层对象名后缀</summary>
        public const string GlowSuffix = "_Glow";

        /// <summary>硬安全网：最大存活时间（秒）</summary>
        public const float MaxLifetime = 300f;

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
