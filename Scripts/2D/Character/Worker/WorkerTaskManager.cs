using System.Collections;
using System.Collections.Generic;
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
        private List<WorkerHungryTask> hungrys;

        public WorkerTaskManager() {
            tasks = new List<Dictionary<WorkerTask, bool>>();
            for(int i = 0; i < 4; i++)
            {
                tasks.Add(new Dictionary<WorkerTask, bool>());
            }
            hungrys = new List<WorkerHungryTask>();
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
            if(task.TaskType == TaskType.Hungry)
            {
                foreach (WorkerHungryTask hungryTask in hungrys)
                {
                    if (hungryTask.TargetMap.x == task.TargetMap.x && hungryTask.TargetMap.y == task.TargetMap.y) {
                        return;
                    }
                }
                hungrys.Add((WorkerHungryTask)task);
            }
            else if(task.TaskType == TaskType.Gather)
            {
                GatherPos.Add(task.TargetMap);
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
                foreach(Dictionary<WorkerTask,bool> _task in tasks)
                {
                    _task.Remove(task);
                    break;
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
            foreach (Dictionary<WorkerTask, bool> _task in tasks)
            {
                _task[task] = false;
                break;
            }
        }

        public string getTaskInfo()
        {
            int count = 0;
            int total = 0;
            foreach (Dictionary<WorkerTask, bool> task in tasks)
            {
                total += task.Count;
                foreach (WorkerTask _task in task.Keys)
                {
                    if (task[_task])
                    {
                        count++;
                    }
                }
            }
            return $"����������: {total}\n" +
                $"����������������: {count}";
        }

        /// <summary>
        /// ɾ���Է�����
        /// ��λ�����ڲֿ��е�ʳ�ﱻ��������
        /// </summary>
        /// <param name="pos"></param>
        public void deleteHungryTask(Vector3Int pos) {
            foreach (WorkerHungryTask hungryTask in hungrys)
            {
                if (hungryTask.TargetMap.x == pos.x && hungryTask.TargetMap.y == pos.y)
                {
                    foreach (Dictionary<WorkerTask, bool> _task in tasks)
                    {
                        _task.Remove(hungryTask);
                        hungrys.Remove(hungryTask);
                        return;
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
            foreach (Dictionary<WorkerTask, bool> task in tasks)
            {
                foreach (WorkerTask _task in task.Keys)
                {
                    if(_task.TaskType == TaskType.Gather && _task.TargetMap.x == posMap.x 
                        && _task.TargetMap.y == posMap.y) {
                        task.Remove(_task);
                        GatherPos.Remove(_task.TargetMap);
                        return;
                    }
                }
            }
        }
    }
}
