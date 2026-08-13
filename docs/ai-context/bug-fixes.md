# Bug Fix Log — Unity 日志修复存档

> 每次通过日志分析并解决 bug 后，把思路追加到此文件。开始新任务前**先通读本文件**，命中历史记录时直接引用验证，避免重复排查。

## 2026-08-13 Gather 无邻居位置死循环刷屏（25k 次）

- **现象**：日志中 `Gather, 没有邻居位置!` 出现 25,189 次，89% 集中在开局 20:27–20:28 两分钟。同一 worker（如"葛杉"）在 30ms 内对同一目标 `(311,411)` 重试 5+ 次，workerPos 不动：`失败 → 自主决策[SelfGather] → 创建自我采集任务 → 又失败`。
- **根因**：`WorkerSeekState.OnEnter` 的"没有邻居位置"失败块只设置 `Task.LastFailedTime`，**未调用 `ASeek.RecordFail`**。而决策 `ScanForResources` 用 `ASeek.IsRecentFail` 过滤失败目标 → 过滤失效 → 失败后 `GiveUpTask` 释放 `GatherMap` 认领 → 决策立刻重新选中同一目标 → 无限循环。（对比：`OnUpdate` 里寻路失败有 `ASeek.RecordFail`。）
- **修复**：
  - `Scripts/2D/Character/Worker/State/WorkerSeekState.cs`：失败块加 `ASeek.RecordFail(this.targetMap)`（FailCacheTtl=30s）。
  - `Scripts/2D/Character/Worker/Task/WorkerGatherTask.cs`：`Init()` 邻居从 2 方向（`Neighbors[1],[3]`）扩为 4 正交方向（`Neighbors[0..3]`，对齐 `WorkerBuildTask`/`WorkerDemolishTask`），提高密集资源区成功率。
- **验证**：重跑开局，观察 20:27 段"没有邻居位置"应从 25k 级降至低位且目标分散。
- **教训**：**决策循环 bug 先查"失败后是否进入冷却/失败缓存"**。`RecordFail` 是寻路失败的标准通道，"没有邻居位置"失败应复用同一通道。

## 2026-08-13 睡眠任务无邻居死循环（早期修复，前提有误但有效）

- **现象**：`Sleep, 没有邻居位置!` 9,346 次 + `创建地面睡觉任务(无床)` 9,969 次刷屏。
- **根因（修正）**：早期分析曾误判"worker 坐标越出 200×200 地图"，实际地图为 **1000×1000**（日志第 18 行 `地图=1000×1000`），坐标从未越界。真正的死循环机制同 Gather：决策→睡眠→无邻居→放弃→重新决策睡眠。越界兜底（`EnsureValidPosition`）从未触发（0 次）即为此证。
- **修复（有效部分）**：`WorkerBrain` 新增 `SleepFailCooldownSeconds=10f`，睡眠失败后 10s 内改发 Wander —— 效果明显：`创建地面睡觉任务` 从 9,969 → 38 次。
- **教训**：**地图尺寸必须以日志/存档为准，不要凭代码默认值推断**（`TileMap.DefaultHeight=548` 与运行时 1000×1000 也不同）。越界修复（`TileMap.IsCanReach` 地图外 false、`GenCanReachPos` clamp）保留，作为防御性加固但非本次主因。

## 2026-08-13 床副格碰撞与 sprite 足迹错位导致寻路穿过床 + 门位堵家具致房间封死

- **现象**（用户报告两个问题）：
  - **问题 1 寻路穿过床/建筑**：A* 路线从床上穿过（床视为可通行），实际移动被床物理挡住，Worker 来回重试、从不入睡。
  - **问题 2 卡顿**：高频刷屏日志 + 大量寻路失败重试。日志观测 [掉落诊断] 3,174 次、[掉落入口] 2,903 次、房间布局字符画 dump ~68 次、大量 `没有找到路!`（单个 worker 50 次/人）。
