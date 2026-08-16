# Technical Specification

## Project Overview

RandomWorld 是一款 2D 像素风生存殖民地建设游戏。玩家在随机生成的地图上建立殖民地，管理工人完成建造、采集、搬运等任务，抵御波次敌人进攻，并通过 LLM 驱动的 NPC 对话系统与角色互动。

## Platform Support

- Windows PC (主要)
- Android (PackageType.Android 已定义)

## Core Features

### 殖民地管理
- **工人系统:** 招募工人执行建造、搬运、采集、种植、吃饭、睡觉、锻炼、穿戴、挖掘 9 种任务
- **任务队列:** 优先级队列(0-3)，KD 树空间查询分配最近任务
- **补给监控:** 饥饿/疲劳状态机 (Healthy → Hungry → Tired → Exhausted → Critical)
- **殖民地指挥中心 (F10):** 实时诊断报告 — 人力分析、任务阻塞原因、补给缺口、拥堵等级
- **建造任务恢复:** 游戏重启/场景加载后自动找回原建造者恢复建造任务
- **建造卡死重试:** `MovementStuckDetector` 每秒位移检测，位移不足先预防性重寻路，硬卡死窗口后最多 3 次重试，避免任务误放弃
- **建造位置预注册:** 建造位置冲突时自我预留跳过，配合建造者名称参数实现任务恢复

### 战斗系统
- **波次敌人:** 普通波 + Boss 波(每 3 波)，难度渐进缩放
- **主动技能 (Q/E/R/F):** 旋风斩、冲刺、力量爆发、治疗之光
- **连击系统:** 多阶连击伤害/经验加成
- **装备稀有度:** Common → Uncommon → Rare → Epic → Legendary → Mythic，属性倍率递增
- **死亡惩罚:** 经验损失 + 复活计时器
- **装备对比弹窗:** 拾取装备时自动弹出对比面板，显示当前装备 vs 新装备属性差异，支持替换/丢弃操作

### Worker 自动生存
- **血瓶自动使用:** HP 低于 30% 时自动消耗背包中的血瓶（3 秒冷却），战斗结束后和低血量检测时触发
- **自主交易:** 背包满时自动出售多余资源，饥饿时自主寻找食物卖家
- **自主决策:** 空闲时 WorkerBrain 根据人格/目标/状态自主选择行动

### 地形系统
- **地形挖掘:** Worker 可挖掘可挖掘地形（如山），复用 GatherTask + GatherMap 认领机制防止多人同时挖掘
- **地形生成:** 程序化地形生成（7 种地形），随机散布 + 最近邻填充
- **地形配置:** TerrainTileConfig ScriptableObject，支持可行走/可建造/可生长树木/可生成资源配置

### LLM NPC 对话
- 本地 llama-server 驱动的 NPC 对话
- 短期记忆 + 长期记忆压缩(每 8 轮)
- RAG 游戏知识检索
- 对话期间工人任务暂停

### 天气系统
- 晴/雨/雪三种天气
- 影响玩家/工人移动速度、任务进度、灵气恢复

### Worker 经济系统
- **货币系统:** `CurrencyAmount` 值对象（Domain 层纯 C#），Worker/Player 双钱包，`CurrencyManager` 管理
- **市场交易:** `MarketService` 价格表，Worker 自主出售资源换金币，Player 购买物品
- **Worker 自主交易 AI:** `WorkerTradeService` — 背包满时自动出售多余资源，饥饿时自主寻找食物卖家购买
- **悬赏任务:** `BountyData` 数据结构 + `PlayerBountyService` — Player 发布悬赏→Worker 领取执行→完成结算/超时退款
- **Worker 人格:** `WorkerPersonality` 4 维值对象（心情/事业心/勤奋/社交），动态影响工作效率、交易决策、社交行为
- **Worker 目标:** `WorkerGoal` 目标驱动（赚钱/建筑/囤食物/做装备），驱动自主行为而非纯被动接任务
- **物品所有权:** `ItemOwnershipService` 追踪物品归属，Worker 采集/制作/购买获得所有权
- **Worker 大脑:** `WorkerSeekState` 空闲时根据人格/目标/状态自主选择行动（采集/出售/买食物/接悬赏）

