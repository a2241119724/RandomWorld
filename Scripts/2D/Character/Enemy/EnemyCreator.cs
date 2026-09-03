namespace LAB2D.Character.Enemy
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Domain.Wave;
    using UnityEngine;

    /// <summary>
    /// 敌人创建器
    /// </summary>
    public class EnemyCreator : CharacterCreator<EnemyCreator>
    {
        private string[] enemyNames = new string[] { PrefabConstant.COMMON_ENEMY, PrefabConstant.SEEK_ENEMY };

        /// <summary>
        /// 待消费的波次扩种种类 Id（-1=无指定，走旧随机池）。
        /// 由 EnemyManager.Create(worldPos, kindId) 设置，DoCreate 消费后即清。
        /// </summary>
        private int pendingEnemyKindId = -1;

        /// <summary>
        /// 敌人种类 Id → prefab 名映射（WaveEnemyKind 协议的 Unity 落点）。
        /// </summary>
        public static string GetPrefabNameForKind(int enemyKindId)
        {
            return enemyKindId switch
            {
                (int)WaveEnemyKind.Seek => PrefabConstant.SEEK_ENEMY,
                (int)WaveEnemyKind.Charge => PrefabConstant.CHARGE_ENEMY,
                (int)WaveEnemyKind.Shoot => PrefabConstant.SHOOT_ENEMY,
                _ => PrefabConstant.COMMON_ENEMY,
            };
        }

        /// <summary>
        /// 指定下一次生成的敌人种类（波次扩种协议入口；只影响紧随其后的一次生成）。
        /// </summary>
        public void SetNextEnemyKindId(int enemyKindId)
        {
            this.pendingEnemyKindId = enemyKindId;
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
            if (!Core.GameServices.EnemyCanCreateProvider())
            {
                this.pendingEnemyKindId = -1;
                return null;
            }

            // 波次扩种：有指定种类走映射；无指定（GenEnemy 固定间隔协程）保留旧随机池
            string prefabName = this.pendingEnemyKindId >= 0
                ? GetPrefabNameForKind(this.pendingEnemyKindId)
                : this.enemyNames[Random.Range(0, this.enemyNames.Length)];
            this.pendingEnemyKindId = -1;

            return base.DoCreate(worldPos, prefabName, LayerConstant.ENEMY_LAYER);
        }
    }
}
