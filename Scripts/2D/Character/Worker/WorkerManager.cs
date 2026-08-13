namespace LAB2D.Character.Worker
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using System.Collections.Generic;

    /// <summary>
    /// Worker管理器
    /// </summary>
    public class WorkerManager : CharacterManager<WorkerManager, AWorker, WorkerCreator>
    {
        /// <summary>
        /// 用于多个Worker概率获取寻路锁
        /// </summary>
        private int countLock = 1;

        /// <inheritdoc/>
        public override void Add(AWorker character)
        {
            base.Add(character);
            Core.GameServices.LocateWorkerUIAddProvider(character);
            Core.ServiceLocator.Get<Gameplay.CurrencyManager>().InitializeWorkerWallet(character);

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
