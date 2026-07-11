namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 基于 EnemyManager 的 IEnemySpawnService 的 Unity 实现。
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

            // 返回实例 ID 作为简单的实体标识符
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
