namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// The next engine-facing action requested by the pure wave flow rules.
    /// Unity code consumes this as coroutine waits, enemy prefab creation, or UI notifications.
    /// </summary>
    public sealed class WaveFlowDecision
    {
        public WaveFlowDecisionType Type { get; set; }

        public int WaveIndex { get; set; }

        public int SpawnIndex { get; set; }

        public int TotalEnemiesInWave { get; set; }

        public int TotalWavesCompleted { get; set; }

        public float DifficultyScale { get; set; }

        public float RestDuration { get; set; }
    }
}