### 好感度系统
- **定向关系:** Worker↔Worker（双向）+ Worker→Player，数值 [0, 100] 初始 50。运行时键 `Worker.GetInstanceID()`，Player 恒为 `PlayerId=0`（与 `PlayerBountyService.PlayerOwnerId` 一致）；懒初始化零预填
- **分层:** `FavorabilityRuleService`（Domain/Worker 纯 C#，集中阈值/增减量/价格纯函数，可单测）+ `FavorabilityManager`（Gameplay，仿 CurrencyManager：ASingletonSaveData + ITickable）
- **态度标签:** <30 敌对 / 30-49 疏远 / 50-69 友好 / 70-84 亲近 / ≥85 挚友（`GetAttitudeLabel`）
- **行为门控（四项）:**
  - **悬赏接取:** `WorkerBountyTask.IsCanWork` — 对玩家好感 <35（`PlayerBountyRefuseThreshold`）拒接玩家悬赏；对发布 Worker 好感 <40 拒接其悬赏
  - **交易:** 卖者对买者好感 <30（`TradeRefuseThreshold`）拒卖；价格乘数 `1+(50-好感)×0.004` clamp [0.7, 1.3]，高好感折扣/低好感加价（`WorkerTradeService`）
  - **对话:** 好感数值+态度标签写入 LLM 提示词（`GameStateContext.favorabilityText`），LLM 据此调整对话态度
  - **协作互助:** Player 击伤敌方后受击点 4 格半径内 Worker 对玩家好感 +8（30s 冷却/Worker，`Character.ReduceHp` 触发）
- **增减触发:** 攻击（Player 打 Worker 每次命中 -15，致死额外 -10；Worker 互殴受害→肇事 -10）；悬赏完成（Player +8 / 低奖励<40 金币 +4，Worker 间双向 +6，`CurrencyManager.RewardBounty`）；交易（成功 buyer→seller +4 / seller→buyer +2，被拒 buyer→seller -3）；对话（结束 +2，每日上限 10，按游戏日 600s 重置）；接近/共事（3s 节流扫描，半径 4 格，Worker↔Worker 每 tick +0.1、与 Player +0.15，每对累计上限 10）
- **Mood 联动:** 好感变动 |delta| ≥5 时 `WorkerPersonality.AfterFavorabilityChange` 将 Mood ± delta×0.05（clamp ±5）
- **存档:** ASingletonSaveData 二进制，按 Name 稳定匹配（"PLAYER" 哨兵），旧档无此文件零迁移；Worker 死亡 `RemoveDeadWorker` 清理档案与引用
- **UI:** F11 好感度 HUD（`FavorabilityHUD`：对玩家好感+态度标签、Top3 Worker↔Worker 关系、空态提示）；`ItemInfoUI` 点击 Worker 显示"对你好感: X（标签）"
- **初始好感:** 取 `NPCPromptProfile.initialFavorability`（Worker profile），默认 50

### 商店与任务板系统
- **商店 NPC:** `ShopNPC` + `ShopNPCGenerator` — 地图就绪后自动生成商店，支持 Worker/Player 买卖交互
- **任务板:** `TaskBoardManager` — 地图中心固定位置，Worker 存取物品的中转站，内存字典存储
- **TaskBoardHUD:** 任务板 UI 面板，展示存取记录

### 房间系统
- **房间列表面板 (RoomListPanel):** 展示 RoomManager 中所有已建造房间及状态，IsOverlay 模式不暂停游戏
- **房间判定:** 射线检测封闭房间，计算温湿度

