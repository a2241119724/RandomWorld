# F015 任务队列 HUD 摘要任务卡

## 基本信息

- 任务 ID：feature_F015_WorkerTaskQueueHUD
- 候选 ID：F015
- 原始候选：任务队列 HUD 摘要
- 创建时间：2026-05-09
- 当前状态：Done
- 本次任务目录：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/`
- 全局候选报告路径：`Agent/Reports/feature_discovery.md`
- 任务分类：UI 数据表现 / 工人任务反馈
- 游戏业务类型：UI 数据表现
- 玩家价值：玩家可以快速看到当前任务总量、等待中任务和进行中任务，理解殖民地当前工作压力。
- 开发价值：为 Worker 任务系统提供结构化只读快照，后续可复用于运营面板、任务提醒、效率分析和新手引导。
- 负责 Agent：UIAgent + AINPCAgent
- 需要的 Skill：ScriptGenerateSkill + SceneAnalyzeSkill + TestSkill
- 风险等级：低

## 影响路径

- `Scripts/2D/Character/Worker/WorkerTaskManager.cs`
- `Scripts/2D/Gameplay/WorkerTaskQueueSnapshot.cs`
- `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
- `Scripts/2D/Constant/WorkerTaskHudConstant.cs`
- `Scripts/2D/UI/WorkerTaskQueueHUD.cs`
- `Scripts/2D/Editor/WorkerTaskQueueHUDMenu.cs`
- `Agent/Reports/feature_discovery.md`

## 不应触碰路径

- `Scenes/Game.unity`：不手写 YAML，不直接改已有 UI 层级。
- `Resources/SO`、`Resources/Tilemap`、`Resources/Images`：只读分析，不改配置或美术资源。
- `ResourcesLocal/Prefabs`：本次不创建 Prefab，避免手写 Unity Prefab YAML。
- `StreamingAssets`、`AddressableAssetsData`、`Build`：不修改 AssetBundle 或运行时资源结构。
- `Scripts/2D/Data`、`ArchiveManager.cs`、`NetworkConnect.cs`：不修改存档结构和 Photon 同步逻辑。

## 功能边界

- 只读取 `WorkerTaskManager` 中已有任务队列，不新增任务、不取消任务、不调整任务优先级。
- HUD 展示任务总量、等待中数量、进行中数量，以及各任务类型的总量/等待/进行中统计。
- 不改变 `GetTaskInfo()` 的既有 DebugUI 文本行为，新增结构化快照和 HUD 文案接口供 UI 使用。
- UI 通过 Editor 菜单安全生成到 `Game.unity`，不直接手写场景文件。

## 业务规则说明

- `WorkerTaskManager` 内部字典的 `bool` 值表示任务是否已被 Worker 接取。
- 等待中数量 = 总任务数量 - 进行中数量。
- 展示任务类型优先复用 `AWorkerTask.WorkerTaskTypeEnum`，不重复定义任务类型枚举。
- HUD 默认使用 F7 显示/隐藏，且会调用 `Tool.IsUIInputActive()` 避免输入穿透。

## 数据流说明

1. `WorkerTaskManager.CreateTaskQueueSnapshot()` 读取内部任务队列。
2. `WorkerTaskSummaryTool.BuildSnapshot()` 统计总量、等待中、进行中和任务类型分布。
3. `WorkerTaskSummaryTool.BuildHudText()` 生成 RichText 文案。
4. `WorkerTaskQueueHUD` 周期刷新文本。
5. `WorkerTaskQueueHUDMenu` 在 Unity Editor 中创建或移除独立 HUD 节点。

## UI 接入策略

- 已搜索真实 `Game.unity` 路径：`Scenes/Game.unity`。
- 不直接修改 `Game.unity` YAML，原因是场景文件已有大量 UI 层级和序列化引用，手写 YAML 容易破坏引用。
- 不创建 `ResourcesLocal` Prefab，原因是当前 `ResourcesLocal/Prefabs` 主要是 Character/Item 规范，没有现成 UI Prefab 目录；手写 Prefab YAML 对脚本 GUID 和 UI 组件引用风险较高。
- 降级方案：新增 Editor 菜单 `工具/任务队列 HUD/创建任务队列 HUD 到 Game 场景`，由 Unity Editor API 复用或创建 Canvas，并生成独立 HUD。

