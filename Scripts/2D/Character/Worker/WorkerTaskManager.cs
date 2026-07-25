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
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker任务管理器。
    /// 通过 ITickable 接口由 GlobalInit 统一驱动任务分配循环，
    /// 同时保留 MonoBehaviour 作为兼容桥（Awake 注册单例）。
    ///
    /// API 层已迁移到 GameGridPosition，旧 Vector3Int 方法标记为 Obsolete 保持向后兼容。
    /// </summary>
    public class WorkerTaskManager : MonoBehaviour, ITickable
    {
        private static long curtaskId = 0;
        private readonly WorkerTaskQueue<AWorkerTask> taskQueue;
        private readonly List<WorkerHungryTask> hungryTasks;
        private readonly List<WorkerWearTask> wearTasks;
        private readonly WorkerTaskAssignmentService<AWorkerTask> assignmentService;
        private readonly List<GameGridPosition> gatherPositions;
        private KDTree taskTree = new KDTree();

        public WorkerTaskManager()
        {
            this.taskQueue = new WorkerTaskQueue<AWorkerTask>();
            this.assignmentService = new WorkerTaskAssignmentService<AWorkerTask>();
            this.hungryTasks = new List<WorkerHungryTask>();
            this.wearTasks = new List<WorkerWearTask>();
            this.gatherPositions = new List<GameGridPosition>();
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static WorkerTaskManager Instance { get; private set; }

        /// <summary>
        /// Worker 列表提供者 — 获取所有 Worker 用于任务分配。
        /// 默认实现访问 ServiceLocator.Get<WorkerManager>().Characters。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Func<List<AWorker>> WorkerListProvider { get; set; }
            = () => ServiceLocator.Get<WorkerManager>().Characters;

        internal static System.Action<IGameEvent> EventBusPublishProvider { get; set; }
            = (e) => ServiceLocator.Get<EventBus>().PublishInternal(e);

        /// <summary>
        /// 记录所有采摘任务的位置（Domain 类型）。
        /// 替代已废弃的 GatherPos (Vector3Int)。
        /// </summary>
        public List<GameGridPosition> GatherPositions
        {
            get { return this.gatherPositions; }
        }

        /// <summary>
        /// [Obsolete] 记录所有采摘任务的位置。
        /// 请改用 GatherPositions (List&lt;GameGridPosition&gt;)。
        /// </summary>
        [Obsolete("Use GatherPositions (List<GameGridPosition>) instead.")]
        public List<Vector3Int> GatherPos
        {
            get
            {
                List<Vector3Int> result = new List<Vector3Int>(this.gatherPositions.Count);
                for (int i = 0; i < this.gatherPositions.Count; i++)
                {
                    result.Add(UnityVectorAdapter.ToVector3Int(this.gatherPositions[i]));
                }

                return result;
            }
        }

        public void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// [Obsolete] Update 由 Unity 引擎驱动。
        /// 任务分配循环已迁移至 Tick(float)，由 GlobalInit 统一驱动。
        /// 保留此方法作为兼容桥：当 ITickable 未注册时仍可正常工作。
        /// </summary>
        public void Update()
        {
            this.Tick(Time.deltaTime);
        }

        /// <summary>
        /// ITickable 实现：每帧执行任务分配循环。
        /// 由 GlobalInit.BuildTickableList() 统一驱动，确保与其他 ITickable 的执行顺序一致。
        /// </summary>
        public void Tick(float deltaTime)
        {
            this.RunTaskAssignmentLoop();
        }

        /// <summary>
        /// 任务分配主循环：遍历空闲 Worker，按优先级分配任务。
        /// </summary>
        private void RunTaskAssignmentLoop()
        {
            List<AWorker> workers = WorkerListProvider();
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

                for (int priority = 0; priority < this.taskQueue.PriorityCount; priority++)
                {
                    WorkerTaskAssignmentResult<AWorkerTask> assignment =
                        this.assignmentService.SelectTask(
                            this.CreateWorkerSnapshot(worker, workerData.Task == null),
                            this.CreateTaskSnapshots(priority, worker));

                    if (assignment.HasTask)
                    {
                        AWorkerTask closedTask = assignment.Task;

                        workerData.Task = closedTask;
                        closedTask.Start(worker);
                        if (workerData.Task == closedTask)
                        {
                            this.taskQueue.MarkRunning(closedTask);
                        }

                        EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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
            IReadOnlyDictionary<AWorkerTask, bool> taskGroup = this.taskQueue.GetTasksAtPriority(priority);
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
                GameGridPosition gatherPos = new GameGridPosition(task.TargetMap.X, task.TargetMap.Y, task.TargetMap.Z);
                this.gatherPositions.Add(gatherPos);
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

            this.taskQueue.Add(task, prior);
            this.taskTree.Insert(Vector3IntLAB.ToVector2ShortLAB(taskPosMap));
            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        /// <param name="task">任务</param>
        public void CompleteTask(AWorkerTask task)
        {
            if (task.TaskType == WorkerTaskType.Eat)
            {
                this.taskQueue.MarkIdle(task);
            }
            else
            {
                this.taskQueue.Remove(task);
            }

            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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

            this.taskQueue.MarkIdle(task);
            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
        }

        /// <summary>
        /// 创建任务队列只读快照。
        /// </summary>
        /// <returns>任务队列快照。</returns>
        public WorkerTaskQueueSnapshot CreateTaskQueueSnapshot()
        {
            return WorkerTaskSummaryTool.BuildSnapshot(this.GetTasksAsList());
        }

        /// <summary>
        /// 创建任务分配只读诊断报告。
        /// A006 殖民地指挥中心使用该报告解释等待任务为什么没有被 Worker 接走；本方法只读取任务队列，不改变任务状态或优先级。
        /// </summary>
        /// <returns>任务分配诊断报告。</returns>
        public WorkerTaskAssignmentReport CreateTaskAssignmentReport()
        {
            return ColonyCommandCenterTool.BuildAssignmentReport(this.GetTasksAsList(), WorkerListProvider());
        }

        private List<Dictionary<AWorkerTask, bool>> GetTasksAsList()
        {
            List<Dictionary<AWorkerTask, bool>> result = new ();
            for (int i = 0; i < this.taskQueue.PriorityCount; i++)
            {
                result.Add(new Dictionary<AWorkerTask, bool>(this.taskQueue.GetTasksAtPriority(i)));
            }

            return result;
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
            int total = this.taskQueue.TotalCount;
            int[] taskCount = new int[10];
            for (int i = 0; i < this.taskQueue.PriorityCount; i++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriority(i))
                {
                    if (pair.Value)
                    {
                        int typeIndex = (int)pair.Key.TaskType;
                        if (typeIndex >= 0 && typeIndex < taskCount.Length)
                        {
                            taskCount[typeIndex]++;
                        }
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
        /// 删除吃饭任务（Domain 类型）。
        /// 该位置在仓库中的食物被消耗完了。
        /// </summary>
        /// <param name="pos">位置（GameGridPosition）。</param>
        public void DeleteHungryTask(GameGridPosition pos)
        {
            for (int i = this.hungryTasks.Count - 1; i >= 0; i--)
            {
                WorkerHungryTask hungryTask = this.hungryTasks[i];
                if (hungryTask.TargetMap.X == pos.X && hungryTask.TargetMap.Y == pos.Y)
                {
                    this.taskQueue.Remove(hungryTask);
                    this.hungryTasks.RemoveAt(i);
                    EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                    return;
                }
            }
        }

        /// <summary>
        /// [Obsolete] 删除吃饭任务。
        /// 请改用 DeleteHungryTask(GameGridPosition)。
        /// </summary>
        /// <param name="pos">位置（Vector3Int）。</param>
        [Obsolete("Use DeleteHungryTask(GameGridPosition) instead.")]
        public void DeleteHungryTask(Vector3Int pos)
        {
            this.DeleteHungryTask(UnityVectorAdapter.ToGameGridPosition(pos));
        }

        /// <summary>
        /// 取消采集任务（Domain 类型）。
        /// </summary>
        /// <param name="posMap">任务位置（GameGridPosition）。</param>
        public void CancelGatherTask(GameGridPosition posMap)
        {
            if (!this.gatherPositions.Contains(posMap))
            {
                return;
            }

            bool removed = this.taskQueue.RemoveWhere(task =>
                task.TaskType == WorkerTaskType.Gather &&
                task.TargetMap.X == posMap.X &&
                task.TargetMap.Y == posMap.Y);

            if (removed)
            {
                this.gatherPositions.Remove(posMap);
                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
            }
        }

        /// <summary>
        /// [Obsolete] 取消采集任务。
        /// 请改用 CancelGatherTask(GameGridPosition)。
        /// </summary>
        /// <param name="posMap">任务位置（Vector3Int）。</param>
        [Obsolete("Use CancelGatherTask(GameGridPosition) instead.")]
        public void CancelGatherTask(Vector3Int posMap)
        {
            this.CancelGatherTask(UnityVectorAdapter.ToGameGridPosition(posMap));
        }
    }
}
