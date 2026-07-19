namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Wave;
    using NUnit.Framework;

    [TestFixture]
    public class WaveSpawnPlanServiceTests
    {
        [Test]
        public void CreatePlan_UsesWaveRuleBaseCountAndAdjustedTotal()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveConfigModel config = new WaveConfigModel
            {
                BaseEnemyCount = 3,
                EnemiesPerWaveIncrease = 2,
                DifficultyScalePerWave = 0.25f,
            };
            WaveSpawnPlanService service = new WaveSpawnPlanService();

            state.BeginNextWave(0);
            state.CompleteCurrentWave();
            state.BeginNextWave(0);

            WaveSpawnPlan plan = service.CreatePlan(state, config, 8);

            Assert.AreEqual(5, plan.BaseEnemyCount);
            Assert.AreEqual(8, plan.TotalEnemyCount);
            Assert.AreEqual(8, plan.Requests.Count);
            Assert.AreEqual(2, plan.Requests[0].WaveIndex);
            Assert.AreEqual(0, plan.Requests[0].SpawnIndex);
            Assert.AreEqual(7, plan.Requests[7].SpawnIndex);
            Assert.AreEqual(1.25f, plan.Requests[0].DifficultyScale);
        }

        [Test]
        public void CreatePlan_ClampsAdjustedTotalToOne()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveSpawnPlanService service = new WaveSpawnPlanService();

            state.BeginNextWave(0);

            WaveSpawnPlan plan = service.CreatePlan(state, new WaveConfigModel(), 0);

            Assert.AreEqual(1, plan.TotalEnemyCount);
            Assert.AreEqual(1, plan.Requests.Count);
        }
    }
}
