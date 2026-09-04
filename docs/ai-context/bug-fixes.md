# Bug Fix Log — Unity 日志修复存档

> 每次通过日志分析并解决 bug 后，把思路追加到此文件。开始新任务前**先通读本文件**，命中历史记录时直接引用验证，避免重复排查。

## 2026-09-04 BountyRestore 每 tick 刷屏（悬赏运行期 ~2400 条/人）
- **现象**：2026-09-03 局 game.log 孔峰瑜/范学各 `SetTask type=Bounty source=BountyRestore` 约 2400 次（悬赏运行期 2 次/秒，16ms 内 5 连发），00:13-00:27 风暴后随悬赏结束自愈；Debug 级只进 game.log 不刷 Console。
- **根因**：`WorkerBountyTask.Execute` 注释意图「innerTask.Finish 清除 Task 后恢复悬赏本体」，实现却是 `if (workerData != null)` 每 tick 无条件 `SetTask(BountyRestore)`——innerTask 未完成时 Task 本来就是 this，恢复是冗余调用。功能因 SetTask 对 BountyRestore 的「不重启不打断」特判（AWorker.cs）而侥幸正确，纯日志刷屏 + 每 tick 无效赋值。
- **修复**：前置条件 `workerData.Task == null` 才恢复——语义完全保留（innerTask Finish 清空后下一次 Execute 恢复本体），其余帧零开销。
- **验证**：Unity Roslyn 按 Bee rsp 全量编译通过；行为验证待新局悬赏运行（预期 BountyRestore 日志从 2/s 降至每 innerTask 完成时 1 次）。
- **教训**：**「每 tick 轮询里恢复状态」必须判断状态确实丢失才恢复**——幂等赋值看似无害，但每次都走日志/分支链路，长任务放大成千条刷屏；恢复语义写 `if (丢失) 恢复`，不写 `无条件恢复`。

## 2026-09-04 启动期 Warning「没有名字为Seed0的道具」+ id=0 幽灵种子入包
- **现象**：每次启动 `Enter: CreateOrJoinPanel` 后必现 1 条 Warning「没有名字为Seed0的道具!!!」（game.log 2026-09-03 局实锤，error.log 空）。
- **根因**：种子 SO 合并为单条 `Name=Seed`（SeedItemData.asset）后，`Seed0.cs` 成「有类无 SO 条目」孤儿——`ItemInstanceFactory.InitItemInstances` 第三步兜底（有类即可入包）把它反射注册进 `backpackItemTypes`，启动背包填充 `BackpackController.Awake → GenBackpackItems()` 对它调 `GetByName("Seed0")` 查不到 → Warning 且 `ItemData.Empty.Id=0`，**背包里多一件 id=0 幽灵种子**。与 2026-08-24「asset not found 刷屏」同型（SO 条目与反射类不对齐，兜底路径放大）。
- **修复**：`Seed0` 改 `abstract`——`Tool.GetChildByParent` 过滤抽象类，孤儿类不再被兜底注册（一行改动，不删文件）。
- **验证**：Unity Roslyn 按 Bee rsp 全量编译通过；种植系统按 `ItemTypeProvider==Seed` 判断不依赖类名（grep 已证）；地面拾取路径 `ItemMap.PickUpItem` 有 KeyNotFoundException 防护，无崩溃风险。
- **教训**：**合并/精简 ItemData SO 条目时，同步处理对应 C# 具体类**（改 abstract 或补新条目名），否则反射兜底静默制造幽灵物品；「有类无 SO」的孤儿类唯一可见症状可能只是一条低频 Warning，但数据污染（id=0）已发生。

## 2026-09-03 启动期两条必现 Warning（Attack Not Found / NullItemData scriptable not found）

- **现象**：game.log 启动早期（13:22 局）各 1 条 Warning：`Attack Not Found!!!`（Tool.GetComponentInChildren 带 name 重载）与 `NullItemData scriptable not found!!!`（ResourceManager.TryGetResource）。每局必现，量小但恒在。
- **根因**（两条独立）：
  1. `ForegroundPanel` 构造器查找名为 "Attack" 的 Button 并绑攻击事件——按钮已从 Foreground UI 移除，查找必失败；且 null 检查块内**重复二次查找**（冗余 GetComponentsInChildren）。攻击功能不受影响：主入口是 `PlayerAttackRequestedEvent` 事件总线（Player.cs 发布 → ForegroundPanel.OnPlayerAttackRequested）。
  2. `AItem.Ranges["Resource"]` 上界写成了 `ItemTypeEnum.Null`——那是**仓库空槽占位值**（注释"空(用于仓库)"），不是资源类型。`ItemDataManager.Awake` 按范围枚举类型拼 `{Type}ItemData` 查 SO，`Null` → 查 "NullItemData" → 不存在 → Warning。`Ranges["Resource"]` 全库唯一消费者就是这段扫描，改上界为 `Tree` 安全。
- **修复**：删 ForegroundPanel Attack 查找块（OnClick_Attack 方法保留供直接调用）；`Ranges["Resource"]` 收窄为 `{ Tree, Tree }` + 注释说明。
- **验证**：csc 编译通过。重进游戏 game.log 不再出现这两条 Warning；攻击功能走事件总线正常（点按攻击键仍触发）。
- **教训**：
  - **枚举占位值（Null/None/Empty）混进"范围扫描"的上下界是幽灵查询的经典来源**——占位值语义是"槽位为空"，被当类型名拼进资源查询必失败。范围字典的上下界应是真实类型的闭区间，扩展时显式登记新类型。
  - UI 按钮移除后要同步删代码侧的查找绑定，否则每次初始化必打一条"Not Found"——这类恒现 Warning 会稀释日志里真正异常的信号密度。
  - 排查"日志字符串在源码中搜不到"先怀疑**参数化拼接**（`$"{name} {kind} not found"`）：直接搜完整消息落空，要拆成"哪个方法能产生此格式"反查调用链（本次 TryGetResource 的 typeLabel 参数）。

## 2026-09-03 GameTimeUI.Awake NRE（场景 GlobalLight 未激活，FindWithTag 返回 null）

- **现象**：error.log 启动早期（index=3，大厅加载阶段）1 条 `NullReferenceException: GameTimeUI.Awake (GameTimeUI.cs:28)`。
- **根因**：`Scenes/Game.unity` 的 GlobalLight 物体 `m_IsActive: 0`（tag 已注册、代码无任何激活逻辑）。`FindGameObjectWithTag` **不检索未激活物体** → 返回 null → `GetComponent<Light2D>()` NRE。Awake 在 28 行中断 → `pointer`/`gameTimeManager` 未赋值，`Update` 里的 deref（`gameTimeManager.CurGameTime`/`globalLight.intensity`/`pointer.localRotation`）全部是每帧 NRE 雷。
- **修复**（`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/GameTimeUI.cs`，纯代码不改场景）：Awake 全字段容错（Text/Image 子组件 `?.` 容错、GlobalLight 缺失记 Warning 不中断）+ `globalLight` 5s 懒重试（场景物体被激活后自动接上，重试失败静默防刷屏）+ `Update` 前置守卫（`gameTimeManager`/`pointer`/`gameTime` 任一 null 直接 return，退化只读）。
- **验证**：csc + Bee rsp 编译通过（EXIT=0）。重进游戏 error.log 不再出现该 NRE；GlobalLight 禁用状态下时间 HUD 正常走字、无光照变化；Inspector 激活 GlobalLight 后 ≤5s 光照开始联动。
- **教训**：
  - **`FindGameObjectWithTag` 对未激活物体返回 null 而非异常**——场景物体 `m_IsActive: 0`（用户手动禁用/忘记恢复）时 Awake 必崩。"按 tag 找场景物体"必须 null 检查。
  - **Awake 中途抛异常的连锁伤害大于崩点本身**：中断点之后的字段赋值全部未执行，NRE 会在 Update 里以"每帧"复发；守卫要覆盖 Awake 里赋值的**全部**字段，不只崩点那一个。
  - 排查时 Log 行格式 `[UnityException] <index> <异常>` 中数字是 LogManager 的全局 index 计数（第几条日志），不是发生次数。

## 2026-08-31 树同位置双渲染（tile 复活）+ GenTree 无限补种（TreeCurCount 负失衡）

- **现象**：用户报告同一位置两个树——一份在 Tilemap 上（不透明、无动画、不淡化），一份是独立视觉物体（TreeVisual_*，有动画、可遮挡淡化）。首报位置 (63,140)。
- **定位**：`TileVisualSpawner.CreateOrUpdate` 加两处 Warning 级诊断——①入口巡检：`visual` 字典已有该格 SR 但 `GetColor(cell).a > 0.5` → `[BuildDiag] 重影实锤`（带 tile 名/alpha/TileFlags）；②`SetColor(a=0)` 后立即回读，失败 → `[BuildDiag] tile 隐藏失败`。一局抓到 18 条"重影实锤"，全部 `a=1 flags=LockColor`，且逐条可与 GenTree/CutTree 位置对上（命中时刻=RefreshAround 路过发现时刻，晚于被弄坏时刻）。
- **根因 1（重影）**：`ResourceMap.GenTree` → `RefreshRound(pos)` 对半径 4 共 81 格 `tilemap.RefreshTile`——**RefreshTile 会从 tile 资产重取 TileData 并把 instance 颜色/flags 重置回资产默认（白 a=1 + LockColor）**，抹掉 `SetColor(a=0)` 隐藏；SR 仍在 → TilemapRenderer 与独立 SpriteRenderer 同位置双渲染。GenTree 位置自身必复发（RefreshRound 含中心格）。
- **修复 1**：删除 `GenTree` 的 `RefreshRound` 调用与方法本体（唯一调用者）。RuleTile 邻域形态调和已由 `SetTile → TileVisualSpawner.RefreshAround → GetSprite 重解析`承担；tile 本体被隐藏，形态不可见，RefreshTile 无意义。
- **根因 2（数目）**：`GenResource` 只设 `TreeTotalCount = resourcesPlaced`，`TreeCurCount` 保持初始 0——初始 N 棵树未计入当前数；Worker 砍初始树 `TreeCurCount--` 无下限 → 日志 remain 一路负到 -42 → `while (TreeCurCount < TreeTotalCount)` 恒真 → GenTree 每 10s 无限补种，树越种越多。
- **修复 2**：`GenResource` 末尾 `TreeCurCount = resourcesPlaced`（与 TreeTotalCount 同起点；砍树减、补种加，总量守恒语义才成立）。
- **验证**：重进游戏 1-2 分钟，grep game.log `重影实锤|隐藏失败` 应为 0；`CutTree ... remain=` 不再负增长（读旧档时存量负值仍会保留，需新档或读档校准）；GenTree 仅在砍树后补种。
- **教训**：
  - **`Tilemap.RefreshTile` 不是只读刷新——它会重置该格 instance color/flags 为 tile 资产默认**。凡用 `SetColor` 做隐藏/状态色的 Tilemap（视觉拆分架构的 ResourceMap/ItemMap/BuildMap），邻域 RefreshTile 会让隐藏静默失效。TileMap 地形 chunk 的边界 RefreshTile（SyncAllChunkBordersCoroutine Phase 2）只刷真实格不刷幽灵格，当前安全——但未来给幽灵格邻域加刷新时警惕同类失效。
  - **"计数守恒"必须两端对齐**：总数与当前数在同一时刻初始化为同一真实值，删除/新增各维护一边；`while (cur < total)` 型补种循环对 cur 无下限保护时，一次计数缺口就是无限循环。
  - **重影类渲染 bug 的定位手法**：先枚举"设计上同位置双渲染"的正常结构（tile 隐藏 + 独立 SR），再对可疑格加"回读校验"（SetColor 后立即 GetColor）与"状态巡检"（字典有 SR 但 tile 不透明）两类事件点日志——一局即可拿到 a/flags 实锤，配合 GenTree/CutTree 时间线对齐锁定凶手。

## 2026-08-23 沙漠/雪地资源树缺 ItemData 回退 Bottom（sortingOrder=-1000）不淡化

- **现象**：用户报告 DesertCoconutTree 配 LayerMode=Alpha 却不淡化，检查 SpriteRenderer 发现 sortingOrder=-1000（= `WorldYSortManager.BottomLayerOrder`）。game.log 观测 `没有名字为DesertCactus` 36 次、`SnowMushroom2` 16 次、`SnowMushroom1` 14 次、`SnowStone` 13 次；DesertCoconutTree 0 次缺失警告。
- **根因**：`Resources/SO/TreeItemData.asset` 只定义了 6 种资源树（GrassTree/DesertCoconutTree/SnowTree/SnowTreeRoot/DesertBushes/DesertGrass），而 `Resources/Tilemap/Map/Resource/` 下实际有 **10 种** tile（多出 DesertCactus/SnowMushroom1/SnowMushroom2/SnowStone）。`ResourceMap.GetResourceLayerMode` → `ItemDataManager.GetByName(tile.name)` 查不到 → 返回 `ItemData.Empty`（LayerMode=Bottom）→ `TileVisualSpawner` 以 Bottom 模式创建（`sr.sortingOrder = -1000` 恒底层，不注册 WorldYSortManager / OcclusionFader）→ 玩家走到其后面不淡化。用户看到的"-1000 椰子树"实为沙漠仙人掌 DesertCactus（沙漠地形 4 种绿植随机生成，外观相近）。
- **修复**：`Resources/SO/TreeItemData.asset` 补 4 个条目（DesertCactus/SnowMushroom1/SnowMushroom2/SnowStone，LayerMode=Alpha=1）。`ItemDataSO.OnEnable` 运行时按列表顺序自动重排 Id（`index = type*100000` 逐条递增），补条目不产生 Id 冲突。补后 10 tile 与注册名单完全对齐。
- **验证**：重进游戏触发 `visuals.RebuildAll()` 重建既有 -1000 视觉，仙人掌/蘑菇/雪石应与其他树一样参与 y 排序、玩家走到后淡化。
- **教训**：**资源 tile 资产与 ItemData 定义必须一一对应**——`GetResourceLayerMode`/`TryGetGatherResourceInfo` 对查不到的名字静默回退 Bottom（-1000）/Id=0，缺失定义不会报错而是降级为恒底层。新增地形资源 tile 时同步补 `TreeItemData.asset`；排查"某树不淡化"先 grep game.log `没有名字为` 警告定位缺失 ItemData 的 tile。

## 2026-08-23 Shop 配 LayerMode=Normal 却仍淡化（DirectBuild 生成顺序回退 Alpha）

- **现象**：Shop 的 BuildItemData.LayerMode=2（Normal，不淡化），但玩家走到商店后面商店仍变半透明。
- **根因**：`ShopNPCGenerator.CreateShopAt` 先 `DirectBuild`（内部 `visuals.CreateOrUpdate` 会读 `BuildMap.GetBuildLayerMode`）再写 PosMap。`GetBuildLayerMode` 在 PosMap 无 Shop 条目时回退 `ItemLayerMode.Alpha` → 商店以"可淡化"模式注册进 OcclusionFader，与 Normal 相悖。属生成顺序 bug（正序 bug 的同族：状态在写入前被读取）。
- **修复**：`Scripts/2D/Gameplay/ShopNPCGenerator.cs` 将 PosMap 登记移到 `DirectBuild` 之前（`if (!PosMap.ContainsKey) PosMap[pos]=...`）。
- **验证**：商店应保持 Normal（不注册 Occluder），玩家走到其后面不淡化。
- **教训**：**DirectBuild 回退 Alpha 是设计上的兜底**，任何先建视觉后登记数据的路径都会踩中。排查"配了 Normal/Bottom 却淡化/未淡化"时，先确认视觉创建时刻 `GetBuildLayerMode`/`GetResourceLayerMode` 读到的数据是否已就绪（PosMap/ItemData 注册）。

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
  - 架构级跟进**已解决（2026-08-14）**：见下方「床足迹错位」条目——统一了 `GetOccupiedPositions` 的轴语义，床的逻辑占用与物理足迹不再分叉。

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

## 2026-08-13 SeekEnemy 被障碍物卡死刷屏：漫游卡死无自救（33k StuckDiag）