#### Worker 建房布局
- **建房流程:** Worker 围墙壁 → 门 → 床 → 4 格仓库，布局由 `WorkerBrain.GenerateRoomLayout` 依据 `HomeRoomWidth/Height/DoorSide/DoorIndex` 动态生成
- **房间尺寸:** 宽高 5~7（5×5 ~ 7×7），门在任意一边（doorSide 0-3）的非角位置
- **家具块 "高2横3":** 仓库 2×2 + 床 1×2，tile 空间 3 列 2 行；5×5 房间内部 3×3 恰好放下
- **床显示方向与足迹:** 床 sprite 永远竖向（上下）显示。物理足迹 = 主格 + 副格 tile-x+1（`BedSecondOffset`，tile 空间横放），转置到屏幕即竖向 1×2；碰撞瓦片注册、寻路与布局打印统一走该足迹（不再用 `ABuildItem.GetOccupiedPositions` 的 tile-y+1 逻辑副格，避免与 sprite 实际阻挡错位）
- **床位置:** `BedOffset=(furnLeft, furnBottom+2)`，床视觉固定在屏幕 `X=furnBottom+2`（右内侧）、`Y=furnLeft..furnLeft+1`，与仓库视觉 `X=furnBottom..furnBottom+1` 不相交
- **门避免堵家具:** `GenerateRandomRoomParams` 逐个候选门位验证"进门第一格"不落在家具占位（床主格/床副格/仓库）上（helper `IsDoorEntryBlockedByFurniture`）；全部门位被堵时换参数重试（最多 8 次），极端几何回退 7×7 可用组合
- **外墙外圈无不可采集碰撞体:** 建房选址 `CanFitRoom` 校验外墙体外一圈（墙外 1 格，含角）不存在"阻挡且不可采集/不可挖掘"的瓦片（水/其他建筑/墙等）；可通行、可采集资源（树/矿）、可挖掘地形（山）允许——Worker 建墙须站外圈，此类瓦片无法清除会导致建造卡死
- **日志级别:** 布局调试用 `LogManager.LogLevelEnum.Info` 才会输出到 Unity Console（Trace/Debug 仅写 game.log）

### 成就系统
- 5 类别(战斗/收集/生存/波次/工人)，最多 20 个成就
- F7 切换成就面板

### 存档系统
- 10 槽位多存档，二进制序列化(.lab)
- 自定义存档名、覆盖确认、清除

## Architecture

```
UI/Panel 层
    |
Gameplay/Manager 层 (运行时状态 + 事件驱动)
    |
Tool 层 (格式化、转换、Unity 适配)
    |
Domain 层 (纯 C# 规则引擎，零 Unity 依赖)
    |- 12 接口, 20 RuleService, EventBus
    |- 值对象: GameGridPosition, GameVector2
    |- 委托注入: ColonyDiagnosticContext
```

### 关键设计决策
- **45° 转置坐标系:** 世界坐标 = tile 坐标转置（`TileMap.MapPosToWorldPos`：world = (tile.y, tile.x)）。所有 tile↔世界转换遵循此规则；房间/家具/门的布局必须在转置后视角验证，否则床/门/墙会错位。注意方向错位：tile 顶墙=屏幕右墙、tile 右墙=屏幕顶墙、tile 左墙=屏幕底墙、tile 底墙=屏幕左墙
- **Domain 纯 C#:** 无 UnityEngine 引用，12 个接口定义在最内层
- **UnityAdapter:** 适配器实现 Domain 接口，通过 ServiceLocator 注册
- **Shared Kernel:** `LAB2D.Enum` 含 16 个跨层枚举(DDD 模式)
- **HUD 热键:** 通过 GlobalInit.Update() 统一分发，避免子对象 inactive 时失效
- **Worker 经济:** Domain 层纯 C# 值对象（CurrencyAmount / WorkerPersonality / WorkerGoal / BountyData），Gameplay 层 Manager 驱动运行时
- **好感度:** 规则纯函数在 Domain（`FavorabilityRuleService`，零 Unity 依赖可单测），运行时状态在 Gameplay（`FavorabilityManager`，仿 CurrencyManager：ASingletonSaveData + ITickable）；定向好感（Worker↔Worker/Worker→Player）懒初始化零预填
- **Worker AI 自主决策:** WorkerBrain 在空闲时根据人格+目标+状态自主选择行动，而非被动等待任务分配
- **日志统一:** `GameLoggerFactory` 统一获取 `IGameLogger`，替换所有硬编码 `Debug.Log`（31 文件迁移）
- **建造韧性:** 位置预注册 + 建造者名称参数 → 任务恢复；每秒位移卡死检测（`MovementStuckDetector`：窗口累计位移 < 期望 40%（`SlidingRatio`）先预防性重寻路，< 15%（`StuckRatio`）判硬卡死窗口，连续硬卡死后最多 3 次重试）→ 避免建造任务误放弃
- **TaskPriority 常量管理:** 任务优先级统一使用常量类（`TaskPriorityConstant`），避免魔法数字
- **UI 预制体加载:** 装备面板改用预制体加载（`ResourceManager.Instantiate`），替代硬编码 UI 层级

