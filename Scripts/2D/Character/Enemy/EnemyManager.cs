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
            foreach (ACommonEnemy.EnemyData enemyData in this.EnemyManagerDataLAB.EnemyDatas)
            {
                GameObject g = this.Create(Vector3LAB.ToVector3(enemyData.Pos));
                g.GetComponent<ACommonEnemy>().CharacterDataLAB = enemyData;
            }

            TileMap.Instance.StartCoroutine(this.GenEnemy());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            this.EnemyManagerDataLAB.EnemyDatas = new ();
            foreach (ACommonEnemy enemy in this.Characters)
            {
                ACommonEnemy.EnemyData enemyData = enemy.CharacterDataLAB as ACommonEnemy.EnemyData;
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
            public List<ACommonEnemy.EnemyData> EnemyDatas;
        }
    }
}