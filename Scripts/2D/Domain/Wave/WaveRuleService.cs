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
            float scale = 1.0f + (MathHelper.ClampMin(totalWavesCompleted, 0) * safeConfig.DifficultyScalePerWave);
            if (safeConfig.IsBloodMoon)
            {
                scale += 0.5f;
            }

            // 每局修饰符·敌方强度：与血月正交叠乘（血月 +0.5 之后再整体缩放）
            return scale * System.Math.Max(0f, safeConfig.EnemyStrengthMultiplier);
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
            if (safeConfig.IsBloodMoon)
            {
                // 血月加成：数量 ×1.5（向上取整，保底不变）
                count = (int)System.Math.Ceiling(count * 1.5f);
            }

            // 每局修饰符·敌方强度：与血月正交叠乘（向上取整，保底 1 条）
            count = (int)System.Math.Ceiling(count * System.Math.Max(0f, safeConfig.EnemyStrengthMultiplier));
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
        /// 波次达到 <see cref="WaveConfigModel.NewEnemyStartWave"/> 前只用旧池，之后混入新种；
        /// 血月夜混池门槛提前 1 波（妖兽倾巢）。
        /// </summary>
        /// <param name="waveIndex">波次号（1 起）。</param>
        /// <param name="spawnIndex">波内生成序号（0 起）。</param>
        /// <param name="config">波次配置。</param>
        /// <returns>该次生成应使用的敌人种类。</returns>
        public WaveEnemyKind PickEnemyKind(int waveIndex, int spawnIndex, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            int normalizedWaveIndex = MathHelper.ClampMin(waveIndex, 1);
            int startWave = MathHelper.ClampMin(safeConfig.NewEnemyStartWave, 1);
            if (safeConfig.IsBloodMoon)
            {
                startWave = MathHelper.ClampMin(startWave - 1, 1);
            }

            WaveEnemyKind[] cycle = normalizedWaveIndex >= startWave
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