- **根因**（两条独立根因，机制同属"决策循环 bug：失败未进冷却/失败缓存"历史族）：
  - **根因 A — 门位堵家具，房间封死**：`GenerateRandomRoomParams` 旧门规避逻辑只避开床主格所在列（doorY==1），漏掉床副格列与仓库列。5×7 房间（doorSide=0 doorIndex=0）门 (465,287)，进门第一格恰为床副格 (466,287)，预注册时该格被 `RegisterCollisionTile` 注册为不可通行碰撞体（IsComplete=true）→ 房间从预注册起永久封死 → 对房间内任意位置寻路全部"没有找到路"（50 次/人）→ GiveUpTask → 重新 SelfBuild → 死循环。
  - **根因 B — 床逻辑副格与 sprite 足迹错位**：`RoomLayout.BedSecondOffset` 原取 `BedDef.GetOccupiedPositions(BedOffset)[1]` = 逻辑副格 **tile-y+1**，转置后落在世界坐标 X+1（(463,303) 附近），而床 sprite 实际沿世界 Y+1 延伸（tile-x+1，如 (464,302)）。物理碰撞体随 sprite 覆盖 tile-x+1，但 WalkabilityCache 只标记了逻辑副格 → A* 认为 (464,302) 可通行而径直穿过床，实际移动被 sprite 碰撞体挡住 → Sliding → 静默无限重寻路（观测 53 次/人，从不入睡），与用户问题 1 描述完全吻合。
- **修复**：
  - `Scripts/2D/AI/Worker/WorkerBrain.cs`：重写 `GenerateRandomRoomParams` 门规避——逐个候选门位调用新 helper `IsDoorEntryBlockedByFurniture` 验证进门第一格不落在家具上（床主格/床副格/仓库）；全部门位被堵时换参数重试（最多 8 次），极端几何回退 7×7 可用组合（审查发现：原"回退 index 0"对 5×5 doorSide=0 会静默复现封门，已修）。
  - `Scripts/2D/AI/Worker/WorkerBrain.cs`：`RoomLayout.BedSecondOffset` 从 `GetOccupiedPositions[1]`（tile-y+1）改为 `new Vector3Int(BedOffset.x + 1, BedOffset.y, 0)`（tile-x+1 = 世界 Y+1 = sprite 实际足迹）。`RegisterCollisionTile` 注册的碰撞瓦片与物理阻挡对齐；`PrintRoomLayout` 字符画本就按 bedVis=tile-x+1 绘制，两者归位一致。
  - `Scripts/2D/Character/Worker/State/WorkerMoveState.cs`：Sliding 分支熔断——同一目标累计 Sliding ≥4 次视为卡死，走统一 `HandleMovementStuck`（建造任务保留 3 次重试，其他任务 `RecordFail` + `GiveUpTask`），打破"Sliding→重寻路→Sliding"静默循环。审查修正：不直接 `GiveUpTask`/设 `LastSleepFailTime`，避免绕过建造重试、以非睡眠失败抑制睡眠决策、以及 `GiveUpTask` 后再切状态的双重转换。
  - `Scripts/2D/Character/Worker/Task/AWorkerTask.cs`：`TryMergeOrPlaceDrop` 的 [掉落诊断] 日志从 `failCount < 3` 改为 `failCount < 1`（每次放置只记一条，减少 5 次 ServiceLocator 查询开销与刷屏）。
- **验证**：逻辑推演——根因 A 修复后进门第一格 (466,287) 变空（碰撞移到 (467,286)），房间可进入；根因 B 修复后 (464,302) 变不可走、(463,303) 变可走，床可到达北邻居入睡；门规避防未来房间封死；Sliding 熔断兜底残留循环。需重跑游戏观察"寻路穿过床"消失、`没有找到路` 计数骤降。
- **审查（/review-work 修正）**：Bug Hunter + Rules Auditor + Architect 三评审者确认 INTENT 一致后，发现并修复两处——
  1. `GenerateRandomRoomParams` 兜底 `Add(0)`：5×5 doorSide=0 时家具块占满左墙内侧列，三个候选门位全堵，兜底接受 index 0 会复现封门（12% 房间触发）。改为重试生成参数（最多 8 次）+ 极端回退 7×7 可用组合。
  2. Sliding 熔断高度：直接 `GiveUpTask` 绕过建造 3 次重试、无条件抑制 10s 睡眠决策、且 `GiveUpTask` 后重复 `ChangeState(Seek)` 造成 `WorkerSeekState.OnEnter` 同帧双跑（决策管线跑两遍）。改为复用 `HandleMovementStuck` 统一路由。
- **教训**：
  - **碰撞注册位置必须与 sprite 实际足迹一致**：家具的多格占用关系（`GetOccupiedPositions`）是"逻辑占位"，不代表物理碰撞体覆盖范围；多格家具注册碰撞瓦片时应以视觉/物理足迹为准。
  - **门规避要验证"进门第一格"而非只避开家具主格所在行列**：家具块 3×2 占满 5 宽房间内部时，任何列都可能是入口格。
  - **Trace 级高频日志（掉落诊断/房间布局 dump）应降频或降级**：每次 ServiceLocator 查询在 3k+ 次规模下贡献明显卡顿。
  - 建议架构级跟进：将床的物理足迹（sprite/碰撞体）与逻辑占位统一为单一数据源（如让 `BedDef.GetOccupiedPositions` 返回世界坐标足迹），避免同类错位再次出现。

