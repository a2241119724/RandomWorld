namespace LAB2D.Core.Seek
{
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using PimDeWitte.UnityMainThreadDispatcher;
    using UnityEngine;

    /// <summary>
    /// 寻路基类。主线程提交请求，固定数量的后台工作线程执行纯数据搜索。
    /// </summary>
    public class ASeek : ISeek
    {
        /// <summary>
        /// 动态并发数：基于 Worker 数量自动调节，最少 2 个，最多 16 个。
        /// 约每 10 个 Worker 分配 1 个额外线程。
        /// </summary>
        private static int MaxConcurrentSearches
        {
            get
            {
                int workerCount = Core.GameServices.WorkerCountProvider?.Invoke() ?? 0;
                return Math.Clamp(Math.Max(2, (workerCount / 10) + 2), 2, 16);
            }
        }

        private const float FailCacheTtl = 30f;
        private static readonly Dictionary<Vector3Int, float> FailCache = new ();
        private static readonly Queue<ASeek> SearchQueue = new (64);
        private static readonly object SearchQueueLock = new ();
        private static readonly WaitCallback SearchWorkerCallback = RunSearchQueue;
        private static int activeSearchWorkers;
        private static Material sharedLineMaterial;
        private static bool quitHookRegistered;

        /// <summary>
        /// 全局关闭标志 — 应用退出时置为 true，阻止新寻路请求入队，
        /// 并让正在运行的后台线程快速退出。
        /// </summary>
        protected static volatile bool isShuttingDown;

        protected static TileMap s_tileMap;
        protected static ResourceMap s_resourceMap;
        protected static BuildMap s_buildMap;
        protected static WeatherGameplayEffect s_weatherEffect;
        protected static ITerrainEffectService s_terrainEffect;
        protected static WorkerConditionManager s_workerConditionManager;
        protected static UnityMainThreadDispatcher s_mainThreadDispatcher;

        private readonly bool isWorker;
        private readonly bool isEnemy;
        private readonly object requestLock = new ();
        private bool requestScheduled;
        private int pendingGeneration;
        private Vector3Int pendingStartMap;
        private Vector3Int pendingTargetMap;
        private volatile SeekResult currentResult;
        private volatile bool isSeeking;
        private volatile int seekGeneration;
        protected volatile bool isStopThread;

        public ASeek(LAB2D.Character.Character character)
        {
            InitServiceCache();
            EnsurePathfindingStorage();

            this.LineRenderer = character.GetComponent<LineRenderer>();
            this.LineRenderer.startWidth = 0.05f;
            this.LineRenderer.endWidth = 0.05f;
            if (sharedLineMaterial == null)
            {
                sharedLineMaterial = new Material(Shader.Find("Unlit/Color"));
            }

            Material instanceMaterial = new (sharedLineMaterial);
            instanceMaterial.color = new Color(
                UnityEngine.Random.Range(0.5f, 1.0f),
                UnityEngine.Random.Range(0.5f, 1.0f),
                UnityEngine.Random.Range(0.5f, 1.0f));
            this.LineRenderer.material = instanceMaterial;
            this.LineRenderer.sortingLayerName = "Highest";

            this.Character = character;
            this.isWorker = character is AWorker;
            this.isEnemy = character is LAB2D.Character.Enemy.AEnemy;
        }

        public Vector3Int TargetMap { get; protected set; }

        public float SeekProgress { get; protected set; }

        public static bool ShowWorkerLine { get; set; }

        public static bool ShowEnemyLine { get; set; }

        public LineRenderer LineRenderer { get; set; }

        public Vector3 Direction { get; private set; }

        /// <summary>
        /// 每秒位移卡死检测器 — 纯逻辑、无 Unity 组件依赖（仅用 UnityEngine.Vector3），
        /// 随 ASeek 生命周期存在。
        /// </summary>
        private readonly LAB2D.MovementStuckDetector stuckDetector = new LAB2D.MovementStuckDetector();

        /// <summary>
        /// 最近一次卡死检测结果（由 MoveByPath 每固定帧更新）。
        /// </summary>
        public LAB2D.BugCheckResult LastStuckResult { get; private set; } = LAB2D.BugCheckResult.None;

        protected LAB2D.Character.Character Character { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Application.quitting -= Shutdown;
            isShuttingDown = false;
            quitHookRegistered = false;
            s_tileMap = null;
            s_resourceMap = null;
            s_buildMap = null;
            s_weatherEffect = null;
            s_terrainEffect = null;
            s_workerConditionManager = null;
            s_mainThreadDispatcher = null;
            sharedLineMaterial = null;

            lock (SearchQueueLock)
            {
                SearchQueue.Clear();
                activeSearchWorkers = 0;
            }

            FailCache.Clear();
            PathfindingWorkspacePool.Clear();
            WalkabilityCache.Clear();
        }

        protected static int CalculateMaxIterations(int width, int height)
        {
            return Math.Max(1, Math.Min(10000, checked(width * height) / 3));
        }

        /// <summary>
        /// 是否可以抵达，不包含资源和不可通行建筑。
        /// 该方法访问 Unity Tilemap，只能在主线程调用。
        /// </summary>
        public static bool IsCanReach(Vector3Int posMap)
        {
            return s_tileMap.IsCanReach(posMap)
                && s_resourceMap.IsCanReach(posMap)
                && s_buildMap.IsCanReach(posMap);
        }

        /// <summary>
        /// 角色感知的通行检查 — 在静态 IsCanReach 基础上增加房间访问权限控制。
        /// Enemy 不能进入 Worker 房间；Worker 不能进入其他 Worker 的房间。
        /// </summary>
        /// <param name="posMap">目标地图坐标。</param>
        /// <returns>该角色是否可以到达该位置。</returns>
        public bool CanCharacterReach(Vector3Int posMap)
        {
            if (!IsCanReach(posMap)) return false;

            try
            {
                var roomManager = Core.ServiceLocator.Get<Item.RoomManager>();
                if (roomManager != null && !roomManager.CanCharacterEnter(posMap, this.Character))
                {
                    return false;
                }
            }
            catch { /* RoomManager 未注册时允许通行 */ }

            return true;
        }

        public static void RecordFail(Vector3Int targetMap)
        {
            FailCache[targetMap] = Time.time;
        }

        public static bool IsRecentFail(Vector3Int targetMap)
        {
            if (FailCache.TryGetValue(targetMap, out float recordTime))
            {
                if (Time.time - recordTime < FailCacheTtl)
                {
                    return true;
                }

                FailCache.Remove(targetMap);
            }

            return false;
        }

        public static void CleanFailCache()
        {
            float now = Time.time;
            List<Vector3Int> expired = null;
            foreach (KeyValuePair<Vector3Int, float> entry in FailCache)
            {
                if (now - entry.Value <= FailCacheTtl)
                {
                    continue;
                }

                expired ??= new List<Vector3Int>();
                expired.Add(entry.Key);
            }

            if (expired == null)
            {
                return;
            }

            foreach (Vector3Int key in expired)
            {
                FailCache.Remove(key);
            }
        }

        /// <summary>
        /// 应用退出时调用，停止所有后台寻路线程并清空队列。
        /// 防止 ThreadPool 线程在 Unity 销毁 GameObject 后仍访问静态状态导致卡死。
        /// 幂等：多次调用安全。
        /// </summary>
        public static void Shutdown()
        {
            if (isShuttingDown)
            {
                return;
            }

            isShuttingDown = true;

            lock (SearchQueueLock)
            {
                SearchQueue.Clear();
                activeSearchWorkers = 0;
            }

            FailCache.Clear();
            PathfindingWorkspacePool.Clear();
            WalkabilityCache.Clear();

            if (sharedLineMaterial != null)
            {
                UnityEngine.Object.Destroy(sharedLineMaterial);
                sharedLineMaterial = null;
            }
        }

        public void Seek(Vector3Int targetMap)
        {
            // 应用正在关闭，拒绝新的寻路请求
            if (isShuttingDown)
            {
                return;
            }

            if (this.IsSeeking())
            {
                AWorkerTask.LogProvider(this.Character.name + ":重新寻路!", LogManager.LogLevelEnum.Trace);
            }

            // Unity Tilemap 查询只在首次构建时运行，后续寻路直接复用快照。
            EnsurePathfindingStorage();
            WalkabilityCache.EnsureBuilt();

            Vector3Int startMap = s_tileMap.WorldPosToMapPos(this.Character.transform.position);
            this.TargetMap = targetMap;
            this.RestartStuckWindow(); // 新路径 → 新窗口，但保留连续卡住计数（重新寻路不赦免）
            int generation = this.StartSeek();
            bool enqueue;
            lock (this.requestLock)
            {
                this.pendingGeneration = generation;
                this.pendingStartMap = startMap;
                this.pendingTargetMap = targetMap;
                enqueue = !this.requestScheduled;
                this.requestScheduled = true;
            }

            if (enqueue)
            {
                EnqueueSearch(this);
            }
        }

        public void StopMove()
        {
            lock (this.requestLock)
            {
                this.isStopThread = true;
                this.seekGeneration++;
                this.isSeeking = false;
                this.currentResult = null;
            }

            this.LineRenderer.positionCount = 0;

            this.RestartStuckWindow(); // 停止移动 → 保留连续卡住计数（重新寻路不赦免）
        }

        /// <summary>
        /// 重启卡死窗口（保留连续卡住计数），用于重新寻路/停止移动。
        /// </summary>
        private void RestartStuckWindow()
        {
            this.stuckDetector.RestartWindow();
            this.LastStuckResult = LAB2D.BugCheckResult.None;
        }

        /// <summary>
        /// 到达终点：停止寻路并完全清空卡死状态（含连续卡住计数）。
        /// </summary>
        private void CompleteMovement()
        {
            this.StopMove(); // 内部 RestartWindow（保留计数）
            this.stuckDetector.Reset(); // 真正到达 → 清空计数
            this.LastStuckResult = LAB2D.BugCheckResult.None;
        }

        /// <summary>
        /// 完全清空卡死状态（放弃任务时调用，避免污染下一任务）。
        /// </summary>
        public void ResetStuckDetection()
        {
            this.stuckDetector.Reset();
            this.LastStuckResult = LAB2D.BugCheckResult.None;
        }

        /// <summary>
        /// 根据已发布的路径移动。路径使用游标推进，避免 RemoveAt(0) 搬移元素。
        /// </summary>
        public bool MoveByPath()
        {
            SeekResult result = this.currentResult;
            if (result == null)
            {
                this.RestartStuckWindow(); // 寻路间隙（重新寻路进行中）→ 保留卡住计数
                return true;
            }

            if (result.PathIndex >= result.Path.Count)
            {
                this.CompleteMovement(); // 到达终点 → 完全清空
                return true;
            }

            Vector3 worldPos = s_tileMap.MapPosToWorldPos(result.Path[result.PathIndex]);
            Vector3 characterPosition = this.Character.transform.position;
            if (Math.Abs(worldPos.x - characterPosition.x) < 0.1f
                && Math.Abs(worldPos.y - characterPosition.y) < 0.1f)
            {
                result.PathIndex++;
                if (result.PathIndex >= result.Path.Count)
                {
                    this.CompleteMovement(); // 到达终点 → 完全清空
                    return true;
                }

                worldPos = s_tileMap.MapPosToWorldPos(result.Path[result.PathIndex]);
            }

            this.Direction = worldPos - this.Character.transform.position;
            float speed = s_weatherEffect.GetAdjustedCharacterMoveSpeed(this.Character, this.Character.MoveSpeed);
            speed *= s_terrainEffect.GetMoveSpeedMultiplier(this.Character);

            if (this.isWorker)
            {
                speed = s_workerConditionManager.GetAdjustedWorkerMoveSpeed((AWorker)this.Character, speed);
            }

            // 卡死检测：必须在 Translate 之前，用物理结算后的真实位置喂入，
            // 否则会测出 Translate 穿透墙体的"假位移"，永远测不出卡死。
            this.LastStuckResult = this.stuckDetector.Feed(Time.fixedDeltaTime, characterPosition, speed);

            this.Character.transform.Translate(speed * Time.fixedDeltaTime * this.Direction.normalized, Space.World);
            if (this.ShouldShowLine())
            {
                this.UpdateLine(true);
            }

            return false;
        }

        public bool IsSeeking()
        {
            return this.isSeeking;
        }

        public bool IsHavePath()
        {
            SeekResult result = this.currentResult;
            return result != null && result.IsReachable;
        }

        protected bool ShouldStop(int generation)
        {
            return this.isStopThread || generation != this.seekGeneration || isShuttingDown;
        }

        protected bool TrySetResult(SeekResult result, int generation)
        {
            lock (this.requestLock)
            {
                if (generation != this.seekGeneration)
                {
                    return false;
                }

                this.currentResult = result;
                this.isSeeking = false;
                return true;
            }
        }

        protected virtual void DoSeek(
            int generation,
            Vector3Int startMap,
            Vector3Int targetMap,
            PathfindingWorkspace workspace)
        {
            throw new NotImplementedException();
        }

        protected void UpdateLine(bool isFirst = false)
        {
            if (this.LineRenderer == null)
            {
                return;
            }

            if (!this.ShouldShowLine())
            {
                this.LineRenderer.positionCount = 0;
                return;
            }

            SeekResult result = this.currentResult;
            if (result == null || !result.IsReachable || result.PathIndex >= result.Path.Count)
            {
                this.LineRenderer.positionCount = 0;
                return;
            }

            int remainingCount = result.Path.Count - result.PathIndex;
            this.LineRenderer.positionCount = remainingCount + 1;
            this.LineRenderer.SetPosition(remainingCount, this.Character.transform.position);
            if (isFirst)
            {
                return;
            }

            for (int i = 0; i < remainingCount; i++)
            {
                this.LineRenderer.SetPosition(
                    remainingCount - i - 1,
                    s_tileMap.MapPosToWorldPos(result.Path[result.PathIndex + i]));
            }
        }

        private static void InitServiceCache()
        {
            if (s_tileMap != null)
            {
                return;
            }

            s_tileMap = Core.ServiceLocator.Get<TileMap>();
            s_resourceMap = Core.ServiceLocator.Get<ResourceMap>();
            s_buildMap = Core.ServiceLocator.Get<BuildMap>();
            s_weatherEffect = Core.ServiceLocator.Get<WeatherGameplayEffect>();
            s_terrainEffect = Core.ServiceLocator.Get<ITerrainEffectService>();
            s_workerConditionManager = Core.ServiceLocator.Get<WorkerConditionManager>();
            s_mainThreadDispatcher = Core.ServiceLocator.Get<UnityMainThreadDispatcher>();

            if (!quitHookRegistered)
            {
                Application.quitting -= Shutdown;
                Application.quitting += Shutdown;
                quitHookRegistered = true;
            }
        }

        private static void EnsurePathfindingStorage()
        {
            var tileMapData = s_tileMap.TileMapDataLAB;
            int gridWidth = tileMapData.MapTiles.GetLength(0);
            int gridHeight = tileMapData.MapTiles.GetLength(1);
            int maxIterations = CalculateMaxIterations(gridWidth, gridHeight);
            PathfindingWorkspacePool.Initialize(gridWidth, gridHeight, maxIterations);
            WalkabilityCache.Initialize(gridWidth, gridHeight, s_tileMap.GetInstanceID());
        }

        private static void EnqueueSearch(ASeek seek)
        {
            // 应用正在关闭，不再入队新搜索
            if (isShuttingDown)
            {
                return;
            }

            bool startWorker = false;
            lock (SearchQueueLock)
            {
                if (isShuttingDown)
                {
                    return;
                }

                SearchQueue.Enqueue(seek);
                if (activeSearchWorkers < MaxConcurrentSearches)
                {
                    activeSearchWorkers++;
                    startWorker = true;
                }
            }

            if (startWorker)
            {
                ThreadPool.QueueUserWorkItem(SearchWorkerCallback);
            }
        }

        private static void RunSearchQueue(object _)
        {
            try
            {
                while (!isShuttingDown)
                {
                    ASeek seek;
                    lock (SearchQueueLock)
                    {
                        if (SearchQueue.Count == 0)
                        {
                            return;
                        }

                        seek = SearchQueue.Dequeue();
                    }

                    try
                    {
                        seek.ProcessPendingSearches();
                    }
                    catch (Exception exception)
                    {
                        AWorkerTask.LogProvider($"寻路调度异常: {exception}", LogManager.LogLevelEnum.Error);
                        seek.HandleSchedulerException(exception);
                    }
                }
            }
            finally
            {
                lock (SearchQueueLock)
                {
                    activeSearchWorkers = Math.Max(0, activeSearchWorkers - 1);
                }
            }
        }

        private void HandleSchedulerException(Exception exception)
        {
            lock (this.requestLock)
            {
                if (this.pendingGeneration == this.seekGeneration)
                {
                    this.currentResult = new SeekResult { IsReachable = false };
                    this.isSeeking = false;
                }

                this.requestScheduled = false;
            }

            // 关闭时不尝试回调主线程
            if (!isShuttingDown && s_mainThreadDispatcher != null)
            {
                s_mainThreadDispatcher.EnqueueAsync(() =>
                    AWorkerTask.LogProvider(
                        this.Character.name + ":寻路调度异常 " + exception,
                        LogManager.LogLevelEnum.Error));
            }
        }

        private void ProcessPendingSearches()
        {
            while (!isShuttingDown)
            {
                int generation;
                Vector3Int startMap;
                Vector3Int targetMap;
                lock (this.requestLock)
                {
                    generation = this.pendingGeneration;
                    startMap = this.pendingStartMap;
                    targetMap = this.pendingTargetMap;
                }

                if (generation == this.seekGeneration && this.isSeeking)
                {
                    this.isStopThread = false;
                    PathfindingWorkspace workspace = PathfindingWorkspacePool.Rent();
                    try
                    {
                        this.DoSeek(generation, startMap, targetMap, workspace);
                    }
                    catch (Exception exception)
                    {
                        AWorkerTask.LogProvider($"寻路异常: {exception}", LogManager.LogLevelEnum.Error);
                        this.TrySetResult(new SeekResult { IsReachable = false }, generation);
                        if (!isShuttingDown && s_mainThreadDispatcher != null)
                        {
                            s_mainThreadDispatcher.EnqueueAsync(() =>
                                AWorkerTask.LogProvider(
                                    this.Character.name + ":寻路异常 " + exception,
                                    LogManager.LogLevelEnum.Error));
                        }
                    }
                    finally
                    {
                        PathfindingWorkspacePool.Return(workspace);
                    }

                    if (generation == this.seekGeneration && this.ShouldShowLine())
                    {
                        if (!isShuttingDown && s_mainThreadDispatcher != null)
                        {
                            s_mainThreadDispatcher.EnqueueAsync(() =>
                            {
                                if (generation == this.seekGeneration)
                                {
                                    this.UpdateLine();
                                }
                            });
                        }
                    }
                }

                lock (this.requestLock)
                {
                    if (this.pendingGeneration == generation)
                    {
                        this.requestScheduled = false;
                        return;
                    }
                }
            }
        }

        private int StartSeek()
        {
            int generation;
            lock (this.requestLock)
            {
                this.isStopThread = true;
                generation = ++this.seekGeneration;
                this.SeekProgress = 0.0f;
                this.currentResult = null;
                this.isSeeking = true;
            }

            this.UpdateLine();
            return generation;
        }

        private bool ShouldShowLine()
        {
            return (this.isWorker && ShowWorkerLine) || (this.isEnemy && ShowEnemyLine);
        }

        public sealed class SeekResult
        {
            public bool IsReachable { get; set; } = true;

            public List<Vector3Int> Path { get; } = new (16);

            public int PathIndex { get; set; }

            internal void Reset()
            {
                this.IsReachable = true;
                this.Path.Clear();
                this.PathIndex = 0;
            }
        }
    }
}
