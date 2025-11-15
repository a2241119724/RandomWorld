namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 锁
    /// </summary>
    public class Lock
    {
        /// <summary>
        /// 等待地图执行完，开启其他协程
        /// </summary>
        public static bool IsCompleteTileMap = false;

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
                if (Random.Range(0.0f, 1.0f) > (1.0f / WorkerManager.Instance.GetCountLock()))
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
