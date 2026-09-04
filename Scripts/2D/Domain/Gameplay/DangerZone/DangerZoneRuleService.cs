namespace LAB2D.Domain.Gameplay.DangerZone
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;

    /// <summary>
    /// 危险区规则（M4 包 4 地图兴趣点）— 撒点约束与空间乘数纯函数。
    /// 运行时宿主 DangerZoneManager；本类零 Unity 依赖。
    /// 惩罚与收益对称：区内移动 ×0.7（玩家/Worker 共用），灵气浓度 ×1.3（险地生灵物）。
    /// </summary>
    public static class DangerZoneRuleService
    {
        /// <summary>区内移动速度乘数。</summary>
        public const float InZoneMoveSpeedMultiplier = 0.7f;

        /// <summary>区内灵气浓度乘数。</summary>
        public const float InZoneQiDensityMultiplier = 1.3f;

        /// <summary>危险区圆心离地图中心（出生区）的最小距离（格）。</summary>
        public const int MinDistanceFromMapCenter = 15;

        /// <summary>危险区模型：圆心（地图格）+ 半径（格）。</summary>
        public struct DangerZoneModel
        {
            /// <summary>圆心（地图格坐标）。</summary>
            public GameVector2 Center;

            /// <summary>半径（格）。</summary>
            public float Radius;

            public DangerZoneModel(GameVector2 center, float radius)
            {
                this.Center = center;
                this.Radius = radius;
            }
        }

        /// <summary>
        /// 点是否在任一危险区内（边界含）。空表/空引用安全返回 false。
        /// </summary>
        public static bool IsInZone(IReadOnlyList<DangerZoneModel> zones, int x, int y)
        {
            if (zones == null || zones.Count == 0)
            {
                return false;
            }

            var pos = new GameVector2(x, y);
            for (int i = 0; i < zones.Count; i++)
            {
                if (pos.SqrDistanceTo(zones[i].Center) <= zones[i].Radius * zones[i].Radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 撒点合法性：圆心距地图中心 ≥ <see cref="MinDistanceFromMapCenter"/>（边界含），
        /// 且与既有区不重叠（圆心距 ≥ 两半径之和，相切合法）。
        /// </summary>
        public static bool IsPlacementValid(
            IReadOnlyList<DangerZoneModel> existing, GameVector2 candidate, float candidateRadius, GameVector2 mapCenter)
        {
            float centerSqr = MinDistanceFromMapCenter * MinDistanceFromMapCenter;
            if (candidate.SqrDistanceTo(mapCenter) < centerSqr)
            {
                return false;
            }

            if (existing == null)
            {
                return true;
            }

            for (int i = 0; i < existing.Count; i++)
            {
                float minDist = existing[i].Radius + candidateRadius;
                if (candidate.SqrDistanceTo(existing[i].Center) < minDist * minDist)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>移动速度乘数：区内 <see cref="InZoneMoveSpeedMultiplier"/>，区外 1。</summary>
        public static float MoveSpeedMultiplier(bool inZone)
        {
            return inZone ? InZoneMoveSpeedMultiplier : 1f;
        }

        /// <summary>灵气浓度乘数：区内 <see cref="InZoneQiDensityMultiplier"/>，区外 1。</summary>
        public static float QiDensityMultiplier(bool inZone)
        {
            return inZone ? InZoneQiDensityMultiplier : 1f;
        }
    }
}
