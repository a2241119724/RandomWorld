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
        /// 动态并发数：基于 Worker 数量自动调节，最少 2 个，最多 8 个。
        /// 约每 50 个 Worker 分配 1 个额外线程。
        /// </summary>
        private static int MaxConcurrentSearches
        {
            get
            {
                int workerCount = Core.GameServices.WorkerCountProvider?.Invoke() ?? 0;
                return Math.Clamp(Math.Max(2, (workerCount / 50) + 2), 2, 8);
            }
        }

        private const float FailCacheTtl = 30f;
        private static readonly Dictionary<Vector3Int, float> FailCache = new ();
        private static readonly Queue<ASeek> SearchQueue = new (64);
        private static readonly object SearchQueueLock = new ();
        private static int activeSearchWorkers;
        private static Material sharedLineMaterial;
        private static bool quitHookRegistered;

        // 每帧入队配额：同帧大量 Seek（同波敌人共振/批量任务切换）不再全部直接进全局搜索队列，
        // 超额请求先进 PendingEnqueue，由主线程 dispatcher 每帧最多放行 MaxEnqueuesPerFrame 个，
        // 摊平入队峰值 → 搜索队列长度有界，角色等待寻路结果从「突发积压数秒」变为「持续 ~百毫秒」。
        private const int MaxEnqueuesPerFrame = 8;
        private static readonly Queue<ASeek> PendingEnqueue = new (64);
        private static bool flushScheduled;

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
        protected static ITemperatureEffectService s_temperatureEffect;
        protected static UnityMainThreadDispatcher s_mainThreadDispatcher;

        private readonly bool isWorker;
        private readonly bool isEnemy;
        private readonly Rigidbody2D rb; // 移动目标刚体（MovePosition 物理帧移动 + 渲染插值，消除 FixedUpdate 步进抖动）
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
            this.rb = character.GetComponent<Rigidbody2D>();
            // 俯视角世界无重力：Player/CommonEnemy 用 Rigidbody Constraints 冻结 Y 抵消 gravityScale=1，
            // Worker/SeekEnemy 走 velocity 网格移动（需保留 Y 自由度，不能冻结 Y），故此处运行时清重力。
            // gravity 每 FixedUpdate 污染 velocity（y += g*dt），与 tile 碰撞体接触时与求解器竞争，
            // 产生"碰到东西就抖动/卡顿、隐藏碰撞体即消失"的物理接触抖动（见 bug-fixes.md 2026-08-15）。
            // linearDrag 每帧按比例衰减 velocity，叠加接触约束使实际位移偏离期望，一并清零。
            // freezeRotation（冻结 Z 旋转）：用户实测"把刚体 Z 冻结后抖动消失"——抖动主因是
            // velocity 重设下碰撞接触点的切向力产生扭矩 → 刚体绕 Z 轴旋转 → 接触几何变化 →
            // 求解器位置修正方向反复改变 → 位置跳动/视觉抖动。圆碰撞体虽旋转不变量，但物理
            // 求解器的旋转积分在数值上仍改变接触反馈。俯视角角色方向由移动/视觉控制，无旋转自由度需求，冻结安全。
            if (this.rb != null)
            {
                this.rb.gravityScale = 0f;
                this.rb.drag = 0f;
                this.rb.freezeRotation = true;
            }
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
        /// 移动前方碰撞预检测（仅平滑滑动）。
        /// 原因：velocity 斜向撞墙时 Box2D 求解器消法向、保切向 → 角色沿墙弹偏一下 = 视觉「被推偏」；
        /// MovementStuckDetector 只看位移模长、不看方向，被推偏仍在动 → ratio≈1 检测不到。
        /// 方案：设 velocity 前沿移动方向 CircleCast 探测 Tile/BuildTile 碰撞体，命中且可滑动 →
        /// 把方向投影到墙面切向提前转向（角色不碰墙）；正对墙/滑向死角 → 不干预，
        /// 物理求解器自然消法向挡住，现有 MovementStuckDetector → Sliding → 状态机重寻路熔断接管。
        /// 不再内部重寻路/升级：卡死中 99% 场景网格判可通而物理挡路（可通行=True），
        /// 重寻路走同一条路空转只会误杀正常角色（见 bug-fixes.md 2026-08-15）。
        /// </summary>
        private const float WallProbeRadius = 0.1f;           // 扫掠圆半径 = 角色 CircleCollider2D.radius
        private const float WallProbeDistanceFactor = 2f;     // 探测距离 = speed * fixedDeltaTime * 此值（≈2 帧位移，只探测即将碰撞）
        private const float WallProbeMin = 0.15f;             // 探测距离下限
        private const float WallProbeMax = 0.3f;              // 探测距离上限（避免提前很远误判）
        private const float HeadOnSlideSqr = 0.15f;           // |slide|² < 此值 → 正对墙（移动方向在法线±23°内）
        private const float WaypointTooCloseMargin = 0.15f;   // 路点比墙近此量则忽略命中（拐点紧邻墙角属正常）
        private const float WallContactEpsilon = 0.02f; // 探测距离≈0（已接触墙）→ 停下，避免贴墙摩擦拖拽
        private static LayerMask s_wallLayerMask;
        private static bool s_wallLayerMaskResolved;

        /// <summary>墙探测层掩码（惰性求值：LayerMask.GetMask 是 icall，放静态字段初始化会让裸 Mono 单测环境的类型初始化直接炸）。</summary>
        private static LayerMask WallLayerMask
        {
            get
            {
                if (!s_wallLayerMaskResolved)
                {
                    s_wallLayerMask = LayerMask.GetMask("Tile", "BuildTile");
                    s_wallLayerMaskResolved = true;
                }

                return s_wallLayerMask;
            }
        }

        private bool slidingAlongWall;         // 贴墙滑动中：探测用固定进入方向，防 Direction 旋转漏墙振荡
        private Vector2 slideEnterDir;         // 进入滑动时的探测方向（指向墙），滑动期间固定

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
            s_temperatureEffect = null;
            s_mainThreadDispatcher = null;
            sharedLineMaterial = null;

            lock (SearchQueueLock)
            {
                SearchQueue.Clear();
                activeSearchWorkers = 0;
            }

            PendingEnqueue.Clear();
            flushScheduled = false;

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

        /// <summary>
        /// 螺旋搜索目标周围最近的角色可达格（Enemy 目标在私人房间内时校正用）。
        /// 找不到可达格（如目标被完全封闭）返回原目标，让后续寻路失败自然处理。
        /// </summary>
        private Vector3Int FindNearestReachable(Vector3Int targetMap)
        {
            const int maxRadius = 30;
            for (int layer = 1; layer <= maxRadius; layer++)
            {
                for (int dx = -layer; dx <= layer; dx++)
                {
                    for (int dy = -layer; dy <= layer; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != layer)
                        {
                            continue;
                        }

                        Vector3Int candidate = new Vector3Int(targetMap.x + dx, targetMap.y + dy, 0);
                        if (this.CanCharacterReach(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return targetMap;
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

            PendingEnqueue.Clear();
            flushScheduled = false;

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

            // 新寻路目标（外部触发）→ 退出贴墙滑动，新路径用新的滑动状态。
            // Worker Move→Seek 不经过 StopMove（WorkerMoveState.OnExit 不调用），旧 slideEnterDir
            // 会残留到新路径首帧探测（见 review 2026-08-15），在此统一复位。
            this.slidingAlongWall = false;

            // 敌人角色感知校正：Enemy 不能寻路进入 Worker 私人房间。
            // AStar 障碍判定用角色无关的 WalkabilityCache（门 IsPass=1 → 房间内部格物理可通行），
            // 若不在此校正，敌人会从门寻路进房间（RoomManager.CanCharacterEnter 规则未接入寻路）。
            if (this.isEnemy && !this.CanCharacterReach(targetMap))
            {
                Vector3Int corrected = this.FindNearestReachable(targetMap);
                if (corrected != targetMap)
                {
                    AWorkerTask.LogProvider(
                        $"[EnemyDiag] {this.Character.name} 目标({targetMap.x},{targetMap.y})在私人房间内, 校正到({corrected.x},{corrected.y})",
                        LogManager.LogLevelEnum.Debug);
                    targetMap = corrected;
                }
            }

            if (this.IsSeeking())
            {
                AWorkerTask.LogProvider(this.Character.name + ":重新寻路!", LogManager.LogLevelEnum.Trace);
            }

            // Unity Tilemap 查询只在首次构建时运行，后续寻路直接复用快照。
            EnsurePathfindingStorage();
            WalkabilityCache.EnsureBuilt();

            Vector3Int startMap = s_tileMap.WorldPosToMapPos(this.Character.transform.position);
            // 寻路提交高频（seekenemy 漫游每 2.5s/实例，峰值每秒数十次），降 Trace + 节流 2s/条
            // （game.log 181 万行/61 分钟刷屏源之一，见 bug-fixes.md 2026-08-15）。
            // 关键结果（不可达/到达）仍保留；提交细节需要时在 Trace 档查看。
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|SeekSubmit", 2f,
                // 惰性求值：寻路提交高频（每 2.5s/实例），被节流时不再构造插值串
                () => $"[SeekDiag] {this.Character.name} 提交寻路 start=({startMap.x},{startMap.y}) target=({targetMap.x},{targetMap.y})",
                LogManager.LogLevelEnum.Trace);
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
                ScheduleEnqueue(this);
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
            this.ClearVelocity(); // 停止移动 → 清刚体速度，防止 velocity 残留滑行
            this.slidingAlongWall = false; // 停止 → 退出贴墙滑动（新路径用新的滑动状态）

            this.RestartStuckWindow(); // 停止移动 → 保留连续卡住计数（重新寻路不赦免）
        }

        /// <summary>
        /// 清空刚体速度（velocity 驱动模式下停止/寻路间隙必须清零，否则角色滑行）。
        /// 对话暂停等"保持路径只站定"的场景也用此方法（勿用 StopMove——会清路径，
        /// 恢复后 Move 状态误判到达提前切状态）。
        /// </summary>
        public void ClearVelocity()
        {
            if (this.rb != null)
            {
                this.rb.velocity = Vector2.zero;
            }
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
            // 到达终点高频（随漫游），Trace + 节流 2s/条（见 bug-fixes.md 2026-08-15）。
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|SeekArrive", 2f,
                // 惰性求值：到达终点高频（随漫游），被节流时不构造插值串
                () => $"[SeekDiag] {this.Character.name} 到达终点 target=({this.TargetMap.x},{this.TargetMap.y})",
                LogManager.LogLevelEnum.Trace);
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
                this.ClearVelocity(); // 寻路间隙（重新寻路进行中）→ 清速度防滑行
                this.RestartStuckWindow(); // 寻路间隙（重新寻路进行中）→ 保留卡住计数
                return true;
            }

            Vector3 characterPosition = this.Character.transform.position;

            // 新路径首次消费：记录压缩路径点 + 后台线程填写的 A* 原始 path/压缩跳转诊断，
            // 验证合并路径是否合理（门口卡住排查 2026-08-16）。Trace + 节流 2s/条。
            if (result.PathIndex == 0 && (result.Path.Count > 0 || result.RawPathDiag != null))
            {
                // 惰性求值：每次消费新路径都会进入此处，但被节流时不再付 O(路径长度) 的
                // 路径串拼接（原实现节流也逐点 += 拼接整条路径）
                AWorkerTask.LogProviderThrottled(
                    $"{this.Character.name}|SeekPath", 2f,
                    () =>
                    {
                        string pathDesc = string.Empty;
                        for (int i = 0; i < result.Path.Count; i++)
                        {
                            pathDesc += $"({result.Path[i].x},{result.Path[i].y})";
                        }

                        string diag = string.Empty;
                        if (result.RawPathDiag != null)
                        {
                            diag += $" A*原始={result.RawPathDiag}";
                        }

                        if (result.CompressJumpDiag != null)
                        {
                            diag += $" 跳转={result.CompressJumpDiag}";
                        }

                        return $"[SeekDiag] {this.Character.name} 压缩路径[{result.Path.Count}] {pathDesc}{diag}";
                    },
                    LogManager.LogLevelEnum.Trace);
            }

            Vector3 worldPos;
            bool unreachableBlocked = false;
            if (result.PathIndex >= result.Path.Count)
            {
                if (result.IsReachable)
                {
                    this.CompleteMovement(); // 到达终点 → 完全清空
                    return true;
                }

                // 目标不可达（A* 确认无路）：原地停住，Feed(真实 speed) → Sliding → 状态机熔断接管。
                // 不再内部重寻路/升级：网格判可通而物理挡路时重寻路走同一条路，只会空转误杀（见 bug-fixes.md）。
                unreachableBlocked = true;
                worldPos = characterPosition; // 哨兵：原地停住
            }
            else
            {
                worldPos = s_tileMap.MapPosToWorldPos(result.Path[result.PathIndex]);
            }

            if (!unreachableBlocked)
            {
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
            }

            this.Direction = worldPos - this.Character.transform.position;
            float speed = this.ComputeAdjustedSpeed();

            // 移动前方碰撞预检测：设 velocity 前沿移动方向 CircleCast 探测 Tile/BuildTile 碰撞体。
            // 命中且可滑动 → 把方向投影到墙面切向平滑滑动（贴墙平行走时投影≈原方向，天然不误判）；
            // 正对墙/滑向死角 → 保持原速度交给物理求解器自然消法向（角色停在墙前，
            // 由下方 Feed(真实 speed) 结算 Sliding/Stuck → 现有状态机熔断接管，不内部重寻路）。
            // 必须在 Feed 之前：预检测决定最终速度，Feed 用一致速度，避免期望位移虚高假 Stuck。
            Vector2 velocityDir = new Vector2(this.Direction.x, this.Direction.y).normalized;
            Vector2 toWp = new Vector2(worldPos.x - characterPosition.x, worldPos.y - characterPosition.y);
            // 到路点的 2D 距离（CircleCast 是 2D 探测，距离比较必须同维度；Vector3.Distance 会引入
            // 角色 z 分量虚高 distToWp → 路点紧邻时误判滑动，见 review 2026-08-15）。
            float distToWp = toWp.magnitude;
            if (toWp.sqrMagnitude > 0f)
            {
                toWp.Normalize();
            }

            if (unreachableBlocked)
            {
                // 目标不可达：不探测（零方向 CircleCast 无意义）、原地停住，
                // Feed(真实 speed) → 1s 窗口 ratio≈0 → Sliding → 状态机熔断接管。
                velocityDir = Vector2.zero;
            }
            else
            {
                float probeDist = Mathf.Clamp(speed * Time.fixedDeltaTime * WallProbeDistanceFactor, WallProbeMin, WallProbeMax);
                // 探测方向：滑动中用"进入滑动时的固定方向"（指向墙），而非当前 Direction。
                // 角色沿墙移动时 Direction 随位置旋转，短探测会漏掉旁边的墙 → 逐帧在
                // 「滑↔走向路点(撞墙)」间切换，角色贴墙抖动、被物理弹开"瞬移一段"（见 bug-fixes.md）。
                // 固定方向保证墙一直在探测窗口内（角色到墙垂直距离滑动中不变），直到真正绕过墙
                // （探测落空）才退出滑动。
                Vector2 probeDir = this.slidingAlongWall ? this.slideEnterDir : velocityDir;
                RaycastHit2D hit = Physics2D.CircleCast(characterPosition, WallProbeRadius, probeDir, probeDist, WallLayerMask);

                if (hit.collider == null)
                {
                    this.slidingAlongWall = false; // 通路恢复/滑动已绕过墙 → 退出滑动
                }
                else if (hit.distance < distToWp - WaypointTooCloseMargin)
                {
                    // 路点比墙近则忽略本次命中（拐点紧邻墙角属正常），否则投影滑动。
                    Vector2 slideDir;
                    bool canSlide = TryGetSlideDirection(probeDir, hit.normal, out slideDir);
                    if (canSlide && hit.distance >= WallContactEpsilon)
                    {
                        Vector2 slideN = slideDir.normalized;
                        // 滑动背离路点（dot<=0）说明滑向死角而非绕过墙角 → 不算滑动，交给物理挡停。
                        if (Vector2.Dot(slideN, toWp) > 0f)
                        {
                            velocityDir = slideN; // 切向投影 → 平滑滑动
                            if (!this.slidingAlongWall)
                            {
                                this.slideEnterDir = probeDir; // 进入滑动：固定探测方向（指向墙）
                                this.slidingAlongWall = true;
                                AWorkerTask.LogProviderThrottled(
                                    $"{this.Character.name}|WallSlideEnter", 2f,
                                    // 惰性求值：贴墙探测每帧运行，被节流时不构造插值串
                                    () => $"[MoveDiag] {this.Character.name} 贴墙滑动 dir=({slideN.x:F2},{slideN.y:F2}) " +
                                        $"pos=({characterPosition.x:F2},{characterPosition.y:F2})",
                                    LogManager.LogLevelEnum.Debug);
                            }
                        }
                    }
                    else
                    {
                        // 正对墙 / 已几乎接触墙（探测≈0）：保持原速度，物理求解器自然消法向挡住。
                        // 抖动由 freezeRotation（冻结 Z 旋转）根治——接触扭矩不再旋转刚体、
                        // 位置修正稳定（见 bug-fixes.md 2026-08-15 根因修正）；此前尝试"按探测距离
                        // 减速停住"（运动学防穿透）实测无效（非根因）已回退。
                        // 不做任何事、不内部重寻路——重寻路在「网格判可通而物理挡路」时走同一条路空转（见 bug-fixes.md）。
                        // 注意：此前曾在此处加"段失效→StopMove+Seek 重寻路"（修复 E），实测造成
                        // 建墙时角色被刚建墙围困 → 重寻路路径仍穿过新墙 → 每 2s 段失效死循环、角色卡死不动
                        // （于发祥 pos=(70.40,130.81) 17:07:40-50 六次同坐标，见 bug-fixes.md 2026-08-15）。
                        // 回归纯物理挡 + MovementStuckDetector 熔断（Sliding→状态机重寻路→同目标4次放弃）。
                        this.slidingAlongWall = false;

                        // 诊断：正对墙事件点（节流 2s/条）。记录命中碰撞体的瓦片名与格子坐标，
                        // 交叉验证「可通行=物理真值」是否一致（见 bug-fixes.md）。
                        // 卡床排查 2026-08-16：追加命中格的「网格判定/缓存判定」分叉诊断——
                        // 物理碰撞体存在（此分支已命中）但网格/缓存判可通即分叉，锁定 UpdateCell 失效点。
                        // 惰性求值：命中格换算/网格与缓存可达性查询全部移入日志委托——此分支在
                        // 正对墙时每帧进入，原实现即使被节流也每帧白付 3 次地图查询 + 插值串分配。
                        AWorkerTask.LogProviderThrottled(
                            $"{this.Character.name}|WallHeadOn", 2f,
                            () =>
                            {
                                Vector3Int blockCell = s_tileMap.WorldPosToMapPos(new Vector3(hit.point.x, hit.point.y, 0));
                                string blocker = hit.collider != null ? $"{hit.collider.name}:({blockCell.x},{blockCell.y})" : "?";
                                bool blockGridReach = ASeek.IsCanReach(blockCell);
                                bool blockCacheWalk = WalkabilityCache.IsWalkable(blockCell.x, blockCell.y);
                                string neighborDiag = string.Empty;
                                if (blockGridReach || blockCacheWalk)
                                {
                                    Vector3Int cur = s_tileMap.WorldPosToMapPos(new Vector3(characterPosition.x, characterPosition.y, 0));
                                    neighborDiag = $" 站在=({cur.x},{cur.y}) 网格可通={ASeek.IsCanReach(cur)} 缓存可通={WalkabilityCache.IsWalkable(cur.x, cur.y)}";
                                }

                                return $"[MoveDiag] {this.Character.name} 正对墙(保持速度,物理挡) dist={hit.distance:F2} " +
                                    $"pos=({characterPosition.x:F2},{characterPosition.y:F2}) hit={blocker} " +
                                    $"网格可通={blockGridReach} 缓存可通={blockCacheWalk}{neighborDiag}";
                            },
                            LogManager.LogLevelEnum.Debug);
                    }
                }
            }

            // 卡死检测：必须在移动之前，用当前真实位置喂入（velocity 撞墙被挡，
            // 位置停住，累计位移≈0 → 检出 Stuck）。
            // 恒喂真实 speed：预检测不再手动停/内部重寻路，正对墙由物理挡 + 本检测 1s 窗口结算 Sliding。
            float appliedSpeed = speed;
            this.LastStuckResult = this.stuckDetector.Feed(Time.fixedDeltaTime, characterPosition, appliedSpeed);
            // 卡墙诊断：Sliding/Stuck 结算时输出一次 Debug（检测窗口 1s，最多每秒一条，不刷屏）。
            // 记录结算结果、实时位置、寻路目标与位移比例，用于定位"A* 认为可通而物理被挡"。
            if (this.LastStuckResult != LAB2D.BugCheckResult.None)
            {
                AWorkerTask.LogProvider(
                    $"[StuckDiag] {this.Character.name} 结算={this.LastStuckResult} " +
                    $"pos=({characterPosition.x:F2},{characterPosition.y:F2}) target=({this.TargetMap.x},{this.TargetMap.y}) " +
                    $"ratio={this.stuckDetector.LastRatio:F2} speed={speed:F2} pathIdx={result.PathIndex}/{result.Path.Count}",
                    LogManager.LogLevelEnum.Debug);
            }

            // 移动：有刚体用 velocity 驱动（Player 同款，PlayerViewAdapter 验证平滑）。
            // 物理引擎每 FixedUpdate 推进位置：速度正确、撞墙碰撞求解器阻挡不穿透，
            // 配合 Rigidbody2D Interpolate 渲染插值（200fps 渲染 vs 50Hz 物理不跳变）。
            // 不用 MovePosition —— 它受碰撞约束削减位移，实测"又慢又卡顿"。
            // 卡死检测不受影响：velocity 撞墙被挡 → 位置不动 → 累计位移≈0 → 检出 Stuck。
            this.ApplyVelocity(velocityDir, speed);

            if (this.ShouldShowLine())
            {
                this.UpdateLine(true);
            }

            return false;
        }

        /// <summary>
        /// 计算经环境/状态逐级调整后的移动速度（MoveByPath / MoveDirect 共用同一管线）：
        /// 天气 → 地形 → 温度 →（Worker）饥饿疲劳状态。
        /// </summary>
        private float ComputeAdjustedSpeed()
        {
            float speed = s_weatherEffect.GetAdjustedCharacterMoveSpeed(this.Character, this.Character.MoveSpeed);
            speed *= s_terrainEffect.GetMoveSpeedMultiplier(this.Character);
            speed *= s_temperatureEffect != null ? s_temperatureEffect.GetCharacterMoveSpeedMultiplier(this.Character) : 1.0f;

            if (this.isWorker)
            {
                speed = s_workerConditionManager.GetAdjustedWorkerMoveSpeed((AWorker)this.Character, speed);
            }

            return speed;
        }

        /// <summary>
        /// 应用移动：有刚体用 velocity 驱动，否则 Translate 回退（MoveByPath / MoveDirect 共用唯一写入点）。
        /// </summary>
        private void ApplyVelocity(Vector2 dir, float speed)
        {
            if (this.rb != null)
            {
                this.rb.velocity = dir * speed;
            }
            else
            {
                this.Character.transform.Translate(speed * Time.fixedDeltaTime * (Vector3)dir, Space.World);
            }
        }

        /// <summary>
        /// 无寻路直接移动（WorkerLocomotion 战斗移动的径向回退调用：背扇采样全堵时的直线后撤）。
        /// 走 MoveByPath 同一速度管线与卡死检测：ComputeAdjustedSpeed 调整、velocity 驱动、
        /// stuckDetector.Feed 结算 Sliding/Stuck（调用方读 LastStuckResult 兜底 Stop，不内部重寻路）。
        /// 不做贴墙滑动预检测——回退场景方向单帧改写频率高，滑动状态机不适用；
        /// 主战斗移动（追击/拉开）走 Seek+MoveByPath 寻路管线，自带贴墙滑动。
        /// </summary>
        /// <param name="worldDir">世界空间移动方向（内部归一化；零向量=原地停住仅继续卡死检测）。</param>
        public void MoveDirect(Vector2 worldDir)
        {
            Vector3 characterPosition = this.Character.transform.position;
            float speed = this.ComputeAdjustedSpeed();

            Vector2 dir = worldDir.sqrMagnitude > 0f ? worldDir.normalized : Vector2.zero;
            if (dir.sqrMagnitude > 0f)
            {
                this.Direction = new Vector3(dir.x, dir.y, 0f);
            }

            // 卡死检测：与 MoveByPath 同口径——移动前喂当前真实位置与真实速度，
            // velocity 撞墙被挡 → 位移≈0 → 结算 Sliding/Stuck。
            this.LastStuckResult = this.stuckDetector.Feed(Time.fixedDeltaTime, characterPosition, speed);
            if (this.LastStuckResult != LAB2D.BugCheckResult.None)
            {
                AWorkerTask.LogProvider(
                    $"[StuckDiag] {this.Character.name} 直移结算={this.LastStuckResult} " +
                    $"pos=({characterPosition.x:F2},{characterPosition.y:F2}) dir=({dir.x:F2},{dir.y:F2}) speed={speed:F2}",
                    LogManager.LogLevelEnum.Debug);
            }

            this.ApplyVelocity(dir, speed);
        }

        /// <summary>
        /// 纯函数：把移动方向投影到墙面切向。返回 false = 正对墙（切向分量太小，|slide|² &lt; HeadOnSlideSqr）。
        /// public 供 Editor 测试访问（与 MovementStuckDetector 测试同模式，见 ASeekSlideDirectionTests）。
        /// </summary>
        public static bool TryGetSlideDirection(Vector2 moveDir, Vector2 wallNormal, out Vector2 slideDir)
        {
            Vector2 d = moveDir.normalized;
            slideDir = d - wallNormal * Vector2.Dot(d, wallNormal);
            return slideDir.sqrMagnitude >= HeadOnSlideSqr;
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
            Vector3Int targetMap;
            lock (this.requestLock)
            {
                if (generation != this.seekGeneration)
                {
                    return false;
                }

                this.currentResult = result;
                this.isSeeking = false;
                targetMap = this.pendingTargetMap;
            }

            // 寻路结果落地。TrySetResult 由后台工作线程调用，LogProvider 不可在线程池内直接调用，
            // 故"不可达"结果通过 s_mainThreadDispatcher 回主线程后记录（卡墙/困住排查关键事件）。
            if (!result.IsReachable && !isShuttingDown && s_mainThreadDispatcher != null)
            {
                LAB2D.Character.Character character = this.Character;
                // 关键事件：保持 Debug，节流 2s/条防连续失败刷屏（见 bug-fixes.md 2026-08-15）。
                s_mainThreadDispatcher.EnqueueAsync(() =>
                    AWorkerTask.LogProviderThrottled(
                        $"{(character != null ? character.name : "?")}|SeekUnreachable", 2f,
                        // 惰性求值：卡墙重试期间该结果反复到达，被节流时不构造插值串
                        () => $"[SeekDiag] {(character != null ? character.name : "?")} 寻路不可达 target=({targetMap.x},{targetMap.y})",
                        LogManager.LogLevelEnum.Debug));
            }

            return true;
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
            if (Core.ServiceLocator.TryGet<ITemperatureEffectService>(out var tempEffect))
            {
                s_temperatureEffect = tempEffect;
            }

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
            PathfindingWorkspacePool.Initialize(gridWidth, gridHeight, maxIterations, MaxConcurrentSearches);
            WalkabilityCache.Initialize(gridWidth, gridHeight, s_tileMap.GetInstanceID());
        }

        /// <summary>
        /// 把新寻路请求放入待入队队列，并确保有一个每帧配额放行的 flush 被调度。
        /// 只在主线程调用（Seek 由各角色状态机驱动，flush 由主线程 dispatcher 执行）。
        /// </summary>
        private static void ScheduleEnqueue(ASeek seek)
        {
            // 应用正在关闭，不再接收新请求
            if (isShuttingDown)
            {
                return;
            }

            // dispatcher 未就绪（启动极早期/纯逻辑测试）→ 退回同步入队，保持旧行为。
            if (s_mainThreadDispatcher == null)
            {
                EnqueueSearch(seek);
                return;
            }

            PendingEnqueue.Enqueue(seek);
            ScheduleFlush();
        }

        private static void ScheduleFlush()
        {
            // dispatcher 正在关闭时其 Enqueue 会静默丢弃，flushScheduled 不能置位，
            // 否则卡死为 true 后新场景再无人调度 flush → 寻路全停。
            if (flushScheduled || isShuttingDown || UnityMainThreadDispatcher.IsShuttingDown)
            {
                return;
            }

            flushScheduled = true;
            s_mainThreadDispatcher.Enqueue(FlushPendingEnqueues);
        }

        /// <summary>
        /// 每帧最多放行 MaxEnqueuesPerFrame 个待入队请求到全局搜索队列；
        /// 仍有剩余时再调度下一帧继续，形成自续的逐帧放行。
        /// </summary>
        private static void FlushPendingEnqueues()
        {
            flushScheduled = false;
            if (isShuttingDown)
            {
                PendingEnqueue.Clear();
                return;
            }

            int budget = MaxEnqueuesPerFrame;
            while (budget-- > 0 && PendingEnqueue.Count > 0)
            {
                EnqueueSearch(PendingEnqueue.Dequeue());
            }

            if (PendingEnqueue.Count > 0)
            {
                ScheduleFlush();
            }
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
                StartSearchWorker();
            }
        }

        /// <summary>
        /// 用专用后台线程跑搜索 worker，不用 ThreadPool：单次搜索可达数十毫秒（长任务会刺激
        /// 线程池注入更多线程），且 Photon 心跳等共用线程池，互相干扰。
        /// 线程数量仍由 activeSearchWorkers 上限（MaxConcurrentSearches）严格约束。
        /// </summary>
        private static void StartSearchWorker()
        {
            var thread = new Thread(RunSearchQueue)
            {
                IsBackground = true,
                Name = "ASeekSearchWorker",
            };
            thread.Start();
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

            /// <summary>
            /// 卡床排查 2026-08-16：A* 原始 path 头几跳（后台线程填充，纯字符串无 Unity 依赖）。
            /// 主线程 MoveByPath 在 PathIndex==0 时打印，用于核对「压缩首点需经过缓存不可通格」矛盾。
            /// 后台线程只写、主线程只在 currentResult 就位后读，无并发写入同一实例的窗口。
            /// </summary>
            public string RawPathDiag;

            /// <summary>
            /// 卡床排查 2026-08-16：压缩跳转轨迹（起点→落点 + 最近失败候选），后台线程填充。
            /// </summary>
            public string CompressJumpDiag;

            internal void Reset()
            {
                this.IsReachable = true;
                this.Path.Clear();
                this.PathIndex = 0;
                this.RawPathDiag = null;
                this.CompressJumpDiag = null;
            }
        }
    }
}
