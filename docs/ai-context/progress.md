# Progress

## Project Status

v0.1.3 — 玩法深度打磨阶段。核心循环（白天经营+夜晚防守）已跑通，Worker AI 已升级为半自主经济模拟，商店/悬赏/货币系统已上线。当前聚焦 UI 优化、多人联机同步完善、内容扩展。

## Recent Changes

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

## Current Work

- **Worker 建造系统优化** — IN PROGRESS
  - [x] 建造位置预注册机制
  - [x] 建造任务恢复（重启时找回原建造者）
  - [x] 建造卡死重试逻辑（最多 3 次）
  - [x] 位置预留冲突时的自我跳过逻辑
  - [ ] 建家碰撞逻辑进一步优化
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
