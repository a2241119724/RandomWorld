# Progress

## Project Status

v0.1.3 — 玩法深度打磨阶段。核心循环（白天经营+夜晚防守）已跑通，Worker AI 已升级为半自主经济模拟，商店/悬赏/货币系统已上线。当前聚焦 UI 优化、多人联机同步完善、内容扩展。

## Recent Changes

- 2026-08 — `feat(worker)`: Worker 生存与成长数值——压力/士气（困苦度动态）/熟练度（每任务+0.8，进度倍率联动）/贪婪懒惰（入 ML 特征 19/20 维），删除已取代的 WorkerMoraleController。spec 见 Worker 生存与成长数值节
- 2026-08 — `feat(worker)`: Worker 心智层完整实现（五阶段）——自主意志（拒绝/拖延/强制双门 + 怨恨感恩累积 + 强制命令伤感情）、事件记忆 + 信念演化、随机人生事件 + 执念、性格随经历演化（滞回/限流/饱和防横跳）、自发社会关系（友谊/敌意/爱慕/记仇 + 互助/回避/送礼/嫉妒四行为）。数据内嵌 `WorkerData.Mind` 随档一次写入（旧档 Ensure 兜底），7 套领域单测。spec 见心智层节
- 2026-08 — `feat(render)`: 物品地面帧动画——`ItemData.IsAnimation`（默认关）+ `SpriteFrameAnimator` 按英文名 `{Name}_0/1/2...` 序列图 6fps 循环播放（非恒底层独立视觉，预制体视觉自管）；`TileVisualSpawner` 加 `animationResolver`，每次 `CreateOrUpdate` 调和动画组件状态（挂载/切换/同格换物品重载）。ItemMap/ResourceMap/BuildMap 三处 Map 均接入。spec 见渲染排序节
- 2026-08 — `feat(render)`: 遮挡淡化 + 分层模式枚举——`OcclusionFader` 淡化挡住本地玩家的环境视觉（alpha→0.3）；`ItemData.LayerMode` 三态（Bottom 恒底层 / Alpha 参与排序且淡化 / Normal 参与排序不淡化）统一控制 y 排序与透明度。spec 见渲染排序节
- 2026-08 — `feat(render)`: Worker 屋顶接入 y 排序——`RoofManager` 创建的屋顶 SpriteRenderer 注册进 `WorldYSortManager`（层仍 `Highest` 盖住屋内一切），多个房间屋顶之间按"视觉底端世界 y"分配唯一 `sortingOrder`（近处盖远处），不再固定 order=0
- 2026-08 — `refactor(worker)`: 疲劳值语义反转——`CurTired` 从剩余体力式（初始=MaxTired、递减、低于阈值判定疲劳）反转为累积疲劳式（初始 0、工作/空闲累积、睡眠降低、疲劳 > `MaxTired-阈值` 判定需休息），20+ 文件阈值判断对称反转，AI 决策/睡眠/悬赏/状态机同步。旧档 `CurTired` 语义错位，睡一觉自愈（未做迁移）
- 2026-08 — `feat(worker home)`: Worker 建房屋顶——房间注册完成时 `RoofManager` 生成覆盖整个房间矩形的 Roof 屋顶（挂 All/Building、Highest 层、无碰撞），本地玩家进出房间隐藏/显示屋顶；拆除房间边界建筑（墙/门）时移除房间与屋顶
- 2026-08 — `feat(render)`: 掉落物/仓库物品（ItemMap）接入 y 排序——`ItemData` 上移 `LayerMode`（默认 Bottom，`BuildItemData` 继承），`ItemMap` 混合渲染：恒底层物品由 TilemapRenderer 直接渲染在 Map 上（不建视觉）、角色下；非恒底层物品 tile 置透明、单独创建 `ItemVisual_*` SpriteRenderer 参与动态排序。spec 见渲染排序节
- 2026-08 — `feat(favorability)`: 好感度系统——Worker↔Worker/Worker→Player 定向好感（0~100 初始 50），四项行为门控（玩家悬赏<35/Worker悬赏<40 拒接、交易<30 拒卖+价格乘数、对话态度入 LLM 提示词、协作互助），增减触发含攻击/致死/互殴/悬赏/交易/对话(日上限10)/接近共事(3s 扫描)，Mood 联动，F11 好感度 HUD。Domain 纯 C# 规则 + ASingletonSaveData 存档，单测 `FavorabilityRuleServiceTests`
- 2026-08 — `feat(render)`: 按图标底端世界 y 全局渲染排序（`WorldYSortManager` + `YSortAlgorithm` + `TileVisualSpawner`），角色/建筑/树统一 `Character` 层交叉排序；修复 Player 恒最顶（过期 AB 包内旧 sorting layer）。存档见 `docs/ai-context/bug-fixes.md`
- 2026-08 — `fix(worker home)`: 修复床副格碰撞与 sprite 足迹错位（寻路穿过床）+ 门位堵家具封死房间（含门规避重写与兜底回归修复）；Sliding 熔断复用 `HandleMovementStuck` 保留建造重试。存档见 `docs/ai-context/bug-fixes.md`
- 2026-08 — `fix(worker gather)`: 修复 Gather "没有邻居位置"死循环刷屏（25k 次）。根因：失败未调用 `ASeek.RecordFail`，决策 `ScanForResources` 的 `IsRecentFail` 过滤失效，GiveUpTask 释放认领后无限重选同一目标。修复：失败块补 `RecordFail` + Gather 邻居 2→4 正交方向。存档见 `docs/ai-context/bug-fixes.md`
- 2026-08 — `refactor(stuck-detection)`: 用每秒位移检测（MovementStuckDetector）替换失效的 OnCollisionStay2D 卡死检测（IntervalTicks 20ms 与 Fixed Timestep 0.02 相撞导致永不触发）；Worker+SeekEnemy 改由 ASeek.LastStuckResult 驱动，位移不足重寻路、真卡死重试/放弃
- 2026-08 — `refactor(worker home)`: 修复 Worker 建房布局中床与墙/门重叠问题，统一"高2横3"家具块，门 index 避开床所在行列，并修复 interiorW 硬编码 7
- 2026-08 — `refactor(worker build)`: 优化工人建造位置预留和任务恢复逻辑，新增建造者名称参数和自我预留跳过
- 2026-08 — `refactor(worker home build)`: 优化工人建家流程与碰撞逻辑，新增位置预注册机制
- 2026-08 — `refactor(worker)`: 新增建造任务恢复（重启时找回原建造者）和卡死重试逻辑（最多 3 次）
- 2026-08 — `feat(ui)`: 添加房间列表面板（RoomListPanel），展示所有已建造房间及状态
- 2026-08 — `feat(terrain)`: 添加地形挖掘功能，Worker 可挖掘可挖掘地形（如山），复用 GatherMap 认领机制
- 2026-08 — `feat(worker)`: 添加 Worker 自动使用血瓶逻辑（HP<30% 触发，3 秒冷却，战斗结束后/低血量检测时触发）
- 2026-08 — `refactor(worker-task)`: 统一重命名拾取相关任务类型与代码
- 2026-07 — `refactor(Map)`: 重构 GenAvailablePosMap，拆分螺旋搜索和全图随机逻辑
- 2026-07 — `refactor(worker AI)`: 重构工人 AI 与建筑系统，优化自主建造与资源管理
- 2026-07 — `refactor(itemmap/workertask)`: 优化物品拾取与工人任务流程
- 2026-07 — `feat`: 新增建造、种植相关 AI 与任务系统
- 2026-07 — `refactor`: 调整 UI 布局，为结构体添加可序列化特性
- 2026-07 — `feat(ui)`: 新增装备对比弹窗（EquipmentComparePopup），拾取装备时自动弹出对比面板
- 2026-07 — `feat(ui)`: 新增装备面板预制体，改用预制体加载 UI
- 2026-07 — `feat`: 添加商店系统，调整 Worker 士气和货币管理
- 2026-07 — `feat(ui)`: 新增 ScrollText 预制体，新增 TaskBoardHUD 面板
- 2026-07 — `feat(taskboard)`: 添加 TaskBoard 任务板系统和 Bounty 悬赏物品
- 2026-07 — `feat(gameplay)`: 添加 Worker 交易、Player 悬赏、商店 NPC 和 Worker 目标系统
- 2026-07 — `feat(worker)`: 添加 Worker 大脑 AI、物品所有权、个性系统和市场交易服务
- 2026-07 — `refactor(drop)`: 统一掉落物放置逻辑，支持可堆叠物品就近合并
- 2026-07 — `feat(worker)`: 添加 Worker 悬赏任务系统和货币管理
- 2026-07 — `feat`: 添加建造道具快速创建工具，支持 8 方向变体自动生成
- 2026-07 — `refactor`: 重构建造物品体系，移除旧的分层抽象类
- 2026-07 — `refactor`: 替换硬编码 Debug 日志为统一的 GameLoggerFactory 获取方式（31 文件）
- 2026-07 — `refactor(worker task)`: 重构任务优先级系统，统一使用常量管理

