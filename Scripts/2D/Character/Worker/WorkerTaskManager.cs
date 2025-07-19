namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 工作者任务管理器
    /// </summary>
    public class WorkerTaskManager : MonoBehaviour
    {
        private List<Dictionary<WorkerTask, bool>> tasks; // 所有任务(list中越靠前优先级越大)
        private List<WorkerHungryTask> hungryTasks; // 饥饿任务与pos挂钩，TODO与worker数量挂钩
        private List<WorkerWearTask> wearTasks;

        /// <summary>
        /// 单例
        /// </summary>
        public static WorkerTaskManager Instance { get; private set; }

        /// <summary>
        /// 记录所有采摘任务的位置
        /// </summary>
        public List<Vector3Int> GatherPos { get; private set; }

        public WorkerTaskManager()
        {
            this.tasks = new List<Dictionary<WorkerTask, bool>>();
            for (int i = 0; i < 4; i++)
            {
                this.tasks.Add(new Dictionary<WorkerTask, bool>());
            }

            this.hungryTasks = new List<WorkerHungryTask>();
            this.wearTasks = new List<WorkerWearTask>();
            this.GatherPos = new List<Vector3Int>();
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="prior">优先级</param>
        public void AddTask(WorkerTask task, int prior = 2)
        {
            if (task == null)
            {
                return;
            }

            // 如果是饥饿任务,一个位置仅对应一个任务
            if (task.TaskType == TaskType.Hungry)
            {
                foreach (WorkerHungryTask hungryTask in this.hungryTasks)
                {
                    if (hungryTask.TargetMap.x == task.TargetMap.x && hungryTask.TargetMap.y == task.TargetMap.y)
                    {
                        return;
                    }
                }

                this.hungryTasks.Add((WorkerHungryTask)task);
            }
            else if (task.TaskType == TaskType.Gather)
            {
                this.GatherPos.Add(task.TargetMap);
            }
            else if (task.TaskType == TaskType.Wear)
            {
                // 一个位置只能有一个穿衣任务
                foreach (WorkerTask wearTask in this.wearTasks)
                {
                    if (wearTask.TargetMap.x == task.TargetMap.x
                        && wearTask.TargetMap.y == task.TargetMap.y)
                    {
                        return;
                    }
                }

                this.wearTasks.Add((WorkerWearTask)task);
            }

            this.tasks[prior].Add(task, false);
            DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        /// <param name="task">任务</param>
        public void CompleteTask(WorkerTask task)
        {
            // 不能删除饥饿任务，需要在deleteHungryTask中删除
            if (task.TaskType != TaskType.Hungry)
            {
                for (int i = 0; i < this.tasks.Count; i++)
                {
                    if (this.tasks[i].ContainsKey(task))
                    {
                        this.tasks[i].Remove(task);
                        DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
                        break;
                    }
                }
            }

            // 是饥饿任务，则将其改为可再次接受状态，即false
            else
            {
                this.tasks[1][task] = false;
            }

            DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        /// <param name="task">任务</param>
        public void GiveUpTask(WorkerTask task)
        {
            if (task == null)
            {
                return;
            }

            for (int i = 0; i < this.tasks.Count; i++)
            {
                if (this.tasks[i].ContainsKey(task))
                {
                    this.tasks[i].Remove(task);
                    DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
                    break;
                }
            }
        }

        /// <summary>
        /// 获取任务信息
        /// </summary>
        /// <returns>任务信息</returns>
        public string GetTaskInfo()
        {
            int total = 0;
            int[] taskCount = new int[10];
            foreach (Dictionary<WorkerTask, bool> task in this.tasks)
            {
                total += task.Count;
                foreach (WorkerTask task1 in task.Keys)
                {
                    if (task[task1])
                    {
                        taskCount[(int)task1.TaskType]++;
                    }
                }
            }

            string res = $"任务总数量: {total}\n";
            for (int i = 0; i < 10; i++)
            {
                res += $"{((TaskType)i).ToString()}:{taskCount[i]}\n";
            }

            return res;
        }

        /// <summary>
        /// 删除吃饭任务
        /// 该位置在仓库中的食物被消耗完了
        /// </summary>
        /// <param name="pos">位置</param>
        public void DeleteHungryTask(Vector3Int pos)
        {
            foreach (WorkerHungryTask hungryTask in this.hungryTasks)
            {
                if (hungryTask.TargetMap.x == pos.x && hungryTask.TargetMap.y == pos.y)
                {
                    for (int i = 0; i < this.tasks.Count; i++)
                    {
                        if (this.tasks[i].ContainsKey(hungryTask))
                        {
                            this.tasks[i].Remove(hungryTask);
                            this.hungryTasks.Remove(hungryTask);
                            DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 取消采集任务
        /// </summary>
        /// <param name="posMap">任务位置</param>
        public void CancelGatherTask(Vector3Int posMap)
        {
            if (!this.GatherPos.Contains(posMap))
            {
                return;
            }

            for (int i = 0; i < this.tasks.Count; i++)
            {
                foreach (WorkerTask task in this.tasks[i].Keys)
                {
                    if (task.TaskType == TaskType.Gather && task.TargetMap.x == posMap.x
                        && task.TargetMap.y == posMap.y)
                    {
                        this.tasks[i].Remove(task);
                        this.GatherPos.Remove(task.TargetMap);
                        DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
                        return;
                    }
                }
            }
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
                if (worker.Manager.Task != null)
                {
                    continue;
                }

                foreach (Dictionary<WorkerTask, bool> task in this.tasks)
                {
                    WorkerTask closedTask = null;
                    float minDistance = 999999.0f;
                    foreach (WorkerTask task1 in task.Keys)
                    {
                        // 该任务是否正在被做
                        if (task[task1])
                        {
                            continue;
                        }

                        // 是否满足做任务的基础条件
                        if (!task1.isCanWork(worker))
                        {
                            continue;
                        }

                        if (closedTask == null)
                        {
                            minDistance = Mathf.Pow(worker.transform.position.y - task1.TargetMap.x, 2) +
                                Mathf.Pow(worker.transform.position.x - task1.TargetMap.y, 2);
                            closedTask = task1;
                        }
                        else
                        {
                            float distance = Mathf.Pow(worker.transform.position.y - task1.TargetMap.x, 2) +
                                Mathf.Pow(worker.transform.position.x - task1.TargetMap.y, 2);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closedTask = task1;
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

                        DebugUI.Instance.updateTaskInfo(this.GetTaskInfo());
                        break;
                    }
                }
            }
        }
    }
}
