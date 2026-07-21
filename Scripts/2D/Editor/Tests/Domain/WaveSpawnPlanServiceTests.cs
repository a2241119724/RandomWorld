namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Wave;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class WaveSpawnPlanServiceTests
    {
        private readonly WaveRuleService ruleService = new WaveRuleService();
        private WaveSpawnPlanService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new WaveSpawnPlanService(this.ruleService);
        }

        private static WaveConfigModel TestConfig()
        {
            return new WaveConfigModel
            {
                BaseEnemyCount = 5,
                EnemiesPerWaveIncrease = 2,
                TotalWaves = 10,
                DifficultyScalePerWave = 0.1f,
            };
        }

        [Test]
        public void CreatePlan_GeneratesRequests()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 10);
            Assert.AreEqual(5, plan.BaseEnemyCount);
            Assert.AreEqual(10, plan.TotalEnemyCount);
            Assert.AreEqual(10, plan.Requests.Count);
        }

        [Test]
        public void CreatePlan_NegativeAdjustedCount_ClampsTo1()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), -5);
            Assert.AreEqual(1, plan.TotalEnemyCount);
            Assert.AreEqual(1, plan.Requests.Count);
        }

        [Test]
        public void CreatePlan_FirstRequestHasCorrectWaveIndex()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 3);
            Assert.AreEqual(1, plan.Requests[0].WaveIndex);
            Assert.AreEqual(0, plan.Requests[0].SpawnIndex);
            Assert.AreEqual(3, plan.Requests[0].TotalEnemiesInWave);
        }

        [Test]
        public void CreatePlan_LastRequestHasLastSpawnIndex()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 3);
            Assert.AreEqual(2, plan.Requests[2].SpawnIndex);
        }

        [Test]
        public void CreatePlan_DifficultyScaleIsPropagated()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 1);
            Assert.AreEqual(1f, plan.Requests[0].DifficultyScale, 0.0001f);
        }
    }
}
