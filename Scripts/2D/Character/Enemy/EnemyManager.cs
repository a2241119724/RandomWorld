namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 敌人管理器
    /// </summary>
    public class EnemyManager : CharacterManager<EnemyManager, Enemy, EnemyCreator>
    {
        /// <summary>
        /// 敌人管理数据
        /// </summary>
        public EnemyManagerData EnemyManagerDataLAB { get; set; } = new ();

        /// <inheritdoc/>
        public override void LoadData()
        {
            AsyncProgressUI.Instance.SetTip("加载敌人管理信息...");
            this.EnemyManagerDataLAB = DataTool.LoadDataByBinary<EnemyManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            foreach (Enemy.EnemyData enemyData in this.EnemyManagerDataLAB.EnemyDatas)
            {
                GameObject g = this.Create(Vector3LAB.ToVector3(enemyData.Pos));
                g.GetComponent<Enemy>().EnemyDataLAB = enemyData;
            }

            TileMap.Instance.StartCoroutine(this.creator.GenEnemy());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            this.EnemyManagerDataLAB.EnemyDatas = new ();
            foreach (Enemy enemy in this.Characters)
            {
                enemy.EnemyDataLAB.Pos = Vector3LAB.ToVector3LAB(enemy.transform.position);
                this.EnemyManagerDataLAB.EnemyDatas.Add(enemy.EnemyDataLAB);
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
            public List<Enemy.EnemyData> EnemyDatas;
        }
    }
}