## 2026-08-13 Worker 卡死循环：放弃任务未进冷却 → 立即重接同一任务（PickUp/Build 站着不动）

- **现象**：用户报告"很多 Worker 处于 PickUp 状态站着不动"。日志显示 7 个 Worker 陷入"卡死→放弃→重接→再卡死"循环，各数百次：黄良 456 次 Stuck/114 放弃、熊茂霖 465/118、戚彬 329/68、苏茂 265/103、宋树 200/103、周刚豪 217/104、禹胜杰 44 次放弃 PickUp。卡死点位均为房间建造现场（床/墙碰撞体已注册）：黄良卡严保床格 (378,122) 可通行=False、宋树/周刚豪卡屈刚床格 (318,158)、苏茂卡屈刚墙位 (318,160)、禹胜杰目标 (367,170) 与自身仅 2 格却完全不动（墙 (367,169) 已 SetComplete 加碰撞体，A* 缓存仍直线穿过）。
- **根因**（命中历史记录族：决策循环 bug — 失败后未进冷却/失败缓存）：
  1. **物理卡死（诱发）**：Worker 在房间建造现场被新注册的床/墙碰撞体困住——部分直接站在碰撞格上（可通行=False），部分被已完成墙挡住但 A* WalkabilityCache 未同步（可通行=True，机制未完全定位，见教训）。
  2. **循环放大（主缺陷）**：`AWorker.HandleMovementStuck` 放弃前只 `RecordFail(Seek.TargetMap)`（寻路目标=邻居格），**未设置 `workerData.Task.LastFailedTime`** → `IsInCooldown=false` → `GiveUpTask` 回池 → `CreateTaskSnapshots` 不跳过 → `RunTaskAssignmentLoop` 立即把同一任务重接回同一 Worker → 无限循环。对比 `WorkerSeekState.OnEnter` 的"没有邻居位置"块**有**设置 `LastFailedTime`——属漏改而非机制缺失。
  3. **PickUp 次要缺陷**：`WorkerPickUpTask.Init()` 只用 `Neighbors[8]`（自身格），worker 相邻格时"没有邻居位置"→放弃（华广 3 次）。
- **修复**：
  - `Scripts/2D/Character/Worker/AWorker.cs` `HandleMovementStuck`：放弃前设置 `Task.LastFailedTime = UnityEngine.Time.time`（进入 10s 冷却，`CreateTaskSnapshots` 跳过）+ 将**任务自身目标**（不只寻路目标）`Vector3IntLAB.ToVector3Int(Task.TargetMap)` 记入 `ASeek.RecordFail`（决策层 `IsRecentFail` 过滤用）。打破立即重接循环。
  - `Scripts/2D/Character/Worker/AWorker.cs` 新增 `TryRescueFromUnwalkableTile`：当前格 `ASeek.IsCanReach(posMap)==false`（站在碰撞体上）时，螺旋搜索半径 6 内最近可行走格并传送，解冻卡死在床/墙上的 Worker（黄良/苏茂/宋树/周刚豪）。**行为变更（直接改 transform.position），需审查，多客户端场景注意同步。**
  - `Scripts/2D/Character/Worker/Task/WorkerPickUpTask.cs` `Init()`：邻居从只 `Neighbors[8]` 扩为 4 正交 + 自身（对齐 Gather/Build/Demolish 历史修复）。`FinishFromGround` 以 `TargetMap` 定位掉落物，不受拾取格影响，扩展安全。
- **验证**：重跑游戏观察——日志中 `[TaskDiag] 卡死→放弃(已RecordFail+冷却)` 应替代原 `(已RecordFail)`；同一 Worker 对同一任务的连续放弃间隔应 ≥10s（冷却）；`[MoveDiag] 救援传送` 应对卡在碰撞体上的 Worker 出现；黄良/熊茂霖等 7 人不再数百次刷屏。
- **教训**：**失败放弃必须同时"进任务冷却 + 记失败缓存"双通道**，缺一都会让决策/分配层立刻重试同一目标。`RecordFail(Seek.TargetMap)` 只覆盖寻路层（邻居格），任务自身目标必须单独记录。另：Worker 站进已注册碰撞体（可通行=False）属无解状态，物理推挤无法自救，需主动救援传送；A* WalkabilityCache 与物理碰撞体在 `SetComplete` 后仍不同步的问题（可通行=True 却物理被挡）未完全定位，建议后续单独排查线程竞态/维度交换。

