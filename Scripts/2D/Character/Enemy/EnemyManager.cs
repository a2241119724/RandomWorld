namespace LAB2D.Character.Enemy
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Serializable;
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
        /// 波次控制标志：true 时由 WaveManager 接管敌人生成，GenEnemy 协程不再自动生成
        /// </summary>
        public static bool IsWaveControlEnabled = false;

        /// <summary>
        /// 敌人管理数据
        /// </summary>
        public EnemyManagerData EnemyManagerDataLAB { get; set; } = new ();

        /// <summary>
        /// 当前真实存活敌人数量。
        /// </summary>
        public int AliveEnemyCount
        {
            get
            {
                this.SyncAliveEnemies();
                return this.Characters.Count;
            }
        }

        /// <summary>
        /// 当前是否还能生成敌人。
        /// </summary>
        public bool CanCreateEnemy()
        {
            return this.EnemyManagerDataLAB.MaxEnemyCount > 0
                && this.AliveEnemyCount < this.EnemyManagerDataLAB.MaxEnemyCount;
        }

        /// <summary>
        /// 每隔一段时间生成敌人
        /// </summary>
        /// <returns>协程</returns>
        public IEnumerator GenEnemy()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete);
            while (true)
            {
                if (!IsWaveControlEnabled)
                {
                    this.Create();
                }

                yield return new WaitForSeconds(InstanceInterval);
            }
        }

        /// <inheritdoc/>
        public override GameObject Create(Vector3 worldPos = default)
        {
            if (!this.CanCreateEnemy())
            {
                return null;
            }

            return base.Create(worldPos);
        }

        /// <inheritdoc/>
        public override void Add(AEnemy character)
        {
            if (character == null)
            {
                AWorkerTask.LogProvider("character is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            if (!this.Characters.Contains(character))
            {
                this.Characters.Add(character);
            }
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            AWorkerTask.AsyncProgressSetTipProvider("加载敌人管理信息...");
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

            ServiceLocator.Get<TileMap>().StartCoroutine(this.GenEnemy());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            this.SyncAliveEnemies();
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

        private void SyncAliveEnemies()
        {
            this.Characters.RemoveAll(enemy => !IsAliveEnemy(enemy));
            foreach (AEnemy enemy in UnityEngine.Object.FindObjectsOfType<AEnemy>())
            {
                if (IsAliveEnemy(enemy) && !this.Characters.Contains(enemy))
                {
                    this.Characters.Add(enemy);
                }
            }
        }

        private static bool IsAliveEnemy(AEnemy enemy)
        {
            return enemy != null
                && enemy.CharacterDataLAB != null
                && enemy.CharacterDataLAB.Hp > 0;
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
