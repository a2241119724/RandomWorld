namespace LAB2D.Domain.Gameplay.AncientCave
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;

    /// <summary>
    /// 上古洞府规则（M4 包 4 地图兴趣点）— 撒点约束与揭示判定纯函数。
    /// 运行时宿主 AncientCaveManager；本类零 Unity 依赖。
    /// 走近才揭示（≤8 格）、探索耗时、有风险（风险/奖励 roll 在轮 3 接入）。
    /// </summary>
    public static class AncientCaveRuleService
    {
        /// <summary>洞府离地图中心（出生区）的最小距离（格）。</summary>
        public const int MinDistanceFromMapCenter = 20;

        /// <summary>洞府之间的最小间距（格）。</summary>
        public const int MinCaveDistance = 30;

        /// <summary>走近揭示半径（格，边界含）。</summary>
        public const float RevealRadius = 8f;

        /// <summary>揭示后视觉淡入时长（秒）。</summary>
        public const float RevealFadeSeconds = 2f;

        /// <summary>洞府生命周期状态。</summary>
        public enum CaveState
        {
            /// <summary>未发现：无视觉，走近才揭示。</summary>
            Hidden = 0,

            /// <summary>已揭示：视觉淡入，可探索（轮 3）。</summary>
            Revealed = 1,

            /// <summary>探索占用中：同一洞府同时仅一个探索者（轮 3）。</summary>
            Exploring = 2,

            /// <summary>已枯竭：探索完毕，一次性奖励已领（轮 3）。</summary>
            Explored = 3,
        }

        /// <summary>洞府模型：位置（地图格）+ 状态。</summary>
        public struct AncientCaveModel
        {
            /// <summary>洞口位置（地图格坐标）。</summary>
            public GameVector2 Pos;

            /// <summary>当前状态。</summary>
            public CaveState State;

            public AncientCaveModel(GameVector2 pos, CaveState state)
            {
                this.Pos = pos;
                this.State = state;
            }
        }

        /// <summary>
        /// 撒点合法性：距地图中心 ≥ <see cref="MinDistanceFromMapCenter"/>（边界含），
        /// 且与既有洞府间距 ≥ <see cref="MinCaveDistance"/>（边界含）。
        /// </summary>
        public static bool IsPlacementValid(
            IReadOnlyList<AncientCaveModel> existing, GameVector2 candidate, GameVector2 mapCenter)
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

            float caveSqr = MinCaveDistance * MinCaveDistance;
            for (int i = 0; i < existing.Count; i++)
            {
                if (candidate.SqrDistanceTo(existing[i].Pos) < caveSqr)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 走近揭示判定：观察者与洞府距离 ≤ <see cref="RevealRadius"/>（边界含）。
        /// </summary>
        public static bool ShouldReveal(float distance)
        {
            return distance <= RevealRadius;
        }

        /// <summary>
        /// 揭示视觉淡入进度：0（全透明）→ 1（全显），线性 + 平滑尾部。
        /// elapsed ≤ 0 返 0；超过时长返 1（稳定终态，供每帧采样无需 clamp）。
        /// </summary>
        public static float RevealProgress(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f)
            {
                return 0f;
            }

            if (elapsedSeconds >= RevealFadeSeconds)
            {
                return 1f;
            }

            // 前 60% 线性爬升，后 40% 平滑趋稳（ease-out，遗迹浮现感）
            float t = elapsedSeconds / RevealFadeSeconds;
            return t < 0.6f ? t / 0.6f * 0.7f : 0.7f + 0.3f * (1f - (1f - (t - 0.6f) / 0.4f) * (1f - (t - 0.6f) / 0.4f));
        }
    }
}
