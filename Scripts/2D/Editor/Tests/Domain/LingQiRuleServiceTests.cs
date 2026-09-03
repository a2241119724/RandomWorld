namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.LingQi;
    using NUnit.Framework;

    /// <summary>
    /// 灵气环境规则 — 空间浓度系数合成：地形×灵脉（单层）×聚灵阵（指数叠封顶）×天气。
    /// </summary>
    [TestFixture]
    public class LingQiRuleServiceTests
    {
        private static IReadOnlyList<GameVector2> Points(params (int x, int y)[] cells)
        {
            var list = new List<GameVector2>();
            foreach ((int x, int y) in cells)
            {
                list.Add(new GameVector2(x, y));
            }

            return list;
        }

        [Test]
        public void GetTerrainMultiplier_PassesThroughValue()
        {
            Assert.AreEqual(1.3f, LingQiRuleService.GetTerrainMultiplier(1.3f), 0.0001f);
            Assert.AreEqual(1f, LingQiRuleService.GetTerrainMultiplier(1f), 0.0001f);
        }

        [Test]
        public void GetTerrainMultiplier_ClampsNegativeToZero()
        {
            Assert.AreEqual(0f, LingQiRuleService.GetTerrainMultiplier(-0.5f), 0.0001f);
        }

        [Test]
        public void NearestVeinDistance_EmptySet_ReturnsMaxValue()
        {
            Assert.AreEqual(float.MaxValue, LingQiRuleService.NearestVeinDistance(Points(), 0, 0));
            Assert.AreEqual(float.MaxValue, LingQiRuleService.NearestVeinDistance(null, 0, 0));
        }

        [Test]
        public void NearestVeinDistance_ReturnsNearest()
        {
            IReadOnlyList<GameVector2> veins = Points((30, 40), (3, 4), (100, 100));

            // 最近点 (3,4) 距原点 5
            Assert.AreEqual(5f, LingQiRuleService.NearestVeinDistance(veins, 0, 0), 0.0001f);
        }

        [Test]
        public void ApplyVeinBoost_InsideRadius_ReturnsBoost_IncludesBoundary()
        {
            // 边界含内：距离 == 半径也增幅
            Assert.AreEqual(
                1.5f,
                LingQiRuleService.ApplyVeinBoost(1f, LingQiRuleService.VeinBoostRadius),
                0.0001f);
            Assert.AreEqual(1.5f, LingQiRuleService.ApplyVeinBoost(1f, 0f), 0.0001f);
        }

        [Test]
        public void ApplyVeinBoost_OutsideRadiusOrNoVein_ReturnsUnchanged()
        {
            Assert.AreEqual(
                1f,
                LingQiRuleService.ApplyVeinBoost(1f, LingQiRuleService.VeinBoostRadius + 0.01f),
                0.0001f);
            // 无灵脉（MaxValue）自然判外
            Assert.AreEqual(1.3f, LingQiRuleService.ApplyVeinBoost(1.3f, float.MaxValue), 0.0001f);
        }

        [Test]
        public void CountArraysInRange_OnlyCountsWithinRadius()
        {
            IReadOnlyList<GameVector2> arrays = Points((0, 0), (3, 4), (10, 0));

            // 半径 4：(0,0) 距 0 ✓；(3,4) 距 5 ✗；(10,0) 距 10 ✗
            Assert.AreEqual(1, LingQiRuleService.CountArraysInRange(arrays, 0, 0));
            Assert.AreEqual(0, LingQiRuleService.CountArraysInRange(Points(), 0, 0));
            Assert.AreEqual(0, LingQiRuleService.CountArraysInRange(null, 0, 0));
        }

        [Test]
        public void ApplySpiritArrayBoost_StacksExponentially()
        {
            // 1.3² = 1.69
            Assert.AreEqual(1.69f, LingQiRuleService.ApplySpiritArrayBoost(1f, 2), 0.001f);
        }

        [Test]
        public void ApplySpiritArrayBoost_CapsAtThreeStacks()
        {
            // n=10 封顶 3 层 ≈ 2.197
            float capped = LingQiRuleService.ApplySpiritArrayBoost(1f, 10);
            float threeStacks = LingQiRuleService.ApplySpiritArrayBoost(1f, 3);
            Assert.AreEqual(threeStacks, capped, 0.0001f);
            Assert.AreEqual(2.197f, capped, 0.001f);
        }

        [Test]
        public void ApplySpiritArrayBoost_ZeroOrNegativeArrays_ReturnsUnchanged()
        {
            Assert.AreEqual(1f, LingQiRuleService.ApplySpiritArrayBoost(1f, 0), 0.0001f);
            Assert.AreEqual(1.2f, LingQiRuleService.ApplySpiritArrayBoost(1.2f, -3), 0.0001f);
        }

        [Test]
        public void ComposeMultiplier_MultipliesAllFactors()
        {
            // 1.3 地形 × 1.5 灵脉 × 1.69 双阵 × 1.05 天气
            float composed = LingQiRuleService.ComposeMultiplier(1.3f, 0f, 2, 1.05f);
            Assert.AreEqual(1.3f * 1.5f * 1.69f * 1.05f, composed, 0.001f);
        }

        [Test]
        public void ComposeMultiplier_DefaultWeather_IsOne()
        {
            float composed = LingQiRuleService.ComposeMultiplier(1f, float.MaxValue, 0);
            Assert.AreEqual(1f, composed, 0.0001f);
        }

        [Test]
        public void ComposeMultiplier_ClampsNegativeWeatherAndTerrain()
        {
            Assert.AreEqual(0f, LingQiRuleService.ComposeMultiplier(1f, 0f, 0, -1f), 0.0001f);
            Assert.AreEqual(0f, LingQiRuleService.ComposeMultiplier(-2f, 0f, 0, 1f), 0.0001f);
        }
    }
}
