using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class Lock
    {
        /// <summary>
        /// 拥有者
        /// </summary>
        public Worker Owner { get; set; }

        public bool getLock(Worker worker)
        {
            if(Owner == null)
            {
                // 第一次概率获取锁
                if (Random.Range(0.0f, 1.0f) > (1.0f / WorkerManager.Instance.getCountLock())) return false;
                Owner = worker;
                //LogManager.Instance.log(worker.name + "获取锁++++++", LogManager.LogLevel.Info);
                return true;
            }
            else if(Owner == worker)
            {
                return true;
            }
            return false;
        }

        public void releaseLock(Worker worker)
        {
            if(Owner == worker)
            {
                //LogManager.Instance.log(worker.name + "释放锁========", LogManager.LogLevel.Info);
                Owner = null;
            }
        }
    }
}
