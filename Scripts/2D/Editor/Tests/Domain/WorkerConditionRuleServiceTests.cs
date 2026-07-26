namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerConditionRuleServiceTests
    {
        private readonly WorkerConditionRuleService service = new WorkerConditionRuleService();

        [Test]
        public void GetSafeRatio_Half_Returns0_5()
        {
            Assert.AreEqual(0.5f, this.service.GetSafeRatio(50f, 100f), 0.0001f);
        }

        [Test]
        public void GetSafeRatio_ZeroMax_Returns0()
        {
            Assert.AreEqual(0f, this.service.GetSafeRatio(50f, 0f), 0.0001f);
        }

        [Test]
        public void ToPercentInt_0_25_Returns25()
        {
            Assert.AreEqual(25, this.service.ToPercentInt(0.25f));
        }

        [Test]
        public void GetState_FullVitals_ReturnsHealthy()
        {
            var snapshot = new WorkerAgentSnapshot(1L, default(GameVector2), true, false, 100f, 100f, 100f, 100f);
            Assert.AreEqual(WorkerConditionState.Healthy, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_LowHungry_ReturnsHungry()
        {
            var snapshot = new WorkerAgentSnapshot(1L, default(GameVector2), true, false, 30f, 100f, 100f, 100f);
            Assert.AreEqual(WorkerConditionState.Hungry, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_LowTired_ReturnsTired()
        {
            var snapshot = new WorkerAgentSnapshot(1L, default(GameVector2), true, false, 100f, 100f, 30f, 100f);
            Assert.AreEqual(WorkerConditionState.Tired, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_BothLow_ReturnsExhausted()
        {
            var snapshot = new WorkerAgentSnapshot(1L, default(GameVector2), true, false, 30f, 100f, 30f, 100f);
            Assert.AreEqual(WorkerConditionState.Exhausted, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_CriticalHungry_ReturnsCritical()
        {
            var snapshot = new WorkerAgentSnapshot(1L, default(GameVector2), true, false, 4f, 100f, 100f, 100f);
            Assert.AreEqual(WorkerConditionState.Critical, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_ZeroHungry_ReturnsCritical()
        {
            var snapshot = new WorkerAgentSnapshot(1L, default(GameVector2), true, false, 0f, 100f, 100f, 100f);
            Assert.AreEqual(WorkerConditionState.Critical, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_NullSnapshot_ReturnsHealthy()
        {
            Assert.AreEqual(WorkerConditionState.Healthy, this.service.GetState(null));
        }

        [Test]
        public void GetMoveSpeedMultiplier_Healthy_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetMoveSpeedMultiplier(WorkerConditionState.Healthy), 0.0001f);
        }

        [Test]
        public void GetMoveSpeedMultiplier_Critical_Returns0_58()
        {
            Assert.AreEqual(0.58f, this.service.GetMoveSpeedMultiplier(WorkerConditionState.Critical), 0.0001f);
        }

        [Test]
        public void GetMoveSpeedMultiplier_Hungry_Returns0_86()
        {
            Assert.AreEqual(0.86f, this.service.GetMoveSpeedMultiplier(WorkerConditionState.Hungry), 0.0001f);
        }

        [Test]
        public void GetTaskProgressMultiplier_EatTask_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetTaskProgressMultiplier(WorkerConditionState.Critical, true), 0.0001f);
        }

        [Test]
        public void GetTaskProgressMultiplier_CriticalNonEat_Returns0_45()
        {
            Assert.AreEqual(0.45f, this.service.GetTaskProgressMultiplier(WorkerConditionState.Critical, false), 0.0001f);
        }

        [Test]
        public void GetTaskProgressMultiplier_Exhausted_Returns0_6()
        {
            Assert.AreEqual(0.6f, this.service.GetTaskProgressMultiplier(WorkerConditionState.Exhausted, false), 0.0001f);
        }
    }
}
