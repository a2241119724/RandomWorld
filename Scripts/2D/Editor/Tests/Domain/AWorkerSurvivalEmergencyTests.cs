namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// WorkerConditionRuleService.IsSurvivalEmergency 四项生存紧急判定
    /// （AWorker.CheckSurvivalEmergency 打断与防守夜补派守卫的阈值单一来源）。
    /// 阈值：饥饿 &lt;15 / 疲劳 &gt;Max-15 / 精气神 &lt;10 / 压力 &gt;Max-10
    /// （均为严格不等式，0/满态边界不算紧急）。
    /// </summary>
    [TestFixture]
    public class AWorkerSurvivalEmergencyTests
    {
        /// <summary>满态基线（CurHungry=100/CurTired=0/CurSpirit=100/CurStress=0，Max 均 100）。</summary>
        private static bool EmergencyAt(
            float hungry = 100f,
            float tired = 0f,
            float spirit = 100f,
            float stress = 0f,
            float maxTired = 100f,
            float maxStress = 100f)
        {
            return WorkerConditionRuleService.IsSurvivalEmergency(hungry, tired, maxTired, spirit, stress, maxStress);
        }

        [Test]
        public void FullVitals_False()
        {
            Assert.IsFalse(EmergencyAt());
        }

        [Test]
        public void Hungry_BelowThreshold_True()
        {
            Assert.IsTrue(EmergencyAt(hungry: 14.9f));

            // 严格 <：恰 15 不算；0（耗尽边界）不算——与 CheckSurvivalEmergency 的 >0 守卫一致
            Assert.IsFalse(EmergencyAt(hungry: 15f));
            Assert.IsFalse(EmergencyAt(hungry: 0f));
        }

        [Test]
        public void Tired_AboveMaxMinusMargin_True()
        {
            Assert.IsTrue(EmergencyAt(tired: 86f));

            // 严格 > Max-15 与 < Max：边界值与满疲劳不算（满值由 < Max 守卫排除）
            Assert.IsFalse(EmergencyAt(tired: 85f));
            Assert.IsFalse(EmergencyAt(tired: 100f));
        }

        [Test]
        public void Spirit_Below10_True()
        {
            Assert.IsTrue(EmergencyAt(spirit: 9.9f));

            // 严格 <10 与 >0
            Assert.IsFalse(EmergencyAt(spirit: 10f));
            Assert.IsFalse(EmergencyAt(spirit: 0f));
        }

        [Test]
        public void Stress_AboveMaxMinusMargin_True()
        {
            Assert.IsTrue(EmergencyAt(stress: 91f));

            // 严格 > Max-10 与 < Max
            Assert.IsFalse(EmergencyAt(stress: 90f));
            Assert.IsFalse(EmergencyAt(stress: 100f));
        }

        [Test]
        public void AnyEmergency_WinsOverHealthyOthers()
        {
            // 其余三项健康，单项越阈即紧急
            Assert.IsTrue(EmergencyAt(tired: 99f, spirit: 100f, stress: 0f));
            Assert.IsTrue(EmergencyAt(stress: 99f));
        }
    }
}
