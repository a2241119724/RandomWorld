using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerTaskManager : MonoBehaviour
    {
        public static WorkerTaskManager Instance { get; private set; }
        /// <summary>
        /// 记录所有采摘任务的位置
        /// </summary>
        public List<Vector3Int> GatherPos { get; private set; }

        /// <summary>
        /// 所有任务(list中越靠前优先级越大)
        /// </summary>
        private List<Dictionary<WorkerTask,bool>> tasks;
        /// <summary>
        /// 饥饿任务与pos挂钩，TODO与worker数量挂钩
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
        /// Worker获取任务，LAB_TODO优先级没有实现
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
                        // 该任务是否正在被做
                        if (task[_task]) continue;
                        // 是否满足做任务的基础条件
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
                    // 获得任务
                    if (closedTask != null)
                    {
                        // 先设置任务
                        worker.Manager.Task = closedTask;
                        closedTask.start(worker);
                        // 同一个饥饿任务还可以继续接
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
        /// 添加任务
        /// </summary>
        /// <param name="task"></param>
        public void addTask(WorkerTask task, int prior = 2) {
            if (task == null) return;
            // 如果是饥饿任务,一个位置仅对应一个任务
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
        /// 完成任务
        /// </summary>
        /// <param name="task"></param>
        public void completeTask(WorkerTask task)
        {
            // 不能删除饥饿任务，需要在deleteHungryTask中删除
            if (task.TaskType != TaskType.Hungry)
            {
                foreach(Dictionary<WorkerTask,bool> _task in tasks)
                {
                    _task.Remove(task);
                    break;
                }
            }
            // 是饥饿任务，则将其改为可再次接受状态，即false
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
            return $"任务总数量: {total}\n" +
                $"正在做的任务数量: {count}";
        }

        /// <summary>
        /// 删除吃饭任务
        /// 该位置再在仓库中的食物被消耗完了
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
        /// 取消采集任务
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