## Completed

| Phase | Description | Date |
|-------|-------------|------|
| 1 | 核心循环：白天采集/建造 + 夜晚波次防守 + 波后奖励 | 2026-06 |
| 2 | Worker 基础任务系统（8 种任务 + 优先级队列 + KD 树分配） | 2026-06 |
| 3 | Domain 层纯 C# 规则引擎（74 文件，零 Unity 依赖） | 2026-06 |
| 4 | 库存系统重构（Domain 层 InventoryService + 双索引 Grid） | 2026-06 |
| 5 | ServiceLocator DI + UnityAdapter 适配器层注册接入 | 2026-07 |
| 6 | Worker 经济系统（货币/市场/交易/悬赏/人格/目标） | 2026-07 |
| 7 | TaskBoard 任务板 + ShopNPC 商店系统 | 2026-07 |
| 8 | 建造系统重构（8 方向变体 + 快速创建工具） | 2026-07 |
| 9 | 装备对比弹窗 + 装备面板预制体 | 2026-07 |
| 10 | GameLoggerFactory 统一日志工厂（替换硬编码 Debug） | 2026-07 |
| 11 | 地形挖掘功能（Worker 挖掘可挖掘地形） | 2026-08 |
| 12 | Worker 血瓶自动使用（HP<30% 触发，3 秒冷却） | 2026-08 |
| 13 | 房间列表面板（RoomListPanel） | 2026-08 |
| 14 | Worker 建造任务恢复 + 卡死重试逻辑 | 2026-08 |
| 15 | 渲染排序：按视觉底端 y 全局排序（角色/建筑/树） | 2026-08 |
| 16 | 好感度系统（定向关系 + 四项行为门控 + Mood 联动 + HUD） | 2026-08 |
| 17 | Worker 心智层（自主意志/记忆信念/人生事件/性格演化/社会关系） | 2026-08 |
| 18 | Worker 生存与成长数值（压力/士气/熟练度/贪婪懒惰 + 进度倍率联动） | 2026-08 |

