namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// Worker 生命周期阶段 — 控制决策优先级和行为模式。
    /// Bootstrap: 刚出生/重生，优先采集食物→采集建材→建家→建床。
    /// Settled: 有家有床，可正常参与市场经济。
    /// Established: 资源充裕，可扩建/升级/奢侈消费。
    /// </summary>
    [System.Serializable]
    public enum WorkerLifeStage
    {
        /// <summary>刚出生/重生：采集食物→采集建材→建家→建床</summary>
        Bootstrap,

        /// <summary>有家有床：正常生产/交易/社交</summary>
        Settled,

        /// <summary>资源充裕：扩建/升级/奢侈消费</summary>
        Established,
    }
}
