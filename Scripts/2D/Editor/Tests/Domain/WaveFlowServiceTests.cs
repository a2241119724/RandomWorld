namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Wave;
    using NUnit.Framework;

    [TestFixture]
    public class WaveFlowServiceTests
    {
        [Test]
        public void BeginNextWave_InitialState_ActivatesFirstWave()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();

            service.BeginNextWave(state, 2);

            Assert.AreEqual(1, state.CurrentWaveIndex);
            Assert.AreEqual(2, state.EnemiesAliveBeforeWave);
            Assert.IsTrue(state.IsWaveActive);
            Assert.IsFalse(state.IsResting);
        }

        [Test]
        public void RegisterSpawnSuccess_AfterTwoSpawns_SyncsAliveCount()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();

            service.BeginNextWave(state, 0);
            service.RegisterSpawnSuccess(state);
            service.RegisterSpawnSuccess(state);
            service.SyncAliveCountAfterSpawning(state);

            Assert.AreEqual(2, state.EnemiesSpawnedThisWave);
            Assert.AreEqual(2, state.EnemiesAliveInWave);
        }

        [Test]
        public void IsCurrentWaveCleared_WhenAliveReturnsToPreWaveCount_ReturnsTrue()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();

            service.BeginNextWave(state, 1);
            service.RegisterSpawnSuccess(state);

            Assert.IsTrue(service.IsCurrentWaveCleared(state, 1));
        }

        [Test]
        public void CompleteCurrentWave_IncrementsCompletedAndDefeated()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();

            service.BeginNextWave(state, 0);
            service.RegisterSpawnSuccess(state);
            service.RegisterSpawnSuccess(state);
            service.CompleteCurrentWave(state);

            Assert.IsFalse(state.IsWaveActive);
            Assert.AreEqual(1, state.TotalWavesCompleted);
            Assert.AreEqual(2, state.EnemiesDefeatedInWave);
            Assert.AreEqual(0, state.EnemiesAliveInWave);
        }

        [Test]
        public void CreateSpawnRequest_UsesCurrentWaveAndDifficulty()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();
            WaveConfigModel config = new WaveConfigModel
            {
                DifficultyScalePerWave = 0.25f,
            };

            service.BeginNextWave(state, 0);
            WaveSpawnRequest request = service.CreateSpawnRequest(state, 3, 8, config);

            Assert.AreEqual(1, request.WaveIndex);
            Assert.AreEqual(3, request.SpawnIndex);
            Assert.AreEqual(8, request.TotalEnemiesInWave);
            Assert.AreEqual(1.0f, request.DifficultyScale);
        }
    }
}
