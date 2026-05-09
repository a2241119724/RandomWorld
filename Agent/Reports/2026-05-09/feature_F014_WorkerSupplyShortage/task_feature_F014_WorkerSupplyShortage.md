# F014 工人补给缺口提示系统任务卡

## 基本信息

- 候选 ID：F014
- 原始候选：工人补给缺口提示系统
- 当前状态：Done
- 本次任务目录：`Agent/Reports/2026-05-09/feature_F014_WorkerSupplyShortage/`
- 全局候选报告：`Agent/Reports/feature_discovery.md`
- 任务分类：游戏业务功能
- 游戏业务类型：交互提示 / Worker 生存管理反馈
- 负责 Agent：AINPCAgent + UIAgent + GameplayAgent
- 需要的 Skill：ScriptGenerateSkill、CodeReviewSkill、TestSkill
- 风险等级：Low

## 玩家价值与开发价值

- 玩家价值：当工人饥饿、疲劳、缺食物或缺床位时，玩家能通过 Tip 和 HUD 快速理解问题来源。
- 开发价值：复用 F013 工人状态能力，补齐“有状态惩罚但缺补给解释”的反馈链路，为后续自动补给、任务目标提示和殖民地运营面板打基础。

## 影响路径

- `Scripts/2D/Enum/WorkerSupplyIssueType.cs`
- `Scripts/2D/Constant/WorkerSupplyConstant.cs`
- `Scripts/2D/Tool/WorkerSupplyTool.cs`
- `Scripts/2D/Gameplay/WorkerSupplyIssueManager.cs`
- `Scripts/2D/UI/WorkerSupplyHUD.cs`
- `Scripts/2D/Editor/WorkerSupplyIssueMenu.cs`
- `Scripts/2D/GlobalInit.cs`
- `Agent/Reports/feature_discovery.md`

## 不应触碰路径

- 不修改存档结构：`Scripts/2D/Data`、`Scripts/2D/Manager/ArchiveManager.cs`
- 不修改 Photon 同步：`Scripts/2D/NetworkConnect.cs`
- 不修改 AssetBundle 或 StreamingAssets 结构
- 不删除、重命名或覆盖 Scene、Prefab、SO、材质、图片、动画、音效或配置资源

## 功能边界

- 只读统计 Worker 当前饥饿、疲劳、床位绑定和仓库食物数量。
- 只输出 Tip、HUD 文案和 Editor 查询入口，不自动扣减食物、不预占资源、不分配床位。
- 不改变 Worker 接任务规则，不改变背包、仓库、地图、存档和联网同步逻辑。

## 业务规则说明

- 当 Worker 饥饿值低于阈值或警戒比例时，计入需要食物的工人。
- 当 Worker 疲劳值低于阈值或警戒比例时，计入需要休息的工人。
- 若需要食物的总恢复缺口大于仓库现有食物可恢复值，提示食物缺口。
- 若需要休息的 Worker 没有绑定床位，提示床位缺口。
- 若 Worker 已进入 Critical 状态，优先提示临界停工风险。

## 数据流说明

1. `GlobalInit.WorkerUpdate()` 已逐帧刷新 Worker 饥饿和疲劳，本任务在末尾调用 `WorkerSupplyIssueManager.Tick()`。
2. `WorkerSupplyIssueManager` 节流读取 `WorkerManager`、`InventoryManager`、`FurnitureManager` 和 `WorkerConditionManager` 的只读数据。
3. `WorkerSupplyTool` 负责缺口类型、百分比、行文案和 Tip 文案格式化。
4. `WorkerSupplyHUD` 订阅报告变化并按固定间隔刷新文本。
5. `WorkerSupplyIssueMenu` 提供安全生成 HUD 到 `Game.unity` 的菜单入口。

## UI 接入策略

- 不直接手写 `Scenes/Game.unity` YAML，避免破坏已有对象引用。
- 不直接生成 `ResourcesLocal` Prefab YAML，避免 MonoScript 和 UI 组件引用误配。
- 采用 Editor 菜单工具生成独立 Canvas/HUD 到真实 `Game.unity`。
- 菜单路径：`工具/工人补给提示/创建补给缺口 HUD 到 Game 场景`。
- HUD 根节点：`Feature_F014_WorkerSupplyHUD_Root`。
- 后续若需要常驻 UI，可在 Unity Editor 内点击菜单生成。

## Tool / Enum / Constant 策略

- 已检查 Tool：`Tool.cs`、`WorkerConditionTool.cs`、`WeatherGameplayTool.cs`、`ResourceTool.cs`。
- 本次复用 Tool：`Tool.GetComponentInChildren<T>()`、`Tool.IsUIInputActive()`、`WorkerConditionTool.GetSafeRatio()`、`WorkerConditionTool.GetState()`。
- 本次新增 Tool：`WorkerSupplyTool.cs`，负责只读计算和文案格式化，不访问 Scene、Prefab、存档、Photon 或 AssetBundle。
- 已检查 Enum：`WorkerConditionState.cs`、`WavePhaseType.cs`、`WaveRewardType.cs`、`PackageTypeEnum.cs`。
- 本次新增 Enum：`WorkerSupplyIssueType.cs`，表达补给缺口类型。
- 已检查 Constant：`WorkerConditionConstant.cs`、`PrefabConstant.cs`、`ResourceConstant.cs`、`TagConstant.cs`、`LayerConstant.cs`。
- 本次复用 Constant：复用 `WorkerConditionConstant.GameSceneName` 与 `WorkerConditionConstant.FontResourcePath`，避免重复硬编码场景名和字体路径。
- 本次新增 Constant：`WorkerSupplyConstant.cs`，维护 HUD 节点名、菜单路径、刷新间隔、Tip 冷却、文案和食物恢复值。