- **现象**：用户报告"很多 SeekEnemy 被各种障碍物卡住了"。单次运行（23:39:14–23:52:42）`[StuckDiag] seekenemy 结算=Stuck` 33,164 条、Sliding 77 条（另 494 条 StuckDiag 为 Worker，Worker 能自救）。卡死随时间**线性累积**（23:40 每分钟 1 条 → 23:52 每分钟 5,944 条），位置遍布全图（Top 卡点 (314,474)/(862,318)/(598,330) 各 1,307/1,215/1,183 条），`ratio=0.00` 占 31,727 条（纹丝不动），`pathIdx=0/N`（连路径首节点都到不了，如 pos=(598,330) 目标 (332,598) 仅隔 2 格）。seekenemy `提交寻路` 7,217 vs `到达终点` 7,140（差≈卡死未完成数）；`→ Move` 7,274 vs `← Move` 7,192（差≈仍卡在 Move）。状态机仍在流转，但单个敌人卡死每秒刷一条 StuckDiag 永不消散。
- **根因**（命中历史记录族：决策循环 bug—失败后未进入冷却/失败缓存；及 WalkabilityCache 与物理碰撞体不同步的未根治深层问题）：
  1. **物理阻挡（诱发）**：敌人被物理碰撞体挡住而 A* 缓存判定可通（卡点附近 MapDiag 仅稀疏 GenTree 记录，绝大多数阻挡来自缓存与物理不同步，见 2026-08-13 Worker 卡死循环条目教训）。`MovementStuckDetector` 每秒结算 `Stuck ratio=0.00`。
  2. **无自救（主缺陷）**：`ASeekEnemy.HandleMovementStuck` 只在 `this.Target != null` 时执行（StopMove+重新寻路），而漫游态（`SeekEnemySeekState.OnEnter` 设 `Target=null`）**空操作** → 不 StopMove、不重新寻路 → 下一帧继续喂同一被挡路径 → 每秒再结算 Stuck → 无限循环。对比 Worker：`AWorker.HandleMovementStuck` 有熔断（建造重试3次→RecordFail+任务冷却+救援传送+GiveUpTask），敌人侧完全没有熔断/放弃/失败缓存。
- **修复**（`Scripts/2D/Character/Enemy/SeekEnemy/ASeekEnemy.cs` + `.../State/SeekEnemyMoveState.cs`，**行为变更需用户确认，未提交**）：
  - `ASeekEnemy.HandleMovementStuck`：`Target==null`（漫游）时改以 `Seek.TargetMap` 为目标重新寻路，不再空操作。
  - 新增 `ASeekEnemy.AbandonMovementStuck`：卡死熔断——StopMove + ResetStuckDetection + 回 Seek 状态换新漫游目标。**不调用 RecordFail**（失败缓存是 Worker 决策层共享状态，敌人点位记入会污染 Worker 资源目标）。
  - `SeekEnemyMoveState` 镜像 WorkerMoveState 熔断：`MaxStuckStreak=4` + `stuckTarget`/`stuckStreak`；`LastStuckResult != None` 时按目标累计，≥4 次→`AbandonMovementStuck`，否则 `HandleMovementStuck`；目标变化/进入 Move 重置计数。到达（isTargetReached）**不**重置计数（与 Worker 一致，避免寻路间隙假到达清空熔断）。
- **验证**：重跑游戏观察——`[StuckDiag] seekenemy 结算=Stuck` 不再无限刷屏，同一敌人同坐标 Stuck 应 ≤4 条后出现 `[EnemyDiag] seekenemy 卡死熔断 ... → 放弃回 Seek`；`→ Move` 与 `← Move` 计数差不再随时间拉大；`到达终点` 计数回升。需在 Unity 中确认敌人会绕开/放弃被挡点位、不会反复抽搐。
- **教训**：
  - **"失败后是否有冷却/失败缓存/放弃机制"是决策循环 bug 的第一检查项**：Worker 有熔断而敌人没有，是同一族缺陷在敌人侧的缺失。
  - **`HandleMovementStuck` 等重寻路方法在"目标为空/漫游"路径必须兜底**：只处理 `Target!=null` 的重寻路会在漫游态静默失效，让卡死刷屏。
  - WalkabilityCache 与物理不同步是**跨 Worker/敌人**的共同诱发根因，本次未根治（修复让敌人能自救脱离，但反复触发仍会有偶发 Stuck）；建议架构级跟进：统一物理足迹与 A* 缓存数据源 + 并发缓存原子写入。

## 2026-08-14 SeekEnemy 卡死救援传送（镜像 Worker TryRescueFromUnwalkableTile）

- **现象**：敌人熔断（AbandonMovementStuck）后虽放弃目标回 Seek，但若当前格本身不可通行（被新完成建筑/床的碰撞体困住），仍物理困在原地，无法执行移动，卡死仍会复发。
- **根因**：敌人侧缺少"卡在不可通行格上"的解冻机制；Worker 侧 `AWorker.TryRescueFromUnwalkableTile`（螺旋搜索半径 6 传送）已存在且有效。
- **修复**（`Scripts/2D/Character/Enemy/SeekEnemy/ASeekEnemy.cs`）：新增 `ASeekEnemy.TryRescueFromUnwalkableTile`（镜像 Worker 实现：当前格 `!ASeek.IsCanReach` 时螺旋搜索最近可通行格传送），在 `AbandonMovementStuck` 熔断放弃时调用。
- **设计取舍**：救援传送只在**熔断放弃**时触发，而非每次 `HandleMovementStuck` 重寻路时——先重寻路尝试绕开，多次无效才传送，避免滥用传送导致敌人闪现。Worker 侧则每次卡死都查（Worker 有任务上下文，频繁传送影响小）。
- **验证**：观察 `[EnemyDiag] <name> 卡死在不可通行格(...) → 救援传送至(...)`（Warning 级进 Console），熔断后敌人应能移动去新漫游点而非原地。
- **教训**：**角色被不可通行格困住时，"换目标/重寻路"都不够，必须提供物理解冻**（传送脱困）。Worker 有、敌人没有，是同一族自救机制的缺失。

## 2026-08-14 床足迹错位：逻辑副格(y+1) vs sprite 物理足迹(x+1)，A* 判可通行而物理被挡

- **现象**：Worker/SeekEnemy 在床旁卡死（`[StuckDiag] ratio=0.00`），"缓存判可通行、物理被挡"的最终机制之一。玩家建造/远程同步的床卡人，自动建家路径却不卡。
- **根因**（WalkabilityCache 与物理不同步的跨 Worker/敌人共同根因，最终定位）：
  - `TileMap.MapPosToWorldPos`(TileMap.cs:757) 做 **90° 转置**：tile (x,y) → 世界 (y,x)。床 sprite 视觉**竖向**（世界 Y 延伸）= tile 空间沿 **x+1** 延伸 → 物理碰撞体覆盖主格与 **tile (x+1,y)**。
  - **根本缺陷——`ABuildItem.GetOccupiedPositions` 轴语义写反**：注释自称"逻辑与 IsAvailableMap.ShowRect 保持一致"，但代码 `positions.Add(x + j, y + i)`（j=width 循环扩展 x、i=height 循环扩展 y）与 `ShowRect` 的 `(x+i, y+j)`（i=height 扩展 x、j=width 扩展 y）**恰恰相反**。对 `SingleBed`（Width=1, Height=2, BottomLeft）按错误语义算出副格 **tile (x,y+1)** = 世界横向 → 与 sprite 物理足迹错位 90°。
  - 结果：碰撞瓦片注册在错误的 (x,y+1)；真正被挡的 (x+1,y) 无 PosMap 条目 → `BuildMap.IsCanReach(x+1,y)` fallback `IsFreeTile` 返回 true → **A* 判可通行、物理被挡**。
  - `WorkerBrain.cs:1204-1211` 开发者已自记此机制，且自动建家路径用 `BedSecondOffset=(x+1,y)` 修复（观测 53 次/人从不入睡）；但玩家建造（`BuildingUI → AddBuildTask`）与远程同步（`SetComplete` 多格传播）仍用 `GetOccupiedPositions` 的错误足迹。
- **修复**（`Scripts/2D/Item/Build/ABuildItem.cs`，**根因修复，用户确认，不设每物品开关**）：
  - `GetOccupiedPositions` 循环改为 `positions.Add(x + i, y + j)`：**height 沿 tile-x 扩展、width 沿 tile-y 扩展**，与 `ShowRect`/`ARoom.GetBoundary` 轴语义统一。`SingleBed` 1×2（BottomLeft）自动得到主格 (x,y)+副格 **(x+1,y)**，与 sprite 物理足迹一致。
  - `AddBuildTask` 删除交换逻辑（曾暂加的 `SwapFootprintDimensions` 虚属性及 `ABed` override 全部移除）：直接以视觉宽高 `effectiveWidth/effectiveHeight` 调用 `GetOccupiedPositions`/`AddBuild`/`RegisterCollisionTile`。`BuildTileData.Width/Height` 存视觉宽高 → `SetComplete` 多格传播用同一函数自动一致。
  - `WorkerBrain.cs` 两处注释同步：`BedSecondOffset=(x+1,y)` 与修正后的 `GetOccupiedPositions` 逻辑副格一致，不再有"旧逻辑副格 y+1"的过时描述。
  - 影响面核验：`ReserveBuildPosition`(BuildMap.cs:134,179) 只被 1×1 墙/门/仓库调用，无副格、不受影响；房间类（ARoom/Inventory/Farmland）各自 override `AddBuildTask` 且用 `GetBoundary`，隔离不受影响；DoubleBed 2×2 正方形无影响。
- **验证**：玩家建造床后，观察 `[BuildDiag] 建造注册 ... cells=[(x,y),(x+1,y)]` 且 `[MapDiag] RegisterCollisionTile pos=(x+1,y)`——副格应从 y+1 变 x+1；再在该格放 Worker/敌人应能绕开而非卡死。重跑游戏看 `[StuckDiag]` 床旁卡点是否消失。
- **教训**：
  - **多格物品的"占用格"必须与 sprite 实际物理足迹一致**，否则 A* 缓存与物理脱节。坐标转置（MapPosToWorldPos）是这类错位的温床——检查多格物品时同时核对"定义宽高 → 占用格 → sprite 覆盖格"三段。
  - **注释声称的轴语义必须与实现核对**：`GetOccupiedPositions` 注释写"与 ShowRect 保持一致"而代码相反，此错位藏了整条 bug 族。当"预览对、注册错"时，优先怀疑共享的占用格函数，而不是给单个物品打补丁——修根因函数让所有物品统一，胜过每物品开关。

## 2026-08-15 SeekEnemy 寻路进入 Worker 房间：CanCharacterEnter 规则未接入寻路（死代码）

- **现象**：用户报告"敌人不应该能寻路进入 Worker 的房间，但出现了"。敌人从房间**门**走进去（墙正常阻挡，门被穿过）。
- **根因**（三类事实叠加）：
  1. **门 `CustomDoor` IsPass=1（可通行）**——`Resources/SO/WallItemData.asset` 配置，Info"可以通过的门"，Worker 要进出房间；墙 `CustomRoomWall` IsPass=0（不可通行，`.asset` m_ColliderType=1 有碰撞体）。
  2. **敌人寻路用角色无关判定**——`ASeekEnemy.Awake` 用 `new AStar(this)`（与 Worker 共用），AStar 障碍判定只走共享的 `WalkabilityCache`（`ASeek.IsCanReach` = tileMap+resourceMap+buildMap 物理碰撞）。门无碰撞体 → 房间内部格物理可通行 → 敌人 A* 路径穿门直达房间内目标（漫游点 `GenCanReachPosProvider` 落在房间内，或追击 Worker/Player 目标在房间内）。
  3. **房间规则是死代码**——`RoomManager.CanCharacterEnter`（RoomManager.cs:164）已实现"Enemy 不能进入任何私人房间；Worker 只能进自己的房间"，`ASeek.CanCharacterReach`（ASeek.cs:163）也封装了它，但 **AStar 从未调用**。规则从定义起就没接入任何寻路路径。
- **修复**（`Scripts/2D/Core/Seek/ASeek.cs`，统一入口校正）：
  - `ASeek.Seek(targetMap)` 开头（isShuttingDown 检查后）：`if (this.isEnemy && !this.CanCharacterReach(targetMap))` → `FindNearestReachable` 螺旋搜索（半径 30，覆盖房间尺寸）校正目标到最近的角色可达格（房间外/门口），打 `[EnemyDiag] 目标(...)在私人房间内, 校正到(...)`（Debug）。
  - 新增 `FindNearestReachable`：螺旋遍历每层边界，返回第一个 `CanCharacterReach` 为 true 的格；全不可达返回原目标（让寻路失败自然处理）。
  - **为什么放 Seek()**：敌人所有寻路入口（漫游 `SeekEnemySeekState.OnEnter`、追击/卡死重寻路 `HandleMovementStuck`、熔断 `AbandonMovementStuck` 回 Seek）最终都走 `ASeek.Seek`，一处覆盖全部。`ASeek` 构造函数已有 `isEnemy = character is AEnemy`（:87），按角色区分本就是设计意图，本次只是把规则接上用。
  - 校正只对 `isEnemy` 生效，Worker 寻路完全不受影响（Worker 进自己房间 CanCharacterEnter=true 不拦）。
  - 敌人已在房间内时能正常寻路出来（房间外格 CanCharacterReach=true）；门格非房间内部（`RoomInfo.IsInterior` 对 Points 返回 false）→ 敌人可寻路到门口。建造中的房间（Progress!=0）不限制（`GetRoomInterior` 跳过），完成后才生效。
- **验证**：观察敌人不再从门进房间；日志 `[EnemyDiag] ... 目标(...)在私人房间内, 校正到(...)` 出现；追击房间内 Worker/Player 时停在门口/房间外徘徊（Move 休息 2s → Seek 换新漫游点）。
- **范围**：只覆盖 AStar 寻路敌人（SeekEnemy_Lv1 类）。普通敌人 `ACommonEnemy` 用 `transform.Translate` 直接移动不走此路径（但被墙碰撞体阻挡）；门无碰撞体，普通敌人物理穿门是另一潜在入口，未处理。
- **教训**：**"规则已实现但未接入实际执行路径"是最隐蔽的死代码**——`CanCharacterEnter`/`CanCharacterReach` 定义了完整规则却零调用。修这类"行为不该发生却发生"的 bug 时，先确认语义规则是否存在、再查它是否被实际路径（寻路/决策/移动）调用。**统一入口（如 `Seek()`）是补角色感知的杠杆点**：一处校正覆盖漫游/追击/重寻路全部分支，胜过在每个状态各自加判断。

## 2026-08-15 敌人状态切换/寻路日志刷屏致帧率 20 多（game.log 181 万行/61 分钟）

- **现象**：用户报告"帧率在 20 多"，让看日志。`game.log` 61 分钟 **1,810,240 行 / 155MB**（01:32→02:33）。刷屏源：
  - `[EnemyDiag]` **107 万行**状态切换日志：`seekenemy →/← Move` 各 24.3 万；`commonenemy →/← Wander` 各 14.3 万；`→/← Seek` 等。
  - `[SeekDiag]` **53 万行**寻路日志：`提交寻路` 26.7 万 + `到达终点` 25.7 万，seekenemy 占 93%。
  - `LogManager` 每 10 条 `File.AppendAllText`（Open+Write+Close）→ 每秒 ~30 次文件写 + 主线程锁阻塞。
- **根因**（两个刷屏源 + 一个 I/O 放大）：
  1. **seekenemy 正常漫游但日志过密**：~16 个实例，每个每 ~2.5s 漫游一次（寻路+走路+休息），每次打 5 条日志（← Move/提交寻路/→ Seek/→ Move/到达终点）。寻路成功率 96%（提交 24.8 万 vs 到达 23.9 万），属正常行为非 bug。
  2. **commonenemy 卡墙乒乓（无效状态往返）**：`ACommonEnemy.OnCollisionStay2D`（Sliding 检测）在 `Target==null` 时也 `ChangeState(Seek)`；`CommonEnemySeekState.OnUpdate` 第一帧发现 `Target==null` 立即 `ChangeState(Wander)`。形成 `← Wander → Seek ← Seek → Wander` **同毫秒 4 条**的无效乒乓，每 ~1s 一次。既有日志刷屏也是 CPU 浪费。
  3. **LogManager 落盘频繁**：`maxLogCount=10` → 每 10 条一次 `File.AppendAllText`（Open/Write/Close 三次系统调用），高频日志下主线程阻塞明显。
- **修复**：
  1. **`LogManager.maxLogCount` 10→100**：文件写频率降 10 倍，日志内容不变（最多滞后 ~0.3-1s，Quit/关闭时 Flush 兜底）。
  2. **修 commonenemy 乒乓**（`ACommonEnemy.OnCollisionStay2D`）：Sliding/Stuck 时 `Target==null` 不进 Seek，改重入 Wander 换方向（OnEnter recordTime=9999 立即随机新方向）。
  3. **状态切换日志节流**（新增 `AWorkerTask.LogProviderThrottled(key, intervalSec, msg, level)`，静态字典按 key 节流）：seekenemy/commonenemy 全部 `→/← Move/Seek/Wander/Chase/Attack` 改用节流版本（`{name}|XxxIn/Out`，2s/条）。低频一次性事件日志（`→/← Dead`、攻击目标选定、攻击执行、卡死熔断、救援传送、生成敌人）保留不节流——死亡稀少但每次击杀的 attacker/经验归因珍贵，节流会丢归因（review 2026-08-15 修正）。节流器对 `elapsed<0`（Time.time 重置，如关闭域重载的新会话）放行并更新时间戳，避免旧时间戳整体抑制日志。
  4. **寻路日志降级+节流**（`ASeek.cs`）：`提交寻路` Debug→Trace + 节流 2s/条；`到达终点` Trace + 节流；`寻路不可达` 保持 Debug（关键事件）+ 节流。