## 2026-08-13 Gather 链式拾取最后一项捡完即卡死：WorkerWorkState 解引用已置空任务（回归）

- **现象**：用户报告"Gather 之后 PickUp 掉落物，捡完之后站着不动"。日志（单次运行 23:28:50–23:32:40）中 **~40 个 Worker** 全部在链式拾取**最后一件** `从地面捡起物品(id=300000, count=50)` 后**彻底静默**（无任何状态切换/新任务/移动日志，如成广从 23:31:18 静默 78s+）。关键证据：`完成任务 type=PickUp` 共 145 条，但 `[StateDiag] 任务完成: PickUp` 仅 144 条——**每个链式拾取的最后一项都缺这条 StateDiag**；`完成任务 type=Gather` 48 条对应 StateDiag=0（Gather 完成时任务被链式替换成 PickUp，StateDiag 显示 PickUp 目标）。
- **根因**（命中历史记录族 + 上一轮引入的回归）：`AWorkerTask.Finish` 末尾**无条件 `workerData.Task = null`**（AWorkerTask.cs:595）。链式拾取中间项由 `WorkerPickUpTask.FinishFromGround` 重建后继任务（`workerData.Task = nextTask`，非空），但**最后一项**（无 pendingPositions、无 chainCompleteTask）不重建 → `workerData.Task` 保持 null。随后 `WorkerWorkState.OnUpdate` 的 `if (isComplete)` 块执行 `workerData.Task.TaskType` 字符串插值 → **NullReferenceException** → `waitOneFrame = true` 永不执行 → 下一帧 `workerData.Task == null` 提前 return → **Worker 永久卡在 Work 状态"站着不动"**。该 deref 是上一轮 `5ba586d5 feat(diag)` 加"任务完成诊断"日志引入的回归（原代码 `if (isComplete) waitOneFrame = true` 对 null 安全）。日志无异常记录原因：自定义 LogManager 未捕获 Unity 的 Debug.LogException，且 AWorker.Update 无 try/catch，异常被 Unity 吞掉后每帧静默 return。
- **修复**：`Scripts/2D/Character/Worker/State/WorkerWorkState.cs` —— `Execute` **前**捕获 `AWorkerTask currentTask = workerData.Task`，完成日志/独白改用 `currentTask`（不再 deref 已置空的 `workerData.Task`）。**涉及状态流转（Work→Seek 完成路径），需用户确认**；改动仅 1 文件，属恢复既定"完成→waitOneFrame→下一帧 Seek 再决策"语义。修复同时纠正诊断日志：链式中间项此前误打"下一个任务的目标"，现在打"已完成任务的目标"（更准确，逻辑不变）。
- **验证**：重跑游戏，观察链式拾取最后一项（id=300000 count=50）之后应出现 `[StateDiag] X 任务完成: PickUp 目标=...` + `[StateDiag] X 状态切换 Work -> Seek` + 新 `[TaskDiag] X 开始任务 type=...` 或漫游日志；不再有"捡完即静默"的 Worker。亦可临时观察：修复前 `完成任务 type=PickUp` 数比 `[StateDiag] 任务完成` 多出"链数"条。
- **教训**：
  - **诊断日志也可能引入回归**：在"Execute 后仍使用 workerData.Task"的位置加日志前，要意识到 `Finish` 会把任务置空或替换，**日志/后续逻辑必须用 Execute 前捕获的引用**。
  - **任何任务类型的 Finish 都会置空 workerData.Task**（AWorkerTask.cs:595），不只是 PickUp：无掉落采集、单阶段 Build 等所有"Finish 不重建后继"的任务完成都会踩同一 NRE。`WorkerWorkState` 是唯一在 Execute 后 deref workerData.Task 的位置，已修复；架构级建议：把"任务完成→置空"与"成功后续任务接管"的职责收口（如 Finish 返回后继任务），避免子类遗漏重建。
  - 自定义 LogManager 与 Unity 原生异常日志分离：**日志驱动排查时，"无异常记录"≠"无异常"**，要结合行为证据（静默 Worker、缺失的 StateDiag 行）推断被吞掉的异常。
