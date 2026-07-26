namespace LAB2D.Core
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using System;

    /// <summary>
    /// 锁 — 工人任务互斥锁。
    /// IsCompleteTileMap 已迁移至 MapInitCoordinator。
    /// </summary>
    public class Lock
    {
        private readonly System.Random random = new System.Random();
        /// <summary>
        /// 拥有者
        /// </summary>
        public AWorker Owner { get; set; }

        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="worker">获取锁的Worker</param>
        /// <returns>是否成功</returns>
        public bool GetLock(AWorker worker)
        {
            if (this.Owner == null)
            {
                // 第一次概率获取锁
                if (this.random.NextDouble() > (1.0 / ServiceLocator.Get<WorkerManager>().GetCountLock()))
                {
                    return false;
                }

                this.Owner = worker;

                // LogManager.Instance.log(worker.name + "获取锁++++++", LogManager.LogLevel.Info);
                return true;
            }
            else if (this.Owner == worker)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="worker">释放锁的Worker</param>
        public void ReleaseLock(AWorker worker)
        {
            if (this.Owner == worker)
            {
                // LogManager.Instance.log(worker.name + "释放锁========", LogManager.LogLevel.Info);
                this.Owner = null;
            }
        }
    }
}
