namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// 基于 EnemyManager 的 IEnemySpawnService 的 Unity 实现。
    /// </summary>
    public sealed class UnityEnemySpawnAdapter : IEnemySpawnService
    {
        /// <inheritdoc/>
        public long CreateEnemy(GameGridPosition position)
        {
            if (!Core.ServiceLocator.TryGet(out EnemyManager em))
            {
                return 0;
            }

            Vector3 worldPos = UnityVectorAdapter.ToUnityVector3(
                new GameVector2(position.X, position.Y));
            GameObject enemyObj = em.Create(worldPos);
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
            if (!Core.ServiceLocator.TryGet(out EnemyManager em))
            {
                return 0;
            }

            return em.AliveEnemyCount;
        }
    }
}
