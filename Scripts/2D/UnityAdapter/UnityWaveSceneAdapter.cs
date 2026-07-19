namespace LAB2D.UnityAdapter
{
    using System;
    using LAB2D;
    using LAB2D.Domain.Wave;
    using UnityEngine;

    /// <summary>
    /// Default Unity implementation of the wave scene bridge.
    /// It preserves the existing WaveManager runtime behavior while concentrating
    /// scene, prefab, player, and reward-manager access in one adapter.
    /// </summary>
    public sealed class UnityWaveSceneAdapter : IWaveSceneAdapter
    {
        public int CountAliveEnemies()
        {
            return EnemyManager.Instance == null ? 0 : EnemyManager.Instance.AliveEnemyCount;
        }

        public int GetRuntimeMaxEnemyCount()
        {
            if (EnemyManager.Instance == null || EnemyManager.Instance.EnemyManagerDataLAB.MaxEnemyCount <= 0)
            {
                return 0;
            }

            return EnemyManager.Instance.EnemyManagerDataLAB.MaxEnemyCount;
        }

        public bool IsPlayerAlive()
        {
            Player player = PlayerManager.Instance?.Mine;
            return player != null && player.CharacterDataLAB.Hp > 0;
        }

        public Vector3 GetSpawnPosition(bool useRandomSpawnPositions)
        {
            if (useRandomSpawnPositions && TileMap.Instance != null)
            {
                try
                {
                    Vector3 centerMap = default;
                    Player player = PlayerManager.Instance?.Mine;
                    if (player != null)
                    {
                        centerMap = TileMap.Instance.WorldPosToMapPos(player.transform.position);
                    }

                    return TileMap.Instance.MapPosToWorldPos(TileMap.Instance.GenCanReachPos(centerMap));
                }
                catch (Exception exception)
                {
                    LogManager.Instance.Log(
                        $"UnityWaveSceneAdapter.GetSpawnPosition failed, fallback to Vector3.zero.\n{exception}",
                        LogManager.LogLevelEnum.Error);
                }
            }

            return Vector3.zero;
        }

        public GameObject CreateEnemy(Vector3 spawnPosition)
        {
            return EnemyManager.Instance == null ? null : EnemyManager.Instance.Create(spawnPosition);
        }

        public bool TrySpawnEnemy(bool useRandomSpawnPositions, WaveSpawnRequest spawnRequest)
        {
            Vector3 spawnPosition = this.GetSpawnPosition(useRandomSpawnPositions);
            GameObject enemyObject = this.CreateEnemy(spawnPosition);
            if (enemyObject == null)
            {
                return false;
            }

            this.ConfigureSpawnedEnemy(enemyObject, spawnRequest);
            return true;
        }

        public void OnWaveStarted(int waveIndex, float difficultyScale)
        {
            WaveBossRewardManager.Instance.OnWaveStarted(waveIndex, difficultyScale);
        }

        public int GetEnemyCountForWave(int waveIndex, int baseEnemyCount)
        {
            return WaveBossRewardManager.Instance.GetEnemyCountForWave(waveIndex, baseEnemyCount);
        }

        public void ConfigureSpawnedEnemy(GameObject enemyObject, WaveSpawnRequest spawnRequest)
        {
            if (enemyObject == null || spawnRequest == null)
            {
                return;
            }

            WaveBossRewardManager.Instance.ConfigureSpawnedEnemy(
                enemyObject,
                spawnRequest.WaveIndex,
                spawnRequest.SpawnIndex,
                spawnRequest.TotalEnemiesInWave,
                spawnRequest.DifficultyScale);
        }
    }
}