## Current Work

- **Worker 建造系统优化** — IN PROGRESS
  - [x] 建造位置预注册机制
  - [x] 建造任务恢复（重启时找回原建造者）
  - [x] 建造卡死重试逻辑（最多 3 次）
  - [x] 位置预留冲突时的自我跳过逻辑
  - [ ] 建家碰撞逻辑进一步优化
    - [x] 卡死检测改为每秒位移（MovementStuckDetector），替换失效的碰撞回调检测（Worker + SeekEnemy；ACommonEnemy 保留原机制）
- **地图系统** — IN PROGRESS
  - [x] GenAvailablePosMap 拆分螺旋搜索和全图随机
  - [x] 地形挖掘功能
  - [ ] 地图生成算法进一步优化
- **Worker AI** — IN PROGRESS
  - [x] 优化自主建造与资源管理
  - [x] 优化物品拾取与任务流程
  - [x] 自动血瓶使用
  - [ ] Worker 大脑决策规则完善
- **UI 优化** — IN PROGRESS
  - [x] 场景 UI 颜色对齐 PixelUITheme
  - [x] ScrollText 预制体、TaskBoardHUD
  - [x] 房间列表面板
  - [x] 装备对比弹窗
  - [ ] 字体批量替换（Arial → ark-pixel）
  - [ ] Scale 规范化（0.5 → 1.0 补偿）

## Known Issues / Blockers

- Game.unity 场景文件 63K+ 行，大量运行时脚本依赖，无法安全手改 YAML，需 Editor 工具批处理
- Photon 多人联机同步覆盖不完整，部分新系统（TaskBoard/ShopNPC）尚未接入网络同步
- Domain 层仍有对 Enum/Serializable 的外部命名空间依赖（架构审查 #2 号问题）

## Technical Debt

| Item | Impact | Next Step |
|------|--------|-----------|
| Singleton 泛滥（30+ 个） | 紧耦合，测试困难 | 渐进迁移到 ServiceLocator 接口注入 |
| GlobalInit 上帝对象 | 初始化逻辑集中，难以拆分 | 提取独立 Bootstrap/Composition Root |
| AgentFull Python 框架在 Assets 内 | 非 Unity 运行时却占用项目空间 | 移出 Unity 项目到 Tools/ |
| 并发寻路 WalkabilityCache 非原子写入 | 潜在数据竞争 | 使用不可变快照替代原地刷新 |

## Next Steps

1. Worker 建家碰撞逻辑进一步优化
2. Worker 大脑决策规则完善（更多目标类型、更智能的资源优先级）
3. Editor 工具：字体批量替换 + Scale 补偿
4. TaskBoard/ShopNPC 网络同步接入
5. 多人联机完整测试
6. 房间判定系统实现

## Future Phases

- 多人合作模式完善（分工防守、资源共用）
- 敌人类型扩展（冲锋/拆墙/偷窃/远程/感染/Boss 变体）
- 天气事件升级（酸雨/暴雪/大雾/流星夜）
- 建筑联动加成系统
- 世界核心主线目标

## Deferred Work

- PvP 竞技模式（优先级低，先完善合作 PvE）
- 遗迹探索系统（依赖房间判定）
- 手机端适配（Android 已定义 PackageType，未实际适配）
