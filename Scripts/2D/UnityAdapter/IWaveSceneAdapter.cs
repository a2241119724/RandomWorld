namespace LAB2D.UnityAdapter
{
    using LAB2D.Domain.Wave;
    using UnityEngine;

    /// <summary>
    /// WaveManager needs Unity scene capabilities, but does not need to know which
    /// Singleton provides them. This adapter keeps wave flow logic away from
    /// EnemyManager, TileMap, PlayerManager, and reward presentation details.
    /// </summary>
    public interface IWaveSceneAdapter
    {
        int CountAliveEnemies();

        int GetRuntimeMaxEnemyCount();

        bool IsPlayerAlive();

        Vector3 GetSpawnPosition(bool useRandomSpawnPositions);

        GameObject CreateEnemy(Vector3 spawnPosition);

        bool TrySpawnEnemy(bool useRandomSpawnPositions, WaveSpawnRequest spawnRequest);

        void OnWaveStarted(int waveIndex, float difficultyScale);

        int GetEnemyCountForWave(int waveIndex, int baseEnemyCount);

        void ConfigureSpawnedEnemy(GameObject enemyObject, WaveSpawnRequest spawnRequest);

        void SetWaveControlEnabled(bool enabled);
    }
}
