# A006 殖民地运营指挥中心任务卡

## 基本信息

- 候选ID：A006
- 原始候选：殖民地运营指挥中心（人力状态 + 任务阻塞诊断 + 补给目标 + 建议 HUD）
- 当前状态：[DONE]
- 本次任务目录：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/`
- 全局候选报告：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：殖民地管理 / Worker 运营反馈 / UI 与表现
- 玩家价值：让玩家快速看懂“为什么任务没人做、哪里缺人、哪里缺补给”，减少误以为系统失效的挫败感。
- 开发价值：把 F013-F016 的 Worker 状态、补给、队列和拥堵提示聚合为可复用的运营诊断层，为后续任务优先级、教程目标、殖民地事件提供统一数据入口。
- 负责 Agent：AINPCAgent + UIAgent + GameplayAgent + ToolAgent
- 需要 Skill：ScriptGenerateSkill + CodeReviewSkill + SceneAnalyzeSkill + EditorToolSkill + TestSkill
- 风险等级：中高

## 修改前状态

- `Scenes/Game.unity` 真实存在，且已包含既有 UI、`Feature_F015_WorkerTaskQueueHUD_Root` 与 `ExperienceHub_Root` 等历史自动化节点。
- 现有 Worker 体验链路：
  - F013 已有 `WorkerConditionManager`、`WorkerConditionTool`、`WorkerConditionState`。
  - F014 已有 `WorkerSupplyIssueManager`、`WorkerSupplyTool`、`WorkerSupplyIssueType`。
  - F015 已有 `WorkerTaskQueueSnapshot`、`WorkerTaskSummaryTool`、任务队列 HUD。
  - F016 已有 `WorkerTaskCongestionAdvisor`、`WorkerTaskCongestionTool`、拥堵 Tip。
- `AWorkerTask.IsCanWork()` 包含任务开关、饥饿、可达性和具体任务条件，但玩家侧缺少统一的阻塞原因展示。
- `WorkerTaskManager.tasks` 是私有队列，当前只暴露数量快照，尚未暴露只读阻塞诊断。
- `ResourcesLocal/Prefabs` 目前没有 UI 子目录；直接手写 Prefab YAML 需要脚本 GUID、Text 组件、RectTransform 和事件引用，自动修改风险高。

## 影响路径

- 新增：
  - `Scripts/2D/Enum/ColonyCommandAlertLevel.cs`
  - `Scripts/2D/Enum/WorkerTaskBlockReason.cs`
  - `Scripts/2D/Constant/ColonyCommandCenterConstant.cs`
  - `Scripts/2D/Tool/ColonyCommandCenterTool.cs`
  - `Scripts/2D/Gameplay/ColonyCommandCenterReport.cs`
  - `Scripts/2D/Gameplay/ColonyCommandCenterManager.cs`
  - `Scripts/2D/UI/ColonyCommandCenterHUD.cs`
  - `Scripts/2D/Editor/ColonyCommandCenterMenu.cs`
- 修改：
  - `Scripts/2D/Character/Worker/WorkerTaskManager.cs`
  - `Scripts/2D/GlobalInit.cs`
  - `Agent/Reports/ambitious_discovery.md`
- 报告：
  - `task_ambitious_A006_Colony_Command_Center.md`
  - `validation_ambitious_A006.md`
  - `rollback_ambitious_A006.md`

## 不应触碰路径

- 不修改 Photon / `NetworkConnect.cs` / RPC / 同步权威逻辑。
- 不修改 AssetBundle / Addressables / `StreamingAssets`。
- 不改 Worker 调度优先级、不自动新增或取消任务、不写入存档结构。
- 不直接手写 `Scenes/Game.unity` 大段 YAML，不覆盖已有 UI 节点。

## 功能边界

本次包含：

1. 只读任务阻塞诊断：分析等待任务的任务开关、饥饿、可达性、食物、床位、种子、仓库容量、绑定 Worker 等原因。
2. 殖民地指挥报告：聚合人力、任务队列、补给缺口、拥堵等级和阻塞原因。
3. 运行时 HUD：进入 Game 后动态创建独立 Canvas / HUD，默认可见，F8 显示隐藏。
4. Tip 提醒：指挥报告进入警告或危急状态且内容变化时，复用 `GlobalInit.ShowTip()`。
5. Editor 工具：查看报告、启停监控、启停 Tip、在 Game 场景创建独立 UI、生成 ResourcesLocal UI Prefab、从当前场景移除 UI。

本次不包含：

1. 不改变 `WorkerTaskManager` 的任务分配算法。
2. 不自动调整 Worker 的任务开关。
3. 不新增工人、不自动建床、不自动生产食物。
4. 不修改存档字段或联网同步。

## 业务规则说明

- 指挥中心只读扫描 Worker 和任务队列。
- 报告等级：
  - `Stable`：暂无明显问题。
  - `Notice`：有轻微等待或关注项。
  - `Warning`：存在补给、拥堵或任务阻塞问题。
  - `Critical`：存在临界工人、严重拥堵或大量阻塞任务。
- 阻塞原因优先级：无 Worker / 全员忙碌 / 任务开关关闭 / 饥饿 / 不可达 / 缺资源 / 仓库满 / 食物不可用 / 缺床 / 缺种子 / 缺农田 / 绑定 Worker 不可用 / 任务专属条件。
- 所有诊断只产生报告和 UI 文案，不改变任务对象、不调用带预留副作用的接口参数。

## 数据流说明

`WorkerTaskManager` → `CreateTaskAssignmentReport()` → `ColonyCommandCenterTool.BuildAssignmentReport()`

`WorkerSupplyIssueManager` / `WorkerTaskCongestionAdvisor` / `WorkerTaskSummaryTool` → `ColonyCommandCenterManager.Refresh()` → `ColonyCommandCenterReport`

`ColonyCommandCenterHUD` 订阅 / 定时读取 `ColonyCommandCenterManager.CurrentReport` 并刷新 UI。

## UI 接入策略

- 不直接手写 `Game.unity`：当前场景 YAML 复杂且已有历史自动生成 UI，直接拼接风险高。
- 不直接手写 Prefab YAML：当前环境未运行 Unity Editor，手写 UI Prefab 容易产生脚本引用、RectTransform、Text 引用错误。
- 采用优先级中的安全降级方案：
  - 运行时代码自动动态创建独立 `Ambitious_A006_ColonyCommandCenter_Canvas` 与 `Ambitious_A006_ColonyCommandCenterHUD_Root`。
  - Editor 菜单可在 Unity 中写入 `Game.unity` 独立 UI 节点。
  - Editor 菜单可生成 `Assets/ResourcesLocal/Prefabs/UI/ColonyCommandCenter/Ambitious_A006_ColonyCommandCenterHUD.prefab`。

## 资源修改清单

- Scene：不直接修改 `Scenes/Game.unity`；仅提供 Editor 菜单安装。
- Prefab：不直接新增 YAML Prefab；提供 Editor 菜单生成。
- ScriptableObject：不修改。
- StreamingAssets：不修改。

## 工具类复用策略

- 已检查 `Scripts/2D/Tool`。
- 复用：
  - `WorkerTaskSummaryTool.GetTaskDisplayName()`、`BuildSnapshot()`。
  - `WorkerTaskCongestionTool.GetLevelName()`、`GetLevelRichColor()`。
  - `WorkerConditionTool.TryGetWorkerData()`、`GetState()`、`FormatPercent()`。
  - `WorkerSupplyTool.GetIssueName()`。
  - `Tool.IsUIInputActive()`。
- 新增：
  - `ColonyCommandCenterTool`：只读诊断任务阻塞、聚合指挥报告、生成 HUD 文案。
- 业务层只保留报告缓存、刷新节流、事件、Tip 请求。

## 枚举复用策略

- 已检查 `Scripts/2D/Enum`。
- 复用：
  - `WorkerConditionState`
  - `WorkerSupplyIssueType`
  - `WorkerTaskCongestionLevel`
  - `AWorkerTask.WorkerTaskTypeEnum`
- 新增：
  - `ColonyCommandAlertLevel`：指挥中心整体警戒等级。
  - `WorkerTaskBlockReason`：任务无法被 Worker 接走的公共原因。

## 常量复用策略

- 已检查 `Scripts/2D/Constant`。
- 复用：
  - `WorkerConditionConstant.FontResourcePath`
  - `WorkerTaskHudConstant` 阈值语义
  - `WorkerTaskCongestionConstant` 拥堵阈值
  - `WorkerSupplyConstant` 补给文案
- 新增：
  - `ColonyCommandCenterConstant`：A006 菜单、节点名、热键、刷新间隔、Tip 冷却、UI 尺寸、Prefab 路径、默认文案。

## 执行步骤

1. 新增公共枚举与常量。
   - 完成标准：枚举值中文注释完整，不改变已有枚举。
2. 新增 `ColonyCommandCenterTool`。
   - 完成标准：任务阻塞诊断无调度副作用，异常降级为报告。
3. 新增报告模型与运行时管理器。
   - 完成标准：可聚合 Worker / 任务 / 补给 / 拥堵数据，提供事件和 Tip。
4. 新增 HUD。
   - 完成标准：运行时自动创建可见 HUD，F8 可显示隐藏，不依赖手写场景引用。
5. 新增 Editor 菜单。
   - 完成标准：可查看报告、启停监控、创建场景 UI、生成 ResourcesLocal Prefab、移除场景 UI。
6. 最小修改接入点。
   - 完成标准：`GlobalInit` 只增加 A006 Tick 与 HUD 自动创建；`WorkerTaskManager` 只增加只读诊断报告方法。
7. 记录验证和回滚。

## 验证步骤

1. 静态检查新增脚本命名空间、中文注释、运行时代码无 `UnityEditor` 引用。
2. 检查新增 `.meta` 是否存在。
3. 检查 `GlobalInit` 接入只调用 A006 管理器和 HUD，不改变原有更新顺序。
4. 检查 `WorkerTaskManager` 新增方法不修改私有队列。
5. 运行 `git diff --check`。
6. 若命令行存在 C# 编译器则尝试编译；否则记录 Unity 编译待人工复验。
7. 记录未直接写入 `Game.unity` 和未直接生成 Prefab 的原因。

## 回滚方案

1. 删除新增脚本及 `.meta`：
   - `ColonyCommandAlertLevel.cs`
   - `WorkerTaskBlockReason.cs`
   - `ColonyCommandCenterConstant.cs`
   - `ColonyCommandCenterTool.cs`
   - `ColonyCommandCenterReport.cs`
   - `ColonyCommandCenterManager.cs`
   - `ColonyCommandCenterHUD.cs`
   - `ColonyCommandCenterMenu.cs`
2. 从 `WorkerTaskManager.cs` 删除 `CreateTaskAssignmentReport()`。
3. 从 `GlobalInit.cs` 删除 `ColonyCommandCenterHUD.EnsureRuntimePanel()` 与 `ColonyCommandCenterManager.Instance.Tick()` 调用。
4. 如果已在 Unity 中执行 Editor 菜单：
   - 使用 `工具/智能体/A006 殖民地指挥中心/从当前场景移除指挥中心 UI`。
   - 删除 `Assets/ResourcesLocal/Prefabs/UI/ColonyCommandCenter/Ambitious_A006_ColonyCommandCenterHUD.prefab` 及 `.meta`。
5. 回滚后进入 Game 场景确认无 `Ambitious_A006_` 前缀对象，脚本编译无错误。

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  - 公共枚举：新增指挥中心警戒等级、Worker 任务阻塞原因。
  - 公共常量：新增 A006 菜单、节点、热键、刷新、Tip、UI 尺寸和 Prefab 路径常量。
  - 公共工具：新增只读任务阻塞诊断和指挥报告聚合工具。
  - 数据层：新增任务分配诊断报告、阻塞明细、指挥中心聚合报告。
  - 业务层：新增 `ColonyCommandCenterManager`，按节流聚合人力、任务、补给、拥堵和阻塞原因，并复用 Tip。
  - 表现层：新增运行时动态 HUD，默认可见，F8 显示隐藏。
  - Editor 层：新增查看报告、启停监控、启停 Tip、安装到 Game 场景、生成 ResourcesLocal Prefab、移除 UI 的菜单。
  - 接入层：`GlobalInit` 自动创建运行时 HUD 并 Tick 指挥报告；`WorkerTaskManager` 提供只读诊断报告入口。
- 新增文件：
  - `Scripts/2D/Enum/ColonyCommandAlertLevel.cs`
  - `Scripts/2D/Enum/WorkerTaskBlockReason.cs`
  - `Scripts/2D/Constant/ColonyCommandCenterConstant.cs`
  - `Scripts/2D/Tool/ColonyCommandCenterTool.cs`
  - `Scripts/2D/Gameplay/ColonyCommandCenterReport.cs`
  - `Scripts/2D/Gameplay/ColonyCommandCenterManager.cs`
  - `Scripts/2D/UI/ColonyCommandCenterHUD.cs`
  - `Scripts/2D/Editor/ColonyCommandCenterMenu.cs`
  - 以上文件对应 `.meta`
  - `validation_ambitious_A006.md`
  - `rollback_ambitious_A006.md`
- 修改文件：
  - `Scripts/2D/Character/Worker/WorkerTaskManager.cs`
  - `Scripts/2D/GlobalInit.cs`
  - `Agent/Reports/ambitious_discovery.md`
- 新增游戏体验能力：
  - 玩家可在一块 HUD 中看到殖民地整体状态、人力、任务等待/阻塞、补给缺口和建议。
  - 任务等待不再只是数量，系统会给出“开关关闭、饥饿、不可达、缺材料、仓库满、缺食物、缺床、缺种子、缺农田、绑定工人不可用”等阻塞原因。
  - 警告/危急状态会按冷却复用现有 Tip UI 提醒玩家。
- 玩家侧效果：
  - 更容易理解任务卡住的原因。
  - 更容易形成下一步目标，如补食物、建床、补材料、清仓库、留可达工作位或打开任务开关。
- UI 生成位置：
  - 未直接写入 `Game.unity`，避免手写复杂场景 YAML。
  - 未直接生成 `ResourcesLocal` Prefab，避免无 Unity Editor 环境下手写 Prefab YAML。
  - 已实现运行时动态 UI：`Ambitious_A006_ColonyCommandCenter_Canvas`、`Ambitious_A006_ColonyCommandCenterHUD_Root`。
  - 已实现 Editor 菜单写入 `Game.unity` 与生成 ResourcesLocal Prefab。
- 开发侧接入方式：
  - 进入 Game 后由 `GlobalInit.Start()` 自动创建 HUD。
  - 使用 F8 显示隐藏。
  - 菜单路径：`工具/智能体/A006 殖民地指挥中心/`。
- 验证结果：
  - 新增 `.meta` 存在。
  - 新增运行时代码未使用 `using UnityEditor`。
  - `Scenes/Game.unity` 未直接写入 A006 节点。
  - `git diff --check` 通过，仅 LF/CRLF 警告。
  - 新增文件未发现行尾空白。
  - 命令行无 .NET SDK，Unity 编译和 Play Mode 待人工复验。
- 验证记录路径：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/validation_ambitious_A006.md`
- 回滚方案验证：已静态验证；路径见 `Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/rollback_ambitious_A006.md`
- `Scripts/2D/Tool`：
  - 已复用 `WorkerTaskSummaryTool`、`WorkerTaskCongestionTool`、`WorkerConditionTool`、`WorkerSupplyTool`、`Tool.IsUIInputActive()`。
  - 新增 `ColonyCommandCenterTool.cs`，供 Manager、Report、Editor 菜单和 HUD 复用。
  - 不涉及 `UnityEditor`。
- `Scripts/2D/Enum`：
  - 已复用 `WorkerConditionState`、`WorkerSupplyIssueType`、`WorkerTaskCongestionLevel` 和 `AWorkerTask.WorkerTaskTypeEnum`。
  - 新增 `ColonyCommandAlertLevel.cs`、`WorkerTaskBlockReason.cs`。
- `Scripts/2D/Constant`：
  - 已复用 `WorkerConditionConstant`、`WorkerTaskHudConstant`、`WorkerTaskCongestionConstant`、`WorkerSupplyConstant`。
  - 新增 `ColonyCommandCenterConstant.cs`。
- 剩余风险：
  - 真实 UI 排版、字号、右上角占位和 F8 热键需 Unity Play Mode 复验。
  - 任务私有字段若未来重命名，阻塞诊断会降级为通用原因，但不会影响任务运行。
  - 自动动态 HUD 默认可见，若觉得遮挡，可按 F8 隐藏或通过 Editor 菜单/代码关闭。
