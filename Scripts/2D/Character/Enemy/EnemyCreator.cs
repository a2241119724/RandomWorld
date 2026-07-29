namespace LAB2D.Character.Enemy
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 敌人创建器
    /// </summary>
    public class EnemyCreator : CharacterCreator<EnemyCreator>
    {
        private string[] enemyNames = new string[] { "CommonEnemy_Lv1", "SeekEnemy_Lv1" };

        /// <summary>
        /// 实例化在玩家附近的敌人
        /// </summary>
        /// <param name="worldPos">世界位置</param>
        /// <param name="name">名字</param>
        /// <param name="layer">层级</param>
        /// <returns>实例化对象</returns>
        protected override GameObject DoCreate(Vector3 worldPos, string name, string layer)
        {
            if (!Core.GameServices.EnemyCanCreateProvider())
            {
                return null;
            }

            // 随机实例化敌人
            return base.DoCreate(worldPos, this.enemyNames[Random.Range(0, this.enemyNames.Length)], LayerConstant.ENEMY_LAYER);
        }
    }
}
