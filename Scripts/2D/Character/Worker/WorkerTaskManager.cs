namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
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
        private readonly WorkerTaskAssignmentService<AWorkerTask> assignmentService;
        private readonly List<GameGridPosition> gatherPositions;
        private KDTree taskTree = new KDTree();

        public WorkerTaskManager()
        {
            this.taskQueue = new WorkerTaskQueue<AWorkerTask>();
            this.assignmentService = new WorkerTaskAssignmentService<AWorkerTask>();
            this.gatherPositions = new List<GameGridPosition>();
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static WorkerTaskManager Instance { get; private set; }

        /// <summary>
        /// 上次执行 Tick 的帧编号，用于防止 Update() 和 GlobalInit ITickable 双重驱动。
        /// </summary>
        private int lastTickFrame = -1;

        /// <summary>
        /// Worker 列表提供者 — 获取所有 Worker 用于任务分配。
        /// 默认实现访问 ServiceLocator.Get<WorkerManager>().Characters。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Func<List<AWorker>> WorkerListProvider { get; set; }
            = () => ServiceLocator.Get<WorkerManager>().Characters;

        /// <summary>
        /// Worker 位置提供者 — 获取 Worker 的 GameVector2 位置（注意 X/Y 坐标交换：map x ← world y，map y ← world x）。
        /// 默认实现封装 Transform.position 访问；可在测试中替换为固定坐标桩。
        /// </summary>
        public static System.Func<AWorker, GameVector2> WorkerPositionProvider { get; set; }
            = (worker) => new GameVector2(worker.transform.position.y, worker.transform.position.x);

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
        /// 内置帧去重保护：同一帧内多次调用（Update 兼容桥 + GlobalInit）只执行一次。
        /// </summary>
        public void Tick(float deltaTime)
        {
            int currentFrame = Time.frameCount;
            if (currentFrame == this.lastTickFrame)
            {
                return;
            }

            this.lastTickFrame = currentFrame;
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
                WorkerPositionProvider(worker),
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
        /// 添加任务（Domain 类型）。
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="taskPosMap">任务位置（GameGridPosition）。</param>
        /// <param name="prior">优先级</param>
        public void AddTask(AWorkerTask task, GameGridPosition taskPosMap, int prior = 2)
        {
#pragma warning disable CS0618 // 内部桥接：GameGridPosition → Vector3IntLAB，保留旧重载的完整实现
            this.AddTask(task, new Vector3IntLAB(taskPosMap.X, taskPosMap.Y, taskPosMap.Z), prior);
#pragma warning restore CS0618
        }

        /// <summary>
        /// [Obsolete] 添加任务。
        /// 请改用 AddTask(AWorkerTask, GameGridPosition, int)。
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="taskPosMap">任务位置（Vector3IntLAB）。</param>
        /// <param name="prior">优先级</param>
        [Obsolete("Use AddTask(AWorkerTask, GameGridPosition, int) instead.")]
        public void AddTask(AWorkerTask task, Vector3IntLAB taskPosMap, int prior = 2)
        {
            if (task == null)
            {
                return;
            }

            task.TaskId = ++WorkerTaskManager.curtaskId;

            TaskTraits traits = task.Traits;

            // 通用位置去重（替代原来的 Eat/Wear 硬编码去重）
            if (traits.HasFlag(TaskTraits.OnePerPosition) && HasTaskAtPosition(task))
            {
                return;
            }

            // 记录位置用于外部取消操作（替代原来的 Gather 硬编码）
            if (traits.HasFlag(TaskTraits.TrackPositions))
            {
                this.gatherPositions.Add(new GameGridPosition(task.TargetMap.X, task.TargetMap.Y, task.TargetMap.Z));
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
            if (task.Traits.HasFlag(TaskTraits.ReturnToIdle))
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
            int typeCount = (int)WorkerTaskType._Count;
            int[] taskCount = new int[typeCount];
            for (int i = 0; i < this.taskQueue.PriorityCount; i++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriority(i))
                {
                    if (pair.Value)
                    {
                        int typeIndex = (int)pair.Key.TaskType;
                        if (typeIndex >= 0 && typeIndex < typeCount)
                        {
                            taskCount[typeIndex]++;
                        }
                    }
                }
            }

            string res = $"任务总数量: {total}\n";
            for (int i = 0; i < typeCount; i++)
            {
                res += $"{(WorkerTaskType)i}:{taskCount[i]}\n";
            }

            return res;
        }

        /// <summary>
        /// 检查队列中是否已存在同位置同类型的任务（用于 OnePerPosition 去重）。
        /// </summary>
        private bool HasTaskAtPosition(AWorkerTask task)
        {
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriority(p))
                {
                    AWorkerTask existing = pair.Key;
                    if (existing.TaskType == task.TaskType &&
                        existing.TargetMap.X == task.TargetMap.X &&
                        existing.TargetMap.Y == task.TargetMap.Y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 删除吃饭任务（Domain 类型）。
        /// 该位置在仓库中的食物被消耗完了。
        /// </summary>
        /// <param name="pos">位置（GameGridPosition）。</param>
        public void DeleteHungryTask(GameGridPosition pos)
        {
            bool removed = this.taskQueue.RemoveWhere(task =>
                task.TaskType == WorkerTaskType.Eat &&
                task.TargetMap.X == pos.X &&
                task.TargetMap.Y == pos.Y);

            if (removed)
            {
                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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
