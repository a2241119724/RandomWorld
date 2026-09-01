namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using NUnit.Framework;

    [TestFixture]
    public class GrowthBonusTests
    {
        [Test]
        public void Zero_AllDimensionsNeutral()
        {
            GrowthBonus zero = GrowthBonus.Zero;
            Assert.AreEqual(BattleStats.Zero.ATN, zero.Stats.ATN);
            Assert.AreEqual(0f, zero.MaxHpFlat, 0.0001f);
            Assert.AreEqual(0f, zero.MaxMpFlat, 0.0001f);
            Assert.AreEqual(0f, zero.MpRegenPerSec, 0.0001f);
            Assert.AreEqual(0f, zero.LifestealRatio, 0.0001f);
            Assert.AreEqual(0f, zero.ReflectRatio, 0.0001f);
            Assert.AreEqual(0f, zero.CultivationSpeedMul, 0.0001f);
        }

        [Test]
        public void OperatorPlus_SumsAllDimensions()
        {
            GrowthBonus a = new GrowthBonus(
                new BattleStats(1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f),
                maxHpFlat: 10f,
                mpRegenPerSec: 2f,
                lifestealRatio: 0.05f);
            GrowthBonus b = new GrowthBonus(
                new BattleStats(2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f),
                maxHpFlat: 5f,
                reflectRatio: 0.1f,
                cultivationSpeedMul: 0.2f);

            GrowthBonus sum = a + b;

            Assert.AreEqual(3f, sum.Stats.ATN, 0.0001f);
            Assert.AreEqual(15f, sum.MaxHpFlat, 0.0001f);
            Assert.AreEqual(2f, sum.MpRegenPerSec, 0.0001f);
            Assert.AreEqual(0.05f, sum.LifestealRatio, 0.0001f);
            Assert.AreEqual(0.1f, sum.ReflectRatio, 0.0001f);
            Assert.AreEqual(0.2f, sum.CultivationSpeedMul, 0.0001f);
        }

        [Test]
        public void GrowthSourceResult_Add_AccumulatesSourcesAndSpecial()
        {
            GrowthSourceResult result = new GrowthSourceResult();
            result.Add(new GrowthBonus(new BattleStats(1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f), maxHpFlat: 10f));
            result.Add(new GrowthBonus(new BattleStats(2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f), mpRegenPerSec: 1f));

            Assert.AreEqual(2, result.Sources.Count);
            Assert.AreEqual(3f, result.Special.Stats.ATN, 0.0001f);
            Assert.AreEqual(10f, result.Special.MaxHpFlat, 0.0001f);
            Assert.AreEqual(1f, result.Special.MpRegenPerSec, 0.0001f);
        }
    }
}
