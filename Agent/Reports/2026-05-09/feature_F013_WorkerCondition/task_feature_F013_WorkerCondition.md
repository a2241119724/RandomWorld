# F013 工人饥饿疲劳状态效果与视觉反馈任务卡

## 基本信息

- 候选 ID：F013
- 原始候选：工人饥饿疲劳状态效果与视觉反馈
- 当前状态：[DONE]
- 本次任务目录：`Agent/Reports/2026-05-09/feature_F013_WorkerCondition/`
- 全局候选报告路径：`Agent/Reports/feature_discovery.md`
- 任务分类：游戏业务功能开发
- 游戏业务类型：成长与奖励 / 工人状态反馈 / 生存管理反馈
- 玩家价值：让工人饥饿与疲劳从单纯数值变成可感知、可管理的状态，增强殖民地运营压力。
- 开发价值：统一沉淀 Worker 状态判断、倍率、提示和 HUD 数据源，便于后续床位、食物、任务效率等系统继续接入。
- 负责 Agent：AINPCAgent + GameplayAgent + UIAgent
- 需要的 Skill：ScriptGenerateSkill、CodeReviewSkill、SceneAnalyzeSkill、TestSkill
- 风险等级：中

## 影响路径

- `Scripts/2D/Enum/WorkerConditionState.cs`
- `Scripts/2D/Constant/WorkerConditionConstant.cs`
- `Scripts/2D/Tool/WorkerConditionTool.cs`
- `Scripts/2D/Gameplay/WorkerConditionManager.cs`
- `Scripts/2D/UI/WorkerConditionHUD.cs`
- `Scripts/2D/Editor/WorkerConditionMenu.cs`
- `Scripts/2D/Core/Seek/ASeek.cs`
- `Scripts/2D/Character/Worker/Task/AWorkerTask.cs`
- `Scripts/2D/GlobalInit.cs`
- `Agent/Reports/feature_discovery.md`

## 不应触碰路径

- `Scripts/2D/Data` 存档结构
- `Scripts/2D/NetworkConnect.cs` 与 Photon 同步逻辑
- `StreamingAssets`
- `AddressableAssetsData`
- 已有 Scene / Prefab / ScriptableObject 资源文件
- `Resources/SO` 与运行时资源配置

## 功能边界

- 本次只实现运行时 Worker 状态效果、提示数据源、HUD 绑定脚本和 Editor 创建菜单。
- 不新增存档字段，不写入 Photon 同步，不改 AssetBundle 或 StreamingAssets。
- 不手写 `Game.unity` YAML，不直接创建带脚本引用的 Prefab，避免破坏 Unity 资源引用。
- 不强制 Worker 停工，只用非致命倍率降低移动与普通任务进度，避免工人无法走向食物或床。

## 业务规则说明

- Worker 饥饿值和疲劳值低于最大值 35% 时进入警戒状态。
- 任一数值低于 5% 或归零时进入“濒临停工”。
- 饥饿、疲劳、复合低状态会降低工人移动速度和普通任务进度。
- 吃饭与睡觉任务不受状态惩罚，保证恢复链路可执行。
- 状态变化会触发 Tip，提示有冷却，避免刷屏。
- HUD 可显示每个 Worker 的饥饿百分比、疲劳百分比、移动倍率和工作倍率。

## 数据流说明

1. `GlobalInit.WorkerUpdate()` 按常量扣减 `CurHungry` 与 `CurTired`。
2. `WorkerConditionManager.UpdateWorkerCondition(worker)` 生成 `WorkerConditionSnapshot`。
3. `WorkerConditionTool` 计算状态、倍率、百分比和文案。
4. `ASeek.MoveByPath()` 在天气移动倍率后继续套用 Worker 状态移动倍率。
5. `AWorkerTask.Execute()` 在天气任务倍率后继续套用 Worker 状态任务倍率，并防止工作疲劳扣减跌为负数。
6. `WorkerConditionHUD` 只读 `WorkerConditionManager.BuildSummaryText()` 刷新文本。

## UI 接入策略

- 已搜索真实 `Game.unity` 路径：`Scenes/Game.unity`。
- `Game.unity` 存在大量已有 UI 与历史自动生成节点，手写 YAML 修改风险高。
- 本次不直接写入 `Game.unity`，改为提供 Editor 菜单：`工具/工人状态/创建工人状态 HUD 到 Game 场景`。
- 菜单会定位真实 `Game.unity`，优先复用已有 Canvas；若无 Canvas，则创建独立 Canvas。
- HUD 根节点命名：`Feature_F013_WorkerConditionHUD_Root`。
- HUD 文本节点命名：`WorkerConditionText`。
- 未创建 `ResourcesLocal` Prefab，原因是手写带脚本 GUID 的 Prefab 风险高；后续可在 Unity Editor 内通过菜单生成后另存 Prefab。

## Scene / Prefab / ResourcesLocal 生成策略

- `Game.unity`：未直接修改，提供 Editor 菜单安全生成。
- `ResourcesLocal` Prefab：未创建。
- Editor 工具：已创建 `WorkerConditionMenu`，可在 Unity 中生成或移除 HUD。
- 运行时代码动态创建 UI：未在 Play Mode 自动创建，避免未经确认污染场景。

## 执行步骤

