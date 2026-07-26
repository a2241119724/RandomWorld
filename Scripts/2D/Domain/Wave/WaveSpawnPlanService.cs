namespace LAB2D.Domain.Wave
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;

    /// <summary>
    /// Builds pure spawn request data for a wave without touching Unity coroutines or prefabs.
    /// </summary>
    public sealed class WaveSpawnPlanService
    {
        private readonly WaveRuleService ruleService;

        public WaveSpawnPlanService()
            : this(new WaveRuleService())
        {
        }

        public WaveSpawnPlanService(WaveRuleService ruleService)
        {
            this.ruleService = ruleService ?? new WaveRuleService();
        }

        public WaveSpawnPlan CreatePlan(
            WaveRuntimeState state,
            WaveConfigModel config,
            int adjustedEnemyCount)
        {
            WaveRuntimeState safeState = state ?? new WaveRuntimeState();
            int baseEnemyCount = this.ruleService.GetEnemyCountForWave(safeState.CurrentWaveIndex, config);
            int totalEnemyCount = MathHelper.ClampMin(adjustedEnemyCount, 1);
            float difficultyScale = this.ruleService.GetDifficultyScale(safeState.TotalWavesCompleted, config);
            List<WaveSpawnRequest> requests = new List<WaveSpawnRequest>(totalEnemyCount);

            for (int i = 0; i < totalEnemyCount; i++)
            {
                requests.Add(new WaveSpawnRequest
                {
                    WaveIndex = safeState.CurrentWaveIndex,
                    SpawnIndex = i,
                    TotalEnemiesInWave = totalEnemyCount,
                    DifficultyScale = difficultyScale,
                });
            }

            return new WaveSpawnPlan(baseEnemyCount, totalEnemyCount, requests);
        }
    }
}
