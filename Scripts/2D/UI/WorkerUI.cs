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
                // 按照时间对饥饿值与疲劳值进行自然衰减
                worker.CurHungry -= Time.deltaTime * 0.1f;
                worker.CurTired -= Time.deltaTime * 0.01f;
            }
        }
    }
}
