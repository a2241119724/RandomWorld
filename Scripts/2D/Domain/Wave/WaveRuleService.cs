namespace LAB2D.Domain.Wave
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// 纯波次推进和生成数量规则。
    /// </summary>
    public sealed class WaveRuleService
    {
        /// <summary>新种出现前的旧池轮转（Common/Seek 交替，即旧行为的种类分布）。</summary>
        private static readonly WaveEnemyKind[] LegacyKindCycle = { WaveEnemyKind.Common, WaveEnemyKind.Seek };

        /// <summary>新种混入后的混池轮转（旧种略多，新种渐进偏向）。</summary>
        private static readonly WaveEnemyKind[] MixedKindCycle =
        {
            WaveEnemyKind.Common, WaveEnemyKind.Seek, WaveEnemyKind.Charge, WaveEnemyKind.Common,
            WaveEnemyKind.Shoot, WaveEnemyKind.Seek, WaveEnemyKind.Charge, WaveEnemyKind.Shoot,
        };

        public float GetDifficultyScale(int totalWavesCompleted, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            return 1.0f + (MathHelper.ClampMin(totalWavesCompleted, 0) * safeConfig.DifficultyScalePerWave);
        }

        public bool AreAllWavesCleared(int totalWavesCompleted, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            return safeConfig.TotalWaves > 0 && totalWavesCompleted >= safeConfig.TotalWaves;
        }

        public int GetEnemyCountForWave(int waveIndex, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            int normalizedWaveIndex = MathHelper.ClampMin(waveIndex, 1);
            int count = safeConfig.BaseEnemyCount + ((normalizedWaveIndex - 1) * safeConfig.EnemiesPerWaveIncrease);
            return MathHelper.ClampMin(count, 1);
        }

        public int GetEffectiveMaxAliveEnemies(int configMaxAliveEnemies, int runtimeMaxEnemyCount)
        {
            int maxAliveEnemies = configMaxAliveEnemies;
            if (runtimeMaxEnemyCount > 0)
            {
                maxAliveEnemies = MathHelper.ClampMax(maxAliveEnemies, runtimeMaxEnemyCount);
            }

            return MathHelper.ClampMin(maxAliveEnemies, 1);
        }

        /// <summary>
        /// 按波次与波内序号确定性挑选敌人种类（轮转制，同波内种类分散，无随机依赖、可测可复现）。
        /// 波次达到 <see cref="WaveConfigModel.NewEnemyStartWave"/> 前只用旧池，之后混入新种。
        /// </summary>
        /// <param name="waveIndex">波次号（1 起）。</param>
        /// <param name="spawnIndex">波内生成序号（0 起）。</param>
        /// <param name="config">波次配置。</param>
        /// <returns>该次生成应使用的敌人种类。</returns>
        public WaveEnemyKind PickEnemyKind(int waveIndex, int spawnIndex, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            int normalizedWaveIndex = MathHelper.ClampMin(waveIndex, 1);
            WaveEnemyKind[] cycle = normalizedWaveIndex >= MathHelper.ClampMin(safeConfig.NewEnemyStartWave, 1)
                ? MixedKindCycle
                : LegacyKindCycle;
            return cycle[MathHelper.ClampMin(spawnIndex, 0) % cycle.Length];
        }

        public bool IsWaveCleared(int enemiesSpawnedThisWave, int currentAliveEnemies, int aliveEnemiesBeforeWave)
        {
            return enemiesSpawnedThisWave > 0 && currentAliveEnemies <= aliveEnemiesBeforeWave;
        }

        public float GetRemainingRestTime(float restDuration, float elapsed)
        {
            float remaining = restDuration - elapsed;
            return remaining < 0.0f ? 0.0f : remaining;
        }

    }
}