## Scene / Prefab / ResourcesLocal 生成策略

- Game.unity：通过 Editor 菜单生成，不在当前任务中直接写入。
- ResourcesLocal Prefab：不生成。
- Editor 工具：新增 `WorkerTaskQueueHUDMenu`，支持创建和移除 HUD。
- 运行时代码动态创建 UI：不作为默认路径，避免进入 Play Mode 后自动改变 UI 层级。

## 工具类复用策略

- 已检查 `Scripts/2D/Tool/Tool.cs`：
  - 复用 `Tool.GetComponentInChildren<T>()` 绑定 HUD 文本。
  - 复用 `Tool.IsUIInputActive()` 保护 HUD 热键。
- 已检查 `WorkerConditionTool.cs`、`WorkerSupplyTool.cs`：它们面向状态和补给，不承载任务队列统计。
- 本次计划新增公共工具类：`Scripts/2D/Tool/WorkerTaskSummaryTool.cs`，负责任务队列快照统计、任务类型中文名、压力等级和 HUD 文案。

## 枚举复用策略

- 已检查 `Scripts/2D/Enum`：没有独立任务队列枚举。
- 本次计划复用 `AWorkerTask.WorkerTaskTypeEnum`，因为它是现有任务系统的实际任务类型来源。
- 本次不新增公共枚举，原因是新增任务展示状态枚举会与简单的等待/进行中布尔状态重复，当前统计边界不需要额外枚举。

## 常量复用策略

- 已检查 `Scripts/2D/Constant/WorkerConditionConstant.cs`：复用 `GameSceneName` 和 `FontResourcePath`。
- 已检查 `PrefabConstant.cs`、`ResourceConstant.cs`、`TagConstant.cs`、`LayerConstant.cs`：不适合承载 F015 HUD 节点名和菜单路径。
- 本次计划新增公共常量类：`Scripts/2D/Constant/WorkerTaskHudConstant.cs`，维护刷新间隔、热键、菜单路径、节点名、默认文案和压力阈值。

## 公共代码分层

- 公共函数沉淀到 `Scripts/2D/Tool`：任务统计、文案格式化、压力计算。
- 公共常量沉淀到 `Scripts/2D/Constant`：HUD 节点、菜单、刷新和文案。
- 数据模型放入 `Scripts/2D/Gameplay`：任务队列快照和任务类型统计。
- 业务脚本保留在 `WorkerTaskManager`：只提供只读快照入口。
- UI Binder 放入 `Scripts/2D/UI`：负责刷新文本和热键显示隐藏。
- Editor 专用逻辑放入 `Scripts/2D/Editor`：负责安全生成场景 HUD。

## 执行步骤

1. 新增任务队列快照数据模型。
2. 新增任务队列 HUD 常量。
3. 新增任务队列统计与 HUD 文案工具。
4. 扩展 `WorkerTaskManager` 的只读快照接口。
5. 新增 `WorkerTaskQueueHUD` 和 Editor 菜单生成工具。
6. 静态检查运行时代码是否误引 `UnityEditor`。
7. 写入验证记录并回写候选状态。

## 验证步骤

1. 静态检查新增运行时代码命名空间和 Unity API 使用。
2. 检查 Editor 脚本是否位于 `Scripts/2D/Editor` 且运行时代码不引用 `UnityEditor`。
3. 检查 Tool / Constant / UI 脚本中文注释、空引用保护和热键输入保护。
4. 检查 `Game.unity` 未被直接修改，Editor 菜单路径和回滚菜单完整。
5. 若可用 Unity Editor，执行菜单创建 HUD 并进入 Play Mode 验证任务统计。

## 回滚方案

