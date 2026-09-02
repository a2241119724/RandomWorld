namespace LAB2D.Character.Worker
{
    using UnityEngine;

    /// <summary>
    /// Worker 移动意图种类。
    /// </summary>
    public enum WorkerMoveIntentKind
    {
        /// <summary>
        /// 无移动（等待寻路/工作中站定等）。
        /// </summary>
        None,

        /// <summary>
        /// 沿已寻好的路径走到目标格（任务/漫游移动）。
        /// </summary>
        ToMap,

        /// <summary>
        /// 与目标角色保持距离带（风筝走位：远了靠近、近了后撤）。
        /// </summary>
        KeepDistance,

        /// <summary>
        /// 追击目标角色至指定距离内（主动追击/贴近攻击）。
        /// </summary>
        Chase,
    }

    /// <summary>
    /// 移动意图（不可变，便于诊断打印与比较）。
    /// </summary>
    public readonly struct WorkerMoveIntent
    {
        public WorkerMoveIntent(WorkerMoveIntentKind kind, Vector3Int targetMap, Character target, float minDistance, float maxDistance)
        {
            this.Kind = kind;
            this.TargetMap = targetMap;
            this.Target = target;
            this.MinDistance = minDistance;
            this.MaxDistance = maxDistance;
        }

        /// <summary>
        /// 意图种类
        /// </summary>
        public WorkerMoveIntentKind Kind { get; }

        /// <summary>
        /// ToMap 意图的目标格
        /// </summary>
        public Vector3Int TargetMap { get; }

        /// <summary>
        /// KeepDistance/Chase 意图的目标角色
        /// </summary>
        public Character Target { get; }

        /// <summary>
        /// KeepDistance 最小距离（低于则后撤）
        /// </summary>
        public float MinDistance { get; }

        /// <summary>
        /// KeepDistance 最大距离 / Chase 的接近半径
        /// </summary>
        public float MaxDistance { get; }

        /// <summary>
        /// 空意图
        /// </summary>
        public static WorkerMoveIntent None => default;

        /// <summary>
        /// 诊断用一行文本。
        /// </summary>
        public string Describe()
        {
            return this.Kind switch
            {
                WorkerMoveIntentKind.ToMap => $"ToMap({this.TargetMap.x},{this.TargetMap.y})",
                WorkerMoveIntentKind.KeepDistance => $"KeepDistance({this.Target?.name},{this.MinDistance:F1},{this.MaxDistance:F1})",
                WorkerMoveIntentKind.Chase => $"Chase({this.Target?.name},{this.MaxDistance:F1})",
                _ => "None",
            };
        }
    }

    /// <summary>
    /// Worker 移动服务层 — 常驻 AWorker，由 AWorker.FixedUpdate 统一驱动（TickFixed）。
    /// 状态层只声明移动意图，不直接消费寻路结果；寻路发起来自两处——任务移动由 Seek 状态负责
    ///（等 IsHavePath 后切 Move 的时序闸门不变——无意图时 TickFixed 不驱动 MoveByPath，
    /// 否则寻路等待期被消费会令 Seek.OnUpdate 误判"没有找到路"），战斗移动由本层
    /// Chase/KeepDistance 语义发起（背扇拉开/目标格追击寻路 + 节流 + 三态子状态）。
    /// 承接原 WorkerMoveState 的移动执行：MoveByPath 消费、到达判定、Sliding/Stuck 熔断。
    /// </summary>
    public class WorkerLocomotion
    {
        /// <summary>
        /// Sliding 熔断阈值：同一目标累计 Sliding 次数上限（原 WorkerMoveState 常量，语义原样）。
        /// </summary>
        private const int MaxSlidingStreak = 4;

        /// <summary>
        /// 战斗脱离距离：目标超出此距离不再追/不再拉开，站定等状态层超时脱离战斗。
        /// 对齐 WorkerAttackState.CounterAttackRange=8（IsStillUnderAttack 的续命判定），
        /// 两处需保持一致语义：移动层不追出 8 格，状态层 1.5s 超时后即自然退出战斗回 Seek 干活。
        /// </summary>
        private const float CombatBreakRange = 8f;

        /// <summary>
        /// 战斗寻路提交节流（秒）：追击/拉开每攻击周期（约 2s）1-2 次提交，量级与 SeekEnemy
        /// 漫游寻路（每 2.5s/实例）相同；节流防目标高频移动导致重寻路风暴。
        /// </summary>
        private const float CombatSeekThrottle = 0.8f;

        /// <summary>
        /// 战斗寻路等待超时（秒）：提交后超时未出路径（目标格不可达/寻路失败）→ 回 Idle，
        /// 下帧语义评估重走（背扇全堵回退径向直线等）。
        /// </summary>
        private const float CombatPathTimeout = 2f;

        /// <summary>
        /// 战斗寻路子状态：Idle（站定，语义满足或等节流窗口）→ WaitPath（已提交寻路，站定等异步路径）
        /// → FollowPath（沿路径消费）。语义评估每帧优先于路径消费——进攻击距离/距离带立即站定。
        /// </summary>
        private enum CombatPathMode
        {
            Idle,
            WaitPath,
            FollowPath,
        }

        private readonly AWorker worker;
        private WorkerMoveIntent intent = WorkerMoveIntent.None;
        private bool hasArrived;
        private Vector3Int lastSlidingTarget; // 上次 Sliding 时的寻路目标，用于统计累计次数（原 WorkerMoveState 字段迁入）
        private int slidingStreak;            // 同一目标累计 Sliding 次数（熔断用）
        private CombatPathMode combatPathMode = CombatPathMode.Idle;
        private float lastCombatSeekTime = float.MinValue; // 上次战斗寻路提交时刻（节流用）
        private float combatSeekDeadline;                  // WaitPath 超时截止时刻

        public WorkerLocomotion(AWorker worker)
        {
            this.worker = worker;
        }

        /// <summary>
        /// 当前意图种类
        /// </summary>
        public WorkerMoveIntentKind Kind => this.intent.Kind;

        /// <summary>
        /// ToMap 意图是否已到达（电平：由 TickFixed 每物理帧刷新，状态 OnUpdate 读取）。
        /// </summary>
        public bool HasArrived => this.hasArrived;

        /// <summary>
        /// 声明"沿当前已寻好的路径走到目标格"意图。
        /// 只声明不发起寻路——寻路仍由 Seek 状态发起并确认可达后切 Move。
        /// 设置意图会重置到达标记（原 Move 状态 OnEnter 重置 isTargetReached 的语义）。
        /// </summary>
        public void GoTo(Vector3Int targetMap)
        {
            this.SetIntent(new WorkerMoveIntent(WorkerMoveIntentKind.ToMap, targetMap, null, 0f, 0f));
        }

        /// <summary>
        /// 声明追击意图：向目标直线移动直到进入 within 距离内（战斗贴近攻击）。
        /// 每帧重声明同参数会被 SetIntent 短路（防风暴）。
        /// </summary>
        public void Chase(Character target, float within)
        {
            this.SetIntent(new WorkerMoveIntent(WorkerMoveIntentKind.Chase, default, target, 0f, within));
        }

        /// <summary>
        /// 声明保距意图：与目标保持 [min,max] 距离带
        ///（风筝走位：远于 max 靠近、近于 min 后撤、带内站定）。
        /// </summary>
        public void KeepDistance(Character target, float min, float max)
        {
            this.SetIntent(new WorkerMoveIntent(WorkerMoveIntentKind.KeepDistance, default, target, min, max));
        }

        /// <summary>
        /// 停止一切移动：清战斗路径子状态 + 清意图 + StopMove（清刚体速度防滑行、取消未决寻路）。
        /// </summary>
        public void Stop()
        {
            bool hadCombatPath = this.combatPathMode != CombatPathMode.Idle;
            this.combatPathMode = CombatPathMode.Idle;
            if (this.intent.Kind == WorkerMoveIntentKind.None)
            {
                if (hadCombatPath)
                {
                    this.worker.Seek.StopMove(); // 意图已空但战斗路径残留 → 补清理
                }

                return;
            }

            this.SetIntent(WorkerMoveIntent.None);
            this.worker.Seek.StopMove();
        }

        /// <summary>
        /// 状态切换收口点（WorkerStateManager.ChangeState 在新状态 OnEnter 前调用）：
        /// 仅清除 ToMap 类意图并 StopMove——离开移动统一清速度防滑行（bug-fixes.md 2026-08-15）。
        /// 仅当存在 ToMap 意图时才 StopMove：Seek→Move 等无意图切换不能清掉刚寻好的路径。
        /// </summary>
        public void ClearGoToIntent()
        {
            if (this.intent.Kind != WorkerMoveIntentKind.ToMap)
            {
                return;
            }

            this.SetIntent(WorkerMoveIntent.None);
            this.worker.Seek.StopMove();
        }

        /// <summary>
        /// 固定帧驱动：执行当前意图（AWorker.FixedUpdate 在状态 OnFixedUpdate 之前调用，
        /// 与原 WorkerMoveState"OnFixedUpdate 写到达标记 → OnUpdate 读"的顺序一致）。
        /// </summary>
        public void TickFixed()
        {
            switch (this.intent.Kind)
            {
                case WorkerMoveIntentKind.ToMap:
                    this.TickToMap();
                    break;
                case WorkerMoveIntentKind.Chase:
                    this.TickChase();
                    break;
                case WorkerMoveIntentKind.KeepDistance:
                    this.TickKeepDistance();
                    break;
                case WorkerMoveIntentKind.None:
                default:
                    // 无意图不驱动：Seek 状态等待寻路期间 MoveByPath 绝不能被消费
                    //（否则 CompleteMovement 清空 currentResult，Seek.OnUpdate 误判"没有找到路"→ GiveUpTask 风暴）。
                    break;
            }
        }

        /// <summary>
        /// Chase 意图执行（前向寻路追击）：语义评估每帧优先于路径消费——
        /// 进攻击距离立即站定挥砍；目标超出 CombatBreakRange 不再追（站定，攻击状态层
        /// 1.5s 超时 + IsStillUnderAttack 8 格判定自然脱离战斗）；其余向目标当前格寻路
        ///（等效前扇 0° 主选，路径不必走完）。目标死亡/丢失 → Stop 站定。
        /// 寻路是异步的（提交到出路径 1-3 帧延迟），中远距绕障正确性优先于即时性；
        /// 节流窗口内站定等待。
        /// </summary>
        private void TickChase()
        {
            Character target = this.intent.Target;
            if (!this.IsCombatTargetAlive(target))
            {
                this.Stop();
                return;
            }

            Vector2 toTarget = target.transform.position - this.worker.transform.position;
            float dist = toTarget.magnitude;

            // 语义评估（含 FollowPath 中）：进攻击距离/目标超程 → 站定（停移动+取消未决寻路）
            if (dist <= this.intent.MaxDistance || dist > CombatBreakRange)
            {
                this.EnterCombatIdle();
                return;
            }

            if (this.combatPathMode != CombatPathMode.Idle)
            {
                this.TickCombatPath();
                return;
            }

            // Idle：向目标当前格寻路（节流；未到节流窗口本帧站定）
            Vector3Int targetCell = AWorkerTask.TileMapWorldToMapProvider(target.transform.position);
            this.TrySeekTo(targetCell);
        }

        /// <summary>
        /// KeepDistance 意图执行（背扇寻路拉开）：贴太近（&lt; min）→ 向背向目标的扇形区域
        ///（±60° 内正后优先）选可走格寻路——正后方是墙时 A* 自动绕向侧后，不再径向卡死；
        /// 扇形全堵（凹角）→ 回退径向直线后撤（撞墙 Sliding→站定，节流窗口后重扫扇形）。
        /// 拉过头（&gt; max）→ 向目标格寻路回带；带内站定。距离带 + 超程评估每帧优先于路径消费。
        /// </summary>
        private void TickKeepDistance()
        {
            Character target = this.intent.Target;
            if (!this.IsCombatTargetAlive(target))
            {
                this.Stop();
                return;
            }

            Vector2 toTarget = target.transform.position - this.worker.transform.position;
            float dist = toTarget.magnitude;

            // 语义评估（含 FollowPath 中）：带内站定；拉过头超 CombatBreakRange 站定（脱离由状态层判定）
            if ((dist >= this.intent.MinDistance && dist <= this.intent.MaxDistance)
                || dist > CombatBreakRange)
            {
                this.EnterCombatIdle();
                return;
            }

            if (this.combatPathMode != CombatPathMode.Idle)
            {
                this.TickCombatPath();
                return;
            }

            if (dist < this.intent.MinDistance)
            {
                // 拉开：节流窗口到 → 扫描背扇可走格，找到则寻路；全堵/未到窗口 → 径向直线后撤
                if (Time.time - this.lastCombatSeekTime >= CombatSeekThrottle)
                {
                    Vector3Int? retreatCell = this.FindRetreatCell(
                        this.worker.transform.position, target.transform.position, this.intent.MaxDistance);
                    if (retreatCell.HasValue && this.TrySeekTo(retreatCell.Value))
                    {
                        return;
                    }
                }

                this.worker.Seek.MoveDirect(-toTarget);
                this.HandleCombatStuck();
                return;
            }

            // 拉过头回带：向目标格寻路（未到节流窗口本帧站定）
            Vector3Int targetCell = AWorkerTask.TileMapWorldToMapProvider(target.transform.position);
            this.TrySeekTo(targetCell);
        }

        /// <summary>
        /// 战斗寻路子状态机（Chase/KeepDistance 共用）：WaitPath 等异步路径（超时/失败回 Idle），
        /// FollowPath 沿路径消费（走完/路径失效回 Idle 下帧重评估；Sliding/Stuck → 站定，
        /// 节流天然限频不做熔断——战斗时长有限，下帧语义评估会重新发起）。
        /// </summary>
        private void TickCombatPath()
        {
            if (this.combatPathMode == CombatPathMode.WaitPath)
            {
                if (Time.time > this.combatSeekDeadline)
                {
                    this.EnterCombatIdle(); // 等路径超时 → 回 Idle 重评估
                    return;
                }

                if (!this.worker.Seek.IsSeeking())
                {
                    // 寻路结束：有路径 → 沿路径走；失败（目标格不可达等）→ 回 Idle
                    this.combatPathMode = this.worker.Seek.IsHavePath()
                        ? CombatPathMode.FollowPath
                        : CombatPathMode.Idle;
                }

                return; // 等待期站定（TrySeekTo 的 StopMove 已清速度）
            }

            // FollowPath：沿路径消费（速度/贴墙滑动/卡死检测全走 MoveByPath 既有管线）
            bool arrived = this.worker.Seek.MoveByPath();
            if (arrived)
            {
                this.combatPathMode = CombatPathMode.Idle; // 走完/路径失效 → 下帧语义评估重走
                return;
            }

            BugCheckResult stuck = this.worker.Seek.LastStuckResult;
            if (stuck == BugCheckResult.Sliding || stuck == BugCheckResult.Stuck)
            {
                this.EnterCombatIdle(); // 路径被挡 → 站定，下帧重评估（节流后再寻）
            }
        }

        /// <summary>
        /// 进入战斗站定：取消未决寻路/停止路径消费并清速度（防滑行）。
        /// 已是 Idle 且无路径时不重复 StopMove（防每帧重置卡死检测窗口）。
        /// </summary>
        private void EnterCombatIdle()
        {
            if (this.combatPathMode == CombatPathMode.Idle)
            {
                return;
            }

            this.combatPathMode = CombatPathMode.Idle;
            this.worker.Seek.StopMove();
        }

        /// <summary>
        /// 提交战斗寻路（节流 CombatSeekThrottle）：先 StopMove 清旧路径与速度再 Seek
        ///（ASeekEnemy.HandleMovementStuck 同序），切 WaitPath 并记超时截止。
        /// 未到节流窗口返回 false（调用方本帧站定或径向回退）。
        /// </summary>
        private bool TrySeekTo(Vector3Int cell)
        {
            float now = Time.time;
            if (now - this.lastCombatSeekTime < CombatSeekThrottle)
            {
                return false;
            }

            this.lastCombatSeekTime = now;
            this.worker.Seek.StopMove();
            this.worker.Seek.Seek(cell);
            this.combatSeekDeadline = now + CombatPathTimeout;
            this.combatPathMode = CombatPathMode.WaitPath;
            AWorkerTask.LogProviderThrottled(
                $"{this.worker.name}|CombatSeek", 2f,
                // 惰性求值：战斗寻路按 CombatSeekThrottle 节流尝试，被节流时不构造插值串
                () => $"[MoveDiag] {this.worker.name} 战斗寻路 目标格=({cell.x},{cell.y}) 意图={this.intent.Kind}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// 背扇采样：以自身为原点、背向目标方向为 0°，±20/±40/±60° 展开（正后优先）×
        /// 距离档 {max, max+1}，候选世界点转地图格后过滤可走（IsCanReach）与房间权限
        ///（CanCharacterReach——后撤点不得选进其他 Worker 房间）及自身/目标所在格。
        /// 返回首个可行格（正后优先）；全部不可达返回 null（调用方径向直线回退）。
        /// 采样频率受 CombatSeekThrottle 节流限制（每秒至多 1 轮 14 次格查询）。
        /// </summary>
        private Vector3Int? FindRetreatCell(Vector3 selfPos, Vector3 targetPos, float maxDist)
        {
            Vector2 away = new Vector2(selfPos.x - targetPos.x, selfPos.y - targetPos.y);
            if (away.sqrMagnitude < 0.001f)
            {
                return null; // 与目标重合，无法定义背向
            }

            float baseAngle = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg;
            Vector3Int selfCell = AWorkerTask.TileMapWorldToMapProvider(selfPos);
            Vector3Int targetCell = AWorkerTask.TileMapWorldToMapProvider(targetPos);
            int[] spins = { 0, 20, -20, 40, -40, 60, -60 };
            float[] dists = { maxDist, maxDist + 1f };

            foreach (int spin in spins)
            {
                float rad = (baseAngle + spin) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                foreach (float d in dists)
                {
                    Vector3 world = selfPos + (Vector3)(dir * d);
                    Vector3Int cell = AWorkerTask.TileMapWorldToMapProvider(world);
                    if (cell == selfCell || cell == targetCell)
                    {
                        continue;
                    }

                    if (!ASeek.IsCanReach(cell) || !this.worker.Seek.CanCharacterReach(cell))
                    {
                        continue;
                    }

                    return cell;
                }
            }

            return null;
        }

        /// <summary>
        /// 战斗移动卡死兜底：被墙/碰撞体挡住（Sliding/Stuck 结算）→ 站定挥砍。
        /// 不做救援传送/重寻路（战斗短距，站定输出 + 超时退出战斗是合理兜底，见计划风险 8）。
        /// </summary>
        private void HandleCombatStuck()
        {
            BugCheckResult stuck = this.worker.Seek.LastStuckResult;
            if (stuck == BugCheckResult.Stuck || stuck == BugCheckResult.Sliding)
            {
                this.Stop();
            }
        }

        /// <summary>
        /// 战斗目标是否仍存活（空/已死 → false，调用方据此 Stop 站定）。
        /// </summary>
        private bool IsCombatTargetAlive(Character target)
        {
            return target != null
                && target.gameObject != null
                && target.CharacterDataLAB != null
                && target.CharacterDataLAB.Hp > 0f;
        }

        /// <summary>
        /// ToMap 意图执行：沿路径移动 + Sliding/Stuck 熔断。
        /// 逻辑自原 WorkerMoveState.OnFixedUpdate 原样迁入（含日志文案与常量，不得简化——
        /// 熔断语义"同目标计数、换目标清零、≥4 次 → HandleMovementStuck"见 bug-fixes.md 2026-08-13）。
        /// </summary>
        private void TickToMap()
        {
            bool arrived = this.worker.Seek.MoveByPath();
            this.hasArrived = arrived;
            if (arrived)
            {
                return; // 到达/无路径：MoveByPath 内部已重置检测
            }

            BugCheckResult stuckResult = this.worker.Seek.LastStuckResult;
            if (stuckResult == BugCheckResult.Sliding)
            {
                // 位移不足但未完全卡死 → 预防性重新寻路绕开障碍。
                // 但若同一目标累计 Sliding 过多（A* 认为可通而物理被挡，如路径穿过床 sprite），
                // 静默重寻路会无限循环且无任何日志/失败缓存。累计 N 次后视为卡死，
                // 走统一的 HandleMovementStuck：建造任务保留 3 次重试，其他任务
                // RecordFail + GiveUpTask（决策层经 IsRecentFail 失败缓存进入冷却），
                // 打破"Sliding→重寻路→Sliding"死循环（观测 53 次/人，从不入睡）。
                if (this.lastSlidingTarget != this.worker.Seek.TargetMap)
                {
                    this.lastSlidingTarget = this.worker.Seek.TargetMap;
                    this.slidingStreak = 0;
                }

                if (++this.slidingStreak >= MaxSlidingStreak)
                {
                    this.slidingStreak = 0;
                    // 熔断诊断：记录卡住格的地图坐标与通行判定，交叉验证"碰撞已注册而 A* 仍判可通"。
                    Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
                    AWorkerTask.LogProvider(
                        $"[MoveDiag] {this.worker.name} Sliding 熔断 目标=({this.worker.Seek.TargetMap.x},{this.worker.Seek.TargetMap.y}) " +
                        $"posMap=({posMap.x},{posMap.y}) 可通行={ASeek.IsCanReach(posMap)} → HandleMovementStuck",
                        LogManager.LogLevelEnum.Debug);
                    this.worker.HandleMovementStuck(); // 内部已切回 Seek / 放弃任务
                    return;
                }

                this.worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                return;
            }

            if (stuckResult == BugCheckResult.Stuck)
            {
                // 真卡死 → 建造重试3次 / 记录失败点位并放弃任务
                // 卡墙诊断：记录卡住格的地图坐标与通行判定，确认是否已陷入墙/家具碰撞体。
                Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
                AWorkerTask.LogProvider(
                    $"[MoveDiag] {this.worker.name} Stuck 目标=({this.worker.Seek.TargetMap.x},{this.worker.Seek.TargetMap.y}) " +
                    $"posMap=({posMap.x},{posMap.y}) 可通行={ASeek.IsCanReach(posMap)} → HandleMovementStuck",
                    LogManager.LogLevelEnum.Debug);
                this.worker.HandleMovementStuck();
            }
        }

        /// <summary>
        /// 设置新意图并重置到达标记；意图实际变化时输出节流诊断。
        /// 战斗意图（Chase/KeepDistance/None）每帧重声明同参数 → 完全相等时短路
        ///（防每帧重置/刷日志）；ToMap 不短路——GoTo 必须重置 hasArrived（原 Move 状态
        /// OnEnter 重置 isTargetReached 的语义，短路会让上次到达残留误判）。
        /// </summary>
        private void SetIntent(WorkerMoveIntent next)
        {
            if (next.Kind != WorkerMoveIntentKind.ToMap
                && next.Kind == this.intent.Kind
                && next.Target == this.intent.Target
                && Mathf.Approximately(next.MinDistance, this.intent.MinDistance)
                && Mathf.Approximately(next.MaxDistance, this.intent.MaxDistance))
            {
                return;
            }

            bool changed = next.Kind != this.intent.Kind
                || (next.Kind == WorkerMoveIntentKind.ToMap && next.TargetMap != this.intent.TargetMap);
            this.intent = next;
            this.hasArrived = false;
            if (changed)
            {
                // 意图种类/目标变化 → 战斗路径子状态作废（旧路径目标点是上一意图语义选的，
                // 如 Chase→KeepDistance 交替；残留会被错误消费。已提交的异步寻路由下一次
                // TrySeekTo/StopMove 的 Seek 覆盖或取消）。
                this.combatPathMode = CombatPathMode.Idle;
                AWorkerTask.LogProviderThrottled(
                    $"{this.worker.name}|LocoIntent", 0.5f,
                    // 惰性求值：意图切换在战斗中高频发生，被节流时连 Describe() 也不调用
                    () => $"[MoveDiag] {this.worker.name} 移动意图 -> {next.Describe()}",
                    LogManager.LogLevelEnum.Debug);
            }
        }
    }
}