- **验证**：重跑后 game.log 体积应降 ~10 倍（状态切换每敌人每 2s 一条，提交/到达 Trace+节流）。文件写次数降 10 倍。帧率应回升（日志 I/O 不再拖主线程）。
- **教训**：
  - **"正常行为 + 事件级日志 + 立即落盘"= 静默性能杀手**。漫游/感知这类每秒几十次的"正常"事件，即使每次只打 1 条 Debug，也会撑爆 game.log 并拖累主线程。高频事件日志应**节流或降级**（与 2026-08-13 的"高频 Trace 降频"一脉相承）。
  - **状态切换日志要警惕"无效往返"**：`→ X` 立即 `← X`（同毫秒）说明进了不该进的状态。`ACommonEnemy` 的 `Target==null 进 Seek` 就是典型——Seek 兜底逻辑（Target==null 退回 Wander）反而掩盖了入口不该进 Seek 的问题。修这类问题修**入口**（不该进），而非依赖状态内兜底。
  - **日志降频优先于日志降级**：降级到 Trace 仍写盘、不减 I/O（Trace 与 Debug 同通道进 game.log）；真正减 I/O 靠**减条数**（节流）或**增大批量**（LogManager maxLogCount）。

## 2026-08-15 MovementStuckDetector 用净位移误判正常绕路 → Worker 正常走路一卡一卡

- **现象**：用户报告「Worker 正常走路一卡一卡的（非碰撞卡死），是刚才引入的问题」。`game.log` 统计 1542 条 `结算=Sliding` 中 **32%（490 条）ratio 落在 0.15-0.4**（位移不足、非硬卡死）。样本如钱俊：`pos=(417.40,411.40)→(418.69,412.99)`、`pathIdx=0/2→2/4`（路径在推进、位置在移动），却判 `ratio=0.20/0.29` 为 Sliding。
- **根因**：`MovementStuckDetector`（`03fbc182` 引入，替换失效碰撞回调）用**窗口净位移**（`Vector3.Distance(窗口起点, 当前位置)`）判定卡死：`ratio=净位移/(期望速度×1s)`，`<0.4→Sliding`。Worker 走长距离路径（采集/建造/找目标）时**蛇形绕障碍**，1 秒内实际走了很多路，但净位移（起点→终点直线）远小于路程 → ratio 偏低 → 误判 Sliding → `WorkerMoveState` 每次 Sliding **无条件 `ChangeState(Seek)` 重新寻路** → 停顿 → 视觉一卡一卡。改动前用碰撞回调，正常走路从不触发；位移阈值对任何位移不足敏感，把「绕路」当「卡住」。
- **修复**：`MovementStuckDetector.Feed` 结算改用**窗口内累计位移**（每帧 `Vector3.Distance(prevPosition, position)` 之和）替代净位移。正常蛇形绕路每帧仍前进 → 累计≈实际路程 → ratio≈1 不误判；真卡死（撞墙穿透→物理推回→回到原点）每帧位移≈0 → 累计≈0 → 照常检出 Stuck。`Reset`/`RestartWindow`/结算后同步重置 `actualDistance`/`prevPosition`。
- **验证**：改后正常绕路 ratio≈1 不再误判 Sliding；真卡死累计位移≈0 仍走 Stuck（重试/放弃）逻辑。需游戏内实测确认走路流畅、且真卡墙仍能自救。
- **教训**：**位移不足 ≠ 卡住**。判定"卡死"应看"实际有没有在动"（每帧位移之和），而非"起点到终点差多远"（净位移）。净位移会被正常蛇形行进/绕障碍误伤——这也解释了为何位移检测器替换碰撞回调后引入回归：碰撞回调只响应真实碰撞，位移阈值则对任何位移不足敏感。若后续需要检测"绕圈但无进展"（净位移低但累计位移高），应单独加"路径推进停滞"判定，而非退回净位移。

## 2026-08-15 Worker 持续走路抖动（不停下）：FixedUpdate 步进与渲染采样错配

- **现象**：累计位移修复后（上一条）Sliding=0 不误判了，但用户报告「走路还是一卡一卡的」，澄清为**「持续走路但抖动（不停下）」**——不是走走停停。低帧率（20 帧左右）下尤其明显。
- **根因**：`96d26170`（08-12）把移动从 `OnUpdate(Time.deltaTime)` 移到 `OnFixedUpdate(Time.fixedDeltaTime)`。Worker/SeekEnemy 是 **Dynamic Rigidbody2D** + `transform.Translate` 直接写 transform + `m_Interpolate:0`（插值关闭）。物理步进 50Hz，渲染帧率 ~20fps：每个渲染帧内物理步进 2-3 次，Translate 每 FixedUpdate 移固定距离，渲染采样点落在不同物理时刻 → 每渲染帧看到的位移量不均匀 → 位置跳变 → 持续抖动。插值关闭意味着物理位置直接驱动渲染，无平滑。Player 用 `rb.velocity`（物理通道驱动、天然平滑）无此问题，佐证差异在移动通道。
- **修复**（方案 A：MovePosition + 开启插值）：
  1. **`ASeek.MoveByPath`**：`transform.Translate` → `Rigidbody2D.MovePosition`（FixedUpdate 内物理同步，位置走刚体统一物理通道；无刚体回退 Translate）。MovePosition 基准用 `characterPosition`（transform.position），与 `stuckDetector.Feed` 同一坐标，保证检测位移与实际移动一致。
  2. **开启插值**：Worker.prefab / SeekEnemy.prefab `m_Interpolate: 0→1`——Rigidbody2D 插值在渲染帧之间平滑物理位置，消除 FixedUpdate 步进与渲染采样错配的抖动。
  3. **卡死检测兼容**：MovePosition 受碰撞约束不穿透，撞墙时位置停住 → MovementStuckDetector 累计位移≈0 → 仍检出 Stuck；SeekEnemyMoveState/WorkerMoveState 的 Sliding/Stuck 熔断（MaxStuckStreak=4）不变。
- **验证**：需游戏内实测。低帧率下走路应连续平滑不再抖动；真卡墙时累计位移≈0 仍走 Stuck 自救。已知遗留：Worker/SeekEnemy `gravityScale=1`（Dynamic Rigidbody 上的重力），MovePosition 与重力 velocity 可能冲突致 Y 轴抖动——保守未动，若仍抖动再处理。
- **迭代（MovePosition 失败 → 又慢又卡顿）**：方案 A 用 `Rigidbody2D.MovePosition` + Interpolate，用户重打 AB 包后实测**「走的又慢，又卡顿」**。根因：**MovePosition 受碰撞约束**——Worker 碰撞体与地面/障碍持续接触时，位移被物理求解削减（慢），且每 FixedUpdate 做接触求解（卡顿）；速度不再是精确的 `speed`。插值本身没问题（200fps 下物理帧间插值能平滑），问题在 MovePosition 的移动方式。
- **最终修复（方案 B：velocity 驱动，Player 同款）**：`MoveByPath` 改为每 FixedUpdate `rb.velocity = Direction.normalized * speed`。物理引擎积分速度推进位置：**速度精确**、**撞墙碰撞求解器阻挡不穿透**、**Interpolate 对物理位置插值渲染平滑**（200fps 渲染 vs 50Hz 物理不跳变）。`StopMove`/`result==null`（寻路间隙）调用 `ClearVelocity()` 防 velocity 残留滑行。改动全部在 `ASeek.cs`（代码），编译即生效，无需重打 AB 包；Interpolate=1 需 prefab 改动进 AB 包（已重打）。
- **验证**：200fps 下走路应连续平滑、速度正常、碰墙不穿透不抖；真卡墙 velocity 被挡 → 位置不动 → 累计位移≈0 → 仍检出 Stuck。
- **教训**：**物理帧内移动的三种方式里，只有 `rb.velocity` 高帧率下既平滑又速度精确**。`MovePosition` 受碰撞约束削减位移（又慢又卡）；`transform.Translate` 绕过刚体、无碰撞约束（穿透）、且插值对非物理移动不生效（跳变）。物理通道移动的正解是 `velocity`（Player 早已如此），配 Interpolate 渲染平滑。
## 2026-08-15 碰撞预检测过度干预：9 分钟 924 次升级 Stuck（瞬移/卡顿/任务失败）

- **现象**：预检测落地后用户反馈「碰到碰撞体后刚体保留速度、会瞬移一段、还卡顿」「刚体是不是有问题」。日志 9 分钟窗口：`内部重寻路` **1401 次**、`内部重寻路耗尽`（升级）**924 次**、`结算=Stuck` **1017 次**、`贴墙滑动` **744 次**。卡死样本 `汤峰 结算=Stuck pos=(94.69,74.20) target=(77,93) ratio=0.00 pathIdx=0/1`——角色从起点就没动过。
- **根因**（两类叠加）：
  1. **网格-物理不一致是宿主场景**：1201 次卡死 `可通行=True` vs 仅 15 次 `可通行=False`。`WalkabilityCache`（`ASeek.IsCanReach` = tile+resource+build 三层 `GetColliderType==None`）判格子可通，但 **Tile/BuildTile 的物理碰撞体在该处挡路**（碰撞体形状与格子判定分叉，见 2026-08-14 床足迹错位同源）。
  2. **预检测的"停→内部重寻路→升级"链条在宿主场景下空转**：角色被 CircleCast 探测判"受阻"→ 停 → 内部 `Seek()` 重寻路——**网格判可通时 A* 永远走同一条路**，重寻路不改变任何东西；3 次后升级 Stuck → `MovementStuckDetector` 喂 `speed` 但位置不动 → 1s 窗口 `ratio≈0` → Stuck → 状态机放弃任务。每个 Worker 每 ~30s 就误杀一次。`rb.velocity` 0↔恢复 + 重寻路间隙 `ClearVelocity` 交替 → 视觉"瞬移一段 + 卡顿"。
- **修复**（`Scripts/2D/Core/Seek/ASeek.cs`，把预检测砍回**纯平滑滑动**）：
  - **删除整条"停→重寻路→升级"链路**：`HandleWallBlock`、`ClearWallBlockState`、`wallBlockFrames`、`wallRepathStreak`、`lastWallCheckPathIndex`、`waitingForRepath`、`escalatedToStuckDetector`、`wallRepathInFlight` 及其 Seek() 预算清理块全部移除。
  - **预检测只保留"可滑动→投影切向滑动"**：`CircleCast` 命中且 `TryGetSlideDirection` 可滑、且 `slideDir·toWp>0`（不滑向死角）→ `velocityDir=slideN` 平滑绕墙。这是消除"碰一下偏一下"的唯一目的。
  - **正对墙/不可滑/已接触（distance<epsilon）→ 保持原速度，交给物理求解器消法向挡住**，不再手动 `velocity=0`、不再内部重寻路。由下方 `Feed(真实 speed)` 1s 窗口结算 Sliding → 现有状态机熔断接管（Worker `HandleMovementStuck` / Enemy `AbandonMovementStuck`，同目标 4 次后放弃）。
  - **探测距离缩短**：`speed*dt*3→*2`，clamp `0.2~0.5→0.15~0.3`——只探测即将碰撞的 2 帧，避免在"网格可通但物理挡"区域提前很远触发误判。
  - **Feed 恒用真实 speed**：不再有 `headOnStop 时喂 0` 的静默等待期（内部重寻路已删除，等待期不存在）。
  - **review 跟进（2026-08-15）**：① 距离比较改 2D——`Vector3.Distance(worldPos,characterPosition)` 会引入角色 z 分量虚高 distToWp，路点紧邻时误判滑动；改用 `toWp.magnitude`（CircleCast 本就是 2D）。② `Seek()` 入口复位 `slidingAlongWall`——Worker Move→Seek 不经过 StopMove，旧 `slideEnterDir` 会残留到新路径首帧探测。
- **验证**：重跑后 `WallRepath`/`WallEscalate`/`WallUnreachable` 日志应消失；`结算=Stuck` 应回到与改动前（2026-08-14）相当的低频；正常走路无瞬移无卡顿。真卡墙（正对墙）由物理挡 + Stuck 熔断兜底，行为与预检测前一致。
- **教训**：
  - **探测/熔断机制必须与"失败时的宿主场景"兼容**：预检测针对的是"物理挡路但网格判可通"这一必然出现的场景。任何"受阻后内部重寻路"的兜底都在该场景下**空转**（A* 用同一缓存判同一条路可通）——兜底重寻路不改变结果，只会放大次数、最终升级 Stuck。**不要在同一判定域内叠加第二套重寻路**；把熔断留给已有的状态机链（Sliding→Seek 重寻路→同目标 4 次→放弃）。
  - **`ratio=0.00` + `pathIdx` 卡在起点 = 系统性误判信号**。单个卡死样本可归因于"撞到墙"，但同一角色在两个不同目标上卡在**同一起点坐标**、且 99% 卡死都 `可通行=True`，说明是预检测把正常角色系统性判死，而非个别物理碰撞。批量统计结算分布（True/False 比例、pathIdx、位置重复）能区分"个别碰撞"和"系统误判"。
  - **网格-物理不一致是长期宿主**（2026-08-13/08-14/08-15 三次均见）：`WalkabilityCache` 用格子碰撞体判定、物理用实际碰撞体形状，二者在墙角/家具/床位处分叉。治本方向是让 `IsCanReach` 与实际物理碰撞体一致（或在探测层容忍一致差异），移动层只是缓解症状。

## 2026-08-15 治本：网格-物理碰撞体真值统一（IsCanReach 读真实碰撞体）

- **现象**：薛彬 15:51:03 卡死，`可通行=True` 但正对刚建成的墙。历史 1201/1216 次卡死都是这一类别——**网格判可通、物理碰撞体挡路**（前三条 08-13/08-14/08-15 已三次定性为长期宿主）。用户选"治本"：不再加移动层补丁，统一判定真值。
- **根因**（三个触发源）：
  1. **数据源分叉（主）**：`BuildMap.IsCanReach`（BuildMap.cs）读逻辑模型（PosMap 字典 + `BuildItemData.IsPass` + `IsFreeTile` fallback），而物理碰撞体由 `tilemap.SetColliderType` 驱动。`GetColliderType==Tile.ColliderType.None` 才是物理真值（`BaseTileMap.IsCanReach` :131-134 早已用它，与 TilemapCollider2D 实际生成的碰撞一致）。
  2. **时序**：墙在寻路后才 `SetComplete` → 旧路径穿过新墙 → 角色直走撞墙。
  3. **压缩路径切墙角**：`AStar.IsLineWalkable`（AStar.cs:181-193）对角移动要求 **both** 角格不可通才拒绝（`&&`）——标准 no-corner-cutting 应为 **either**（`||`）。薛彬 (141,106)→(142,108) 穿过刚建墙 (142,106) 的角。
- **修复**（A+B 必须同一改动集原子上线，R1）：
  - **A. `BuildMap.IsCanReach` 读真实碰撞体**：整体替换为 `this.tilemap.GetColliderType(posMap) == Tile.ColliderType.None`。删 PosMap 查询、`GetBuildItemDataByName`、`IsFreeTile` fallback。连带影响（期望）：`UnityMapAdapter`、`ShopNPCGenerator`、`AEnemy` 视线、`AWorkerTask.IsCanWork`、WorkerBrain 建房选址、救援传送全部与物理一致。
  - **B. 统一碰撞体不变量** `ApplyCollider(pos, isComplete, isPass)`：`!isComplete || isPass ? None : Sprite`。接入全部写路径：`AddBuild` 的 `!isNeedBuild` 完成分支（此前依赖资产默认）、`SetComplete`（:305 改 `ApplyCollider(..., buildItemData?.IsPass == true)`）、`SyncDataResp` 全量（补 else 完成分支）、`SyncDataResp` 单格（未完成**无条件** None，顺带修 `buildItemData.IsPass` NRE）、`LoadData` 完成分支、`DoDirectBuild`。**关键**：单独上 A 会让远端建造中墙（同步路径未改）在远端带 Sprite → 网格误判不可通行（反向不一致），故 A+B 原子。
  - **C. `IsLineWalkable` 角检测 `&&`→`||` + 可测试化重构**：`AStar.cs` 拆私有转发重载（生产传 `WalkabilityCache.IsWalkable`）+ `public static` 委托注入重载（测试 + 修复 E 复用）。
  - **D. 正对墙日志记录阻挡碰撞体身份**：`ASeek.cs` WallHeadOn 日志追加 `hit={collider.name}:({cellX},{cellY})`。
  - **E. 在飞路径段重验证 + 重寻路**（收尾网）：A+B 后网格=物理真值，重寻路不再空转（旧教训 924 Stuck→5 的前提已消失）。PathIndex 推进后验证 `Path[PathIndex-1]→Path[PathIndex]` 段仍可通（`IsLineWalkable`），失效则 `StopMove()+Seek(TargetMap)` 打 `[MoveDiag] PathStale`；正对墙分支同款验证，段仍可通则维持现状（交给物理 + Stuck 熔断）。`PathIndex==0` 验证 `Path[0]`。
  - **F. 单测**：新建 `Scripts/2D/Editor/Tests/Tool/AStarLineWalkableTests.cs`（4 例）与 `BuildColliderInvariantTests.cs`（3 例）。
