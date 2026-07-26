namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class AttributeCalculationServiceTests
    {
        private AttributeCalculationService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new AttributeCalculationService();
        }

        [Test]
        public void ComputeFinalStats_NonPlayer_ReturnsBaseStatsUnchanged()
        {
            BattleStats baseStats = new BattleStats(10f, 5f, 3f, 2f, 0.1f, 1.5f, 2f, 1f);
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, false, null, null);
            Assert.AreEqual(10f, result.ATN, 0.0001f);
            Assert.AreEqual(5f, result.INT, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_PlayerLevel1_AppliesLevelRatio()
        {
            BattleStats baseStats = new BattleStats(10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, true, null, null);
            Assert.AreEqual(11f, result.ATN, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_PlayerLevel5_AppliesHigherRatio()
        {
            BattleStats baseStats = new BattleStats(100f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats result = this.service.ComputeFinalStats(baseStats, 5, true, null, null);
            Assert.AreEqual(150f, result.ATN, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_WithWeapon_AddsWeaponStats()
        {
            BattleStats baseStats = new BattleStats(10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats weaponStats = new BattleStats(5f, 3f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, false, weaponStats, null);
            Assert.AreEqual(15f, result.ATN, 0.0001f);
            Assert.AreEqual(3f, result.INT, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_WithEquipments_AddsAllEquipments()
        {
            BattleStats baseStats = new BattleStats(10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            List<BattleStats> equipments = new List<BattleStats>
            {
                new BattleStats(2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f),
                new BattleStats(3f, 0f, 0f, 0f, 0f, 0f, 0f, 0f),
            };
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, false, null, equipments);
            Assert.AreEqual(15f, result.ATN, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_PlayerWithWeaponAndEquipments_AllStacked()
        {
            BattleStats baseStats = new BattleStats(100f, 50f, 30f, 20f, 0.05f, 2.0f, 1f, 1f);
            BattleStats weaponStats = new BattleStats(20f, 10f, 0f, 0f, 0.02f, 0.5f, 0f, 0f);
            List<BattleStats> equipments = new List<BattleStats>
            {
                new BattleStats(5f, 0f, 10f, 5f, 0f, 0f, 2f, 0f),
                new BattleStats(0f, 5f, 0f, 10f, 0.01f, 0.3f, 0f, 3f),
            };

            BattleStats result = this.service.ComputeFinalStats(baseStats, 3, true, weaponStats, equipments);

            float ratio = 1f + 3 * 0.1f;
            Assert.AreEqual(100f * ratio + 20f + 5f + 0f, result.ATN, 0.0001f);
            Assert.AreEqual(50f * ratio + 10f + 0f + 5f, result.INT, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_NullWeapon_NoEffect()
        {
            BattleStats baseStats = new BattleStats(10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, false, null, null);
            Assert.AreEqual(10f, result.ATN, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_NullEquipments_NoEffect()
        {
            BattleStats baseStats = new BattleStats(10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, false, null, null);
            Assert.AreEqual(10f, result.ATN, 0.0001f);
        }

        [Test]
        public void ComputeFinalStats_EmptyEquipments_NoEffect()
        {
            BattleStats baseStats = new BattleStats(10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            List<BattleStats> equipments = new List<BattleStats>();
            BattleStats result = this.service.ComputeFinalStats(baseStats, 1, false, null, equipments);
            Assert.AreEqual(10f, result.ATN, 0.0001f);
        }
    }
}
