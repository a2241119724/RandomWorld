namespace LAB2D.UnityAdapter
{
    using System;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
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
            return Core.ServiceLocator.TryGet(out EnemyManager em) ? em.AliveEnemyCount : 0;
        }

        public int GetRuntimeMaxEnemyCount()
        {
            if (!Core.ServiceLocator.TryGet(out EnemyManager em) || em.EnemyManagerDataLAB.MaxEnemyCount <= 0)
            {
                return 0;
            }

            return em.EnemyManagerDataLAB.MaxEnemyCount;
        }

        public bool IsPlayerAlive()
        {
            Player player = Core.ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
            return player != null && player.CharacterDataLAB.Hp > 0;
        }

        public Vector3 GetSpawnPosition(bool useRandomSpawnPositions)
        {
            if (useRandomSpawnPositions && Core.ServiceLocator.TryGet(out TileMap tileMap))
            {
                try
                {
                    Vector3 centerMap = default;
                    Player player = Core.ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
                    if (player != null)
                    {
                        centerMap = tileMap.WorldPosToMapPos(player.transform.position);
                    }

                    return tileMap.MapPosToWorldPos(tileMap.GenCanReachPos(centerMap));
                }
                catch (Exception exception)
                {
                    AWorkerTask.LogProvider(
                        $"UnityWaveSceneAdapter.GetSpawnPosition failed, fallback to Vector3.zero.\n{exception}",
                        LogManager.LogLevelEnum.Error);
                }
            }

            return Vector3.zero;
        }

        public GameObject CreateEnemy(Vector3 spawnPosition)
        {
            return Core.ServiceLocator.TryGet(out EnemyManager em) ? em.Create(spawnPosition) : null;
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
            Core.ServiceLocator.Get<WaveBossRewardManager>().OnWaveStarted(waveIndex, difficultyScale);
        }

        public int GetEnemyCountForWave(int waveIndex, int baseEnemyCount)
        {
            return Core.ServiceLocator.Get<WaveBossRewardManager>().GetEnemyCountForWave(waveIndex, baseEnemyCount);
        }

        public void ConfigureSpawnedEnemy(GameObject enemyObject, WaveSpawnRequest spawnRequest)
        {
            if (enemyObject == null || spawnRequest == null)
            {
                return;
            }

            Core.ServiceLocator.Get<WaveBossRewardManager>().ConfigureSpawnedEnemy(
                enemyObject,
                spawnRequest.WaveIndex,
                spawnRequest.SpawnIndex,
                spawnRequest.TotalEnemiesInWave,
                spawnRequest.DifficultyScale);
        }

        public void SetWaveControlEnabled(bool enabled)
        {
            EnemyManager.IsWaveControlEnabled = enabled;
        }
    }
}
