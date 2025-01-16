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
        /// ����������pos�ҹ���TODO��worker�����ҹ�
        /// </summary>
        private List<HungryTask> hungrys;

        public WorkerTaskManager() {
            tasks = new Dictionary<WorkerTask, bool>();
            hungrys = new List<HungryTask>();
        }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Worker��ȡ����LAB_TODO���ȼ�û��ʵ��
        /// </summary>
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
                    // ����ֵС��һ��ֵֻ���Խ��ռ�������
                    if (worker.CurHungry < Worker.ThresholdHungry && task.TaskType != TaskType.Hungry) continue;
                    // ���û���������ܼ�������
                    if (worker.CurHungry >= Worker.ThresholdHungry && task.TaskType == TaskType.Hungry) continue;
                    // �������Ƿ����ڱ���
                    if (tasks[task]) continue;
                    // �Ƿ�����������Ļ�������
                    if (!task.isCanWork(worker)) continue;
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
                // �������
                if (closedTask != null)
                {
                    // ����������
                    worker.Manager.Task = closedTask;
                    closedTask.start(worker);
                    // ͬһ���������񻹿��Լ�����
                    if (closedTask.TaskType != TaskType.Hungry)
                    {
                        tasks[closedTask] = true;
                    }
                }
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="task"></param>
        public void addTask(WorkerTask task) {
            if (task == null) return;
            // ����Ǽ�������,һ��λ�ý���Ӧһ������
            if(task.TaskType == TaskType.Hungry)
            {
                foreach (HungryTask hungryTask in hungrys)
                {
                    if (hungryTask.TargetMap.x == task.TargetMap.x && hungryTask.TargetMap.y == task.TargetMap.y) {
                        return;
                    }
                }
                hungrys.Add((HungryTask)task);
            }
            tasks.Add(task, false);
            //DebugUI.Instance.updateTaskInfo(getTaskInfo());
        }

        public void completeTask(WorkerTask task)
        {
            // ����ɾ������������Ҫ��deleteHungryTask��ɾ��
            if (task.TaskType != TaskType.Hungry)
            {
                tasks.Remove(task);
            }
            //DebugUI.Instance.updateTaskInfo(getTaskInfo());
        }

        public void giveUpTask(WorkerTask task) {
            if (task == null) return;
            tasks[task] = false;
        }

        public string getTaskInfo()
        {
            int count = 0;
            foreach (WorkerTask task in tasks.Keys)
            {
                if (tasks[task])
                {
                    count++;
                }
            }
            return $"����������: {tasks.Count}\n" +
                $"����������������: {count}";
        }

        public void deleteHungryTask(Vector3Int pos) {
            foreach (HungryTask hungryTask in hungrys)
            {
                if (hungryTask.TargetMap.x == pos.x && hungryTask.TargetMap.y == pos.y)
                {
                    tasks.Remove(hungryTask);
                    hungrys.Remove(hungryTask);
                    return;
                }
            }
        }
    }
}
