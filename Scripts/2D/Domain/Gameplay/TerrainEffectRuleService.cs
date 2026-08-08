namespace LAB2D.Domain.Gameplay
{
    /// <summary>
    /// 地形效果规则服务 — 提供安全数学计算和默认常量。
    /// 纯 C# 领域服务，无 Unity 依赖。
    ///
    /// 注意：具体的各地形倍率存储在 TerrainTileConfig.asset 中，
    /// 本服务只提供默认值和安全的乘法计算，不硬编码各地形倍率。
    /// </summary>
    public sealed class TerrainEffectRuleService
    {
        /// <summary>
        /// 默认移速倍率（当地形配置缺失或 terrainId 无效时使用）。
        /// </summary>
        public const float DefaultMoveSpeedMultiplier = 1.0f;

        /// <summary>
        /// 默认疲劳衰减倍率。
        /// </summary>
        public const float DefaultTiredDecayMultiplier = 1.0f;

        /// <summary>
        /// 默认饥饿衰减倍率。
        /// </summary>
        public const float DefaultHungryDecayMultiplier = 1.0f;

        /// <summary>
        /// 按倍率计算数值，并保证结果不低于最小值。
        /// </summary>
        /// <param name="baseValue">基础值。</param>
        /// <param name="multiplier">倍率（负值会被安全钳制为 0）。</param>
        /// <param name="minValue">允许的最小值。</param>
        /// <returns>套用倍率后的安全值。</returns>
        public float ApplyMultiplier(float baseValue, float multiplier, float minValue = 0.0f)
        {
            float safeMultiplier = multiplier < 0.0f ? 0.0f : multiplier;
            float result = baseValue * safeMultiplier;
            return result < minValue ? minValue : result;
        }
    }
}
