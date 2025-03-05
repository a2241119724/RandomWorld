using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LAB2D {
    public class WorkerTaskManager : MonoBehaviour
    {
        public static WorkerTaskManager Instance { get; private set; }
        /// <summary>
        /// ��¼���в�ժ�����λ��
        /// </summary>
        public List<Vector3Int> GatherPos { get; private set; }

        /// <summary>
        /// ��������(list��Խ��ǰ���ȼ�Խ��)
        /// </summary>
        private List<Dictionary<WorkerTask,bool>> tasks;
        /// <summary>
        /// ����������pos�ҹ���TODO��worker�����ҹ�
        /// </summary>
        private List<WorkerHungryTask> hungryTasks;
        private List<WorkerWearTask> wearTasks;

        public WorkerTaskManager() {
            tasks = new List<Dictionary<WorkerTask, bool>>();
            for(int i = 0; i < 4; i++)
            {
                tasks.Add(new Dictionary<WorkerTask, bool>());
            }
            hungryTasks = new List<WorkerHungryTask>();
            wearTasks = new List<WorkerWearTask>();
            GatherPos = new List<Vector3Int>();
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
                foreach (Dictionary<WorkerTask, bool> task in tasks)
                {
                    WorkerTask closedTask = null;
                    float minDistance = 999999.0f;
                    foreach (WorkerTask _task in task.Keys)
                    {
                        // �������Ƿ����ڱ���
                        if (task[_task]) continue;
                        // �Ƿ�����������Ļ�������
                        if (!_task.isCanWork(worker)) continue;
                        if (closedTask == null)
                        {
                            minDistance = Mathf.Pow(worker.transform.position.y - _task.TargetMap.x, 2) +
                                Mathf.Pow(worker.transform.position.x - _task.TargetMap.y, 2);
                            closedTask = _task;
                        }
                        else
                        {
                            float distance = Mathf.Pow(worker.transform.position.y - _task.TargetMap.x, 2) +
                                Mathf.Pow(worker.transform.position.x - _task.TargetMap.y, 2);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closedTask = _task;
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
                            task[closedTask] = true;
                        }
                        DebugUI.Instance.updateTaskInfo(getTaskInfo());
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="task"></param>
        public void addTask(WorkerTask task, int prior = 2) {
            if (task == null) return;
            // ����Ǽ�������,һ��λ�ý���Ӧһ������
            if (task.TaskType == TaskType.Hungry)
            {
                foreach (WorkerHungryTask hungryTask in hungryTasks)
                {
                    if (hungryTask.TargetMap.x == task.TargetMap.x && hungryTask.TargetMap.y == task.TargetMap.y)
                    {
                        return;
                    }
                }
                hungryTasks.Add((WorkerHungryTask)task);
            }
            else if (task.TaskType == TaskType.Gather)
            {
                GatherPos.Add(task.TargetMap);
            }
            else if (task.TaskType == TaskType.Wear)
            {
                // һ��λ��ֻ����һ����������
                foreach (WorkerTask wearTask in wearTasks)
                {
                    if (wearTask.TargetMap.x == task.TargetMap.x
                        && wearTask.TargetMap.y == task.TargetMap.y)
                    {
                        return;
                    }
                }
                wearTasks.Add((WorkerWearTask)task);
            }
            tasks[prior].Add(task, false);
            DebugUI.Instance.updateTaskInfo(getTaskInfo());
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="task"></param>
        public void completeTask(WorkerTask task)
        {
            // ����ɾ������������Ҫ��deleteHungryTask��ɾ��
            if (task.TaskType != TaskType.Hungry)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].ContainsKey(task))
                    {
                        tasks[i].Remove(task);
                        DebugUI.Instance.updateTaskInfo(getTaskInfo());
                        break;
                    }
                }
            }
            // �Ǽ������������Ϊ���ٴν���״̬����false
            else
            {
                tasks[1][task] = false;
            }
            DebugUI.Instance.updateTaskInfo(getTaskInfo());
        }

        public void giveUpTask(WorkerTask task) {
            if (task == null) return;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].ContainsKey(task))
                {
                    tasks[i].Remove(task);
                    DebugUI.Instance.updateTaskInfo(getTaskInfo());
                    break;
                }
            }
        }

        public string getTaskInfo()
        {
            int total = 0;
            int[] taskCount = new int[10];
            foreach (Dictionary<WorkerTask, bool> task in tasks)
            {
                total += task.Count;
                foreach (WorkerTask _task in task.Keys)
                {
                    if (task[_task])
                    {
                        taskCount[(int)_task.TaskType]++;
                    }
                }
            }
            string res = $"����������: {total}\n";
            for(int i = 0; i < 10; i++)
            {
                res += $"{((TaskType)i).ToString()}:{taskCount[i]}\n";
            }
            return res;
        }

        /// <summary>
        /// ɾ���Է�����
        /// ��λ�����ڲֿ��е�ʳ�ﱻ��������
        /// </summary>
        /// <param name="pos"></param>
        public void deleteHungryTask(Vector3Int pos) {
            foreach (WorkerHungryTask hungryTask in hungryTasks)
            {
                if (hungryTask.TargetMap.x == pos.x && hungryTask.TargetMap.y == pos.y)
                {
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        if (tasks[i].ContainsKey(hungryTask))
                        {
                            tasks[i].Remove(hungryTask);
                            hungryTasks.Remove(hungryTask);
                            DebugUI.Instance.updateTaskInfo(getTaskInfo());
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ȡ���ɼ�����
        /// </summary>
        /// <param name="posMap"></param>
        public void cancelGatherTask(Vector3Int posMap)
        {
            if (!GatherPos.Contains(posMap)) return;
            for (int i = 0; i < tasks.Count; i++)
            {
                foreach (WorkerTask _task in tasks[i].Keys)
                {
                    if (_task.TaskType == TaskType.Gather && _task.TargetMap.x == posMap.x
                        && _task.TargetMap.y == posMap.y)
                    {
                        tasks[i].Remove(_task);
                        GatherPos.Remove(_task.TargetMap);
                        DebugUI.Instance.updateTaskInfo(getTaskInfo());
                        return;
                    }
                }
            }
        }
    }
}