- **验证**：
  - 编译：`BuildMap.cs/AStar.cs/ASeek.cs` 改动后 Assembly-CSharp.dll 用 Unity Roslyn csc 编译通过（无 error CS）。
  - 逻辑：两测试文件用 Unity shims（`NetStandard/compat/2.1.0/shims/{netstandard,netfx}`，.NET Standard 2.1 真实基础库）独立编译通过 + 迷你 runner 实际执行 **8/8 断言 PASS**——对角角格 (1,0)/(0,1) 为墙→拒绝（旧 `&&` 放行）、开阔对角→放行、直线穿墙→拒绝；`ColliderFor` 四个不变量全对。**关键教训（编译配置）**：测试文件必须用 **Unity 的 shims 全套** 而非 Mono 4.7.1-api 基础库——后者与 netstandard 2.1 引用重复定义（CS0518/CS0433/CS8356）；shims 是类型转发 facade，不冲突。
  - 运行时（待 Unity 编辑器内确认）：`可通行=False` 提前改道、`PathStale` 重寻路日志、`hit=BuildMap:(x,y)`、远端建造中墙全程 None、床两格不可通行、空地漫游无新增 Stuck。
- **教训**：
  - **"判可通行"必须读与移动碰撞体同一个数据源**。`IsCanReach` 与 `SetColliderType` 分叉（一个查逻辑模型、一个写 tile cell）是全部三类卡死的宿主——任何移动层兜底（预检测重寻路、滑动、Stuck 熔断）都只缓解症状。让**判定函数直接读物理真值 `GetColliderType`**，A*、WalkabilityCache、救援、视线全部自动一致。
  - **写路径必须维护同一不变量**：只改读取不改写入（或反过来）会制造反向不一致（远端建造中墙误判不可通）。把所有 SetTile/SetColliderType 写点收敛到 `ApplyCollider(pos, isComplete, isPass)` 一个入口。
  - **once 被放弃的方案前提变了就要重新评估**：内部重寻路因"网格判可通时重寻路走同一条路空转"被放弃；A+B 让网格=物理真值后，重寻路会走新路，是安全且必要的收尾网（E）。修复文档里的旧教训要标注失效前提。
  - **UnityEditor 测试编译的 .NET profile 泥潭**：csproj（LangVersion 9 / .NET Framework 引用）过期不可信；`csc` 引用必须用 Unity 安装目录的 `NetStandard/ref/2.1.0` + `compat/2.1.0/shims`，路径用 Windows 格式（`D:/...`，Git Bash 的 `/d/...` 会让 Windows csc CS0006 找不到元数据）。

## 2026-08-15 Worker 停止时滑行：MoveByPath 最后一次 velocity 残留

- **现象**：用户报告"Worker 在停止时还会滑行"。
- **根因**：`WorkerMoveState.OnExit` 只在切换移动状态时清速度（`SeekEnemyMoveState.OnExit` 有 `StopMove`），而 Worker 缺；`MoveByPath` 每 FixedUpdate 设 `rb.velocity = velocityDir * speed`，停下后最后一次 velocity 残留；诊断修复又把 `rb.drag` 设 0（防每帧按比例衰减速度造成位移偏差）→ 残留速度永不衰减 → 滑行明显。
- **修复**：`Scripts/2D/Character/Worker/State/WorkerMoveState.cs` `OnExit()` 追加 `this.Character.Seek?.StopMove()`（`StopMove` 内部 `ClearVelocity()` 已把 `rb.velocity=0`），与 `SeekEnemyMoveState.OnExit` 对齐。
- **教训**：**drag=0 的 velocity 驱动移动，必须由状态机在"停止移动"语义点显式清速度**。靠 drag 衰减（=1 时每帧留 63%，滑动明显）或不动（=0）都不是可依赖的停止机制；`StopMove`/`ClearVelocity` 是唯一停止通道，任何离开移动的路径都要调用。

## 2026-08-15 修复 E 段失效重寻路死循环（回退）：建墙时角色被刚建墙围困 → 每 2s 重寻路空转

- **现象**：于发祥 pos=(70.40,130.81) 17:07:40-50 六次同坐标卡死不动；建墙现场角色被刚建墙围困。
- **根因（修复 E 引入的回归）**：修复 E 在"正对墙"分支做"段失效 → `StopMove()+Seek` 重寻路"。当角色被刚建墙四面围困时，**重寻路路径仍穿过新墙**（A+B 后 `GetColliderType` 判新墙不可通，但角色站在墙中间/紧贴新墙，可绕行的邻居格也全部被墙占，A* 只能再次穿墙）→ 段检查再次失效 → 每 2s（重寻路节流）死循环，角色卡死不动。与"网格判可通而物理挡路"的历史空转不同：这里网格**正确**判不可通，但**无路可绕**，重寻路不可能产生新解。
- **修复（回退 E 的正对墙分支）**：`ASeek.cs` 删除正对墙分支的段失效重寻路块，回归纯物理挡 + `MovementStuckDetector` 熔断（`Sliding`→状态机重寻路→同目标 4 次放弃，见 08-13 Sliding 熔断条目）。保留 D 的 WallHeadOn 阻挡碰撞体日志。PathIndex 推进处的段失效验证（E1）保留——它在"路径中段被新墙阻断"且**有绕行空间**时仍有效，与正对墙被围困场景正交。
- **教训**：**"段失效→重寻路"只在网格判可通而物理挡路（判定不一致）时有解；网格已正确判不可通但被围困（判定一致、无路可绕）时，重寻路必然空转**。重寻路前必须确认存在替代解——把"可通行判定"与"是否存在可达绕行路"分开判断，否则任何"验证+重寻路"网络在围困几何下都会变成死循环。诊断日志先于修复上线（jitter 帧位移窗口 + WallHeadOn hit 身份），用帧级数据确认抖动形态后再定最终修法。

## 2026-08-15 卡顿根因（最终）：velocity 硬闯碰撞体 → 求解器位置修正 → 卡帧+瞬移帧抖动，瞬移推入床格

- **现象**：用户报告"卡顿没有解决"+"出现了之前被床卡住的bug"。jitter 帧位移诊断（本会话加入）给出决定性数据：**正常走路 `min≈0.07-0.09 max≈0.10-0.18 avg≈expect`（干净连续）；`滑=True`（碰撞接触）时 `min=0.000 max=0.44~1.12`（多帧完全不动 + 单帧瞬移 8-10 倍）并存**——即"卡-跳循环"。avg≈expect 掩盖了它（窗口内多数帧正常），故之前的 vel/expect 采样（只在设置瞬间）与 avg 诊断都看不出问题，必须看窗口 min/max。
- **根因**：`ASeek.MoveByPath` 正对墙分支"保持原速度，物理求解器自然消法向挡住"——velocity 每帧推入碰撞体，求解器每帧位置修正（位移≈0 卡帧），穿透积累到修正量超过 1 格时单帧瞬移（max=1.0+）；**瞬移帧可能把角色推入/穿过相邻碰撞格（床/墙）→ 站进可通行=False 的格 → "被床卡住"**。与"隐藏碰撞体就不卡"（无碰撞体→无求解器→无修正）完全吻合。这也解释了 150 次 Stuck 里多数 `可通行=True` 却 ratio≈0 卡住（在碰撞格旁被挡，非判定不一致）。
- **修复**（`ASeek.cs` MoveByPath，运动学防穿透）：正对墙分支**不再保持速度硬闯**，改为按 CircleCast 命中距离平滑减速，在墙前停下、**不侵入碰撞体**：
  ```csharp
  float wallGap = Mathf.Max(0f, hit.distance - WallProbeRadius - WallContactEpsilon);
  appliedMoveSpeed = Mathf.Min(speed, wallGap / Time.fixedDeltaTime);
  ```
  新增 `float appliedMoveSpeed = speed`（默认满速，仅正对墙分支收缩）；velocity 与 Translate 用 `appliedMoveSpeed`；**`MovementStuckDetector.Feed` 仍用原始 `speed`**（期望位移不变）——否则 `expectedDistance < MinExpectedDistance(0.5)` 会跳过判定，角色停在墙前却不报卡死。减速停住后实际位移≈0 → ratio≈0 → 照常结算 Sliding/Stuck → 状态机熔断（Worker 4 次 / SeekEnemy 4 次）接管。物理求解器全程不介入 → 无抖动、无瞬移、不推入床格。
- **验证**：Assembly-CSharp.dll 编译通过（无 error）。待运行时：`滑=True` 采样应消失 min=0/max 跳变（减速后 `speed=` 显示收缩值）；"被床卡住"（可通行=False 的 Stuck）应不再新增，存量由救援传送兜底。
- **教训**：**velocity 驱动角色严禁"保持速度硬闯"碰撞体**——物理求解器对持续侵入的位置修正就是视觉抖动源，且修正过大会把角色瞬移进相邻碰撞格。正对墙的正确行为是**按探测距离运动学减速停住**（角色永不接触碰撞体），用探测结果完全接管"接近墙"的执行，物理只作漏探测兜底。**诊断必须看窗口 min/max 而非 avg**：卡-跳循环的 avg 会被多数正常帧稀释成"正常"。**改速度必须同步检查卡死检测的期望位移口径**：Feed 的 expectedSpeed 与实际移动速度分离，减速不能污染期望值，否则熔断失效。

## 2026-08-15 抖动根因修正：主因是刚体旋转自由度（冻结 Z 有效），位置侵入只是次要放大

- **现象**：上一条减速修复（运动学防穿透）后用户实测**抖动未解决**；用户自行尝试"把刚体 Z 冻结"后**抖动消失**——这是决定性反证：抖动主因不是位置侵入，而是**旋转自由度**。
- **根因（修正）**：`rb.velocity = velocityDir * speed` 每 FixedUpdate 重设下，角色贴墙/滑动/正对墙时，碰撞接触点的**切向力（摩擦）产生扭矩 → 刚体绕 Z 轴旋转** → 旋转改变接触几何与求解器反馈方向 → 位置修正反复变化 → 视觉抖动 + jitter 的 min=0/max=1.0。我的减速修复只消除"位置侵入"（正对墙硬闯），不涉及旋转，故无效；冻结 Z 旋转后接触稳定，抖动消失。jitter 的 max 跳变是**旋转诱导的接触/修正变化**，不是纯位置修正。隐藏碰撞体无接触 → 无扭矩 → 无旋转 → 不抖，与此一致。
- **修复**：`ASeek` 构造函数运行时设置 `this.rb.freezeRotation = true`（Rigidbody2D 冻结 Z 旋转，与 gravityScale=0/drag=0 同处；prefab 走 AB 包改不了，必须代码设）。俯视角角色朝向由移动/视觉（`Direction`）控制，物理旋转自由度无任何消费方，冻结安全。Player/CommonEnemy 若同样抖动也可照此处理（它们冻结了 Y 但未冻结旋转）。
- **验证**：编译通过。待运行时：`滑=True` 采样的 min=0/max 跳变应消失；正对墙减速分支的 `speed=` 收缩保留（防侵入，与冻结旋转互补）。
- **教训（方法论）**：**"隐藏碰撞体就不抖"的定位是"接触参与物理"，但不等于"位置侵入"**——接触的副作用还有扭矩/旋转。jitter min=0/max=1.0 是"接触时物理反馈不稳"的通用信号，具体机制（位置修正 vs 旋转）需要对照实验区分（这次用户手动冻结 Z 就是最好的对照）。**先复现用户的最小改动再定根因**——减速修复若未经验证就上线，会白改一轮。运行时物理配置（重力/拖拽/旋转）统一收敛到 ASeek 构造函数一处，便于用单一开关对照排查。

## 2026-08-15 收尾：回退减速修复、删除排查诊断，最终方案 = 冻结 Z 旋转

- **最终修复**（保留）：`ASeek` 构造函数 `this.rb.freezeRotation = true`（运行时冻结 Z 旋转）+ 既有 `gravityScale=0`/`drag=0`。抖动与被床卡住一并根治（用户实测）。
- **回退**：上一轮"正对墙按探测距离运动学减速停住"（`appliedMoveSpeed` + `wallGap`）——用户实测无效（抖动主因是旋转非位置侵入），且引入"停在墙前→Sliding"的额外行为。正对墙分支恢复"保持速度，物理求解器挡"（原语义），保留 WallHeadOn 事件点诊断。
- **删除排查诊断**：jitter 帧位移窗口（字段+诊断块）、MoveSample vel/grav/drag 采样——两者是排查用的临时 Trace 日志，问题已定位，删除以免 game.log 噪音。保留事件点 Debug 诊断：`WallHeadOn`（正对墙+阻挡碰撞体）、`WallSlideEnter`（进入滑动）、`StuckDiag`（结算结果）。
- **验证**：Assembly-CSharp.dll 编译通过（无 error）；grep 确认 `appliedMoveSpeed`/`jitter`/`MoveSample` 无残留。
- **当前状态**：`ASeek` 移动核心 = velocity 驱动 + CircleCast 预检测（滑动/正对墙物理挡）+ MovementStuckDetector 熔断 + 运行时 freezeRotation/gravity=0/drag=0。抖动根因链全部落档（本条目族）。

## 2026-08-15 Player 恒最顶（y-sort 后仍盖所有物体）：过期 AB 包内旧 sorting layer

- **现象**：y-sort 三阶段（角色/建筑/树统一 Character 层排序）落地后，Worker 遮挡正确，但 Player 始终排序最顶、盖住一切树/建筑/其他角色。
- **根因**：`ResourceManager.Instantiate` 从 `StreamingAssets/prefab` AB 包加载 prefab。角色 prefab 已改为 `Character` 层（磁盘 `m_SortingLayerID:-1403816847`），但 AB 包（构建于 prefab 改动前）内仍是旧层：Player 在 `Player` 层（index 4，在 Character index 3 之上）→ 无论排序器给多少 order 都恒盖 Character 层；CommonEnemy/SeekEnemy 仍在 `Enemy` 层（index 2）→ 恒被树盖。Worker 碰巧正确：Worker 层改名 Character 时 uniqueID（2891150449）不变，旧包里的层 ID 现解析为 Character。
- **定位**：`WorldYSortManager` 加事件点诊断日志（`YSortRegister` 打 sortingLayerName、`YSortTop` 打当前最顶条目 + bottomY）。日志显示：`YSortTop top=TreeVisual_24_91`（地图最低端树，排序本身正确）唯一一条 → 排序机制正常；`YSortRegister layer=Player`/`layer=Enemy` 与磁盘 prefab（Character）矛盾 → 运行时资产非磁盘 prefab → 锁定 AB 包陈旧（bundle 18:11 vs prefab 20:00）。
- **修复**：`工具/其他/打AB包` 重建 `StreamingAssets/prefab`（`BuildPipeline.BuildAssetBundles`）。重建后运行时 `YSortRegister layer=Character`，Player/敌人都正确参与 y 排序。
- **教训**：**Character 层排序成立的前提是所有参与 renderer 在 Character 层，而角色层由 AB 包 prefab 决定——改 prefab 层后忘记重打 AB 包，排序会静默失效且症状隐蔽（按层隔离，表现为"某对象恒最顶/恒被盖"而非排序错误）**。排查此类问题要看"运行时实际值"（`sr.sortingLayerName`）而非磁盘 prefab；事件点诊断日志（Register 打 layer + sprite + offset、Top 打最顶条目）能高效区分"排序本身错"与"资产与磁盘不符"。

## 2026-08-15 建筑/树无光照（tile 视觉拆分到 SpriteRenderer 后）：新 SpriteRenderer 用默认 unlit 材质

