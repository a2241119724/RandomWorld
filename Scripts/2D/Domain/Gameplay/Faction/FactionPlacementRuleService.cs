namespace LAB2D.Domain.Gameplay.Faction
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Common;

    /// <summary>
    /// 阵营核心选址规则（纯函数，照 AncientCaveRuleService.IsPlacementValid 平方距离模式）。
    /// 核心位于成员房间质心，但须与地图中心（山门/任务栏）、其他阵营核心、
    /// 地图兴趣点（洞府/毒雾圈）保持最小间距。
    /// </summary>
    public static class FactionPlacementRuleService
    {
        /// <summary>阵营核心之间的最小间距（格，欧氏距离）。</summary>
        public const float MinCoreSpacing = 60f;

        /// <summary>核心距地图中心（山门核心/任务栏）的最小间距（格）。</summary>
        public const float MinCoreToMapCenter = 20f;

        /// <summary>核心距地图兴趣点（洞府/毒雾圈中心）的最小间距（格）。</summary>
        public const float MinCoreToPoi = 30f;

        /// <summary>
        /// 成员房间锚点（已建家 or 规划位）的质心，四舍五入取整格。
        /// 空列表返回 mapCenter（回退：选址环搜以地图中心为原点展开）。
        /// </summary>
        public static GameVector2 ComputeCentroid(IReadOnlyList<GameVector2> anchorPoints, GameVector2 fallback)
        {
            if (anchorPoints == null || anchorPoints.Count == 0)
            {
                return fallback;
            }

            float sumX = 0f;
            float sumY = 0f;
            for (int i = 0; i < anchorPoints.Count; i++)
            {
                sumX += anchorPoints[i].X;
                sumY += anchorPoints[i].Y;
            }

            return new GameVector2(
                (float)Math.Round(sumX / anchorPoints.Count),
                (float)Math.Round(sumY / anchorPoints.Count));
        }

        /// <summary>
        /// 候选位是否合法：距地图中心、其他核心、兴趣点均满足最小间距。
        /// </summary>
        public static bool IsPlacementValid(
            GameVector2 candidate,
            IReadOnlyList<GameVector2> otherCorePositions,
            IReadOnlyList<GameVector2> poiPositions,
            GameVector2 mapCenter)
        {
            float centerSqr = MinCoreToMapCenter * MinCoreToMapCenter;
            if (candidate.SqrDistanceTo(mapCenter) < centerSqr)
            {
                return false;
            }

            float spacingSqr = MinCoreSpacing * MinCoreSpacing;
            if (otherCorePositions != null)
            {
                for (int i = 0; i < otherCorePositions.Count; i++)
                {
                    if (candidate.SqrDistanceTo(otherCorePositions[i]) < spacingSqr)
                    {
                        return false;
                    }
                }
            }

            float poiSqr = MinCoreToPoi * MinCoreToPoi;
            if (poiPositions != null)
            {
                for (int i = 0; i < poiPositions.Count; i++)
                {
                    if (candidate.SqrDistanceTo(poiPositions[i]) < poiSqr)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
