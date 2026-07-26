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

        [Test]
        public void BeginRestAndCreateDecision_ClampsNegativeDuration()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();

            WaveFlowDecision decision = service.BeginRestAndCreateDecision(state, -1.0f);

            Assert.AreEqual(WaveFlowDecisionType.RestStarted, decision.Type);
            Assert.AreEqual(0.0f, decision.RestDuration);
            Assert.IsTrue(state.IsResting);
        }

        [Test]
        public void BeginNextWaveAndCreateDecision_UsesNextWaveState()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();
            WaveConfigModel config = new WaveConfigModel
            {
                DifficultyScalePerWave = 0.5f,
            };

            WaveFlowDecision decision = service.BeginNextWaveAndCreateDecision(state, 4, 7, config);

            Assert.AreEqual(WaveFlowDecisionType.WaveStarted, decision.Type);
            Assert.AreEqual(1, decision.WaveIndex);
            Assert.AreEqual(7, decision.TotalEnemiesInWave);
            Assert.AreEqual(1.0f, decision.DifficultyScale);
            Assert.AreEqual(4, state.EnemiesAliveBeforeWave);
        }

        [Test]
        public void CreateSpawnDecision_UsesCurrentWaveAndSpawnIndex()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();
            WaveConfigModel config = new WaveConfigModel();

            service.BeginNextWave(state, 0);
            WaveFlowDecision decision = service.CreateSpawnDecision(state, 2, 5, config);

            Assert.AreEqual(WaveFlowDecisionType.SpawnEnemy, decision.Type);
            Assert.AreEqual(1, decision.WaveIndex);
            Assert.AreEqual(2, decision.SpawnIndex);
            Assert.AreEqual(5, decision.TotalEnemiesInWave);
            Assert.AreEqual(1.0f, decision.DifficultyScale);
        }

        [Test]
        public void TryCreateAllWavesClearedDecision_WhenLimitReached_ReturnsDecision()
        {
            WaveRuntimeState state = new WaveRuntimeState();
            WaveFlowService service = new WaveFlowService();
            WaveConfigModel config = new WaveConfigModel
            {
                TotalWaves = 1,
            };

            service.BeginNextWave(state, 0);
            service.CompleteCurrentWave(state);

            bool hasDecision = service.TryCreateAllWavesClearedDecision(state, config, out WaveFlowDecision decision);

            Assert.IsTrue(hasDecision);
            Assert.AreEqual(WaveFlowDecisionType.AllWavesCleared, decision.Type);
            Assert.AreEqual(1, decision.WaveIndex);
            Assert.AreEqual(1, decision.TotalWavesCompleted);
        }
    }
}
