namespace LAB2D
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// 敌人创建器
    /// </summary>
    public class EnemyCreator : CharacterCreator<EnemyCreator>
    {
        private const float InstanceInterval = 3.0f; // 实例化时间间隔

        /// <summary>
        /// 每隔一段时间生成敌人
        /// </summary>
        /// <returns>协程</returns>
        public IEnumerator GenEnemy()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Lock.IsCompleteTileMap);
            while (true)
            {
                EnemyManager.Instance.Create();
                yield return new WaitForSeconds(InstanceInterval);
            }
        }

        /// <summary>
        /// 实例化在玩家附近的敌人
        /// </summary>
        /// <param name="worldPos">世界位置</param>
        /// <param name="name">名字</param>
        /// <param name="layer">层级</param>
        /// <returns>实例化对象</returns>
        protected override GameObject DoCreate(Vector3 worldPos, string name, string layer)
        {
            if (EnemyManager.Instance.Characters.Count >= EnemyManager.Instance.EnemyManagerDataLAB.MaxEnemyCount)
            {
                return null;
            }

            return base.DoCreate(worldPos, "Enemy_Lv1", "Enemy");
        }
    }
}
