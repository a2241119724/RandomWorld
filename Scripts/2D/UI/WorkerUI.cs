using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerUI : MonoBehaviour
    {
        void Update()
        {
            List<Worker> workers = WorkerManager.Instance.Characters;
            foreach(Worker worker in workers)
            {
                // ����ʱ��Լ���ֵ��ƣ��ֵ������Ȼ˥��
                worker.CurHungry -= Time.deltaTime * 0.1f;
                worker.CurTired -= Time.deltaTime * 0.01f;
            }
        }
    }
}
