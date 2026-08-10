namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Core.KDTree;
    using LAB2D.Core.Seek;
    using LAB2D.Gameplay;
    using LAB2D.Map;
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
        private readonly List<GameGridPosition> demolishPositions;
        private KDTree taskTree = new KDTree();

        public WorkerTaskManager()
        {
            this.taskQueue = new WorkerTaskQueue<AWorkerTask>();
            this.assignmentService = new WorkerTaskAssignmentService<AWorkerTask>();
            this.gatherPositions = new List<GameGridPosition>();
            this.demolishPositions = new List<GameGridPosition>();
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

        /// <summary>
        /// 记录所有拆除任务的位置（Domain 类型）。
        /// </summary>
        public List<GameGridPosition> DemolishPositions
        {
            get { return this.demolishPositions; }
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

            // 任务分配循环：每5帧执行一次（减少80%的每帧迭代开销）
            if (currentFrame % 5 == 0)
            {
                this.RunTaskAssignmentLoop();
            }

            // 悬赏过期检查：每30帧执行一次（悬赏过期窗口为数分钟，半秒延迟可忽略）
            if (currentFrame % 30 == 0)
            {
                this.ExpireBountyTasks();
            }

            // 每60帧清理一次过期的寻路失败缓存
            if (currentFrame % 60 == 0)
            {
                ASeek.CleanFailCache();
            }

            // 每300帧（约5秒）扫描 BuildMap 中未完成建造，确保存在对应任务
            if (currentFrame % 300 == 0)
            {
                this.VerifyBuildTasks();
            }
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

        /// <summary>
        /// 尝试将玩家悬赏任务（优先级 0）分配给指定 Worker。
        /// WorkerSeekState 在自主决策前调用，确保玩家任务优先于 Worker 自我任务。
        /// </summary>
        /// <param name="worker">目标 Worker</param>
        /// <returns>成功分配返回 true</returns>
        public bool TryAssignPlayerTask(AWorker worker)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null || workerData.Task != null)
            {
                return false;
            }

            WorkerAgentSnapshot snapshot = this.CreateWorkerSnapshot(worker, true);
            IReadOnlyList<WorkerTaskSnapshot<AWorkerTask>> tasks = this.CreateTaskSnapshots(
                WorkerTaskPriority.PlayerBounty, worker);

            WorkerTaskAssignmentResult<AWorkerTask> result =
                this.assignmentService.SelectTask(snapshot, tasks);

            if (result.HasTask)
            {
                workerData.Task = result.Task;
                result.Task.Start(worker);
                if (workerData.Task == result.Task)
                {
                    this.taskQueue.MarkRunning(result.Task);
                }

                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试将非玩家悬赏的全局任务（优先级 1→2：WorkerBounty → SystemDefault）分配给指定 Worker。
        /// 不包括 PlayerBounty(0)——玩家指令由 TryAssignPlayerTask 单独在最前面处理。
        /// 不包括 Idle(3)——那是 Worker 自用的锻炼任务。
        /// 调用方应在确认 Worker 无需自保（不饥饿、不疲劳）后才调用，
        /// 确保 Worker 优先处理生存需求再帮助全局队列。
        /// </summary>
        /// <param name="worker">目标 Worker</param>
        /// <returns>成功分配返回 true</returns>
        public bool TryAssignGlobalTask(AWorker worker)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null || workerData.Task != null)
            {
                return false;
            }

            WorkerAgentSnapshot snapshot = this.CreateWorkerSnapshot(worker, true);

            // 从 WorkerBounty(1) 开始，跳过已处理的 PlayerBounty(0) 和自用的 Idle(3)
            for (int priority = WorkerTaskPriority.WorkerBounty; priority < WorkerTaskPriority.Idle; priority++)
            {
                IReadOnlyList<WorkerTaskSnapshot<AWorkerTask>> tasks =
                    this.CreateTaskSnapshots(priority, worker);

                WorkerTaskAssignmentResult<AWorkerTask> result =
                    this.assignmentService.SelectTask(snapshot, tasks);

                if (result.HasTask)
                {
                    workerData.Task = result.Task;
                    result.Task.Start(worker);
                    if (workerData.Task == result.Task)
                    {
                        this.taskQueue.MarkRunning(result.Task);
                    }

                    EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
                    return true;
                }
            }

            return false;
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
                workerData?.MaxTired ?? 0f,
                workerData?.Wallet ?? Domain.Worker.CurrencyAmount.Zero,
                workerData?.Personality ?? Domain.Worker.WorkerPersonality.Neutral);
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
        /// 清理过期的悬赏任务。
        /// 遍历队列中所有 WorkerBountyTask，检查其过期时间，
        /// 对已过期的任务退款给发布者并从队列移除。
        /// </summary>
        private void ExpireBountyTasks()
        {
            float currentGameTime = Core.ServiceLocator.Get<IGameTime>().Time;
            List<AWorkerTask> expiredTasks = new List<AWorkerTask>();

            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriority(p))
                {
                    if (pair.Key is WorkerBountyTask bountyTask
                        && bountyTask.BountyInfo.IsExpired(currentGameTime)
                        && !pair.Value) // 只在未被接取（Posted 状态）时过期
                    {
                        expiredTasks.Add(pair.Key);
                    }
                }
            }

            foreach (AWorkerTask task in expiredTasks)
            {
                if (task is WorkerBountyTask bounty)
                {
                    try
                    {
                        Core.ServiceLocator.Get<Gameplay.CurrencyManager>()
                            .RefundBounty(bounty.BountyInfo.IssuerWorkerId, bounty.BountyInfo.Reward);
                    }
                    catch (System.Exception e)
                    {
                        AWorkerTask.LogProvider(
                            $"悬赏退款失败: issuer={bounty.BountyInfo.IssuerWorkerId}, error={e.Message}",
                            LogManager.LogLevelEnum.Error);
                    }
                }

                this.taskQueue.Remove(task);
            }
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

            // 记录位置用于外部取消操作（按任务类型分发到对应的位置列表）
            if (traits.HasFlag(TaskTraits.TrackPositions))
            {
                var pos = new GameGridPosition(task.TargetMap.X, task.TargetMap.Y, task.TargetMap.Z);
                if (task.TaskType == WorkerTaskType.Demolish)
                {
                    this.demolishPositions.Add(pos);
                }
                else
                {
                    this.gatherPositions.Add(pos);
                }
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

            // 采集任务被放弃时：释放 GatherMap 认领 + 从队列彻底删除，避免其他 Worker 重复尝试
            if (task.TaskType == WorkerTaskType.Gather)
            {
                Core.ServiceLocator.Get<GatherMap>().CancelGather(
                    Vector3IntLAB.ToVector3Int(task.TargetMap));
                this.taskQueue.Remove(task);
            }
            else
            {
                this.taskQueue.MarkIdle(task);
            }

            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
        }

        /// <summary>
        /// 扫描 BuildMap.PosMap 中所有未完成的建造位置，确保它们有对应的活跃/闲置任务。
        /// 用于恢复因 Self-Build 任务不在全局队列中而永久丢失的建造任务。
        /// </summary>
        private void VerifyBuildTasks()
        {
            BuildMap buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null)
            {
                return;
            }

            foreach (var kv in buildMap.BuildMapDataLAB.PosMap)
            {
                BuildMap.BuildTileData tileData = kv.Value;
                if (tileData.IsComplete)
                {
                    continue;
                }

                Vector3Int pos = Vector3IntLAB.ToVector3Int(kv.Key);

                // 已有运行中或闲置的建造任务 → 跳过
                if (this.HasBuildTaskAtPosition(pos))
                {
                    continue;
                }

                // 已有 Worker 正在携带该位置的建造任务（Self-Build 场景）→ 跳过
                if (this.HasWorkerCarryingBuildTaskAt(pos))
                {
                    continue;
                }

                // 位置属于某 Worker 的规划房间区域（预注册的 Self-Build 墙壁）→ 跳过
                if (this.IsPartOfWorkerPlannedRoom(pos))
                {
                    continue;
                }

                // 无任务覆盖 → 重新创建建造任务
                Dictionary<int, ResourceInfo> buildCosts = BuildMap.GetBuildCost(tileData.Name);
                if (buildCosts == null || buildCosts.Count == 0)
                {
                    continue;
                }

                // 找回原建造者（如果 BuilderName 有记录）
                AWorker owner = !string.IsNullOrEmpty(tileData.BuilderName)
                    ? WorkerListProvider().Find(w => w.name == tileData.BuilderName)
                    : null;

                WorkerBuildTask.BuildTaskBuilder builder = new WorkerBuildTask.BuildTaskBuilder()
                    .SetBuildPos(pos)
                    .SetNeedResource(buildCosts);

                // 有原 Builder → 设置 Owner，保证只有他能接（即使走 AddTask 入全局队列）
                if (owner != null)
                {
                    builder.SetOwnerWorkerId(owner.GetInstanceID());
                }

                WorkerBuildTask newTask = builder.Build();

                // 优先尝试找回原 Worker 并直接指派（保持 Self-Build 语义）
                // 但仅在 Worker 空闲时指派，避免中断正在执行的任务（Sleep/Eat/Gather 等）
                bool assignedDirectly = false;
                if (owner != null)
                {
                    AWorker.WorkerData wd = owner.CharacterDataLAB as AWorker.WorkerData;
                    if (wd != null && wd.Task == null)
                    {
                        wd.Task = newTask;
                        newTask.Start(owner);
                        assignedDirectly = true;

                        AWorkerTask.LogProvider(
                            $"VerifyBuildTasks: 重新指派建造任务给 {owner.name} pos=({pos.x},{pos.y}) tile={tileData.Name}",
                            LogManager.LogLevelEnum.Warning);
                    }
                }

                if (!assignedDirectly)
                {
                    // Fallback: 原 Worker 正忙或不存在 → 创建任务入队
                    // 如果有原 Builder，任务已设置 OwnerWorkerId，只有该 Builder 可接
                    this.AddTask(newTask, new GameGridPosition(pos.x, pos.y, pos.z));

                    string ownerInfo = owner != null ? $" (专属 {owner.name})" : string.Empty;
                    AWorkerTask.LogProvider(
                        $"VerifyBuildTasks: 为未完成建造重新创建任务{ownerInfo} pos=({pos.x},{pos.y}) tile={tileData.Name}",
                        LogManager.LogLevelEnum.Warning);
                }
            }
        }

        /// <summary>
        /// 检查任务队列中是否已存在指定位置的建造任务（含运行中和闲置）。
        /// </summary>
        private bool HasBuildTaskAtPosition(Vector3Int pos)
        {
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (var kv in this.taskQueue.GetTasksAtPriority(p))
                {
                    if (kv.Key.TaskType == WorkerTaskType.Build
                        && kv.Key.TargetMap.X == pos.x
                        && kv.Key.TargetMap.Y == pos.y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否已有 Worker 正在携带指定位置的建造任务（Self-Build 场景中任务不经过全局队列）。
        /// </summary>
        private bool HasWorkerCarryingBuildTaskAt(Vector3Int pos)
        {
            List<AWorker> workers = WorkerListProvider();
            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd?.Task != null
                    && wd.Task.TaskType == WorkerTaskType.Build
                    && wd.Task.TargetMap.X == pos.x
                    && wd.Task.TargetMap.Y == pos.y)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查指定位置是否属于某 Worker 的规划房间区域（预注册的 Self-Build 墙壁/门/床）。
        /// 若属于规划房间，则该位置由 Worker 自行建造，不应创建全局任务。
        /// </summary>
        private bool IsPartOfWorkerPlannedRoom(Vector3Int pos)
        {
            List<AWorker> workers = WorkerListProvider();
            var wallOffsets = LAB2D.AI.Worker.WorkerBrain.GetWallOffsets();
            var doorOffset = LAB2D.AI.Worker.WorkerBrain.DoorOffset;
            int wallCount = LAB2D.AI.Worker.WorkerBrain.WallCount;

            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd?.PlannedHomePosition == null) continue;
                if (wd.HomeBuildStage >= LAB2D.AI.Worker.WorkerBrain.CompleteStage) continue;

                Vector3Int center = LAB2D.Serializable.Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);

                // 检查是否匹配墙壁位置
                for (int i = 0; i < wallCount; i++)
                {
                    if (pos == center + wallOffsets[i]) return true;
                }

                // 检查是否匹配门位置
                if (pos == center + doorOffset) return true;

                // 检查是否匹配床位置（房间中心）
                if (pos == center) return true;
            }

            return false;
        }

        /// <summary>
        /// 删除队列中所有与指定 Worker 相关的任务。
        /// Worker 死亡时调用，清理：悬赏发布、专属任务（Wear/Sleep/Exercise）、
        /// PickUp 目标所有、Carry(ToBoard) 指定执行者。
        /// </summary>
        /// <param name="workerInstanceId">Worker 的 GameObject instance ID。</param>
        public void RemoveTasksForWorker(int workerInstanceId)
        {
            if (workerInstanceId == 0)
            {
                return;
            }

            bool removed = this.taskQueue.RemoveWhere(task => task.OwnerWorkerId == workerInstanceId);
            if (removed)
            {
                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
            }
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
        /// 移除指定位置的 Carry 任务（用于将自动创建的 CarryTask 替换为 CarryTask(ToBoard)）。
        /// </summary>
        public void RemoveCarryTaskAt(Vector3Int posMap)
        {
            this.taskQueue.RemoveWhere(task =>
                task.TaskType == WorkerTaskType.Carry &&
                task.TargetMap.X == posMap.x &&
                task.TargetMap.Y == posMap.y);
        }

        /// <summary>
        /// 获取所有悬赏任务列表（含运行状态），供任务栏 HUD 展示。
        /// </summary>
        public List<(WorkerBountyTask task, bool isRunning)> GetBountyTasks()
        {
            var result = new List<(WorkerBountyTask, bool)>();
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (var kv in this.taskQueue.GetTasksAtPriority(p))
                {
                    if (kv.Key is WorkerBountyTask bounty)
                    {
                        result.Add((bounty, kv.Value));
                    }
                }
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

        /// <summary>
        /// 取消拆除任务（Domain 类型）。
        /// </summary>
        /// <param name="posMap">任务位置（GameGridPosition）。</param>
        public void CancelDemolishTask(GameGridPosition posMap)
        {
            if (!this.demolishPositions.Contains(posMap))
            {
                return;
            }

            bool removed = this.taskQueue.RemoveWhere(task =>
                task.TaskType == WorkerTaskType.Demolish &&
                task.TargetMap.X == posMap.X &&
                task.TargetMap.Y == posMap.Y);

            if (removed)
            {
                this.demolishPositions.Remove(posMap);
                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
            }
        }
    }
}
