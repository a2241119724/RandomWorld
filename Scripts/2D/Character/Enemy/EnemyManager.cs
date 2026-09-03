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
        private const float InstanceInterval = 10.0f; // 实例化时间间隔

        /// <summary>FindObjectsOfType 全量补漏的最小间隔（秒）。Create 管线必经 Add，补漏纯防御性，可降频。</summary>
        private const float FullSyncInterval = 10.0f;

        /// <summary>敌人空间网格 cell 尺寸 = 最大查询半径（WorkerDefendTask.DefendSightRange=8），使桶覆盖恒 3×3。</summary>
        public const float EnemyGridCellSize = 8f;

        /// <summary>
        /// 波次控制标志：true 时由 WaveManager 接管敌人生成，GenEnemy 协程不再自动生成
        /// </summary>
        public static bool IsWaveControlEnabled = false;

        /// <summary>
        /// 敌人管理数据
        /// </summary>
        public EnemyManagerData EnemyManagerDataLAB { get; set; } = new ();

        /// <summary>存活敌人空间网格（索敌查询索引；随 Characters 惰性重建，仅主线程）。</summary>
        public Domain.Common.SpatialGrid<AEnemy> EnemyGrid { get; } = new (EnemyGridCellSize);

        /// <summary>网格上次重建的帧号（同帧多次查询只重建一次）。</summary>
        private int gridRebuildFrame = -1;

        /// <summary>FindObjectsOfType 补漏上次执行时间（Time.time）。</summary>
        private float lastFullSyncTime = float.MinValue;

        /// <summary>
        /// 惰性重建敌人空间网格：当前帧首次查询时全量重建（O(敌人数)，≤548 微秒级）。
        /// 网格是重建时刻的快照，消费方查询时须带实时判活 filter（见 SkillTool）。
        /// </summary>
        public void EnsureEnemyGridRebuilt()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == this.gridRebuildFrame)
            {
                return;
            }

            this.gridRebuildFrame = frame;
            this.EnemyGrid.BeginRebuild();
            foreach (AEnemy enemy in this.Characters)
            {
                if (!IsAliveEnemy(enemy))
                {
                    continue;
                }

                Vector3 p = enemy.transform.position;
                this.EnemyGrid.Add(new Domain.Common.GameVector2(p.x, p.y), enemy);
            }
        }

        /// <summary>
        /// 当前真实存活敌人数量。
        /// </summary>
        public int AliveEnemyCount
        {
            get
            {
                this.PruneDeadEnemies();
                this.MaybeFullSync();
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

            GameObject g = base.Create(worldPos);
            if (g != null)
            {
                // 敌人生成事件（一次性）
                AWorkerTask.LogProvider(
                    $"[EnemyDiag] 生成敌人 {g.name} alive={this.Characters.Count} pos=({worldPos.x:F1},{worldPos.y:F1})",
                    LogManager.LogLevelEnum.Debug);
            }

            return g;
        }

        /// <summary>
        /// 按波次扩种协议创建指定种类的敌人，并把种类写入存档数据（防读档换种）。
        /// </summary>
        /// <param name="worldPos">生成位置。</param>
        /// <param name="enemyKindId">敌人种类 Id（WaveEnemyKind）。</param>
        /// <returns>实例化对象；达到上限或创建失败返回 null。</returns>
        public GameObject Create(Vector3 worldPos, int enemyKindId)
        {
            if (!this.CanCreateEnemy())
            {
                return null;
            }

            this.creator.SetNextEnemyKindId(enemyKindId);
            GameObject g;
            try
            {
                g = this.Create(worldPos);
            }
            finally
            {
                // 清残留：无论成败都不让指定种类泄漏进后续的默认随机生成
                this.creator.SetNextEnemyKindId(-1);
            }

            if (g != null && g.GetComponent<AEnemy>()?.CharacterDataLAB is AEnemy.EnemyData enemyData)
            {
                enemyData.EnemyKindId = enemyKindId;
            }

            return g;
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
            Core.GameServices.AsyncProgressSetTipProvider("加载敌人管理信息...");
            this.EnemyManagerDataLAB = DataTool.LoadDataByBinary<EnemyManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            this.EnemyManagerDataLAB ??= new EnemyManagerData();
            this.EnemyManagerDataLAB.EnemyDatas ??= new List<AEnemy.EnemyData>();
            foreach (AEnemy.EnemyData enemyData in this.EnemyManagerDataLAB.EnemyDatas)
            {
                // 按存档种类选 prefab（防读档换种）；随后 CharacterDataLAB 整体替换为存档数据
                GameObject g = this.Create(Vector3LAB.ToVector3(enemyData.Pos), enemyData.EnemyKindId);
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

        /// <summary>
        /// 全量同步：清死 + 无条件 FindObjectsOfType 补漏。仅供存档等必须完整列表的场合。
        /// </summary>
        private void SyncAliveEnemies()
        {
            this.PruneDeadEnemies();
            this.FullSyncFromScene();
        }

        /// <summary>清死：移除已死亡/已销毁（null）条目。O(敌人数)，计数准确性的唯一依赖。</summary>
        private void PruneDeadEnemies()
        {
            this.Characters.RemoveAll(enemy => !IsAliveEnemy(enemy));
        }

        /// <summary>降频补漏：FindObjectsOfType 全场景扫描兜底找回绕过 Create 管线的敌人（理论不存在，防御性）。</summary>
        private void MaybeFullSync()
        {
            if (UnityEngine.Time.time - this.lastFullSyncTime < FullSyncInterval)
            {
                return;
            }

            this.lastFullSyncTime = UnityEngine.Time.time;
            this.FullSyncFromScene();
        }

        private void FullSyncFromScene()
        {
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
