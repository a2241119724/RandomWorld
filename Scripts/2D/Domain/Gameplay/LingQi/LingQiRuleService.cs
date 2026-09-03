namespace LAB2D.Domain.Gameplay.LingQi
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Common;

    /// <summary>
    /// 灵气环境规则 — 空间浓度系数合成：地形 × 灵脉 × 聚灵阵 × 天气。
    /// 浓度直接乘修炼速率（RealmRuleService.ComputeQiGain 的 envMultiplier），
    /// 基地选址在灵脉旁/聚灵阵覆盖区成为空间策略。纯 C# 实现，可独立测试。
    /// </summary>
    public sealed class LingQiRuleService
    {
        /// <summary>灵脉增幅半径内乘数。</summary>
        public const float VeinBoostMultiplier = 1.5f;

        /// <summary>灵脉增幅半径（格，欧氏距离，边界含内）。</summary>
        public const int VeinBoostRadius = 10;

        /// <summary>单座已建成聚灵阵的乘数。</summary>
        public const float SpiritArrayBoostMultiplier = 1.3f;

        /// <summary>聚灵阵增幅半径（格，欧氏距离）。</summary>
        public const int SpiritArrayBoostRadius = 4;

        /// <summary>聚灵阵叠加上限层数（×1.3³≈2.2，防指数膨胀）。</summary>
        public const int MaxSpiritArrayStacks = 3;

        /// <summary>
        /// 地形浓度系数直通（TerrainTileConfig.effectData.qiDensityMultiplier 配置值）。
        /// </summary>
        /// <param name="configuredQiMultiplier">SO 配置值。</param>
        /// <returns>钳到 ≥0 的系数（资产漏配/非法时安全退化为 0，由 ComposeMultiplier 正常参与乘法）。</returns>
        public static float GetTerrainMultiplier(float configuredQiMultiplier)
        {
            return Math.Max(0f, configuredQiMultiplier);
        }

        /// <summary>
        /// 点到灵脉点集的最近欧氏距离（格）。
        /// </summary>
        /// <returns>无灵脉时为 <see cref="float.MaxValue"/>（后续 ApplyVeinBoost 自然判外）。</returns>
        public static float NearestVeinDistance(IReadOnlyList<GameVector2> veins, int x, int y)
        {
            if (veins == null || veins.Count == 0)
            {
                return float.MaxValue;
            }

            var pos = new GameVector2(x, y);
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < veins.Count; i++)
            {
                float sqr = pos.SqrDistanceTo(veins[i]);
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                }
            }

            return (float)Math.Sqrt(nearestSqr);
        }

        /// <summary>
        /// 灵脉增幅：最近灵脉距离在半径内（边界含）→ ×1.5（单层，多脉不叠）。
        /// </summary>
        public static float ApplyVeinBoost(float multiplier, float nearestVeinDist)
        {
            return nearestVeinDist <= VeinBoostRadius
                ? multiplier * VeinBoostMultiplier
                : multiplier;
        }

        /// <summary>
        /// 统计增幅半径内（边界含）的已建成聚灵阵数。
        /// </summary>
        public static int CountArraysInRange(IReadOnlyList<GameVector2> arrayCenters, int x, int y)
        {
            if (arrayCenters == null || arrayCenters.Count == 0)
            {
                return 0;
            }

            var pos = new GameVector2(x, y);
            float radiusSqr = SpiritArrayBoostRadius * (float)SpiritArrayBoostRadius;
            int count = 0;
            for (int i = 0; i < arrayCenters.Count; i++)
            {
                if (pos.SqrDistanceTo(arrayCenters[i]) <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 聚灵阵增幅：×1.3^min(n, 3)，可叠封顶。
        /// </summary>
        public static float ApplySpiritArrayBoost(float multiplier, int arraysInRange)
        {
            int stacks = Math.Max(0, Math.Min(arraysInRange, MaxSpiritArrayStacks));
            float result = multiplier;
            for (int i = 0; i < stacks; i++)
            {
                result *= SpiritArrayBoostMultiplier;
            }

            return result;
        }

        /// <summary>
        /// 总合成：地形 × 灵脉 × 聚灵阵 × 天气（天气缺省 1）。
        /// </summary>
        /// <param name="terrainMultiplier">地形系数（SO 配置，建议先过 GetTerrainMultiplier）。</param>
        /// <param name="nearestVeinDist">最近灵脉距离（NearestVeinDistance 产出）。</param>
        /// <param name="arraysInRange">半径内聚灵阵数（CountArraysInRange 产出）。</param>
        /// <param name="weatherMultiplier">天气乘数（IWeatherGameplayService.EnergyRecoveryMultiplier）。</param>
        public static float ComposeMultiplier(
            float terrainMultiplier, float nearestVeinDist, int arraysInRange, float weatherMultiplier = 1f)
        {
            float composed = GetTerrainMultiplier(terrainMultiplier);
            composed = ApplyVeinBoost(composed, nearestVeinDist);
            composed = ApplySpiritArrayBoost(composed, arraysInRange);
            return composed * Math.Max(0f, weatherMultiplier);
        }
    }
}
