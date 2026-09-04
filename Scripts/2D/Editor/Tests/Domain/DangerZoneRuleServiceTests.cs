namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.DangerZone;
    using NUnit.Framework;

    [TestFixture]
    public class DangerZoneRuleServiceTests
    {
        private static readonly GameVector2 Origin = new GameVector2(50f, 50f);

        [Test]
        public void IsInZone_EmptyOrNull_ReturnsFalse()
        {
            Assert.IsFalse(DangerZoneRuleService.IsInZone(new List<DangerZoneRuleService.DangerZoneModel>(), 10, 10));
            Assert.IsFalse(DangerZoneRuleService.IsInZone(null, 10, 10));
        }

        [Test]
        public void IsInZone_CenterAndBoundary_True()
        {
            var zones = new List<DangerZoneRuleService.DangerZoneModel> { new DangerZoneRuleService.DangerZoneModel(Origin, 12f) };

            // 圆心必中；边界（恰为半径）含；半径 +0.1 之外为 false
            Assert.IsTrue(DangerZoneRuleService.IsInZone(zones, 50, 50));
            Assert.IsTrue(DangerZoneRuleService.IsInZone(zones, 62, 50));
            Assert.IsFalse(DangerZoneRuleService.IsInZone(zones, 63, 50));
        }

        [Test]
        public void IsInZone_SecondZoneHit()
        {
            var zones = new List<DangerZoneRuleService.DangerZoneModel>
            {
                new DangerZoneRuleService.DangerZoneModel(new GameVector2(20f, 20f), 5f),
                new DangerZoneRuleService.DangerZoneModel(new GameVector2(80f, 80f), 10f),
            };

            Assert.IsFalse(DangerZoneRuleService.IsInZone(zones, 50, 50));
            Assert.IsTrue(DangerZoneRuleService.IsInZone(zones, 88, 80));
        }

        [Test]
        public void MoveSpeedMultiplier_ByInZoneFlag()
        {
            Assert.AreEqual(0.7f, DangerZoneRuleService.MoveSpeedMultiplier(true), 0.0001f);
            Assert.AreEqual(1f, DangerZoneRuleService.MoveSpeedMultiplier(false), 0.0001f);
        }

        [Test]
        public void QiDensityMultiplier_ByInZoneFlag()
        {
            Assert.AreEqual(1.3f, DangerZoneRuleService.QiDensityMultiplier(true), 0.0001f);
            Assert.AreEqual(1f, DangerZoneRuleService.QiDensityMultiplier(false), 0.0001f);
        }

        [Test]
        public void IsPlacementValid_TooCloseToMapCenter_False()
        {
            // 圆心距中心 14 格 < 15 → 拒绝；恰 15 格（边界含）→ 合法
            var candidate = new GameVector2(64f, 50f);
            Assert.IsFalse(DangerZoneRuleService.IsPlacementValid(null, candidate, 10f, Origin));
            Assert.IsTrue(DangerZoneRuleService.IsPlacementValid(
                null, new GameVector2(65f, 50f), 10f, Origin));
        }

        [Test]
        public void IsPlacementValid_OverlapsExisting_False()
        {
            // 既有区 (80,50) r=10；候选 (60,50) r=10 → 圆心距 20 = r1+r2 相切合法；
            // 候选 (61,50) → 圆心距 19 < 20 重叠拒绝
            var existing = new List<DangerZoneRuleService.DangerZoneModel>
            {
                new DangerZoneRuleService.DangerZoneModel(new GameVector2(80f, 50f), 10f),
            };

            Assert.IsTrue(DangerZoneRuleService.IsPlacementValid(existing, new GameVector2(60f, 50f), 10f, Origin));
            Assert.IsFalse(DangerZoneRuleService.IsPlacementValid(existing, new GameVector2(61f, 50f), 10f, Origin));
        }

        [Test]
        public void IsPlacementValid_EmptyMap_ValidWhenFarEnough()
        {
            // 无既有区：只要离中心够远即合法（首区撒点路径）
            Assert.IsTrue(DangerZoneRuleService.IsPlacementValid(
                new List<DangerZoneRuleService.DangerZoneModel>(), new GameVector2(90f, 90f), 14f, Origin));
        }
    }
}
