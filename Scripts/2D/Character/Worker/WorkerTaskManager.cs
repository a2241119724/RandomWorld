namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Character.Worker.Task.Individual;
    using LAB2D.Core.KDTree;
    using LAB2D.Gameplay;
    using LAB2D.Serializable;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
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
        private readonly WorkerTaskAssignmentService<AWorkerTask> assignmentService;
        private KDTree taskTree = new KDTree();

        public WorkerTaskManager()
        {
            this.tasks = new List<Dictionary<AWorkerTask, bool>>();
            this.assignmentService = new WorkerTaskAssignmentService<AWorkerTask>();
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
                if (worker.IsDialoguePaused)
                {
                    continue;
                }

                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData.Task != null)
                {
                    continue;
                }

                for (int priority = 0; priority < this.tasks.Count; priority++)
                {
                    WorkerTaskAssignmentResult<AWorkerTask> assignment =
                        this.assignmentService.SelectTask(
                            this.CreateWorkerSnapshot(worker, workerData.Task == null),
                            this.CreateTaskSnapshots(priority, worker));

                    // 获得任务
                    if (assignment.HasTask)
                    {
                        AWorkerTask closedTask = assignment.Task;

                        // 先设置任务
                        workerData.Task = closedTask;
                        closedTask.Start(worker);
                        if (workerData.Task == closedTask)
                        {
                            this.tasks[assignment.Priority][closedTask] = true;
                        }

                        EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                        break;
                    }
                }
            }
        }

        private WorkerAgentSnapshot CreateWorkerSnapshot(AWorker worker, bool isIdle)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            return new WorkerAgentSnapshot(
                worker.GetInstanceID(),
                new GameVector2(worker.transform.position.y, worker.transform.position.x),
                isIdle,
                worker.IsDialoguePaused,
                workerData?.CurHungry ?? 0f,
                workerData?.MaxHungry ?? 0f,
                workerData?.CurTired ?? 0f,
                workerData?.MaxTired ?? 0f);
        }

        private List<WorkerTaskSnapshot<AWorkerTask>> CreateTaskSnapshots(int priority, AWorker worker)
        {
            List<WorkerTaskSnapshot<AWorkerTask>> result = new ();
            Dictionary<AWorkerTask, bool> taskGroup = this.tasks[priority];
            foreach (KeyValuePair<AWorkerTask, bool> taskPair in taskGroup)
            {
                AWorkerTask task = taskPair.Key;
                result.Add(new WorkerTaskSnapshot<AWorkerTask>(
                    task,
                    task.TaskId,
                    priority,
                    new GameVector2(task.TargetMap.X, task.TargetMap.Y),
                    taskPair.Value,
                    () => task.IsCanWork(worker)));
            }

            return result;
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
            if (task.TaskType == WorkerTaskType.Eat)
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
            else if (task.TaskType == WorkerTaskType.Gather)
            {
                this.GatherPos.Add(Vector3IntLAB.ToVector3Int(task.TargetMap));
            }
            else if (task.TaskType == WorkerTaskType.Wear)
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
            EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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
                    if (task.TaskType == WorkerTaskType.Eat)
                    {
                        this.tasks[i][task] = false;
                    }
                    else
                    {
                        this.tasks[i].Remove(task);
                    }

                    EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                    return;
                }
            }

            EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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
                    EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                    break;
                }
            }
        }

        /// <summary>
        /// 创建任务队列只读快照。
        /// </summary>
        /// <returns>任务队列快照。</returns>
        public WorkerTaskQueueSnapshot CreateTaskQueueSnapshot()
        {
            return WorkerTaskSummaryTool.BuildSnapshot(this.tasks);
        }

        /// <summary>
        /// 创建任务分配只读诊断报告。
        /// A006 殖民地指挥中心使用该报告解释等待任务为什么没有被 Worker 接走；本方法只读取任务队列，不改变任务状态或优先级。
        /// </summary>
        /// <returns>任务分配诊断报告。</returns>
        public WorkerTaskAssignmentReport CreateTaskAssignmentReport()
        {
            return ColonyCommandCenterTool.BuildAssignmentReport(this.tasks, WorkerManager.Instance.Characters);
        }

        /// <summary>
        /// 获取适合 HUD 展示的任务队列摘要。
        /// </summary>
        /// <returns>任务队列摘要文本。</returns>
        public string GetTaskQueueSummaryText()
        {
            return WorkerTaskSummaryTool.BuildHudText(this.CreateTaskQueueSnapshot());
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
                res += $"{(WorkerTaskType)i}:{taskCount[i]}\n";
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
                            EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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
                    if (task.TaskType == WorkerTaskType.Gather && task.TargetMap.X == posMap.x
                        && task.TargetMap.Y == posMap.y)
                    {
                        this.tasks[i].Remove(task);
                        this.GatherPos.Remove(Vector3IntLAB.ToVector3Int(task.TargetMap));
                        EventBus.Instance.Publish(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                        return;
                    }
                }
            }
        }
    }
}
