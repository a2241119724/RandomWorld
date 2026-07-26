namespace LAB2D.Domain.Wave
{
    using System.Collections.Generic;

    /// <summary>
    /// Engine-independent spawn plan for a single wave.
    /// Unity-side code consumes requests and decides prefab, position, and timing.
    /// </summary>
    public sealed class WaveSpawnPlan
    {
        private readonly List<WaveSpawnRequest> requests;

        public WaveSpawnPlan(int baseEnemyCount, int totalEnemyCount, List<WaveSpawnRequest> requests)
        {
            this.BaseEnemyCount = baseEnemyCount < 1 ? 1 : baseEnemyCount;
            this.TotalEnemyCount = totalEnemyCount < 1 ? 1 : totalEnemyCount;
            this.requests = requests ?? new List<WaveSpawnRequest>();
        }

        public int BaseEnemyCount { get; private set; }

        public int TotalEnemyCount { get; private set; }

        public IReadOnlyList<WaveSpawnRequest> Requests
        {
            get { return this.requests; }
        }
    }
}
