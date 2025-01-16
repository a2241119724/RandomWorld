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
                worker.CurHungry -= Time.deltaTime * 0.1f;
            }
        }
    }
}
