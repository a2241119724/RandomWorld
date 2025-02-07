using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerManager : CharacterManager<WorkerManager,Worker,WorkerCreator>
    {
        /// <summary>
        /// ���ڸ��ʻ�ȡ��
        /// </summary>
        private int countLock = 1;

        public override void add(Worker character)
        {
            base.add(character);
            WorkerInfoUI.Instance.addWorkerItem(character);
        }

        /// <summary>
        /// ÿ�λ�ȡ-1����0���ٱ�ΪѰ·Worker������
        /// ����Worker�����ȡ��
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