- **现象**：y-sort 落地（`TileVisualSpawner` 把建筑/树视觉从 TilemapRenderer 拆到独立 SpriteRenderer）后，建筑与资源不再接收 2D Light 光照（角色仍有光）。
- **根因**：`AddComponent<SpriteRenderer>()` 默认材质是 `Sprites-Default`（unlit），URP 2D 的 Light2D 只照亮 lit 材质（`Universal Render Pipeline/2D/Sprite-Lit-Default`）。拆分前 TilemapRenderer 显式引用 lit 材质（场景 Game.unity 中 7 个 TilemapRenderer 共用 guid `a97c105638bdf8b4a8650670310a4cd3`）→ 有光照；拆分后新 SpriteRenderer 无材质赋值 → unlit → 无光照。角色 prefab 显式引用 lit 材质（`f36a54b0b21e1db4c9bc02407eeab188`）→ 角色正常，正好解释"只有建筑与资源没光"。光照 targetSortingLayers 已含 Character/ResourceMap/Tile 层，层配置无误（解码 GlobalLight 的 `m_ApplyToSortingLayers` 验证）。
- **定位**：解码场景 GlobalLight `m_ApplyToSortingLayers` 确认光照作用层齐全；对比拆分前后渲染材质差异锁定 unlit 根因（与"恒最顶"按层隔离的症状不同，本次是材质差异）。
- **修复**：`TileVisualSpawner` 构造函数 `ResolveMaterial` 复制宿主 TilemapRenderer 的 sharedMaterial（与 `TileMap.cs` chunk 材质复制同模式）；无 renderer 时 fallback `Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")`。创建 SpriteRenderer 时 `sr.sharedMaterial = material`。构造打一条 `[BuildDiag] TileVisualSpawner ... mat=<shader.name>` Debug 日志便于验证。
- **验证**：编译待 Unity 确认；运行时看 game.log `[BuildDiag]` 应显示 lit shader 名、建筑/树恢复光照。
- **教训**：**拆分渲染路径（TilemapRenderer → SpriteRenderer）时材质不随组件自动迁移**——Tilemap 的 lit 材质是 TilemapRenderer 序列化引用，新建 SpriteRenderer 必须显式复制，否则静默退 unlit、2D Light 失效。凡"某类对象不受光/不受后处理"，先查其 renderer 材质 shader 是否 lit，而非只查 sorting layer。

## 2026-08-16 床副格碰撞体注册失效：SetColliderType 对无 tile 格无效，A* 穿过副格列撞床主格卡死

- **现象**：舒宏才"直接走向床"被卡死。`[MoveDiag] 正对墙 hit=BuildMap:(101,184) 网格可通=True 缓存可通=True`（物理挡但网格/缓存判可通），A* 原始 path 头 `(101,184)(101,185)...`（穿过床副格列），Stuck→重试3次→放弃，同一 Build 任务反复重试不脱困。此前姜兴庆卡的是床主格（SetComplete 正常、缓存不可通，是另一种情况）；本次卡的是**床副格**。
- **根因**：`RegisterCollisionTile`（BuildMap.cs:249）对副格只 `SetColliderType(pos, Sprite)`、**不 SetTile**。Unity Tilemap 对**无 tile 的格子 SetColliderType 无效**：`TilemapCollider2D` 不生成物理 shape、`GetColliderType` 返回 None → 副格网格可通（`BuildMap.IsCanReach`=tilemap 真值）、缓存可通（`UpdateCell` 判可通）、物理无碰撞。但床主格 SetComplete 后有 Sprite shape，物理覆盖主格顶边；A* 因副格"可通"而沿副格列走，角色到达副格时身体（半径 0.1）与邻格主格碰撞体重叠 → 撞上卡死。
- **放大因素**：`WorkerBrain.PreReserveAllRoomPositions` 把床主格 `ReserveBuildPosition(..., w=1, h=1)`（统一 1×1）→ 主格 `BuildTileData.Width/Height=1×1` → `SetComplete` 的多格同步分支（`Width>1 || Height>1`）不触发 → 副格永远走不到 SetComplete，只能靠 RegisterCollisionTile（无效）。而玩家建造路径 `ABuildItem.AddBuildTask` 用 `AddBuild(1×2)` 触发多格逻辑，两条路径尺寸不一致。即便改成 1×2，ReserveBuildPosition 的多格副格注册（BuildMap.cs:219）同样只 SetColliderType 不 SetTile → 依旧无效。
- **修复**（BuildMap.cs `IsCanReach`）：tilemap 判可通后追加 PosMap 补充真值——该格在 `BuildMapDataLAB.PosMap` 中且 `IsComplete=true` 且对应 BuildItem `IsPass=false` → 判不可通。床副格（RegisterCollisionTile 建的 `IsComplete=true`，SingleBed `IsPass=false`）因此正确不可通，A* 绕开整张床。未完成墙主格（IsComplete=false）仍可通，普通地面（PosMap 无记录）不受影响，主格（tilemap=Sprite）仍走 tilemap 真值。
- **验证**：编译待 Unity 确认。运行时看新局：奚武/舒宏才床副格 `[MapDiag] 缓存更新 pos=(101,184) 判不可通`（RegisterCollisionTile 时 UpdateCell 用新 IsCanReach）；舒宏才路径不再沿副格列穿过床，`[StuckDiag]` 床旁卡点消失。
- **教训**：**Tilemap 物理碰撞体的唯一真值是"有 tile + ColliderType"，`SetColliderType` 对无 tile 格静默无效**（不报错、不生成 shape、GetColliderType 返回 None）——凡"注册了碰撞体但寻路仍穿过"先核对目标格是否真的有 tile。**多格物品的副格注册必须同时 SetTile**（或让通行判定以 PosMap 数据为补充），否则"纯阻挡寻路"的副格名存实亡。**两条建造路径（WorkerBrain 预注册 vs ABuildItem.AddBuildTask）的多格尺寸必须一致**，否则 SetComplete 多格同步与副格状态分叉。blockCell 由 `WorldPosToMapPos(hit.point)` 取整，邻格贴边撞击可能标到相邻格，判读正对墙日志时以"物理 shape 实际覆盖"为准而非仅看 hit 坐标。

## 2026-08-16 合并路径卡死：IsLineWalkable 是零宽度格中心线，实际移动是有宽度圆 → 合并后直线擦墙

- **现象**：用户报告"合并路径有问题，本来可以走的，合并之后就不行了"。开启 WorkerPathMerge（合并）后 Worker 走压缩直线卡死；关闭合并（走 A* 原始逐格路径）不卡。
- **根因**：`CompressPath` 的合并判定 `IsLineWalkable` 只做 **Bresenham 格中心线走查**（零宽度），从 `path[0]`（=start 格 = Worker 提交寻路时所在格的**格中心**，`ASeek.cs:363` WorldPosToMapPos 取整）开始。但实际移动 `MoveByPath`（`ASeek.cs:529`）从 Worker **实际位置**（格内任意偏移点，半径 0.1 的圆）走向路点格中心。两处不一致：
  - **起点偏移**：合并假设从格中心出发，实际从格边（滑墙后停住的偏移点）出发，轨迹整体平移 → 擦墙角；
  - **宽度缺失**：格中心线可通但直线边缘擦过物理碰撞体（床主格 Sprite、墙边），角色半径 0.1 撞上；
  - **长跳 + 短探测**：合并第一跳可跨最多 30 格（`AStar.cs:345` scope），`WallProbeMax=0.3`（`ASeek.cs:138`）只探测即将碰撞的一小段，中间撞墙时 CircleCast 预检测覆盖不到 → 直接撞上、无机会 Sliding → 卡死。
- **修复**（`AStar.cs` `IsLineWalkable`）：按主导方向（|dx|>=|dy| 为水平）检查直线每格的垂直方向两侧邻格是否可通，模拟"碰撞体两端切面射线"——合并直线要求≥3 格宽通道，两侧有缓冲、不贴墙角。合并失败 → 回退 A* 原始逐格路径（原本可走），**安全兜底不卡死，只损失一点路径长度**。后台线程安全（纯格子查询，不碰物理/transform）。
- **验证**：`AStarLineWalkableTests` 新增 `SideWallAlongHorizontal_Rejects`/`SideWallAlongVertical_Rejects` 锁死侧墙语义（现有 4 例在宽度检查下仍全过：`OtherCornerWall` 提前被宽度检查拒绝、`OpenDiagonal` 无墙放行）。编译待 Unity 确认。
- **教训**：**寻路压缩的"可通行直线"验证必须与移动执行同一几何口径**——格中心线验证（零宽度、从格中心出发）与实体移动（有宽度圆、从实际位置出发）不一致，是合并后"本来能走却走不了"的机制根因。压缩检测应在后台线程用格子级宽度近似（检查法向两侧邻格）模拟碰撞体切面；物理 CircleCast 只覆盖即将碰撞的短距离，护不住跨格长跳的中段，不能替代合并时的直线级验证。

## 2026-08-16 Worker 死亡后房间全部发布为悬赏：预注册未完成瓦片失去规划归属 → VerifyBuildTasks 逐格重建无主任务

- **现象**：开局创建多个 Worker，突然某个 Worker 把自己的整间房间全部发布为建造悬赏（任务板刷满一房间的 CustomRoomWall_0~7/CustomDoor/SingleBed/InventoryWall_0~7 任务）。
- **根因**（用户观察"是 Worker 死亡导致的"后定位）：
  1. **建房预注册**：`WorkerBrain.PreReserveAllRoomPositions` 在建房时把整间房（墙/门/床/仓库）预注册为 `IsComplete=false` 的半透明瓦片。
  2. **死亡失去归属**：`IsPartOfWorkerPlannedRoom` 只查**存活** Worker，死亡 Worker 的房间瓦片不再被任何房间认领（PlannedHomePosition 归属判定失效）。
  3. **扫描重建**：`VerifyBuildTasks`（WorkerTaskManager.cs:528）每 300 帧（约 5 秒）扫描未完成 BuildMap 瓦片，发现"无主"瓦片就重建无主建造任务 → 整间房逐格发布为悬赏。日志铁证：何飞死亡 pos=(214,271) 后 0.8 秒内 VerifyBuildTasks 重建 20+ 条任务覆盖 212~218×268~274 整片房间。
  4. 排除项：被攻击本身（`ReduceHp`）只切 Attack/Escape 状态，`AttackEffect.OnParticleCollision` 只对 AttackTags 角色造成伤害、不破坏建筑——不是"被攻击破坏房间"。
- **修复**（用户选定"死亡清理"方案，而非"VerifyBuildTasks 跳过无主房间"）：
  - `Scripts/2D/AI/Worker/WorkerBrain.cs`：`ClearAbandonedBuildTilesCore` 从 instance 改为 `private static`（方法不使用实例状态，全部静态服务访问），同步修复行 2235/2328 两处调用点去掉 `this.` 前缀；新增 `public static void ClearDeadWorkerRoomTiles(AWorker.WorkerData wd)` 包装方法——从 `wd.PlannedHomePosition` 推导 center，复用核心清理逻辑，对房间布局（墙 WallOffsets/门 DoorOffset/床两格 BedOffset+BedSecondOffset/仓库 StorageOffsets）中每个 `IsComplete=false` 的瓦片执行 `PosMap.Remove` + `buildMap.CancelBuilding(pos)`，另单独处理床第二格碰撞体。
  - `Scripts/2D/Character/Worker/State/WorkerDeadState.cs`：`RemoveTasksForWorker` 之后、`DeleteWorkerPre` 之前插入 `WorkerBrain.ClearDeadWorkerRoomTiles(workerData)`，带死亡清理语义注释。
  - 语义边界：**已建成的瓦片（IsComplete=true）保留**——那是真实建筑，死亡不影响已有建筑；只清理预注册未完成（IsComplete=false）的规划瓦片。
- **验证**：编译待 Unity 确认（Unity 项目无法命令行直接编译）。运行时观察：Worker 死亡后其房间未完成瓦片立即被清除（不会刷出半透明残留 5 秒），任务板不再刷满"房间全部发布"的任务。
- **教训**：**"任务凭空刷满"类问题先查"归属判定失效 + 定时扫描重建"的组合**——预注册瓦片的归属依赖存活 Worker，死亡/移除后归属消失，周期性扫描把"无主"变成"新任务"。生命周期事件（死亡/删除）必须同时清理其在共享数据结构中的挂账（未完成瓦片），而不是等周期性扫描兜底；兜底扫描是为异常情况设计的，不应承担正常生命周期的清理责任。清理时以 `IsComplete` 区分"规划"与"真实建筑"，只清未完成，避免误删建成物。

## 2026-08-16 Worker/SeekEnemy 攻击"拐到其他方向攻击一次"：多目标跟错 + 攻击→移动→攻击切换时序（两处根因）

- **现象**：用户报告 Worker 与 SeekEnemy 攻击时"总是会拐到其他方向攻击一次"——武器突然转到旁边另一方向打一下再回来。反复日志（`[WeaponDiag]` 实例化/拐向/攻击 + 攻击状态偏差日志）最终定位：
  ```
  seekenemy@(51,111) CustomSword 实例化 初始z=83.8           ← OnEnter 已把武器初始朝向对准 Target(player)
  seekenemy@(51,111) CustomSword 武器拐向 108.9° 目标=韩东瑜   ← 武器 Update 拐向范围内"最近目标"韩东瑜
  seekenemy@(51,111) CustomSword 攻击 武器指向=108.9°         ← 朝韩东瑜打
  seekenemy@(51,111) 攻击方向偏差 64.9° 武器=108.9° 目标=player 目标角=173.8°  ← 攻击状态锁定 player，武器却朝韩东瑜
  ```
- **真正根因**：**武器方向由 `AWeaponObject.Update` 的"范围内重叠碰撞最近目标"（`minDistanceCharacter`）决定，而攻击状态锁定的目标（`SeekEnemyAttackState.Target` / `WorkerAttackState.LastAttacker`）是另一个角色**。战场混战时武器范围内同时有多个角色，武器每帧拐向最近的那个并在它们之间切换——视觉上"拐到其他方向攻击一次"。第一击方向正确（`aimInitialized` 门控 + OnEnter 初始朝向已验证生效，日志 `攻击 武器指向=目标角` 恒等），错的不是第一击，而是**持续攻击期间武器跟错了目标**。
- **前置修复**（保留，解决"拿起瞬间朝上突转"与"退出→重进销毁重建"两处表面问题）：
  1. `AWeapon.cs` `AWeaponObject` 新增 `bool aimInitialized`：`Update()` 首帧置 true；`Attack()` 开头 `if (!this.aimInitialized) return;`——武器刚实例化朝向未矫正时跳过攻击。
  2. 攻击状态 `OnEnter` 拿起武器后一次性设初始朝向为攻击目标（`WorkerAttackState` 朝 `LastAttacker`、`SeekEnemyAttackState` 朝 `Target`），消除拿起瞬间朝上。
  3. `WorkerAttackState` 超时（1.5s）先判 `IsStillUnderAttack()`（`LastAttacker` 存活且距离 <= 8）再决定是否退出——避免敌人持续攻击时"退出攻击→瞬间又被打回攻击"反复销毁重建武器。SeekEnemy 侧无此问题（退出需 2s 感知失败、重进需经实际寻路）。
- **最终修复（根治"跟错目标"）**：`AWeaponObject` 新增 `public Transform AimTarget`，攻击状态 `OnUpdate` 把锁定目标传给武器（`SeekEnemyAttackState` → `Target`、`WorkerAttackState` → `LastAttacker`）；`AWeaponObject.Update` 的跟踪目标改为 `AimTarget ?? minDistanceCharacter`（攻击目标优先，范围内最近目标兜底）；退出攻击状态时清空 `AimTarget`。视线检测（墙遮挡跳过攻击）也改为用 `AimTarget` 优先。
- **验证**：编译待 Unity 确认（Unity 项目无法命令行直接编译）。运行时观察：SeekEnemy 锁定 player 后武器持续朝 player（不再拐向旁边的韩东瑜/邵元/凤敬）；`攻击方向偏差` 日志从 64.9° 降到 <5°；Worker 反击 `LastAttacker` 同理。
- **教训**：
  1. **"武器方向"与"攻击目标"是两个独立数据源，必须显式对齐**：`AWeaponObject.Update` 的 `minDistanceCharacter`（范围内最近目标）是 Player 常驻武器的通用跟踪逻辑，攻击状态必须把锁定的 `Target`/`LastAttacker` 传给武器，否则混战时武器会跟旁边更近的角色而拐走。武器层的通用"最近目标"逻辑只应做兜底。
  2. **诊断日志要同时记录"武器指向"与"攻击状态锁定的目标角"**：单看武器方向看不出错，只有 `武器=108.9° 目标=player 目标角=173.8°` 这种并排对比才能暴露"跟错目标"。`攻击方向偏差` 日志（武器 vs 锁定目标角度差）是关键指标。
  3. **注意属性 getter 语义**：`ASeekEnemy.Direction` 优先返回寻路方向 `Seek.Direction`，`Direction = X` 赋值仅写私有字段且只在 `Seek.Direction==zero` 时被读到——"用了属性设方向却仍朝旧方向"先查 getter 是否有更高优先级来源。
  4. **诊断日志要区分"角色名"与"实例"**：敌人无名字（`Character.Awake` 设类型名），同类敌人数个时日志必须带坐标（`name@(x,y)`）区分；"拐弯很快 <0.5s"的事件点日志不能用长节流，改用方向变化触发（>5° 即记、不限频）。
