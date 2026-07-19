namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// Coordinates wave runtime state transitions without depending on Unity coroutines,
    /// prefabs, maps, or scene objects.
    /// </summary>
    public sealed class WaveFlowService
    {
        private readonly WaveRuleService ruleService;

        public WaveFlowService()
            : this(new WaveRuleService())
        {
        }

        public WaveFlowService(WaveRuleService ruleService)
        {
            this.ruleService = ruleService ?? new WaveRuleService();
        }

        public float GetDifficultyScale(WaveRuntimeState state, WaveConfigModel config)
        {
            WaveRuntimeState safeState = state ?? new WaveRuntimeState();
            return this.ruleService.GetDifficultyScale(safeState.TotalWavesCompleted, config);
        }

        public bool AreAllWavesCleared(WaveRuntimeState state, WaveConfigModel config)
        {
            WaveRuntimeState safeState = state ?? new WaveRuntimeState();
            return this.ruleService.AreAllWavesCleared(safeState.TotalWavesCompleted, config);
        }

        public int GetEnemyCountForWave(int waveIndex, WaveConfigModel config)
        {
            return this.ruleService.GetEnemyCountForWave(waveIndex, config);
        }

        public int GetEffectiveMaxAliveEnemies(int configMaxAliveEnemies, int runtimeMaxEnemyCount)
        {
            return this.ruleService.GetEffectiveMaxAliveEnemies(configMaxAliveEnemies, runtimeMaxEnemyCount);
        }

        public void Reset(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.Reset();
        }

        public void BeginRest(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.BeginRest();
        }

        public void EndRest(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.EndRest();
        }

        public void BeginNextWave(WaveRuntimeState state, int aliveEnemiesBeforeWave)
        {
            if (state == null)
            {
                return;
            }

            state.BeginNextWave(aliveEnemiesBeforeWave);
        }

        public void RegisterSpawnSuccess(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.RegisterSpawnSuccess();
        }

        public void SyncAliveCountAfterSpawning(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.SyncWaveAliveCountToSpawned();
        }

        public bool IsCurrentWaveCleared(WaveRuntimeState state, int currentAliveEnemies)
        {
            if (state == null)
            {
                return false;
            }

            return this.ruleService.IsWaveCleared(
                state.EnemiesSpawnedThisWave,
                currentAliveEnemies,
                state.EnemiesAliveBeforeWave);
        }

        public void CompleteCurrentWave(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.CompleteCurrentWave();
        }

        public void Stop(WaveRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.Stop();
        }

        public WaveSpawnRequest CreateSpawnRequest(
            WaveRuntimeState state,
            int spawnIndex,
            int totalEnemiesInWave,
            WaveConfigModel config)
        {
            WaveRuntimeState safeState = state ?? new WaveRuntimeState();
            return new WaveSpawnRequest
            {
                WaveIndex = safeState.CurrentWaveIndex,
                SpawnIndex = spawnIndex,
                TotalEnemiesInWave = totalEnemiesInWave,
                DifficultyScale = this.GetDifficultyScale(safeState, config),
            };
        }
    }
}
