using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerTaskManager : MonoBehaviour
    {
        public static WorkerTaskManager Instance { get; private set; }

        /// <summary>
        /// ��������
        /// </summary>
        private Dictionary<WorkerTask,bool> tasks;
        /// <summary>
        /// ��ֹ�ڱ�����ʱ��������ݱ���
        /// </summary>
        private WorkerTask _task;

        public WorkerTaskManager() {
            tasks = new Dictionary<WorkerTask, bool>();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            List<Worker> workers = WorkerManager.Instance.Characters;
            foreach (Worker worker in workers)
            {
                if (worker.Manager.Task != null) continue;
                WorkerTask closedTask = null;
                float minDistance = 999999.0f;
                foreach (WorkerTask task in tasks.Keys)
                {
                    if (tasks[task]) continue;
                    if (closedTask == null)
                    {
                        minDistance = Mathf.Pow(worker.transform.position.y - task.TargetMap.x, 2) +
                            Mathf.Pow(worker.transform.position.x - task.TargetMap.y, 2);
                        closedTask = task;
                    }
                    else
                    {
                        float distance = Mathf.Pow(worker.transform.position.y - task.TargetMap.x, 2) +
                            Mathf.Pow(worker.transform.position.x - task.TargetMap.y, 2);
                        if(distance < minDistance)
                        {
                            minDistance = distance;
                            closedTask = task;
                        }
                    }        
                }
                // ����������
                worker.Manager.Task = closedTask;
                // ���빤��״̬
                worker.Manager.changeState(WorkerStateType.Seek);
                tasks[closedTask] = true;
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="task"></param>
        public void addTask(WorkerTask task) {
            tasks.Add(task,false);
        }

        public void completeTask(WorkerTask task)
        {
            tasks.Remove(task);
        }

        public string getTaskInfo()
        {
            int count = 0;
            string pos_s = "";
            foreach (WorkerTask task in tasks.Keys)
            {
                if (tasks[task])
                {
                    count++;
                }
                pos_s += task.TargetMap + ":" + tasks[task] + "\n";
            }
            return $"����������: {tasks.Count}\n" +
                $"����������������: {count}\n" +
                pos_s;
        }
    }
}