- **补充（同日二次定位，真正的用户现象）**：用户澄清"拐向的是**没有角色的空方向**，且发生在攻击中突然切到移动状态、又切回攻击状态时"——不是多目标切换。真正根因是 **`SeekEnemyMoveState` 先 `ChangeState(Attack)` 后赋 `Target`**：`ChangeState` 是**同步**调用（立即执行 `OnEnter`），此时 `Target` 仍是 null（或上一轮旧值），`OnEnter` 的初始朝向不生效 → 武器保持 prefab 默认朝上（z=0，即空方向）；同帧 `SeekEnemyAttackState.OnUpdate` 里 `AttackRange/SightRange` 跟随武器 rotation 朝上 → 视觉"拐向空方向攻击一次"，下一帧武器矫正回目标 = "拐回去"。触发链路：SeekEnemy 攻击玩家 >5s 被反击（`ReduceHp` → `ChangeState(Move)`，武器销毁）→ Move 感知到目标又 `ChangeState(Attack)`（武器重建、方向朝上）。Worker 侧无此问题：`LastAttacker` 在 `CharacterHealthComponent.ApplyDamage`（`ReduceHp` 内）已先于 `ChangeState(Attack)` 赋值，OnEnter 初始朝向有值。
  - **修复**：
    - `SeekEnemyMoveState.cs`：先赋 `Target` 再 `ChangeState(Attack)`，OnEnter 初始朝向才能读到新目标。
    - `SeekEnemyAttackState.cs`：`AttackRange/SightRange` 直接用目标方向（`Quaternion.FromToRotation(Vector3.up, dirToTarget)`）计算，不再跟随武器 rotation——武器由 `AWeaponObject.Update` 动态矫正，进攻击状态首帧可能尚未矫正，视觉范围不应依赖它。
  - **教训（补充）**：**先 `ChangeState` 后赋状态数据 = 初始化代码读到旧值/null**。`ChangeState` 同步执行 `OnEnter`，"进状态时要读的目标/参数"必须在调用 `ChangeState` 之前准备好。
- **补充（同日三次定位，单目标复现确认，真正的周期来源）**：用户明确"**单目标**也复现：先攻击几秒 → 突然 Move → 瞬间又攻击"。触发源不在切换瞬间，而在 **`ReduceHp` 的换目标条件**：`在攻击状态 && AttackTime > ChangeTarget(5s) && 被反击 → 切 Move/Seek`（`ASeekEnemy.ReduceHp`、`ACommonEnemy.ReduceHp`）。单目标战斗中敌人攻击玩家几秒、玩家反击 → 条件成立 → 切走 → 目标仍在感知范围 → 下一感知帧立刻切回攻击 → 武器销毁重建 → **每 ~5s 循环一次**（正是用户"时不时"、且单目标也出现的现象）。修复：条件加 `attacker != this.Target` —— 被**当前攻击目标**反击时不换目标、继续攻击；被**其他目标**打才切（保留多目标换仇恨的设计）。
- **补充（同日四次，Worker 同构改动）**：用户确认 Enemy 修复有效后，要求 Worker 同样"被攻击就切换目标 → 改为和 Enemy 一样"。Worker 原把 `LastAttacker`（每次 `ReduceHp` 由 `CharacterHealthComponent.ApplyDamage` 更新为最新攻击者）直接当武器 `AimTarget`——被旁边目标打一下就换目标，没有锁定语义。改：`AWorker` 新增 `AttackTarget`（反击锁定目标）；`WorkerAttackState.OnEnter` 锁定 `LastAttacker`、`OnUpdate` 与 `IsStillUnderAttack` 改用 `AttackTarget`、`OnExit` 清空；`AWorker.ReduceHp` 攻击分支只在 `attacker != AttackTarget` 时更新目标（与 Enemy.ReduceHp 对称）。注意：批量替换 `LastAttacker→AttackTarget` 时把 OnEnter 的锁定赋值误替换成自赋值（`AttackTarget = AttackTarget`），需人工检查。
- **补充（同日五次，Worker 专注期·失败尝试）**：用户测试四次改动后反馈"**谁攻击 Worker 他就会转头，没有 Enemy 的持续攻击几秒**"。四次改动只加了锁定语义（`attacker != AttackTarget` 才换），没有**时间门控**。尝试：`WorkerAttackState` 新增 `public float AttackTime { get; private set; }`（OnEnter 置 0、OnUpdate 累加 `DeltaTime`、`Reset()` 不重置），`AWorker.ReduceHp` 攻击分支改为 `attackState.AttackTime > ChangeTarget(5s)` 才换。**此方案无效**（用户复测仍转头），根因：`AttackTime` 是"进入攻击状态的累计时长"，而用户场景是 Worker 与 Enemy **互殴**——Worker 早已攻击超过 5 秒，`AttackTime` 恒 `>5s`，此时玩家一打立即满足换目标条件 → 转头。专注期必须基于"**被打时刻**"而非"进入攻击时长"。
- **补充（同日六次，Worker 被打锁定期·有效）**：五次方案失败的教训——"持续攻击几秒"指**被打后**继续攻击当前目标几秒，与 Worker 已经攻击了多久无关。最终修复：`WorkerAttackState` 新增 `FocusDuration = 5.0f` 常量与 `focusEndTime` 字段（OnEnter 重置为 `float.MinValue`，以 `AttackTime` 为时间轴）；新增 `OnHit()`（被打时若当前不在锁定中则 `focusEndTime = AttackTime + FocusDuration`，锁定中再次被打**不刷新**——否则持续被打会永远刷新锁定、永不换目标）与 `CanSwitchTarget()`（`AttackTime > focusEndTime`）。`AWorker.ReduceHp` 攻击分支：先 `attackState.OnHit()`，再 `attackState.CanSwitchTarget() && attacker != null && attacker != AttackTarget` 才换目标；换目标时 `LogProviderThrottled`（`[StateDiag]` 换反击目标，2s 节流）便于复现定位。语义：**任何人打 Worker → 锁定当前目标 5 秒（期间继续攻击不转头）→ 5 秒后若再被其他目标打才换**；被当前攻击目标打永远不换（对称）。该方案同时覆盖"互殴很久后被打"与"刚进攻击被打"两种情况。`AWorker.ChangeTarget` 常量随之删除（移入 AttackState 为 `FocusDuration`）。

## 2026-08-16 Worker 携带>80% 仍不入仓：溢出冷却死循环 + 目标材料硬排除 + 出售抢跑 → 保留量+超额可存、溢出先出售、存储先于出售

- **现象**：Worker 有家且携带量长期 >80%（最高 195/200）仍不入仓。日志统计（grep game.log）：
  - 195 次"溢出冷却"（大部分 0.0s）——拾取溢出失败设 `LastStorageOverflowTime` → 10s 内 Store 被冷却阻止 → 期间继续采集 → 再溢出 → 无限循环；
  - 42 次"无可存物品"，目标分布 BuildStructure(6)/StockFood(5)/EarnMoney(2)——目标材料/食物被 `IsDepositable` 硬排除；
  - 出售仅移除 1 个资源（`出售1个资源(1种)获得1G (总携带163/200)`），压不下 80%；
  - 全程 `type=Store` 决策 0 次下发。
- **根因**：① 仓库入仓判定 `IsDepositable` 硬排除 Food/Seed/Consumable/Equipment/Weapon/目标材料——溢出时"无物可存"；② 溢出失败只设冷却、无出售出路，冷却期间继续采集再溢出 = 死循环；③ 出售阈值(0.75)低于存储阈值(0.8)，出售先触发但只卖 1 个，把携带量始终卡在 80% 附近且 Store 永不触发。
- **修复**（用户选定三方向，全部落地）：
  1. **AWorker 入仓判定改为"保留量+超额可存"**：`IsDepositable` → `GetDepositableCount(ResourceInfo)`（public）。新规则：他人悬赏物不可存；当前建房目标材料按 `RequiredMaterials` 所需数量保留、超额可存（与出售不同——出售完全保留不卖）；其余按类型保留（抽公共 `GetTypeKeepReserve`：食物10/饥饿15、种子5、材料15、药水5、装备武器1、默认5）。`GetDepositableResources` 同步返回超额数量。`DepositToStorage` 加防超扣保护（`actualCount = Math.Min(resourceInfo.Count, GetResourceCountById(id))`，`actualCount<=0` 直接 false）。
  2. **溢出失败先出售腾空间**：`WorkerPickUpTask.TryRedirectOverflowToStorage` 失败路径先调 `worker.GetSellableSurplus()`（新增 public，镜像出售保留规则：建房目标材料 ContainsKey 跳过不卖、其余按类型保留、他人悬赏物不卖），可售超额 `>= needToFree` 时 `MarketService.WorkerAutoSellFiltered` 出售 → 链回 resume PickUpTask 继续拾取；否则才放弃+冷却。
  3. **存储先于出售**：`WorkerSeekState.TryAutoSellResources` 触发门槛 `0.75f → 0.85f`（`carryRatio <= 0.85f && sellList < 5` 才跳过），存储阈值 0.8 恒定低于出售——0.8~0.85 区间只触发 Store，>0.85 才卖（存储放不下再卖）。
- **验证**：编译待 Unity 确认（Unity 项目无法命令行直接编译）。运行时观察：Worker 携带过 80% → `type=Store` 决策下发、`[TaskDiag] ... 存入仓库` 出现；溢出拾取时 `[TaskDiag] ... 出售N个腾空间` 或 `先回家存N个再回来拾取`，不再出现"溢出冷却"刷屏。
- **教训**：
  1. **"有仓库却不入仓"先查三件事：入仓判定是否硬排除目标类物品、溢出失败是否有出路、存储/出售阈值顺序是否让出售抢跑**。三处任一失衡都会让携带量卡死在高位。
  2. **"携带量 >80% 但 Store 决策 0 次"这类矛盾，日志要同时统计"冷却次数/无可存物品/Store 下发次数"三个计数器**才能定位是"决策被挡"（冷却）还是"决策无内容"（无可存）还是"决策从没走到"。
  3. **入仓与出售的"保留量"语义不同，必须分开实现**：入仓只需保留建造所需量（超额可存，材料进仓不算浪费）；出售对目标材料完全保留不卖（材料卖掉建不了房）。两者共用一个"按类型保留"底层，只让目标材料分支不同——抽公共方法避免谓词漂移。
  4. **出售"按种类 1 个 1 个卖"压不下携带量**：触发出售时应卖"超过保留量的全部超额"而非只卖 1 个。

## 2026-08-16 入仓保留量改为「携带上限百分比」：消耗品2.5%、材料4%，建房目标不再特殊保留

- **现象**：仓库存储修复后血瓶/石头开始入仓，但木材（CustomWood，带 8~10 个）仍不入仓。日志铁证：入仓记录只有 id=200000(血瓶)/300001(石头)/500000，木材(300000)从未入仓。
- **根因**：`GetKeepReserve` 对**当前建房目标材料**（BuildStructure 默认 `{CustomWood:10, CustomStone:8}`）保留 `RequiredMaterials` 所需数量。木材是建房主料保留 10，而身上木材 ≤10 → 超额 0 → 永不入仓。血瓶/石头超额多所以入仓（石头也是目标材料但保留只 8、掉落多、超额大）。
- **用户决策**（多轮澄清后）：① 不要因建房目标特殊保留材料——建房不够可以从仓库取（`TryMakeWithdrawForBuild`）；② 保留量用**百分比结构**而非绝对数（MaxResourceCount=200 将来可能修改，绝对数不随 Max 缩放）。
- **关键数字约束**：木材实测带 8~10，要让木材入仓，保留量必须 <8 → 材料比例必须 ≤4%（200×4%=8）。用户最初提的"材料 1/4=50"、"1/3=66"都远高于实测持有量，会导致血瓶/石头/木材全部不入仓、仓库停摆——**比例定太高，物品永远入不了仓**。反推日志：石头带 20~30 → 入仓 x12~x22 推出保留 8；血瓶带 9~17 → 入仓 x4~x12 推出保留 5。
- **修复**（`AWorker.cs` `GetKeepReserve`）：
  - 消耗品（血瓶等）：`MaxResourceCount * ConsumableKeepRatio(2.5%)`（200→5）
  - 材料（木头/石头等）：`MaxResourceCount * MaterialKeepRatio(4%)`（200→8）
  - 其他（食物/种子/装备）：保留现状类型保留（食物10/饥饿15、种子5、装备1）
  - 删除建房目标 `RequiredMaterials` 特殊保留分支。
  - 百分比提取为命名常量，Max 修改自动缩放。
- **效果**（Max=200）：血瓶带 9~17 入仓 4~12、石头带 20~30 入仓 12~22（与日志吻合）；木材带 9/10 首次能入仓 1~2。入仓量 = 持有 − 保留量（只放超额，不整类清空）。
- **教训**：
  1. **设计"占最大值的比例"前先看日志实测单种持有量**——否则比例定太高（1/3、1/4）会让所有物品都达不到阈值、仓库停摆。保留量必须 < 目标物品的实测最小持有量。
  2. **绝对数与百分比的选择**：Max 可能改的系统必须用百分比结构，但百分比要按"当前 Max × 百分比 ≤ 目标实测持有量"反推，而不是拍脑袋。
  3. **物品 ID 要先映射**：日志里 300000=木材/300001=石头/200000=血瓶/500000=装备类（SO 在 `Resources/SO/Backpack/MaterialItemData.asset`/`ConsumableItemData.asset`），先建立 id→类型映射再分析"为什么某类不入仓"。

## 2026-08-16 Worker 家庭仓库格右键：ItemInfo 显示数量等信息（图标在 ItemMap 上、未注册进 Drop/Inventory）

- **现象**：右键 Worker 家庭仓库（4 槽，`PlannedHomePosition + StorageOffsets[i]`）的物品图标，ItemInfo 只显示 `Build: InventoryWall...`，看不到数量/名称等物品信息。
- **根因**：仓库物品图标由 `WorkerStorageTask.RefreshStorageIcons` 画在 **ItemMap**（`ItemMapProvider().AddTile`），但只画图标，数据仍在 `wd.Storage`（`Dictionary<int, ResourceInfo>`）。`ItemInfoUI.GetResource` 只查 `DropManager`（掉落物注册表）与 `InventoryManager`（玩家仓库格）——Worker 仓库格都不在其中 → 返回空 → 落回 `GetTile` 显示 BuildMap 的 InventoryWall 瓦片信息。
- **修复**：
  1. `WorkerStorageTask.TryGetStorageItemAt(posMap, out AWorker owner, out ResourceInfo item)`（public static）：遍历 `WorkerManager.Characters`，对每个有家且建完（`HomeBuildStage >= layout.CompleteStage`）的 Worker，**复刻 RefreshStorageIcons 的逐格映射**（仓库内容按字典序一格一个、跳过 itemData==null / tile 资源缺失条目）比对 `center + StorageOffsets[index] == posMap`，命中即返回所属 Worker 与该格物品。仅在右键时调用（遍历全部 Worker），非每帧逻辑。
  2. `ItemInfoUI.Update` 右键流程：`GetResource` 落空后、落回 `GetTile` 前插入分支，命中仓库格 → `select = "WorkerStorage"`，`BuildStorageItemText(owner, item)` 输出 `ID/名称/英文名/类型/数量/所属Worker/拥有者/信息/可堆叠`（格式对齐 `DropManager.ToString`），物品数据缺失时兜底只显示 ID/数量/所属Worker/拥有者。
- **验证**：编译待 Unity 确认。运行时观察：右键仓库有物品的格 → ItemInfo 显示名称与数量；右键空格 → 仍显示 InventoryWall 建造信息。
- **教训**：
  1. **"地图上有图标 ≠ 有数据可查"**：Worker 仓库图标画在 ItemMap 是纯表现层，查询必须回 `wd.Storage`；ItemInfoUI 的三条查询路径（Character/Drop/Inventory）都不覆盖它，须显式加第四路。
  2. **位置→数据映射必须与图标绘制同一规则**（同一套跳过条件、同一 `StorageOffsets` 索引），否则右键展示的物品与玩家看到的图标错位。
  3. **复用 `WorkerManager.Characters` 全量遍历即可**（Worker 数量级小、仅右键触发），无需为仓库格建空间索引。

## 2026-08-16 温度系统落地 + 非房间位置误判为房间内部（右键显示 25）

- **现象**：右键点击**非房间内**位置（野外空地），ItemInfo 显示 `温度:25`（房间默认温度），而非室外温度。
- **根因**：`EnvironmentManager.ToString(posMap)` 调用 `RoomManager.GetRoomByPos` —— 四方向 1000 单位 `Physics2D.Raycast` + `count >= 2` 即判"在房间中"；且 `posMap1` 在四个方向命中时被反复覆盖（最后一次命中为准），野外点只要两个方向能打到远处墙（如远处房子的墙）就误判，随后用 `posMap1` 查 `Points` 命中便返回写死 `Temperature=25` 的房间。
- **修复**：
  1. `EnvironmentManager.ToString(posMap)` 改用 `RoomManager.GetRoomInterior(posMap)`（包围盒精确判断、避免射线）；`GetRoomByPos` 加 `[Obsolete]`（唯一调用者已移除）。
  2. 顺带建立完整温度系统（本次主变更）：
     - 新建 `TemperatureRuleService`（纯规则）：季节基础温度（春18/夏30/秋18/冬2）+ 天气偏移（晴0/雨-6/雪-12）+ 昼夜波动（±4，相位与 GameTimeUI 光照一致）；房间 = 室外 + 保温6 + ΣHeatPower；温度→移动倍率/疲劳倍率映射。
     - 新建 `TemperatureEffect`（Singleton + ITickable）：室外温度平滑 0.5℃/s、每 1.5s 扫描房间热源刷新 `RoomInfo.Temperature`（直接写字段，RoomListUI/ItemInfo 自动变实时）、每 0.5s 缓存角色位置温度避免每帧全房间遍历。
     - 接入点（乘法叠加）：`Player` 移动、`ASeek` 工人移动、`WorkerUpdateSystem` 疲劳累积（空闲递增，疲劳值越大越疲）。
     - `BuildItemData.HeatPower` 数据驱动供暖（本期 SO 不配数值，后续配置即生效）；`EnvironmentManager.Temperature` 死字段删除，湿度占位值 -10 → 25。
