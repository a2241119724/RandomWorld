namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Unity implementation of IEnemySpawnService wrapping EnemyManager.
    /// </summary>
    public sealed class UnityEnemySpawnAdapter : IEnemySpawnService
    {
        /// <inheritdoc/>
        public long CreateEnemy(GameGridPosition position)
        {
            if (EnemyManager.Instance == null)
            {
                return 0;
            }

            Vector3 worldPos = UnityVectorAdapter.ToUnityVector3(
                new GameVector2(position.X, position.Y));
            GameObject enemyObj = EnemyManager.Instance.Create(worldPos);
            if (enemyObj == null)
            {
                return 0;
            }

            // Return the instance ID as a simple entity identifier
            return enemyObj.GetInstanceID();
        }

        /// <inheritdoc/>
        public int CountAliveEnemies()
        {
            if (EnemyManager.Instance == null)
            {
                return 0;
            }

            return EnemyManager.Instance.AliveEnemyCount;
        }
    }
}
