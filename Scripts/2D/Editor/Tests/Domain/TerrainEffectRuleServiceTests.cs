namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    /// <summary>
    /// 地形效果安全乘法 — 负倍率钳 0、结果下限保护、默认倍率常量为 1。
    /// </summary>
    [TestFixture]
    public class TerrainEffectRuleServiceTests
    {
        private readonly TerrainEffectRuleService service = new TerrainEffectRuleService();

        [Test]
        public void DefaultMultipliers_AreOne()
        {
            Assert.AreEqual(1.0f, TerrainEffectRuleService.DefaultMoveSpeedMultiplier);
            Assert.AreEqual(1.0f, TerrainEffectRuleService.DefaultTiredDecayMultiplier);
            Assert.AreEqual(1.0f, TerrainEffectRuleService.DefaultHungryDecayMultiplier);
        }

        [Test]
        public void ApplyMultiplier_NormalCase()
        {
            Assert.AreEqual(7.5f, this.service.ApplyMultiplier(10f, 0.75f), 1e-4f);
            Assert.AreEqual(20f, this.service.ApplyMultiplier(10f, 2f), 1e-4f);
        }

        [Test]
        public void ApplyMultiplier_NegativeMultiplier_ClampedToZero()
        {
            Assert.AreEqual(0f, this.service.ApplyMultiplier(10f, -0.5f));
        }

        [Test]
        public void ApplyMultiplier_ResultBelowMin_IsRaisedToMin()
        {
            Assert.AreEqual(2f, this.service.ApplyMultiplier(10f, 0.1f, minValue: 2f), 1e-4f);
        }

        [Test]
        public void ApplyMultiplier_NegativeBase_NotRaisedByMin()
        {
            // 结果 -5 低于 minValue=0 会被抬升；本例验证下限保护方向：不高于 minValue
            float result = this.service.ApplyMultiplier(-10f, 0.5f);
            Assert.AreEqual(0f, result);
        }
    }
}