- **验证**：右键野外 → 显示实时室外温度；房间内 → 室外+6。单测 `TemperatureRuleServiceTests` 覆盖季节循环/天气偏移/倍率/边界。
- **教训**：**射线判房间不可靠** —— 物理射线命中任意障碍物（含远处房间的墙），无法区分"站在房间内"与"朝向房间"。房间内部判断必须用房间自己的包围盒几何（`IsInterior`，向内收缩一格），与物理世界解耦。

## 2026-08-22 web_labeler 批间延迟不生效：delay_sec 只挂在重试分支，成功路径零停顿

- **现象**：用户反馈「model_config.yaml 的 delay_sec（已设 100）好像不起作用，打标速度很快」。批间几乎无停顿，多平台连续猛打。
- **根因**：`src/web_labeler.py` `WebTeacher._label_batch` 中全文件唯一的 `time.sleep(self.delay_sec)` 位于**批次失败重试分支**（`if attempt < max_retries: ... time.sleep(self.delay_sec)`，行 ~404）。**成功路径直接 `return parsed`，批间无任何 sleep**；`WebTeacherPool` 的 worker 每批成功后立刻拉下一批。`delay_sec` 的注释语义是「批间停顿」（`web_platforms.py:49`），但实现只把它当「重试间隔」——成功批次之间从未消费它，配置再大也不起作用。
- **修复**：`_label_batch` 的两个成功返回点（完整解析通过、追问补全完成）前补 `time.sleep(self.delay_sec)`，让批间降速真正生效（单教师顺序版与 pool 并行版共用该方法，一并覆盖）。
- **验证**：重跑打标，每批成功后应停顿 delay_sec（当前 100s），单平台请求频率降至 ~1 批/100s，符合「降速防风控」意图。
- **教训**：**「名义节流/延时」配置必须核对成功路径是否消费**——`delay_sec` 语义是批间停顿，实现却只挂在重试（异常）分支，成功路径永不触发，形成"配置大、行为快"的静默失效。排查"延时/频率配置不生效"时，先 grep 配置项的所有消费点，确认它挂在主路径而非仅异常兜底路径。

## 2026-08-23 web_labeler 规则生成路径漏掉 wenxin 惰性输出追问补全（--gen-rules 反复「规则校验失败」）

- **现象**：`--gen-rules` 时 wenxin 回复 len=49 的元话语（"用户现在需要我生成这个工人行为决策的JSON规则，我已明确规则制定优先级，将按需求、生存等维度排序"），`_parse_rules` 拿不到规则 JSON → 日志「规则校验失败」→ 重试发同一 prompt 必然再次惰性输出 → 「写规则失败，跳过」。
- **根因**：打标路径 `_label_batch` 有 `_complete_labels` 追问补全专门对付 wenxin 惰性输出（首轮只给部分/只给规划 → 同会话追问「只输出 JSON」），但规则生成路径 `generate_rule_set` 只有「重试同一 prompt」没有追问兜底；重试对惰性输出无效。
- **修复**：`generate_rule_set` 首轮解析/校验为空时调用新增 `_complete_rules(schema_keys)`（与 `_complete_labels` 同款：同会话追问「只输出规则 JSON 对象本身，不要任何解释、计划、开头语」，最多 3 轮，`_wait_reply` timeout=90），拿到合法规则即返回；仍拿不到才走外层重试。成功打印行 `len(parsed)` 改 `len(valid)`（追问路径下外层 parsed 为 None，`len(None)` 会抛异常并丢弃已追问成功的 19 条规则）。
- **验证**：实测重跑 `--gen-rules`：wenxin 首轮惰性输出 → 追问补全产出 19 条合法规则（日志「追问补全规则 19 条」），`len(None)` 修复后不再抛异常、规则正常写入。
- **教训**：**同一惰性输出问题，两个入口（打标/规则生成）要成对修**——补丁只修了一个路径，另一路径复现时容易被当成「新问题」重复排查。出现「输出规划/元话语而不是内容本体」的惰性模式时，先 grep 所有解析入口，逐一对齐追问兜底。

## 2026-08-23 拾取溢出"无物可存"直接放弃：建房后身上"多种少量"建材每项低于入仓保留量，掉落永留地面

- **现象**：用户报告"拾取数量超过空闲空间 → 不拾取 → Worker 去仓库放物品 → 该掉落永不被拾取、任务被丢弃"。日志：`拾取溢出且无物可存, 放弃拾取` 几十次（王保/罗和泰/邬彪等），同一位置每 12-13s（>10s 冷却）反复重试；`放弃任务 type=PickUp` 26 次 vs `从地面捡起` 238 次。
- **根因**：
  1. **数值失衡（直接诱因）**：`DropItemDataSO.asset` Default 组砍树一次掉 **CustomWood×50**，而建房每格默认只需 CustomWood×5（`GetBuildMaterialNeeds` 兜底；所有 Build SO 的 BuildCosts 均为空）。一次砍树 = 10 格墙的材料，Worker 捡 3-4 次就 150+/200 接近上限。
  2. **无物可存机制**：`GetDepositableCount` 只收集"超保留量"部分（材料 4%Max=8、消耗品 2.5%Max=5），`GetSellableSurplus` 同口径（且建房目标材料完全排除）。王保建房（19 墙+门+床+4 仓库）后身上剩"多种少量"建材，每种 ≤ 保留量 → 溢出时 `freed < needToFree` → `TryRedirectOverflowToStorage` 直接放弃。铁证：王保 03:30:13 存仓库只存了血瓶×5（165→160），证明可存超额 < 10。
  3. 放弃后掉落留地面 → `LastStorageOverflowTime` 冷却 10s → 决策层重新选中 → 又溢出 → 循环。物品永不被拾取。
- **修复**（用户选定预防方向，非溢出兜底）：
  - `Resources/SO/DropItemDataSO.asset`：砍树 CustomWood Count 50→10（一次砍树 ≈ 5 格墙材料）。
  - `WorkerBrain.GetBuildMaterialNeeds`：默认建房材料 CustomWood×5→×2。
  - 效果：Worker 单次采集量从 52 降到 12，捡 16 次才满（200/12），溢出频率降一个数量级。
- **验证**：重跑游戏，观察 `拾取溢出且无物可存, 放弃拾取` 应基本消失；王保类建房 Worker 身上携带应在采集与建房间良性循环，不再堆到近满。溢出路径已加诊断日志（`carried/Max needToFree 可存=[...] freed 可卖=[...] sellableCount 仓库槽`），若残余溢出仍出现，用日志确认构成。
- **教训**：
  - **掉落数量与单次消耗的数量级必须匹配**：一次砍树 = 10 格墙材料，导致 Worker 采集 3-4 次就背包近满。数值设计时掉落量应 ≈ 单位消耗量（或略高），否则 Worker 背上大量"暂时用不完"的物资。
  - **"溢出腾空间"依赖"超额可存"，而保留量规则让"多种少量"状态无解**：建房/消耗后身上每种材料都 ≤ 保留量，总量却接近上限——溢出时 `GetDepositableResources`/`GetSellableSurplus` 都只给超额部分，腾不出空间。这是容量语义的盲区：保留量保护了"单种够用"，但没保护"总量溢出时能腾挪"。若后续仍需兜底，可让溢出场景突破保留量（每种最低留 1）。

## 2026-08-23 拾取溢出"可存=空"根因确认：OwnerId 归属污染（普通掉落物携带采集者归属，拾取后污染自用物品）

- **现象**：数值调整（掉落 50→10、建房 5→2）生效后溢出仍发生（10 分钟 6 次），失败日志 `可存=空` 异常——钱广 carried=200/200 但 `GetDepositableResources` 返回空。用户反馈"没有根本解决"。
- **根因（`AWorker.AddResource` 叠加不更新 OwnerId 的首写污染）**：
  1. 背包 `resourceInfos` 按 Id 合并物品，OwnerId 由**首个写入**决定且叠加时**只加 Count 不改 OwnerId**（AWorker.cs:449）。
  2. 普通采集掉落物 `OwnerId=采集者`（WorkerGatherTask 旧逻辑）。掉落物留在地面后（采集者死亡掉落 / 中途放弃 / 被他人搬运），**任何工人拾取都原样保留掉落者 OwnerId**（`WorkerCarryTask` ToInventory、`WorkerPickUpTask` FromGround 直接 `AddResource(ri)`）。
  3. 拾到他人归属物后，工人自己再采集同 Id 物品 → 叠加 → **自己的物品被判为"他人悬赏物"**。`GetDepositableCount`/`GetSellableSurplus` 排除 `OwnerId != 0 && OwnerId != self` → 溢出时可存=空 → 放弃拾取。
  4. 铁证：酆清瑜(-9274) 17:42:11 生成，17:42:12 接近秦明(-9436)，17:43:03 起苹果与木头都已带 OwnerId=-9436；`AddResource 同ID异Owner` 390 次全为 300000/500000（木头/苹果）。附加污染源：交易 `buyer.AddResource(食物, OwnerId=卖家)` 立即吃掉后 Count=0 残留项保留卖家 OwnerId；仓库取物 `WithdrawFromStorage` 保留仓库里他人物归属。
- **修复（用户确认完整修复）**：阻断点在"进背包"——拾取/搬运掉落物时统一归**拾取者自己**（保持"Worker 的物归 Worker"），悬赏物（ToBoard）保留发布者归属。第一版曾把普通物归 0，会把 Worker 自采物品误标为 Player 无主（用户反馈"拥有者怎么都是 0,Player 了"），已撤销改为归自己：
  - `WorkerPickUpTask.FinishFromGround`：普通拾取 `AddResource` 归 **`worker.GetInstanceID()`**；**悬赏链**（`chainCompleteTask` 为 ToBoard CarryTask）保留 OwnerId=发布者。
  - `WorkerCarryTask`：新增 `IsBoardMode`；ToInventory 拾取归**执行者自己**，ToBoard 保留。`SelfCarry`/`FromBoard`（悬赏物 OwnerId=自己）不受影响。
  - `WorkerGatherTask.ProcessDrops`：**不改掉落物归属**（普通=采集者、悬赏=发布者，防他人抢捡）；污染由拾取/搬运处归"自己"阻断。
  - `WorkerTradeService.TryBuyFood`：移除 `buyer.AddResource(食物, OwnerId=卖家)` + 立即 `SubResource` 绕圈，食物直接消耗恢复饥饿（不经背包）。
  - `AWorker.SubResource`（两重载）：扣到 0 移除字典项，消除 Count=0 残留首写 OwnerId。
  - `AWorker.WithdrawFromStorage`：仓库取物归自己。
- **验证**：重跑游戏，Worker 身上物品 OwnerId=自己（非 0/Player）；`AddResource 同ID异Owner` 应消失（或仅剩悬赏物与自身物叠加的极端边角）；溢出"可存"应非空，放弃拾取基本消失。
- **教训**：
  - **OwnerId 语义：掉落物保留采集者/发布者归属（防抢捡），进背包时归"拾取者自己"**。背包按 Id 合并、单一 OwnerId 无法表达"既含悬赏又含自用"的同 Id 物品——一旦他人归属进背包并叠加，自用物即被污染。阻断必须在"进背包"点（拾取/搬运/仓库取物），而非改掉落物归属。
  - **"进背包再扣掉"的绕圈是污染温床**（交易食物），消耗品应直接消耗，不进背包。
  - 诊断日志要在失败路径打印完整清单 `id:count@ownerId` + selfId，才能区分"保留量不足"与"OwnerId 污染"两类"可存=空"。

## 2026-08-24 背包初始填充 asset not found 刷屏（背包简化引入）
- **现象**：开局 `BackpackController.Awake → GenBackpackItems` 期间 Console 连续刷 `Torch asset not found!!!` / `Seed1~Seed14 asset not found!!!` 等 Error。
- **根因**：背包实例化简化为"所有 Backpack SO 物品进背包"后，初始填充数量大增；`GetBackpackItemByName`（ItemInstanceFactory.cs:47）**无条件** `item.Tile = GetAsset(name)`：
  1. `GetAsset` 是 Error 级日志（ResourceManager.cs:216），无对应 tile 资源的物品（Torch/Seed1~14）逐件刷屏；
  2. **无条件覆盖**子类构造函数已设置的 `Tile`（AddHp/CustomWood/CustomStone/Apple 构造里 `this.Tile = GetAsset("...")`）——若 `GetAsset(name)` 返回 null 会清空子类已设的 Tile。
- **修复**：
  - `ResourceManager` 新增 `TryGetAsset`（`assetDic.TryGetValue` 失败返回 null，**不打日志**）。
  - `GetBackpackItemByName`：`if (item.Tile == null) item.Tile = TryGetAsset(name)`——子类已设 Tile 不覆盖；无 tile 资源不打日志，Tile 保持 null。
- **验证**：编译后开局无 `asset not found` 刷屏；AddHp 等仍保留构造函数设置的 Tile；Torch/Seed 等 Tile=null 不影响背包显示（UI 图标走 `GetImage`）。
- **教训**：
  - **可选资源查询用无日志变体（TryGetAsset），强制语义（必须有）才用 Error 级 GetAsset**。Tile 对背包物品只是"掉落/放置时"才需要的可选元数据。
  - **`Activator.CreateInstance` 后不应无条件覆盖子类构造已初始化的字段**——先判空。
  - 批量初始填充会放大单件日志成本（每物品×1 条），可选资源缺失必须静默。

## 2026-08-25 帧动画中心偏移（裁剪子 sprite pivot 在左下角 + 各帧裁剪框大小不一）

- **现象**：用户从一张 sprite sheet（Sprite Editor）裁出 5 帧 GrassTree_0~4 做地面帧动画，动画能播但"中心位置不对"——视觉内容整体偏到格子右上。
- **根因**（资源导入设置，非代码 bug）：
  1. **pivot 全在左下角**：`GrassTree.png.meta` 每个子 sprite `pivot: {x:0,y:0}`（`alignment: 0`），而 `TileVisualSpawner.CreateOrUpdate` 把 `transform.position = tilemap.GetCellCenterWorld(cell)`——sprite pivot 落在格子中心，rect 从格子中心向右上铺开。以 264×646 帧算，视觉中心偏到格子右上 ~(1.32, 3.23) 世界单位。静态 tile 图正常是因为其 pivot 是中心 (0.5,0.5)。
  2. **各帧裁剪框不一致**：GrassTree_1 宽 525，其余 ~262——即使 pivot 改中心，各帧 rect 中心不重合，播放时仍有轻微跳动。
- **修复**：在 Sprite Editor 里把 Multiple 图的各帧 pivot 统一设为中心（全选帧 → pivot 改 Center）。曾临时写过 Editor 工具批量处理（`FramePivotTool.cs`），后按用户要求删除；如需再次批量处理可重写同逻辑（`TextureImporter.spritesheet` → 各 `SpriteMetaData.alignment=Center/pivot=(0.5,0.5)` → `SaveAndReimport`）。
- **验证**：菜单执行后动画帧视觉中心与静态图对齐到格子中心；若 GrassTree_1（525 宽）仍明显跳，需在 Sprite Editor 里重裁该帧或统一各帧框选范围。
- **教训**：**Sprite Editor 手动裁剪出的子 sprite 默认 pivot 在左下角，必须统一设为中心（或裁剪时保持各帧 rect 一致 + 内容对齐）**。帧动画视觉中心依赖 sprite pivot 与 `GetCellCenterWorld` 的配合，pivot 异常不会报错而是静默偏移。

## 2026-08-31 树动画不显示（clip 名不匹配 + Animation 组件 Culling 死锁）

