namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using NUnit.Framework;

    [TestFixture]
    public class BattleStatsTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            BattleStats stats = new BattleStats(1f, 2f, 3f, 4f, 0.1f, 1.5f, 2f, 3f);
            Assert.AreEqual(1f, stats.ATN, 0.0001f);
            Assert.AreEqual(2f, stats.INT, 0.0001f);
            Assert.AreEqual(3f, stats.DEF, 0.0001f);
            Assert.AreEqual(4f, stats.RES, 0.0001f);
            Assert.AreEqual(0.1f, stats.CRT, 0.0001f);
            Assert.AreEqual(1.5f, stats.CSD, 0.0001f);
            Assert.AreEqual(2f, stats.SPD, 0.0001f);
            Assert.AreEqual(3f, stats.HIT, 0.0001f);
        }

        [Test]
        public void Zero_ReturnsAllZeros()
        {
            BattleStats stats = BattleStats.Zero;
            Assert.AreEqual(0f, stats.ATN, 0.0001f);
            Assert.AreEqual(0f, stats.INT, 0.0001f);
            Assert.AreEqual(0f, stats.DEF, 0.0001f);
            Assert.AreEqual(0f, stats.RES, 0.0001f);
            Assert.AreEqual(0f, stats.CRT, 0.0001f);
            Assert.AreEqual(0f, stats.CSD, 0.0001f);
            Assert.AreEqual(0f, stats.SPD, 0.0001f);
            Assert.AreEqual(0f, stats.HIT, 0.0001f);
        }

        [Test]
        public void OperatorPlus_AddsAllFields()
        {
            BattleStats a = new BattleStats(1f, 2f, 3f, 4f, 0.1f, 1.5f, 2f, 3f);
            BattleStats b = new BattleStats(5f, 6f, 7f, 8f, 0.2f, 2.5f, 4f, 5f);
            BattleStats result = a + b;
            Assert.AreEqual(6f, result.ATN, 0.0001f);
            Assert.AreEqual(8f, result.INT, 0.0001f);
            Assert.AreEqual(10f, result.DEF, 0.0001f);
            Assert.AreEqual(12f, result.RES, 0.0001f);
            Assert.AreEqual(0.3f, result.CRT, 0.0001f);
            Assert.AreEqual(4.0f, result.CSD, 0.0001f);
            Assert.AreEqual(6f, result.SPD, 0.0001f);
            Assert.AreEqual(8f, result.HIT, 0.0001f);
        }

        [Test]
        public void OperatorPlus_WithZero_ReturnsOriginal()
        {
            BattleStats a = new BattleStats(10f, 20f, 30f, 40f, 0.5f, 3f, 5f, 7f);
            BattleStats result = a + BattleStats.Zero;
            Assert.AreEqual(10f, result.ATN, 0.0001f);
            Assert.AreEqual(20f, result.INT, 0.0001f);
        }

        [Test]
        public void OperatorMultiply_ScalesAllFields()
        {
            BattleStats a = new BattleStats(10f, 20f, 30f, 40f, 0.5f, 3f, 5f, 7f);
            BattleStats result = a * 1.5f;
            Assert.AreEqual(15f, result.ATN, 0.0001f);
            Assert.AreEqual(30f, result.INT, 0.0001f);
            Assert.AreEqual(45f, result.DEF, 0.0001f);
            Assert.AreEqual(60f, result.RES, 0.0001f);
            Assert.AreEqual(0.75f, result.CRT, 0.0001f);
            Assert.AreEqual(4.5f, result.CSD, 0.0001f);
            Assert.AreEqual(7.5f, result.SPD, 0.0001f);
            Assert.AreEqual(10.5f, result.HIT, 0.0001f);
        }

        [Test]
        public void OperatorMultiply_ByOne_ReturnsEqual()
        {
            BattleStats a = new BattleStats(10f, 20f, 30f, 40f, 0.5f, 3f, 5f, 7f);
            BattleStats result = a * 1f;
            Assert.AreEqual(10f, result.ATN, 0.0001f);
            Assert.AreEqual(20f, result.INT, 0.0001f);
        }

        [Test]
        public void OperatorMultiply_ByZero_ReturnsAllZeros()
        {
            BattleStats a = new BattleStats(10f, 20f, 30f, 40f, 0.5f, 3f, 5f, 7f);
            BattleStats result = a * 0f;
            Assert.AreEqual(0f, result.ATN, 0.0001f);
            Assert.AreEqual(0f, result.INT, 0.0001f);
        }

        [Test]
        public void OperatorMultiply_NegativeScalar_PreservesSign()
        {
            BattleStats a = new BattleStats(10f, 20f, 0f, 0f, 0f, 0f, 0f, 0f);
            BattleStats result = a * -1f;
            Assert.AreEqual(-10f, result.ATN, 0.0001f);
            Assert.AreEqual(-20f, result.INT, 0.0001f);
        }
    }
}
