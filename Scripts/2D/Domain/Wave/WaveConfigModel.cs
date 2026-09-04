namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// 引擎无关的波次规则配置。
    /// </summary>
    public sealed class WaveConfigModel
    {
        public int BaseEnemyCount { get; set; }

        public int EnemiesPerWaveIncrease { get; set; }

        public int MaxAliveEnemies { get; set; }

        public int TotalWaves { get; set; }

        public float DifficultyScalePerWave { get; set; }

        /// <summary>
        /// 新种妖兽（Charge/Shoot）开始混入的波次；之前的波次只用旧池（Common/Seek）。
        /// 默认 3，与 WaveConfig.newEnemyStartWave 一致。
        /// </summary>
        public int NewEnemyStartWave { get; set; } = 3;

        /// <summary>
        /// 当晚是否血月（事件天气）：数量 ×1.5（向上取整）、混池门槛提前 1 波、难度 +0.5。
        /// 默认 false——非血月路径与旧行为完全一致。
        /// </summary>
        public bool IsBloodMoon { get; set; }

        /// <summary>
        /// 每局修饰符·敌方强度通道（SessionModifierManager）：数量与难度同时缩放。
        /// 默认 1f——无修饰符路径与旧行为完全一致。
        /// </summary>
        public float EnemyStrengthMultiplier { get; set; } = 1f;
    }
}