- **现象**：勾选 `ItemData.IsAnimation` 后 GrassTree 的 tile/碰撞体正常创建，但独立视觉物体（TreeVisual_*，可遮挡淡化的 SpriteRenderer）不显示；用户先误判为"物体没创建"（实际已创建，只是 sprite 恒 null 隐形）。game.log 先刷 `GrassTree animation clip not found!!!`，clip 名修正后物体仍在但不显示。
- **根因**（两段叠加）：
  1. **clip 资产名必须与 `ItemData.Name` 完全一致**：AnimationManager 按 SO 英文名（`GrassTree`）查 clip，生成的资产却叫 `GrassSakuraTree` → `GetClip` 查不到 → 回退静态（此段用户自建 `GrassTree.anim` 后已消除）。
  2. **Animation 组件默认 `cullingType = BasedOnRenderers`**：动画接管 sprite 显示后（TileVisualSpawner 跳过静态赋值），SpriteRenderer 初始 sprite=null → 无渲染边界 → 被判定"不可见" → 动画永不求值 → sprite 永不被写。**空 sprite + 基于 Renderer 的剔除 = 自举死锁**，物体隐形但组件齐全。
- **修复**：`SpriteFrameAnimator.Init` 中 `AddComponent<Animation>()` 后设置 `animation.cullingType = AnimationCullingType.AlwaysAnimate`（SpriteFrameAnimator.cs）。附带本轮重构：SpriteFrameAnimator 内部从"探测 `{prefix}_0/_1...` 序列图逐帧换图"改为"从 AnimationManager 取 AnimationClip 经 legacy Animation 组件播放"；`Assets/Animation` 整体移入 `Resources/Animation`（Resources.Load 运行时只能加载 Resources 下资产）；删除仅服务旧帧探测的 `ResourceManager.TryGetImage` 与废弃单测 `SpriteFrameAnimatorTests`。
- **验证**：重进 Play，GrassTree 应显示帧动画并摆动（多实例相位随机不齐摆）；game.log 无 `animation clip not found`；随机相位日志无异常。
- **教训**：
  - **动画资产名与 SO 英文名的一致是隐式约定**——生成交付物（.anim）时命名以 `ItemData.Name` 为准，不是以源图文件名为准。
  - **Animation 组件接管 sprite 显示时必须 `AlwaysAnimate`**（或先赋一帧静态图自举可见性），否则空 sprite + BasedOnRenderers 永不启动且无任何报错。
  - **Editor 开着时勿在文件系统移动资产目录**——AssetDatabase 会按内存缓存重建出重复资产（本次 `Animation/` 目录双份、GUID 分叉），移动/重命名一律在 Project 窗口内做。
- **追加（同日）**：legacy `Animation` 组件方案在 2022.3 实测无效（AlwaysAnimate 修复后仍静默不播，无任何报错）——非 legacy .anim 交给 Animation 组件不可靠。已整体切换为 **Animator + AnimatorController**：AnimationManager 改加载 `RuntimeAnimatorController`（`GetController`），SpriteFrameAnimator 挂 `Animator`（同样必须 `cullingMode = AlwaysAnimate`）。**新约定：每个动画物品一个 controller，资产名 == ItemData.Name**；新增 Editor 右键「为选中动画生成 Controller」（AnimationControllerGenerator.cs）一键从 clip 生成配套单状态 controller。

## 2026-09-01 CommonEnemy 漫游撞墙（无前方感知，全靠碰撞事后熔断）

- **现象**：CommonEnemy 是矩形（BoxCollider2D 0.4×0.4、根 scale 2），Wander 随机转头直线行走，前方无任何障碍感知——顶到墙后才由 `OnCollisionStay2D` 的 Sliding/Stuck 熔断重入 Wander 换向，视觉上反复"撞墙→弹偏→换向"。
- **根因**：`CommonEnemyWanderState.OnFixedUpdate` 只按 `rotationAngle` 渐近转向 + `MoveToForward` 直线平移，选向与移动均不看前方；事后熔断依赖物理接触，必然先撞后知。
- **修复**：
  - `ACommonEnemy.IsForwardBlocked(direction)`：矩形**两边及中线**三条 `Physics2D.Raycast`（中线 origin=中心，两侧边 origin=中心±垂直×半宽），距离=沿向半长+`ProbeDistance`(0.6)；半尺寸由 `BoxCollider2D.size×lossyScale` 投影算支撑半径（任意旋转自适应）。LayerMask 仅 `Tile`/`BuildTile`（同 ASeek 墙壁探测）→ 天然不命中自身与角色。
  - `CommonEnemyWanderState`：随机选角抽成 `PickNewDirection()`——最多 8 个候选角用三射线预判，选第一个不通墙的（全堵取最后兜底）；`OnFixedUpdate` 移动前探测目标朝向，受阻即换向 + `[EnemyDiag] WanderReroute` 节流日志。定时换向同样走预判。
- **验证**：Play 中敌人靠近墙群即提前换向，不再顶墙抖动；game.log 无刷屏。Chase/Seek/Attack 不变，`OnCollisionStay2D` 熔断保留兜底（角色碰撞、贴墙滑动）。
- **教训**：**运动学平移的角色没有"即将碰撞"概念，前方短距探测要在选向/移动前做**，事后熔断只应作兜底；探测层收紧到墙体层即可省去"排除自身"的射线过滤。
  - **`LayerMask.GetMask`（内部 NameToLayer）不能在 MonoBehaviour 的静态字段初始化器里调用**——实例化 prefab（MonoBehaviour 构造路径）触发 cctor 时 Unity 直接抛 `TypeInitializationException`。普通 C# 类（如 ASeek 的 `s_wallLayerMask`）不受限，照抄进 MonoBehaviour 子类必炸；改实例字段在 `Start()` 赋值。
- **追加（同日）——水/山地形避让失效根因**：地形 tile 碰撞整体探不到，非水特有。`TileMap.Awake` 中 `chunksRoot = new GameObject("TileChunks")`（默认 **Default 层**，SetParent 不继承 layer），而全部 chunk 的 TilemapCollider2D `usedByComposite=true` 后几何被吸收、碰撞由挂在该 chunksRoot 上的 **CompositeCollider2D** 统一提供（TileMap.cs:175）→ 地图 tile 碰撞实际都在 Default 层：mask=`Tile` 的射线**探不到**（山/水/地形全中招），但碰撞接触照常（Default↔角色默认允许）→"物理挡得住、射线探不到"。建造墙（BuildMap 场景物体、层正确）能探到，故此前验证"避让了"实为 BuildTile。**修复**：chunksRoot 创建后显式 `layer = this.gameObject.layer`；已核对 Physics2D 碰撞矩阵 Default 行与 Tile 行均全开、各角色行 bit6 均开，改层零行为变化。**连带的正向修复**：ASeek 的墙壁 CircleCast（`s_wallLayerMask=Tile/BuildTile`）此前对地图 tile 同样从未命中，Worker 对山/水的贴墙滑动自此恢复生效。
- **教训（追加）**：**运行时 `new GameObject` 默认 Default 层，SetParent 不继承 layer**——把碰撞体挂到运行时创建的物体上必须显式设层，否则"物理交互正常、LayerMask 查询全哑"这类症状极难从表现推断；Physics2D collision matrix 要同时核对旧行/新行与相对位的对称性，确认改层零副作用。

## 2026-09-01 TechPanel 从未创建（BuildUI 双调用 + Image DisallowMultipleComponent）

- **现象**：K 键 CultivationPanel 正常，TechPanel 节点在 Hierarchy 中不存在（T/F12 均无反应）。用户先疑热键失效，实为面板未创建。
- **根因**（三段链条，`[PanelDiag]` 诊断日志 + Editor.log 堆栈实锤）：
  1. `AddComponent<TechPanel>()` 触发 Awake → `BuildUI` 第 1 遍（节点 active，正常建完）；
  2. `EnsureRuntimePanel` 创建分支又显式调 `BuildUI()` 第 2 遍（此时节点已被第 1 遍末尾 `SetActive(false)`）——第 2 遍再次 `AddComponent<Image>()` 时，**`Image` 带 `[AddComponentMenu]` 特性 → 隐含 `DisallowMultipleComponent`** → 同物体重复添加**返回 null（不报错）** → `bg.color` NRE（CultivationPanel.cs:115）；
  3. 异常传播出 `EnsureRuntimePanel` → `GlobalPanelInitializer.InitializeAll` 中断 → **下一行 `TechPanel.EnsureRuntimePanel()` 永不执行**。CultivationPanel"看似正常"只因节点与 runtimeInstance 在炸前已就绪（用的是第 1 遍成品，代价是全部子物体重复建了一份叠着）。
- **修复**：两面板（CultivationPanel/TechPanel）对称——`BuildUI` 加 `uiBuilt` 防重入守卫（重复调用直接 return）；`Awake` 改判 `uiBuilt`；`EnsureRuntimePanel` existing 分支补 `if (!runtimeInstance.uiBuilt) BuildUI()`（inactive 场景节点上 AddComponent 不触发 Awake，rootObj 会是 null）。
- **验证**：重新编译后 Play，game.log 应见两面板 EnsureRuntimePanel 各自走完（"进入→parent=UI→创建→BuildUI 开始→完成→EnsureRuntimePanel 完成"四件套），Hierarchy 的 UI 节点下 TechPanel 存在，T 键可开关；Console 无 NRE。
- **教训**：
  - **`[AddComponentMenu]` 特性隐含 DisallowMultipleComponent，重复 AddComponent 返回 null 而非抛错**——"AddComponent 后立刻取返回值解引用"的代码在双调用路径下必炸，且炸点是返回值 null 的 NRE 而非 AddComponent 本身。
  - **纯代码构建 UI 的面板，BuildUI 必须防重入**——Awake（AddComponent 同步触发）与工厂方法显式调用天然构成双调用。
  - **InitializeAll 这类顺序初始化清单对异常零防御**：任何一项抛异常，其后的面板全部静默消失且游戏照常运行（异常只在 Console 红字一次），表现为"某个面板没了"这种点状症状。`[PanelDiag]` 事件点日志（进入/分支/完成）+ game.log 是定位此类"静默中断"的最短路径。
- **追加（同日）——形态已改**：用户随后**直接在场景中拷贝创建了两个面板节点**，两面板已删除全部代码自建逻辑（创建分支/BuildUI/构建辅助/尺寸常量）。现形态：`EnsureRuntimePanel` 仅按名查找绑定（UIRoot 下 `Find` → `GameObject.Find` 兜底，找不到 Error 不自建），`BindSceneUI` 按**固定子物体名**回填字段引用并 `AddListener`（Info/ButtonRow/MeditateBtn/BreakthroughBtn/GongFaRow{i}/GongFaBtn{i}/TechRow{i}/TechBtn{i}）——**场景子物体改名会静默断绑定**（RefreshPanel 判 null 跳过、按钮无响应），改名时须同步 `BindSceneUI`。

## 2026-09-03 Worker×Enemy 索敌双循环复杂度优化（空间网格 + 收口改造）

- **现象**（主动优化，非日志驱动）：夜间防守全员 Worker 每 0.5s 经 `SkillTool.GetEnemiesInRadius` 全量线性扫描敌人列表取最近——O(W×E)（W=100、E=548 最坏 ~11 万次距离计算/秒 + 200 次 List 分配/秒）整夜持续；同族热点：箭塔 O(塔×E)/1.5s、`EnemyManager.AliveEnemyCount` 每次读取触发 `FindObjectsOfType` 全场景扫描（波次生成/清场期每秒一次）、攻击特效清理 O(活跃特效²)/次攻击。
- **根因**：全项目索敌收口在 `SkillTool.GetEnemiesInRadius` 一个函数但走 `List<AEnemy>` 线性扫描，无任何空间分区（`Core/KDTree` 存在但零调用且无删除 API/无对象载荷，不可复用）。
- **修复**（纯性能优化，行为不变）：
  1. 新建 `Domain/Common/SpatialGrid.cs`：纯 C# 泛型均匀网格哈希（cellSize=8=最大查询半径→桶覆盖恒 3×3），**惰性全量重建**（帧号脏检查，同帧多查询只建一次；不选增量维护——Photon 销毁残留 null、死亡时机复杂易出 bug，E≤548 全量重建微秒级）；桶内存 `Entry(pos,item)` 供查询时精确距离过滤；`usedKeys` 只清非空桶（`List.Clear` 保容量防 GC）。
  2. **快照滞后双保险**：网格是重建时刻快照，重建时 `IsAliveEnemy` 过滤 + 查询 API 带 `filter` 参数**查询时刻实时判活**（`Character.ReduceHp` 不判目标已死，ArrowTower 直打会打到已死目标——filter 不可省）。
  3. `EnemyManager` 挂 `EnemyGrid` + `EnsureEnemyGridRebuilt()`；`AliveEnemyCount` 拆快慢路径（`PruneDeadEnemies` O(E) 每次跑 + `MaybeFullSync` FindObjectsOfType 补漏 10s 节流；`SaveData` 保留全量 `SyncAliveEnemies` 存档语义零变化）。`WorkerManager` 同款 `WorkerGrid` 供好感通知。
  4. `SkillTool.GetEnemiesInRadius` 保持签名走网格（**保留每次 new List**——`ExecuteSelfAOE` 遍历中同步 ReduceHp 可能重入查询，池化 List 会被踩）；新增零分配 `GetNearestEnemyInRadius`；`WorkerDefendTask`/`ArrowTowerManager` 换用（消 List 分配+二次遍历）。
  5. 错相三处：`WorkerDefendTask.enemyScanTimer` 负随机起点（入夜同帧全员派任务否则齐扫）、`SeekEnemyMoveState` 感知按 `GetInstanceID()&3` 错相（`frameCount%4` 全局相位→全部 SeekEnemy 第 4n 帧同帧尖峰）、`AttackEffectManager.GetEffect` 倒序原地清理（原 `ToArray()`+`List.Remove` 每次攻击分配+O(n²)；null 条目只移不入队——enqueue(null) 会把 NRE 延迟到 Dequeue）。
- **验证**：`verify_compile.py` 全量 ERRORS:0；`SpatialGridTests`（EditMode，暴力线性扫描对照：随机布置/边界含等/二轮重建/filter/cellSize 校验）待 Unity Test Runner 运行；运行时待夜间防守实测（防守接敌/箭塔射击日志频率与目标行为应与改造前一致）。
- **教训**：
  - **角色列表的"邻近查询"一律走 SpatialGrid，勿手写线性扫描**——收口单函数+空间索引，W×E 乘积降为 W×局部E；新增查询半径显著大于 cellSize(8) 时重评（r=16 时 25 桶仍良性）。
  - **快照型空间索引必须配查询时刻 filter 复查判活**，否则同帧"重建后死亡"的实体会被命中（ReduceHp 不挡已死目标）。
  - **`FindObjectsOfType` 型全场景扫描做防御性兜底时必须降频**（计时脏检查），它在波次生成闸门/清场轮询路径上每秒一次地放大。
  - **全局 `frameCount % N` 分帧是"分帧未错相"**：所有实例同帧执行形成周期尖峰，错相用实例 id（`GetInstanceID()&3`，补码低位对负 id 安全）或负随机起点。

## 2026-09-03 启动崩溃：Build Torch 与消耗品 Torch 撞名（nameToId 全局键冲突）

- **现象**：进 Play 即 `ArgumentException: An item with the same key has already been added. Key: Torch`，栈在 `ItemDataManager.Awake` Backpack 段（nameToId.Add）。
- **根因**：`Name` 是跨**全部** ItemData SO 的全局键（`ItemDataManager.Awake` 三段 Build/Backpack/Resource 全部 `nameToId.Add(itemData.Name, id)` 直 Add 无查重）。光照工具 `BuildLightAssetGenerator` 往 BuildOtherItemData 写入建筑 `Torch`（三同约定全套：Torch.cs/tile/动画），与旧消耗品火把 `Torch`（ConsumableItemData Id=200001，早期手持照明半成品设计 EquipSlot=14，无引用/掉落/获取途径）撞名。Build 段先注册 `"Torch"`，Backpack 段再 Add 即炸。栈落点反推法：炸在 Backpack 段说明 Build 段已成功加过同名键。
- **修复**：消耗品侧改名 `Torch → HandTorch`（保留手持照明设计位，Id=200001 不变；无获取途径故无存档迁移问题）；工具侧 `BuildLightAssetGenerator` 加 `IsNameTakenByOtherSO` 跨 SO 撞名检测（写入前 AssetDatabase 扫全部 `BuildItemDataSO`+`ItemDataSO` 条目 Name，撞名 LogError 跳过）。
- **验证**：`verify_compile.py` ERRORS:0；Play 启动 ItemDataManager.Awake 三段全部通过（可临时在 Backpack 段前后打 Debug）。
- **教训**：
  - **三同约定只覆盖 Build 域内唯一；`Name` 实际是全物品空间的全局键**——新资产接入（含 Editor 工具）必须做跨 SO 撞名检测，不能只查目标 SO 自身去重。
  - **`BuildItemDataSO/ItemDataSO` 磁盘 Id 是占位值**：`OnEnable` 加载时按列表顺序重排（`ItemType*100000+顺序`），`GetExpandedItems` 再兜一次——磁盘上的 `Id: 0` 不是 bug 不用修；代价是「磁盘 Id 与运行时 Id 解耦」，排查 Id 问题以运行时为准。
  - 消耗品侧 `ItemDataSO.OnEnable` 同样按顺序重排，改名不动列表位置则 Id 稳定。