- 删除新增脚本及其 `.meta`：
  - `Scripts/2D/Gameplay/WorkerTaskQueueSnapshot.cs`
  - `Scripts/2D/Constant/WorkerTaskHudConstant.cs`
  - `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
  - `Scripts/2D/UI/WorkerTaskQueueHUD.cs`
  - `Scripts/2D/Editor/WorkerTaskQueueHUDMenu.cs`
- 回退 `Scripts/2D/Character/Worker/WorkerTaskManager.cs` 中新增的只读接口。
- 若已通过菜单生成 HUD，在 Unity 中执行 `工具/任务队列 HUD/从当前场景移除任务队列 HUD`。
- 将 `Agent/Reports/feature_discovery.md` 中 F015 状态恢复为 `[TODO]`。

## 结果区

- 最终状态：`[DONE]`
- 已完成内容：新增任务队列只读快照、HUD 常量、统计工具、HUD 绑定脚本和 Editor 菜单；扩展 `WorkerTaskManager` 只读查询接口。
- 修改文件：
  - `Scripts/2D/Character/Worker/WorkerTaskManager.cs`
  - `Scripts/2D/Gameplay/WorkerTaskQueueSnapshot.cs`
  - `Scripts/2D/Constant/WorkerTaskHudConstant.cs`
  - `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
  - `Scripts/2D/UI/WorkerTaskQueueHUD.cs`
  - `Scripts/2D/Editor/WorkerTaskQueueHUDMenu.cs`
  - `Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/task_feature_F015_WorkerTaskQueueHUD.md`
  - `Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/validation_feature_F015.md`
- 新增游戏业务能力：玩家可通过任务队列 HUD 查看当前工人任务总量、等待中数量、进行中数量，以及建造、搬运、采集、吃饭、睡觉等任务类型分布。
- 玩家侧效果：能快速判断当前殖民地任务压力，知道工人是在忙、在排队还是暂无任务。
- UI 生成位置：未直接写入 `Game.unity`；未创建 `ResourcesLocal` Prefab；提供 Editor 菜单生成独立 HUD 到 Game 场景。
- 开发侧接入方式：Unity 菜单 `工具/任务队列 HUD/创建任务队列 HUD 到 Game 场景`；运行时 HUD 挂载 `WorkerTaskQueueHUD` 后自动读取 `WorkerTaskManager.GetTaskQueueSummaryText()`。
- 验证结果：静态检查通过；运行时代码未引用 Editor API；新增脚本与任务卡均有 `.meta`；未直接修改 Scene/Prefab。
- 验证记录路径：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/validation_feature_F015.md`
- 未完成项：未在 Unity Editor 内实际执行菜单生成 HUD；未运行 Play Mode。
- 剩余风险：HUD 屏幕位置、字体加载、Canvas 层级和任务刷新节奏需在 Unity Editor / Play Mode 内验证。
- 是否复用 `Scripts/2D/Tool`：是，复用 `Tool.GetComponentInChildren<T>()`、`Tool.IsUIInputActive()`。
- 是否新增或修改 Tool：新增 `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`，用于任务队列统计、任务类型中文名、压力标签和 HUD 文案。
- 是否复用 `Scripts/2D/Enum`：复用 `AWorkerTask.WorkerTaskTypeEnum`。
- 是否新增或修改 Enum：否；避免重复定义任务类型或迁移既有内嵌枚举带来的大范围风险。
- 是否复用 `Scripts/2D/Constant`：复用 `WorkerConditionConstant.GameSceneName`、`WorkerConditionConstant.FontResourcePath`。
- 是否新增或修改 Constant：新增 `Scripts/2D/Constant/WorkerTaskHudConstant.cs`，维护刷新间隔、热键、菜单路径、节点名、默认文案、压力阈值和 HUD 尺寸。
- 后续建议：可将 F013/F014/F015 合并为一个 Worker 运营面板，统一展示工人状态、补给缺口和任务队列压力。
- 是否存在未抽取的重复逻辑、重复枚举、重复常量或魔法值：未发现需要本次抽取的重复公共逻辑；任务类型继续复用现有 `AWorkerTask.WorkerTaskTypeEnum`，暂不迁移。
