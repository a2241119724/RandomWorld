using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class Lock
    {
        /// <summary>
        /// ӵ����
        /// </summary>
        public Worker Owner { get; set; }

        public bool getLock(Worker worker)
        {
            if(Owner == null)
            {
                // ��һ�θ��ʻ�ȡ��
                if (Random.Range(0.0f, 1.0f) > (1.0f / WorkerManager.Instance.getCountLock())) return false;
                Owner = worker;
                //LogManager.Instance.log(worker.name + "��ȡ��++++++", LogManager.LogLevel.Info);
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
                //LogManager.Instance.log(worker.name + "�ͷ���========", LogManager.LogLevel.Info);
                Owner = null;
            }
        }
    }
}
