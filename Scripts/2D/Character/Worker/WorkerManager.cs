using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerManager : CharacterManager<WorkerManager,Worker,WorkerCreator>
    {
        /// <summary>
        /// 用于概率获取锁
        /// </summary>
        private int countLock = 1;

        public override void add(Worker character)
        {
            base.add(character);
            WorkerInfoUI.Instance.addWorkerItem(character);
        }

        /// <summary>
        /// 每次获取-1，到0后再变为寻路Worker的数量
        /// 用于Worker随机获取锁
        /// </summary>
        /// <returns></returns>
        public int getCountLock() { 
            if(countLock == 1)
            {
                foreach(Worker worker in Characters)
                {
                    if(worker.Manager.CurrentStateType == WorkerStateType.Seek)
                    {
                        countLock++;
                    }
                }
                return countLock;
            }
            else
            {
                return --countLock;
            }
        }
    }
}
