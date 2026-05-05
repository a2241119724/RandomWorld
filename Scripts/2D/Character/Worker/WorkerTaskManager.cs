namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker任务管理器
    /// </summary>
    public class WorkerTaskManager : MonoBehaviour
    {
        private static long curtaskId = 0;
        private readonly List<Dictionary<AWorkerTask, bool>> tasks; // 所有任务(list中越靠前优先级越大), TODO分离正在做的任务
        private readonly List<WorkerHungryTask> hungryTasks; // 饥饿任务与pos挂钩，TODO与worker数量挂钩
        private readonly List<WorkerWearTask> wearTasks;
        private KDTree taskTree = new KDTree();

        public WorkerTaskManager()
        {
            this.tasks = new List<Dictionary<AWorkerTask, bool>>();
            for (int i = 0; i < 4; i++)
            {
                this.tasks.Add(new Dictionary<AWorkerTask, bool>());
            }

            this.hungryTasks = new List<WorkerHungryTask>();
            this.wearTasks = new List<WorkerWearTask>();
            this.GatherPos = new List<Vector3Int>();
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static WorkerTaskManager Instance { get; private set; }

        /// <summary>
        /// 记录所有采摘任务的位置
        /// </summary>
        public List<Vector3Int> GatherPos { get; private set; }

        public void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Worker获取任务
        /// </summary>
        public void Update()
        {
            List<AWorker> workers = WorkerManager.Instance.Characters;
            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData.Task != null)
                {
                    continue;
                }

                foreach (Dictionary<AWorkerTask, bool> task in this.tasks)
                {
                    AWorkerTask closedTask = null;
                    float minDistance = 999999.0f;
                    foreach (AWorkerTask task1 in task.Keys)
                    {
                        // 该任务是否正在被做
                        if (task[task1])
                        {
                            continue;
                        }

                        // 是否满足做任务的基础条件
                        if (!task1.IsCanWork(worker))
                        {
                            continue;
                        }

                        if (closedTask == null)
                        {
                            minDistance = Mathf.Pow(worker.transform.position.y - task1.TargetMap.X, 2) +
                                Mathf.Pow(worker.transform.position.x - task1.TargetMap.Y, 2);
                            closedTask = task1;
                        }
                        else
                        {
                            float distance = Mathf.Pow(worker.transform.position.y - task1.TargetMap.X, 2) +
                                Mathf.Pow(worker.transform.position.x - task1.TargetMap.Y, 2);
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
                        workerData.Task = closedTask;
                        closedTask.Start(worker);
                        if (workerData.Task == closedTask)
                        {
                            task[closedTask] = true;
                        }

                        DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="taskPosMap">任务位置</param>
        /// <param name="prior">优先级</param>
        public void AddTask(AWorkerTask task, Vector3IntLAB taskPosMap, int prior = 2)
        {
            if (task == null)
            {
                return;
            }

            task.TaskId = ++WorkerTaskManager.curtaskId;

            // 如果是饥饿任务,一个位置仅对应一个任务
            if (task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Eat)
            {
                foreach (WorkerHungryTask hungryTask in this.hungryTasks)
                {
                    if (hungryTask.TargetMap.Equals(task.TargetMap))
                    {
                        return;
                    }
                }

                this.hungryTasks.Add((WorkerHungryTask)task);
            }
            else if (task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Gather)
            {
                this.GatherPos.Add(Vector3IntLAB.ToVector3Int(task.TargetMap));
            }
            else if (task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Wear)
            {
                // 一个位置只能有一个穿衣任务
                foreach (AWorkerTask wearTask in this.wearTasks)
                {
                    if (wearTask.TargetMap.X == task.TargetMap.X
                        && wearTask.TargetMap.Y == task.TargetMap.Y)
                    {
                        return;
                    }
                }

                this.wearTasks.Add((WorkerWearTask)task);
            }

            this.tasks[prior].Add(task, false);
            this.taskTree.Insert(Vector3IntLAB.ToVector2ShortLAB(taskPosMap));
            DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        /// <param name="task">任务</param>
        public void CompleteTask(AWorkerTask task)
        {
            for (int i = 0; i < this.tasks.Count; i++)
            {
                if (this.tasks[i].ContainsKey(task))
                {
                    if (task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Eat)
                    {
                        this.tasks[i][task] = false;
                    }
                    else
                    {
                        this.tasks[i].Remove(task);
                    }

                    DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
                    return;
                }
            }

            DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        /// <param name="task">任务</param>
        public void GiveUpTask(AWorkerTask task)
        {
            if (task == null)
            {
                return;
            }

            for (int i = 0; i < this.tasks.Count; i++)
            {
                if (this.tasks[i].ContainsKey(task))
                {
                    this.tasks[i][task] = false;
                    DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
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
            foreach (Dictionary<AWorkerTask, bool> task in this.tasks)
            {
                total += task.Count;
                foreach (AWorkerTask task1 in task.Keys)
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
                res += $"{(AWorkerTask.WorkerTaskTypeEnum)i}:{taskCount[i]}\n";
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
                if (hungryTask.TargetMap.X == pos.x && hungryTask.TargetMap.Y == pos.y)
                {
                    for (int i = 0; i < this.tasks.Count; i++)
                    {
                        if (this.tasks[i].ContainsKey(hungryTask))
                        {
                            this.tasks[i].Remove(hungryTask);
                            this.hungryTasks.Remove(hungryTask);
                            DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
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
                foreach (AWorkerTask task in this.tasks[i].Keys)
                {
                    if (task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Gather && task.TargetMap.X == posMap.x
                        && task.TargetMap.Y == posMap.y)
                    {
                        this.tasks[i].Remove(task);
                        this.GatherPos.Remove(Vector3IntLAB.ToVector3Int(task.TargetMap));
                        DebugUI.Instance.UpdateInfo(this.GetTaskInfo());
                        return;
                    }
                }
            }
        }
    }
}
