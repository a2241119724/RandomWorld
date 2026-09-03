namespace LAB2D.Character.Worker
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker管理器
    /// </summary>
    public class WorkerManager : CharacterManager<WorkerManager, AWorker, WorkerCreator>
    {
        /// <summary>Worker 空间网格 cell 尺寸（与 EnemyManager 一致，好感通知等邻近查询用）。</summary>
        public const float WorkerGridCellSize = 8f;

        /// <summary>
        /// 用于多个Worker概率获取寻路锁
        /// </summary>
        private int countLock = 1;

        /// <summary>Worker 空间网格（邻近查询索引；随 Characters 惰性重建，仅主线程）。</summary>
        public SpatialGrid<AWorker> WorkerGrid { get; } = new (WorkerGridCellSize);

        /// <summary>网格上次重建的帧号（同帧多次查询只重建一次）。</summary>
        private int gridRebuildFrame = -1;

        /// <summary>
        /// 惰性重建 Worker 空间网格：当前帧首次查询时全量重建。
        /// 与既有消费方（FavorabilityManager.NotifyPlayerHelpsNearby）语义一致：只过滤 null，不判 Hp。
        /// </summary>
        public void EnsureWorkerGridRebuilt()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == this.gridRebuildFrame)
            {
                return;
            }

            this.gridRebuildFrame = frame;
            this.WorkerGrid.BeginRebuild();
            foreach (AWorker worker in this.Characters)
            {
                if (worker == null)
                {
                    continue;
                }

                Vector3 p = worker.transform.position;
                this.WorkerGrid.Add(new GameVector2(p.x, p.y), worker);
            }
        }

        /// <inheritdoc/>
        public override void Add(AWorker character)
        {
            base.Add(character);
            Core.GameServices.LocateWorkerUIAddProvider(character);
            Core.ServiceLocator.Get<Gameplay.CurrencyManager>().InitializeWorkerWallet(character);
            Core.ServiceLocator.Get<Gameplay.FavorabilityManager>().InitializeWorkerFavorability(character);

            // [TaskDiag] 记录 Worker 生成（任务执行者生命周期起点）
            AWorkerTask.LogProvider(
                $"[TaskDiag] Worker 生成 {character.name}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 每次获取-1，到0后再变为寻路Worker的数量
        /// 用于Worker随机获取寻路锁
        /// </summary>
        /// <returns>当前寻路Worker的数量</returns>
        public int GetCountLock()
        {
            if (this.countLock == 1)
            {
                // 初始时或只有一个Worker在寻路时, 获取寻路Worker的数量
                foreach (AWorker worker in this.Characters)
                {
                    if (worker.Manager.CurrentStateType == AWorkerState.TypeEnum.Seek)
                    {
                        this.countLock++;
                    }
                }

                return this.countLock;
            }
            else
            {
                return --this.countLock;
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            // 存档前：将携带资源从 MonoBehaviour 同步到 WorkerData
            foreach (AWorker worker in this.Characters)
            {
                if (worker == null) continue;
                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd != null)
                {
                    Dictionary<int, ResourceInfo> carried = worker.GetCarriedResources();
                    wd.CarriedResources = carried != null && carried.Count > 0
                        ? new Dictionary<int, ResourceInfo>(carried)
                        : null;
                }
            }

            base.SaveData();
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();

            // 读档后：将携带资源从 WorkerData 恢复到 MonoBehaviour
            foreach (AWorker worker in this.Characters)
            {
                if (worker == null) continue;
                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd?.CarriedResources != null && wd.CarriedResources.Count > 0)
                {
                    worker.RestoreCarriedResources(new Dictionary<int, ResourceInfo>(wd.CarriedResources));
                }
            }
        }
    }
}
