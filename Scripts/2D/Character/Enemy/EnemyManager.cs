namespace LAB2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 敌人管理器
    /// </summary>
    public class EnemyManager : CharacterManager<EnemyManager, AEnemy, EnemyCreator>
    {
        private const float InstanceInterval = 60.0f; // 实例化时间间隔

        /// <summary>
        /// 敌人管理数据
        /// </summary>
        public EnemyManagerData EnemyManagerDataLAB { get; set; } = new ();

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
                this.Create();
                yield return new WaitForSeconds(InstanceInterval);
            }
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            AsyncProgressUI.Instance.SetTip("加载敌人管理信息...");
            this.EnemyManagerDataLAB = DataTool.LoadDataByBinary<EnemyManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            this.EnemyManagerDataLAB ??= new EnemyManagerData();
            this.EnemyManagerDataLAB.EnemyDatas ??= new List<AEnemy.EnemyData>();
            foreach (AEnemy.EnemyData enemyData in this.EnemyManagerDataLAB.EnemyDatas)
            {
                GameObject g = this.Create(Vector3LAB.ToVector3(enemyData.Pos));
                if (g == null)
                {
                    continue;
                }

                AEnemy enemy = g.GetComponent<AEnemy>();
                if (enemy == null)
                {
                    continue;
                }

                enemy.CharacterDataLAB = enemyData;
                enemy.CharacterDataLAB.Character = enemy;
            }

            TileMap.Instance.StartCoroutine(this.GenEnemy());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            this.EnemyManagerDataLAB.EnemyDatas = new ();
            foreach (AEnemy enemy in this.Characters)
            {
                if (enemy == null || enemy.CharacterDataLAB is not AEnemy.EnemyData enemyData)
                {
                    continue;
                }

                enemy.CharacterDataLAB.Pos = Vector3LAB.ToVector3LAB(enemy.transform.position);
                this.EnemyManagerDataLAB.EnemyDatas.Add(enemyData);
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.EnemyManagerDataLAB);
        }

        /// <summary>
        /// 敌人管理数据
        /// </summary>
        [Serializable]
        public class EnemyManagerData
        {
            /// <summary>
            /// 最大敌人数量
            /// </summary>
            public int MaxEnemyCount;

            /// <summary>
            /// 敌人数据
            /// </summary>
            public List<AEnemy.EnemyData> EnemyDatas;
        }
    }
}