## 执行步骤

1. 新增公共枚举、常量和工具。
2. 新增补给缺口运行时管理器与报告数据结构。
3. 新增 HUD 绑定脚本与 Editor 菜单生成工具。
4. 在 `GlobalInit` 接入节流刷新。
5. 做静态检查，写验证记录。
6. 更新任务卡结果区和全局候选报告。

## 验证步骤

1. 静态检查新增 C# 文件是否命名空间一致且无 `UnityEditor` 污染运行时代码。
2. 检查 Tool / Enum / Constant 是否有中文注释，且业务脚本引用公共层而非散落魔法值。
3. 检查 Editor 菜单是否仅位于 `Scripts/2D/Editor`。
4. 检查 `GlobalInit` 接入点不改变原有饥饿、疲劳扣减逻辑。
5. Unity 编译与 Play Mode 需要在 Unity Editor 内人工验证。

## 回滚方案

- 删除本次新增的 F014 文件及 `.meta`。
- 从 `GlobalInit.WorkerUpdate()` 移除 `WorkerSupplyIssueManager.Instance.Tick()`。
- 删除通过 Editor 菜单生成的 `Feature_F014_WorkerSupplyHUD_Root` 或 `Feature_F014_WorkerSupply_Canvas`。
- 将 `Agent/Reports/feature_discovery.md` 中 F014 状态恢复为 `[TODO]`。

## 结果区

- 最终状态：[DONE]
- 已完成内容：新增工人补给缺口枚举、常量、工具、运行时报告管理器、HUD 绑定脚本和 Editor 菜单；在 `GlobalInit.WorkerUpdate()` 接入节流刷新。
- 修改文件：
  - `Scripts/2D/Enum/WorkerSupplyIssueType.cs`
  - `Scripts/2D/Constant/WorkerSupplyConstant.cs`
  - `Scripts/2D/Tool/WorkerSupplyTool.cs`
  - `Scripts/2D/Gameplay/WorkerSupplyIssueManager.cs`
  - `Scripts/2D/UI/WorkerSupplyHUD.cs`
  - `Scripts/2D/Editor/WorkerSupplyIssueMenu.cs`
  - `Scripts/2D/GlobalInit.cs`
  - `Agent/Reports/2026-05-09/feature_F014_WorkerSupplyShortage/validation_feature_F014.md`
  - `Agent/Reports/feature_discovery.md`
- 新增游戏业务能力：自动只读统计工人饥饿、疲劳、仓库食物、床位绑定和临界停工风险，生成 Tip 和 HUD 摘要。
- 玩家侧效果：玩家可看到食物不足、缺床位、饥饿工人、疲劳工人和临界停工等补给提示。
- UI 生成位置：未直接写入 `Game.unity`；未创建 `ResourcesLocal` Prefab；提供 Editor 菜单生成独立 HUD 到 Game 场景。
- 开发侧接入方式：运行时自动通过 `GlobalInit.WorkerUpdate()` 调用 `WorkerSupplyIssueManager.Instance.Tick()`；需要常驻 HUD 时点击 `工具/工人补给提示/创建补给缺口 HUD 到 Game 场景`。
- 验证结果：静态验证通过；新增运行时代码未引用 `UnityEditor`；Unity 编译与 Play Mode 待人工验证。
- 验证记录路径：`Agent/Reports/2026-05-09/feature_F014_WorkerSupplyShortage/validation_feature_F014.md`
- 未完成项：未在 Unity Editor 内实际生成 HUD；未运行 Play Mode。
- 剩余风险：HUD 位置、字体和 Tip 节奏需要在 Unity Editor 中验证；食物恢复估算依赖当前吃饭任务每份食物恢复 10 点的规则。
- Tool 复用：复用 `Tool.GetComponentInChildren<T>()`、`Tool.IsUIInputActive()`、`WorkerConditionTool.GetSafeRatio()`、`WorkerConditionTool.GetState()`。
- Tool 新增或修改：新增 `Scripts/2D/Tool/WorkerSupplyTool.cs`，用于补给缺口判断和文案格式化；未修改旧 Tool 签名。
- Enum 复用：复用 `WorkerConditionState` 作为工人状态来源。
- Enum 新增或修改：新增 `Scripts/2D/Enum/WorkerSupplyIssueType.cs`；未修改旧枚举。
- Constant 复用：复用 `WorkerConditionConstant.GameSceneName`、`WorkerConditionConstant.FontResourcePath`。
- Constant 新增或修改：新增 `Scripts/2D/Constant/WorkerSupplyConstant.cs`；未修改旧公共常量。
- 后续建议：可继续开发 F015 任务队列 HUD 摘要，与 F014 形成 Worker 运营面板组合。
- 未抽取项：没有发现需要进一步抽取的重复枚举；F014 业务报告结构只服务本功能，保留在业务脚本中。
