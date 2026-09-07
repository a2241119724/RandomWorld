namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using LAB2D.Gameplay;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections.Generic;
    using System.Text;
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
        private readonly List<GameGridPosition> gatherPositions;
        private readonly List<GameGridPosition> demolishPositions;

        // VerifyBuildTasks 的反转索引（跨调用复用，Clear 不缩容）：把逐格 × (全队列扫描 +
        // 100 Worker 携带检查 + 100 Worker × 房间布局逐偏移比对) 的 O(N×W) 遍历反转为
        // 一次构建集合、逐格 O(1) 查。key 用 Vector2Int——原三层谓词只比 X/Y（Z 忽略）。
        private readonly HashSet<Vector2Int> verifyQueuedBuildPos = new (64);
        private readonly HashSet<Vector2Int> verifyCarryingBuildPos = new (16);
        private readonly HashSet<Vector2Int> verifyPlannedRoomPos = new (1024);

        public WorkerTaskManager()
        {
            this.taskQueue = new WorkerTaskQueue<AWorkerTask>();
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

            // [GcDiag] 本 Tick 内 mono 堆增量采样（定位 Profiler「WorkerTaskManager.Update
            // ~167KB/帧」分配源用；静态走查已排除每帧路径，改由运行时指认，排查后删除）
            long gcTickStart = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();

            // 任务分配循环：每帧进入、时间配额切片——每秒恒定处理 WorkersPerSecond 个 Worker
            //（60fps = 每帧 2 个），帧率无关：低帧率自动加大每帧预算、高帧率自动减小，
            // 全员一轮耗时稳定 ≈ count / 120 秒（100 Worker ≈ 0.83s），不随帧率波动。
            // 原「每 15 帧一个整帧全员跑完」在 100 空闲 Worker × 多任务时形成周期尖峰，
            // 且周期实际时长随帧率漂移（30fps 时翻倍）；时间配额两问题一并消除
            //（100 Worker 卡顿治理 2026-09-06）。
            this.RunTaskAssignmentLoop(deltaTime);

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

            // [GcDiag] Tick 内增量结算（见方法开头采样注释）：与 AWorker 同款 5s 汇总 + 单帧 max。
            long gcTickDelta = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - gcTickStart;
            if (gcTickDelta > 0)
            {
                this.gcTickWindowBytes += gcTickDelta;
                if (gcTickDelta > this.gcTickMaxFrame)
                {
                    this.gcTickMaxFrame = gcTickDelta;
                }
            }

            this.gcTickReportTimer += deltaTime;
            if (this.gcTickReportTimer >= 5f)
            {
                this.gcTickReportTimer -= 5f;
                if (this.gcTickWindowBytes > 10240)
                {
                    AWorkerTask.LogProvider(
                        $"[GcDiag] TaskManager 5s窗口Tick内分配≈{this.gcTickWindowBytes / 1024}KB 单帧max={this.gcTickMaxFrame}B 队列任务数={this.taskQueue.TotalCount} frame={Time.frameCount}",
                        LogManager.LogLevelEnum.Debug);
                }

                this.gcTickWindowBytes = 0;
                this.gcTickMaxFrame = 0;
            }
        }

        // ---- [GcDiag] Tick 内分配采样字段（见 Tick 内注释，排查完成后整体删除）----
        private long gcTickWindowBytes;
        private long gcTickMaxFrame;
        private float gcTickReportTimer;

        /// <summary>
        /// 任务分配主循环：遍历空闲 Worker，按优先级分配任务。
        /// 时间配额切片：每帧累计 <see cref="WorkersPerSecond"/> × deltaTime 个配额，
        /// 整数部分即本帧处理预算（60fps = 每帧 2 个），从 <see cref="assignmentCursor"/>
        /// 起处理后推进游标、到尾回卷。一轮耗时 = count / WorkersPerSecond 秒，与帧率解耦；
        /// 卡顿尖帧的超额配额被 <see cref="MaxWorkersPerFrame"/> 截断（不补偿性还债，防再现单帧尖峰）。
        /// Worker 列表增减导致游标错位时无害（个别 Worker 最多多等一轮）。
        /// </summary>
        private void RunTaskAssignmentLoop(float deltaTime)
        {
            // 队列空（常态）时整体退出，避免空闲 Worker 每帧白跑全员循环
            if (this.taskQueue.TotalCount == 0)
            {
                this.assignmentCursor = 0;
                this.assignmentQuota = 0f;
                return;
            }

            // 帧率无关的时间配额：每秒吞吐恒定，帧预算随 dt 缩放（高帧率下小数累计，
            // 会出现 0 预算帧——配额仍在累计，下一帧补足，行为正确）
            this.assignmentQuota += WorkersPerSecond * deltaTime;
            int budget = (int)this.assignmentQuota;
            this.assignmentQuota -= budget;
            if (budget <= 0)
            {
                return;
            }

            // 卡顿尖帧（dt 异常大）截断超额预算：不把欠的债一次还清，防补偿性单帧尖峰
            if (budget > MaxWorkersPerFrame)
            {
                budget = MaxWorkersPerFrame;
                this.assignmentQuota = 0f;
            }

            List<AWorker> workers = WorkerListProvider();
            int count = workers.Count;
            if (count == 0)
            {
                return;
            }

            // 列表缩短时游标可能越界，先归位（该帧不处理，下一帧从头开始）
            if (this.assignmentCursor >= count)
            {
                this.assignmentCursor = 0;
            }

            int end = System.Math.Min(this.assignmentCursor + budget, count);

            bool anyAssigned = false;
            for (int i = this.assignmentCursor; i < end; i++)
            {
                AWorker worker = workers[i];
                if (worker.IsDialoguePaused)
                {
                    continue;
                }

                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData.Task != null)
                {
                    continue;
                }

                GameVector2 workerPos = WorkerPositionProvider(worker);
                for (int priority = 0; priority < this.taskQueue.PriorityCount; priority++)
                {
                    if (this.taskQueue.GetCount(priority) == 0)
                    {
                        continue;
                    }

                    if (this.TrySelectNearestAssignable(worker, workerPos, priority, out AWorkerTask closedTask))
                    {
                        worker.SetTask(closedTask, WorkerTaskSource.PushAssignment);
                        if (workerData.Task == closedTask)
                        {
                            this.taskQueue.MarkRunning(closedTask);
                        }

                        anyAssigned = true;

                        // [TaskDiag] 记录任务分配：谁接到了什么任务（任务调度主循环）
                        AWorkerTask.LogProvider(
                            $"[TaskDiag] {worker.name} 分配任务 type={closedTask.TaskType} target=({closedTask.TargetMap.X},{closedTask.TargetMap.Y}) pri={priority}",
                            LogManager.LogLevelEnum.Debug);
                        break;
                    }
                }
            }

            // 游标推进，到尾回卷
            this.assignmentCursor = end >= count ? 0 : end;

            // 队列变化事件节流：切片后分配分散到各帧，按 ≥0.25s 节流（帧率无关，对齐
            // LogProviderThrottled 的秒计惯例）保持原「一轮最多一两次」的合并语义——事件携带的
            // 是全量 TaskInfo 快照，少发不影响最终一致，消费者用最新即可，防止分发窗口期每帧一发。
            if (anyAssigned && Time.time - this.lastQueueChangedEventTime >= 0.25f)
            {
                this.lastQueueChangedEventTime = Time.time;
                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
            }
        }

        /// <summary>分配循环吞吐（Worker 数/秒）——帧率无关的切片配额，60fps 时即每帧 2 个。</summary>
        private const float WorkersPerSecond = 120f;

        /// <summary>单帧处理预算上界——截断卡顿尖帧的超额配额（&lt;12fps 才触顶，防补偿性单帧尖峰）。</summary>
        private const int MaxWorkersPerFrame = 10;

        /// <summary>分配循环的累计待处理配额（每帧累加 WorkersPerSecond × deltaTime，整数部分为本帧预算）。</summary>
        private float assignmentQuota;

        /// <summary>分配循环的 Worker 列表游标（每帧按预算推进，到尾回卷）。</summary>
        private int assignmentCursor;

        /// <summary>队列变化事件上次发布的时间（秒计节流，见 RunTaskAssignmentLoop 尾部）。</summary>
        private float lastQueueChangedEventTime = -1f;

        /// <summary>
        /// 在指定优先级中直选距离最近的可分配任务（内联替代「快照列表 + SelectTask」）。
        /// 原实现每 15 帧 × 每空闲 Worker × 每优先级都为每个任务构建闭包委托与快照对象（持续 GC 大户）；
        /// 此版本零分配，选择语义与快照版一致：跳过运行中/冷却中/不可做的任务，取 SqrDistance 最近，
        /// 平局取字典遍历序靠前者。
        /// </summary>
        private bool TrySelectNearestAssignable(AWorker worker, GameVector2 workerPos, int priority, out AWorkerTask selected)
        {
            selected = null;
            float minDistance = 0f;
            foreach (KeyValuePair<AWorkerTask, bool> taskPair in this.taskQueue.GetTasksAtPriorityForIteration(priority))
            {
                if (taskPair.Value)
                {
                    continue; // 已被接取（Running）
                }

                AWorkerTask task = taskPair.Key;
                if (task.IsInCooldown || !task.IsCanWork(worker))
                {
                    continue;
                }

                float distance = workerPos.SqrDistanceTo(new GameVector2(task.TargetMap.X, task.TargetMap.Y));
                if (selected == null || distance < minDistance)
                {
                    selected = task;
                    minDistance = distance;
                }
            }

            return selected != null;
        }

        /// <summary>
        /// 尝试将玩家悬赏任务（优先级 0）分配给指定 Worker。
        /// WorkerSeekState 在自主决策前调用，确保玩家任务优先于 Worker 自我任务。
        /// </summary>
        /// <param name="worker">目标 Worker</param>
        /// <returns>成功分配返回 true</returns>
        public bool TryAssignPlayerTask(AWorker worker, WorkerTaskSource source = WorkerTaskSource.SelfDecision)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null || workerData.Task != null)
            {
                return false;
            }

            if (this.taskQueue.GetCount(WorkerTaskPriority.PlayerBounty) == 0
                || !this.TrySelectNearestAssignable(
                    worker, WorkerPositionProvider(worker), WorkerTaskPriority.PlayerBounty, out AWorkerTask task))
            {
                return false;
            }

            worker.SetTask(task, source);
            if (workerData.Task == task)
            {
                this.taskQueue.MarkRunning(task);
            }

            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });

            // [TaskDiag] 记录玩家悬赏分配
            AWorkerTask.LogProvider(
                $"[TaskDiag] {worker.name} 分配玩家悬赏 type={task.TaskType} target=({task.TargetMap.X},{task.TargetMap.Y})",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// 非破坏性检查：是否存在当前可分配给该 Worker 的玩家悬赏（优先级 0）。
        /// 供心智层体验门使用——决定是否给出拒绝/拖延反馈，避免对不存在的命令空拒绝。
        /// 与 TryAssignPlayerTask 共享快照/选择逻辑，但不做任何分配。
        /// </summary>
        public bool HasAssignablePlayerTask(AWorker worker)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null || workerData.Task != null)
            {
                return false;
            }

            return this.taskQueue.GetCount(WorkerTaskPriority.PlayerBounty) > 0
                && this.TrySelectNearestAssignable(
                    worker, WorkerPositionProvider(worker), WorkerTaskPriority.PlayerBounty, out _);
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
        public bool TryAssignGlobalTask(AWorker worker, WorkerTaskSource source = WorkerTaskSource.SelfDecision)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null || workerData.Task != null)
            {
                return false;
            }

            GameVector2 workerPos = WorkerPositionProvider(worker);

            // 从 WorkerBounty(1) 开始，跳过已处理的 PlayerBounty(0) 和自用的 Idle(3)
            for (int priority = WorkerTaskPriority.WorkerBounty; priority < WorkerTaskPriority.Idle; priority++)
            {
                if (this.taskQueue.GetCount(priority) == 0
                    || !this.TrySelectNearestAssignable(worker, workerPos, priority, out AWorkerTask task))
                {
                    continue;
                }

                worker.SetTask(task, source);
                if (workerData.Task == task)
                {
                    this.taskQueue.MarkRunning(task);
                }

                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });

                // [TaskDiag] 记录全局任务分配
                AWorkerTask.LogProvider(
                    $"[TaskDiag] {worker.name} 分配全局任务 type={task.TaskType} target=({task.TargetMap.X},{task.TargetMap.Y}) pri={priority}",
                    LogManager.LogLevelEnum.Debug);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清理过期的悬赏任务。
        /// 遍历队列中所有 WorkerBountyTask，检查其过期时间，
        /// 对已过期的任务退款给发布者并从队列移除。
        /// </summary>
        private void ExpireBountyTasks()
        {
            if (this.taskQueue.TotalCount == 0)
            {
                return;
            }

            float currentGameTime = Core.ServiceLocator.Get<IGameTime>().Time;

            // 懒初始化（常态无过期任务时不分配）
            List<AWorkerTask> expiredTasks = null;

            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriorityForIteration(p))
                {
                    if (pair.Key is WorkerBountyTask bountyTask
                        && bountyTask.BountyInfo.IsExpired(currentGameTime)
                        && !pair.Value) // 只在未被接取（Posted 状态）时过期
                    {
                        (expiredTasks ?? (expiredTasks = new List<AWorkerTask>())).Add(pair.Key);
                    }
                }
            }

            if (expiredTasks == null)
            {
                return;
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
        /// 清理指定阵营所有「未被接取」的阵营任务（FactionManager 日更生成前调用，
        /// 防旧任务堆积）。未接取任务的 GatherMap 认领由任务自身释放；
        /// 已接取（运行中）的任务不动 — 执行者的正常完成/放弃路径自行收尾。
        /// </summary>
        /// <param name="factionId">阵营 ID。</param>
        public void RemoveFactionQuestTasks(int factionId)
        {
            if (this.taskQueue.TotalCount == 0)
            {
                return;
            }

            List<AWorkerTask> staleTasks = null;
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriorityForIteration(p))
                {
                    if (pair.Key is WorkerFactionQuestTask quest
                        && quest.QuestFactionId == factionId
                        && !pair.Value) // 只清未被接取的
                    {
                        (staleTasks ?? (staleTasks = new List<AWorkerTask>())).Add(pair.Key);
                    }
                }
            }

            if (staleTasks == null)
            {
                return;
            }

            foreach (AWorkerTask task in staleTasks)
            {
                ((WorkerFactionQuestTask)task).ReleaseUnclaimed();
                this.taskQueue.Remove(task);
            }

            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
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
            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });

            // [TaskDiag] 记录任务创建/入队（含目标坐标与优先级）
            AWorkerTask.LogProvider(
                $"[TaskDiag] 创建任务 type={task.TaskType} target=({taskPosMap.X},{taskPosMap.Y}) pri={prior}",
                LogManager.LogLevelEnum.Debug);
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

            // [TaskDiag] 队列认领释放（Gather 移除队列释放认领锁，其他类型标记空闲可重试）：
            // 释放随任务失败/完成高频发生（100 Worker 下常态 20 次/秒、6,932 条/6 分钟），
            // 改全局节流 2s + Trace 采样——释放本身无诊断价值，异常方向是"同一任务反复
            // 释放重试"，节流采样配合 SetTask/开始任务 链路足够定位（bug-fixes.md 2026-09-06）。
            AWorkerTask.LogProviderThrottled(
                "QueueRelease", 2f,
                () =>
                {
                    string releaseAction = task.TaskType == WorkerTaskType.Gather ? "移除队列(Gather认领)" : "标记Idle可重试";
                    return $"[TaskDiag] 队列释放 type={task.TaskType} target=({task.TargetMap.X},{task.TargetMap.Y}) {releaseAction}";
                },
                LogManager.LogLevelEnum.Trace);

            EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
        }

        /// <summary>
        /// 从任务队列中永久移除任务（不可达目标等致命失败场景使用）。
        /// 与 GiveUpTask 的区别：GiveUpTask 对非 Gather 任务仅标记 Idle 允许重试，
        /// 此方法直接删除任务，阻止任何 Worker 再次接取。
        /// </summary>
        /// <param name="task">要移除的任务</param>
        public void RemoveTask(AWorkerTask task)
        {
            if (task == null)
            {
                return;
            }

            if (task.TaskType == WorkerTaskType.Gather)
            {
                Core.ServiceLocator.Get<GatherMap>().CancelGather(
                    Vector3IntLAB.ToVector3Int(task.TargetMap));
            }

            this.taskQueue.Remove(task);
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

            // 反转索引（见字段区注释）：100 Worker 建房期 ~2400 未完成格 × (全队列扫描 +
            // 100 Worker 携带检查 + 100 Worker × ~30 偏移比对) ≈ 数百万次比较，全部落在
            // 每 300 帧一次的单帧里 = 「WorkerTaskManager.Update 每 300 帧耗时」尖峰
            // （2026-09-06 Profiler）。改为一次构建三个集合（O(队列 + Worker + 房间格)），
            // 逐格 O(1) 查——三层跳过语义与原嵌套遍历完全等价。
            this.verifyQueuedBuildPos.Clear();
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (var kv in this.taskQueue.GetTasksAtPriorityForIteration(p))
                {
                    if (kv.Key.TaskType == WorkerTaskType.Build)
                    {
                        this.verifyQueuedBuildPos.Add(new Vector2Int(kv.Key.TargetMap.X, kv.Key.TargetMap.Y));
                    }
                }
            }

            List<AWorker> workers = WorkerListProvider();
            this.verifyCarryingBuildPos.Clear();
            this.verifyPlannedRoomPos.Clear();
            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd?.Task != null && wd.Task.TaskType == WorkerTaskType.Build)
                {
                    this.verifyCarryingBuildPos.Add(new Vector2Int(wd.Task.TargetMap.X, wd.Task.TargetMap.Y));
                }

                // 规划房间认领条件与原 IsPartOfWorkerPlannedRoom 一致：有规划、参数已生成、未建完
                if (wd?.PlannedHomePosition == null || wd.HomeRoomWidth == 0)
                {
                    continue;
                }

                var layout = LAB2D.AI.Worker.WorkerHomeLayout.GetRoomLayout(wd);
                if (wd.HomeBuildStage >= layout.CompleteStage)
                {
                    continue;
                }

                Vector3Int center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
                for (int i = 0; i < layout.WallCount; i++)
                {
                    Vector3Int cell = center + layout.WallOffsets[i];
                    this.verifyPlannedRoomPos.Add(new Vector2Int(cell.x, cell.y));
                }

                Vector3Int door = center + layout.DoorOffset;
                this.verifyPlannedRoomPos.Add(new Vector2Int(door.x, door.y));
                Vector3Int bed = center + layout.BedOffset;
                this.verifyPlannedRoomPos.Add(new Vector2Int(bed.x, bed.y));
                Vector3Int bedSecond = center + layout.BedSecondOffset;
                this.verifyPlannedRoomPos.Add(new Vector2Int(bedSecond.x, bedSecond.y));
                foreach (var so in layout.StorageOffsets)
                {
                    Vector3Int cell = center + so;
                    this.verifyPlannedRoomPos.Add(new Vector2Int(cell.x, cell.y));
                }
            }

            foreach (var kv in buildMap.BuildMapDataLAB.PosMap)
            {
                BuildMap.BuildTileData tileData = kv.Value;
                if (tileData.IsComplete)
                {
                    continue;
                }

                Vector3Int pos = Vector3IntLAB.ToVector3Int(kv.Key);

                // 三层跳过：已有任务 / 已有 Worker 携带（Self-Build）/ 属规划房间 → O(1) 集合查
                Vector2Int posXY = new Vector2Int(pos.x, pos.y);
                if (this.verifyQueuedBuildPos.Contains(posXY)
                    || this.verifyCarryingBuildPos.Contains(posXY)
                    || this.verifyPlannedRoomPos.Contains(posXY))
                {
                    continue;
                }

                // 无任务覆盖 → 重新创建建造任务
                Dictionary<int, ResourceInfo> buildCosts = BuildMap.GetBuildCost(tileData.Name);
                if (buildCosts == null || buildCosts.Count == 0)
                {
                    continue;
                }

                // 找回原建造者（如果 BuilderName 有记录）——手写循环替代 Find 闭包，避免每次捕获分配
                AWorker owner = null;
                if (!string.IsNullOrEmpty(tileData.BuilderName))
                {
                    for (int w = 0; w < workers.Count; w++)
                    {
                        if (workers[w].name == tileData.BuilderName)
                        {
                            owner = workers[w];
                            break;
                        }
                    }
                }

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
                        owner.SetTask(newTask, WorkerTaskSource.PushAssignment);
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
        /// 删除队列中所有与指定 Worker 相关的任务。
        /// Worker 死亡时调用，清理：悬赏发布、专属任务（Wear/Sleep/Exercise）、
        /// PickUp 目标所有、Carry(ToBoard) 指定执行者。
        /// 同时退款所有待接取悬赏的托管资金。
        /// </summary>
        /// <param name="workerInstanceId">Worker 的 GameObject instance ID。</param>
        public void RemoveTasksForWorker(int workerInstanceId)
        {
            if (workerInstanceId == 0)
            {
                return;
            }

            // 先收集并退款所有待接取悬赏的托管资金
            var cm = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
            // 尝试标准退款（Worker GameObject 可能还存活）
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (var pair in this.taskQueue.GetTasksAtPriorityForIteration(p))
                {
                    if (pair.Key is WorkerBountyTask bounty
                        && bounty.OwnerWorkerId == workerInstanceId
                        && bounty.BountyInfo.State == BountyState.Posted)
                    {
                        try
                        {
                            cm.RefundBounty(bounty.BountyInfo.IssuerWorkerId, bounty.BountyInfo.Reward);
                        }
                        catch (System.Exception e)
                        {
                            AWorkerTask.LogProvider(
                                $"死亡Worker[{workerInstanceId}]悬赏退款失败: {e.Message}",
                                LogManager.LogLevelEnum.Error);
                        }
                    }
                }
            }

            // 释放剩余托管余额（RefundBounty 未处理的，例如已执行中的悬赏）
            cm.ReleaseDeadIssuerEscrow(workerInstanceId);

            bool removed = this.taskQueue.RemoveWhere(task => task.OwnerWorkerId == workerInstanceId);
            if (removed)
            {
                EventBusPublishProvider(new WorkerTaskQueueChangedEvent { TaskInfo = this.GetTaskInfo() });
            }
        }

        /// <summary>
        /// 检查是否存在距离可接受的待接取悬赏（用于 Worker 决策时过滤过远悬赏）。
        /// </summary>
        /// <param name="workerPos">Worker 地图坐标。</param>
        /// <param name="maxDistance">最大接受距离（格）。</param>
        /// <returns>存在可用附近悬赏返回 true。</returns>
        public bool HasNearbyBounty(Vector3IntLAB workerPos, float maxDistance)
        {
            Vector3 workerWorldPos = new Vector3(workerPos.X, workerPos.Y, 0);
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (var pair in this.taskQueue.GetTasksAtPriorityForIteration(p))
                {
                    if (pair.Key is WorkerBountyTask bounty
                        && bounty.BountyInfo.State == BountyState.Posted)
                    {
                        Vector3 taskWorldPos = new Vector3(
                            bounty.TargetMap.X, bounty.TargetMap.Y, 0);
                        float distance = Vector3.Distance(workerWorldPos, taskWorldPos);
                        if (distance <= maxDistance)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 任务总数（Σ 各优先级字典 Count，含进行中）——轻量计数（优先级数量级遍历、零分配），
        /// 供决策热路径（WorkerFeatureExtractor.GetTaskPressure，~40 次/秒）替代
        /// CreateTaskQueueSnapshot 全套快照字典构建（语义一致，见 BuildSnapshot 的 totalTaskCount）。
        /// </summary>
        public int TotalTaskCount => this.taskQueue.TotalCount;

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
                result.Add(new Dictionary<AWorkerTask, bool>(this.taskQueue.GetTasksAtPriorityForIteration(i)));
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
                foreach (var kv in this.taskQueue.GetTasksAtPriorityForIteration(p))
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
            int typeCount = (int)WorkerTaskType._Count;

            // 复用计数缓冲与 StringBuilder（高频路径：每次任务增删/分配都会调用；
            // 原实现每次 new int[] + res += 循环拼接产生约 10 个中间字符串）
            if (this.taskInfoCounts == null || this.taskInfoCounts.Length < typeCount)
            {
                this.taskInfoCounts = new int[typeCount];
            }
            else
            {
                Array.Clear(this.taskInfoCounts, 0, typeCount);
            }

            for (int i = 0; i < this.taskQueue.PriorityCount; i++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriorityForIteration(i))
                {
                    if (pair.Value)
                    {
                        int typeIndex = (int)pair.Key.TaskType;
                        if (typeIndex >= 0 && typeIndex < typeCount)
                        {
                            this.taskInfoCounts[typeIndex]++;
                        }
                    }
                }
            }

            StringBuilder builder = this.taskInfoBuilder ?? (this.taskInfoBuilder = new StringBuilder(256));
            builder.Length = 0;
            builder.Append("任务总数量: ").Append(this.taskQueue.TotalCount).Append('\n');
            for (int i = 0; i < typeCount; i++)
            {
                builder.Append(TaskTypeNames[i]).Append(':').Append(this.taskInfoCounts[i]).Append('\n');
            }

            return builder.ToString();
        }

        /// <summary>GetTaskInfo 的复用缓冲（避免每次调用分配数组与 StringBuilder）。</summary>
        private int[] taskInfoCounts;
        private StringBuilder taskInfoBuilder;

        /// <summary>任务类型名静态缓存（避免每次 GetTaskInfo 对枚举 ToString 的分配）。</summary>
        private static readonly string[] TaskTypeNames = BuildTaskTypeNames();

        private static string[] BuildTaskTypeNames()
        {
            var names = new string[(int)WorkerTaskType._Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = ((WorkerTaskType)i).ToString();
            }

            return names;
        }

        /// <summary>
        /// 检查队列中是否已存在同位置同类型的任务（用于 OnePerPosition 去重）。
        /// </summary>
        private bool HasTaskAtPosition(AWorkerTask task)
        {
            for (int p = 0; p < this.taskQueue.PriorityCount; p++)
            {
                foreach (KeyValuePair<AWorkerTask, bool> pair in this.taskQueue.GetTasksAtPriorityForIteration(p))
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