### 渲染排序（y-sort）
- **统一层:** 参与排序的 renderer 全部在 `Character` sorting layer（index 3，由 Worker 层改名而来，uniqueID 不变）。武器（`Item`）、寻路线/光束（`Highest`）、Tool 调试（`Enemy`）不参与。
- **排序:** `WorldYSortManager`（`Scripts/2D/Render/`，MonoBehaviour 单例，DontDestroyOnLoad）每帧 `LateUpdate` 按"视觉底端世界 y"降序分配唯一 `sortingOrder`（0..N-1）：底端 y 大（屏幕上方/远处）→ order 小（先绘制，被覆盖）；底端 y 小（下方/近处）→ order 大（后绘制，盖住）。`YSortAlgorithm.AssignOrders` 是纯函数，可单测。
- **底端 y:** `position.y + bottomOffset`；`bottomOffset = sprite.bounds.min.y - position.y`，仅 sprite 引用或 lossyScale 变化时重算（Player 换动画帧等），不做每帧 bounds。
- **注册:** 角色在 `Character.Start` 经 `YSortRegisterProvider` 注册；建筑/树/掉落物视觉由 `TileVisualSpawner`（`Scripts/2D/Map/`）创建时注册。注销不强制，LateUpdate 懒清扫已销毁 renderer（覆盖延迟销毁/Player 永不销毁）。
- **建筑/树视觉拆分:** `TileVisualSpawner` 把 tile 视觉拆到独立 SpriteRenderer（挂 host 下 `VisualSprites` 子节点，Character 层），参与 y 排序；Tilemap 保留碰撞体/寻路/数据/存档/网络，`TilemapRenderer` 在宿主（BuildMap/ResourceMap）Awake 禁用防双重渲染。多格物品副格（纯碰撞无 tile）不建视觉。
- **掉落物/仓库物品混合渲染（ItemMap）:** `ItemMap`（`Scripts/2D/Map/`）**不**禁用 TilemapRenderer——恒底层物品（`ItemData.IsBottomLayer` 默认开启）由 TilemapRenderer 直接渲染在 Map 上，不建 `ItemVisual_*`；非恒底层物品的 tile 保留数据/碰撞但颜色置透明（`SetColor alpha=0`，`useTilemapColor:false` 保证拆分出的 SpriteRenderer 不透明），视觉由独立 SpriteRenderer（Character 层 `ItemVisual_*`）参与 y 排序。写路径统一在 `AddTile`/`DeleteTile`/`SyncDataResp` 经 `ApplyTileVisual` 分流，先 `RemoveTileFlags(LockColor)` 再 `SetColor`（否则透明隐藏失效）。
- **恒底层视觉:** `ItemData.IsBottomLayer`（SO 开关，**默认开启**，`BuildItemData` 继承），默认所有建筑/掉落物恒底层；需要参与 y 排序（如高墙/树遮挡角色、需与角色交叉遮挡的物品）取消勾选。恒底层语义两种呈现：BuildMap/ResourceMap 拆出的 SpriteRenderer 不注册进 `WorldYSortManager`，固定 `sortingOrder = WorldYSortManager.BottomLayerOrder`（-1000）；ItemMap 恒底层物品直接由 TilemapRenderer 渲染（Tile 层，低于 Character 层），根本不出 SpriteRenderer。判定委托：`IsBottomLayerBuilding` 经 `GetBuildTileData`→`GetBuildItemDataByName`；`IsBottomLayerItem` 经 tile 名→`GetByName`（查不到返回 `ItemData.Empty`，`IsBottomLayer` 默认开启，按恒底层处理）。
- **约束:** Character 层内 renderer 必须全部注册（未注册者 order 固定 0 会错乱）；唯一例外是 `IsBottomLayer` 恒底层视觉（固定负 order，恒在最底，不会错乱）。角色 layer 由 prefab 经 AB 包（`StreamingAssets/prefab`）加载——**改 prefab 层后必须重打 AB 包**（`工具/其他/打AB包`），否则加载到旧层导致排序按层隔离而失效。