1. 读取 Agent 文档、候选报告、历史任务卡和验证记录。
2. 扫描 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`、Worker、任务、寻路、UI、`Game.unity` 和 `ResourcesLocal`。
3. 自动选择 F013，跳过已完成候选和重复候选 F007。
4. 新增 Worker 状态公共枚举、常量、工具类。
5. 新增 Worker 状态运行时管理器、HUD 绑定脚本、Editor 创建菜单。
6. 接入 Worker 移动速度和任务进度倍率。
7. 将原有饥饿/疲劳自然衰减魔法值抽到常量。
8. 完成静态验证、任务记录和全局候选状态回写。

## 验证步骤

- 静态检查新增运行时代码不引用 `UnityEditor`。
- 检查 `ASeek`、`AWorkerTask`、`GlobalInit` 接入点范围。
- 检查新增 `.meta` 文件存在。
- 检查 `WorkerConditionState` 未重复已有枚举。
- 检查 `WorkerConditionConstant` 未修改已有公共常量。
- 检查 Editor 菜单路径和 UI 节点名来自常量。
- Unity 编译与 Play Mode 因当前环境未启动 Unity Editor，待人工在 Unity 中执行。

## 回滚方案

- 删除新增文件：
  - `Scripts/2D/Enum/WorkerConditionState.cs`
  - `Scripts/2D/Constant/WorkerConditionConstant.cs`
  - `Scripts/2D/Tool/WorkerConditionTool.cs`
  - `Scripts/2D/Gameplay/WorkerConditionManager.cs`
  - `Scripts/2D/UI/WorkerConditionHUD.cs`
  - `Scripts/2D/Editor/WorkerConditionMenu.cs`
- 回退 `ASeek.cs` 中 WorkerConditionManager 移动倍率调用。
- 回退 `AWorkerTask.cs` 中 WorkerConditionManager 任务倍率调用。
- 回退 `GlobalInit.cs` 中常量化衰减与状态刷新调用。
- 若已通过菜单生成 HUD，可在 Unity 中执行 `工具/工人状态/从当前场景移除工人状态 HUD`。

## Tool 复用策略

- 已检查工具类：`Tool.cs`、`WeatherGameplayTool.cs`、`ResourceTool.cs`、`DateTool.cs`、`DataTool.cs`、`VectorTool.cs`、`SyncDataTool.cs`。
- 本次复用：
  - `Tool.GetComponentInChildren<T>()`：HUD 自动查找 Text。
  - `Tool.IsUIInputActive()`：HUD 热键避免 UI 输入穿透。
  - `WeatherGameplayTool.ApplyMultiplier()`：复用安全倍率计算。
- 本次新增：
  - `Scripts/2D/Tool/WorkerConditionTool.cs`：状态判断、倍率计算、百分比和文案格式化。

## Enum 复用策略

- 已检查枚举：`Scripts/2D/Enum/PackageTypeEnum.cs`。
- 未复用原因：现有枚举只表达背包包裹类型，与 Worker 生存状态语义不一致。
- 本次新增：
  - `Scripts/2D/Enum/WorkerConditionState.cs`：Healthy、Hungry、Tired、Exhausted、Critical。

## Constant 复用策略

- 已检查常量类：`PrefabConstant.cs`、`ResourceConstant.cs`、`TagConstant.cs`、`LayerConstant.cs`、`Lock.cs`。
- 本次复用：未复用已有类承载 F013 常量，避免把状态倍率、HUD 节点和菜单路径塞入无关常量类。
- 本次新增：
  - `Scripts/2D/Constant/WorkerConditionConstant.cs`：衰减速度、阈值、倍率、Tip 冷却、HUD 节点名、菜单路径、字体路径。

## 逻辑分层

- 公共枚举：`WorkerConditionState`。
- 公共常量：`WorkerConditionConstant`。
- 公共函数：`WorkerConditionTool`。
- 业务状态管理：`WorkerConditionManager`。
- UI Binder：`WorkerConditionHUD`。
- Editor 专用逻辑：`WorkerConditionMenu`。
- 接入点：`ASeek`、`AWorkerTask`、`GlobalInit`。

## 结果区

- 最终状态：[DONE]
- 已完成内容：Worker 状态判断、移动倍率、任务倍率、Tip、HUD 数据源、Editor HUD 创建菜单、候选状态回写。
- 修改文件：见“影响路径”。
- 新增游戏业务能力：工人饥饿/疲劳会影响移动与普通工作效率，并可通过 Tip/HUD 反馈给玩家。
- 玩家侧效果：低状态工人更慢，玩家能看到并理解工人需要食物或休息。
- UI 生成位置：未直接写入 `Game.unity`，未创建 `ResourcesLocal` Prefab；已提供 Editor 菜单生成独立 HUD。
- 开发侧接入方式：Unity 菜单 `工具/工人状态/创建工人状态 HUD 到 Game 场景`。
- 验证结果：静态验证通过；Unity 编译和 Play Mode 待人工验证。
- 验证记录路径：`Agent/Reports/2026-05-09/feature_F013_WorkerCondition/validation_feature_F013.md`
- 未完成项：未在当前 shell 环境运行 Unity 编译与 Play Mode。
- 剩余风险：倍率手感需要 Play Mode 微调；HUD 菜单需要在 Unity Editor 内实际执行验证。
- 是否复用 `Scripts/2D/Tool`：是。
- 是否新增或修改 Tool：新增 `WorkerConditionTool.cs`。
- 是否复用 `Scripts/2D/Enum`：已检查，未复用旧枚举。
- 是否新增或修改 Enum：新增 `WorkerConditionState.cs`。
- 是否复用 `Scripts/2D/Constant`：已检查，新增专用常量类。
- 是否新增或修改 Constant：新增 `WorkerConditionConstant.cs`。
- 是否存在未抽取的重复逻辑：未发现必须本次抽取的重复 Worker 状态逻辑。
- 后续建议：将床位不足、食物库存不足与 WorkerConditionManager 事件联动，形成“缺口提示”。
