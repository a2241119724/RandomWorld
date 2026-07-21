namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using NUnit.Framework;

    [TestFixture]
    public class WorkerConditionRuleServiceTests
    {
        private WorkerConditionRuleService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new WorkerConditionRuleService();
        }

        [Test]
        public void GetState_NormalHungryAndTired_ReturnsHealthy()
        {
            var snapshot = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 80f, 100f, 80f, 100f);
            Assert.AreEqual(WorkerConditionState.Healthy, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_LowHungry_ReturnsHungry()
        {
            var snapshot = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 20f, 100f, 80f, 100f);
            Assert.AreEqual(WorkerConditionState.Hungry, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_LowTired_ReturnsTired()
        {
            var snapshot = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 80f, 100f, 20f, 100f);
            Assert.AreEqual(WorkerConditionState.Tired, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_BothLow_ReturnsExhausted()
        {
            var snapshot = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 20f, 100f, 20f, 100f);
            Assert.AreEqual(WorkerConditionState.Exhausted, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_CriticalHungry_ReturnsCritical()
        {
            var snapshot = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 3f, 100f, 80f, 100f);
            Assert.AreEqual(WorkerConditionState.Critical, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_ZeroHungry_ReturnsCritical()
        {
            var snapshot = new WorkerAgentSnapshot(1, new GameVector2(0, 0), true, false, 0f, 100f, 80f, 100f);
            Assert.AreEqual(WorkerConditionState.Critical, this.service.GetState(snapshot));
        }

        [Test]
        public void GetState_NullSnapshot_ReturnsHealthy()
        {
            Assert.AreEqual(WorkerConditionState.Healthy, this.service.GetState(null));
        }

        [Test]
        public void GetMoveSpeedMultiplier_Healthy_ReturnsOne()
        {
            Assert.AreEqual(1.0f, this.service.GetMoveSpeedMultiplier(WorkerConditionState.Healthy), 0.0001f);
        }

        [Test]
        public void GetMoveSpeedMultiplier_Critical_IsLowest()
        {
            float crit = this.service.GetMoveSpeedMultiplier(WorkerConditionState.Critical);
            float exhausted = this.service.GetMoveSpeedMultiplier(WorkerConditionState.Exhausted);
            float hungry = this.service.GetMoveSpeedMultiplier(WorkerConditionState.Hungry);
            Assert.Less(crit, exhausted, "Critical 应比 Exhausted 更慢");
            Assert.Less(crit, hungry, "Critical 应比 Hungry 更慢");
        }

        [Test]
        public void GetTaskProgressMultiplier_EatOrSleepTask_ReturnsOneRegardlessOfState()
        {
            Assert.AreEqual(1.0f, this.service.GetTaskProgressMultiplier(WorkerConditionState.Critical, true), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetTaskProgressMultiplier(WorkerConditionState.Exhausted, true), 0.0001f);
        }

        [Test]
        public void GetTaskProgressMultiplier_CriticalNormalTask_IsLowest()
        {
            float crit = this.service.GetTaskProgressMultiplier(WorkerConditionState.Critical, false);
            float exhausted = this.service.GetTaskProgressMultiplier(WorkerConditionState.Exhausted, false);
            Assert.Less(crit, exhausted, "Critical 普通任务进度应最低");
        }

        [Test]
        public void GetTaskProgressMultiplier_Healthy_ReturnsOne()
        {
            Assert.AreEqual(1.0f, this.service.GetTaskProgressMultiplier(WorkerConditionState.Healthy, false), 0.0001f);
        }
    }
}
