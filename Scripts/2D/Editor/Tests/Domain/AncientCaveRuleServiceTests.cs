namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.AncientCave;
    using NUnit.Framework;

    [TestFixture]
    public class AncientCaveRuleServiceTests
    {
        private static readonly GameVector2 Origin = new GameVector2(50f, 50f);

        [Test]
        public void IsPlacementValid_TooCloseToMapCenter_False()
        {
            // 距中心 19 格 < 20 → 拒绝；恰 20 格（边界含）→ 合法
            Assert.IsFalse(AncientCaveRuleService.IsPlacementValid(null, new GameVector2(69f, 50f), Origin));
            Assert.IsTrue(AncientCaveRuleService.IsPlacementValid(null, new GameVector2(70f, 50f), Origin));
        }

        [Test]
        public void IsPlacementValid_TooCloseToExistingCave_False()
        {
            // 既有洞府 (70,50)（距中心 20 已合法）；候选 (99,50) 距离 29 < 30 → 拒绝；
            // (100,50) 距离 30 边界含 → 合法。候选必须距中心 ≥20，否则先被中心约束拒绝
            // 测不到间距分支（首版 (50,50) 压中心踩坑）。
            var existing = new List<AncientCaveRuleService.AncientCaveModel>
            {
                new AncientCaveRuleService.AncientCaveModel(new GameVector2(70f, 50f), AncientCaveRuleService.CaveState.Hidden),
            };

            Assert.IsFalse(AncientCaveRuleService.IsPlacementValid(existing, new GameVector2(99f, 50f), Origin));
            Assert.IsTrue(AncientCaveRuleService.IsPlacementValid(existing, new GameVector2(100f, 50f), Origin));
        }

        [Test]
        public void IsPlacementValid_EmptyExisting_ValidWhenFarEnough()
        {
            // 空列表（首洞府撒点路径）：距中心够远即合法
            Assert.IsTrue(AncientCaveRuleService.IsPlacementValid(
                new List<AncientCaveRuleService.AncientCaveModel>(), new GameVector2(90f, 90f), Origin));
            Assert.IsTrue(AncientCaveRuleService.IsPlacementValid(null, new GameVector2(90f, 90f), Origin));
        }

        [Test]
        public void ShouldReveal_BoundaryInclusive()
        {
            Assert.IsFalse(AncientCaveRuleService.ShouldReveal(8.1f));
            Assert.IsTrue(AncientCaveRuleService.ShouldReveal(8f));
            Assert.IsTrue(AncientCaveRuleService.ShouldReveal(0f));
        }

        [Test]
        public void RevealProgress_EndStates()
        {
            Assert.AreEqual(0f, AncientCaveRuleService.RevealProgress(0f), 0.0001f);
            Assert.AreEqual(0f, AncientCaveRuleService.RevealProgress(-1f), 0.0001f);
            Assert.AreEqual(1f, AncientCaveRuleService.RevealProgress(AncientCaveRuleService.RevealFadeSeconds), 0.0001f);
            Assert.AreEqual(1f, AncientCaveRuleService.RevealProgress(999f), 0.0001f);
        }

        [Test]
        public void RevealProgress_MonotonicAndMidway()
        {
            // 前 60% 线性：30% 时间处进度 0.35；中点（0.6 处）达 0.7；全程单调
            Assert.AreEqual(0.35f, AncientCaveRuleService.RevealProgress(AncientCaveRuleService.RevealFadeSeconds * 0.3f), 0.001f);
            Assert.AreEqual(0.7f, AncientCaveRuleService.RevealProgress(AncientCaveRuleService.RevealFadeSeconds * 0.6f), 0.001f);

            float prev = 0f;
            for (int i = 1; i <= 20; i++)
            {
                float p = AncientCaveRuleService.RevealProgress(AncientCaveRuleService.RevealFadeSeconds * i / 20f);
                Assert.GreaterOrEqual(p, prev, "淡入进度必须单调不减");
                prev = p;
            }
        }
    }
}
