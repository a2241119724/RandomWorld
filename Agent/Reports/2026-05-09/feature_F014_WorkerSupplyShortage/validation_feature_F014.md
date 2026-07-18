# F014 工人补给缺口提示系统验证记录

## 验证范围

- 运行时业务脚本：`Scripts/2D/Gameplay/WorkerSupplyIssueManager.cs`
- UI 脚本：`Scripts/2D/UI/WorkerSupplyHUD.cs`
- Editor 工具：`Scripts/2D/Editor/WorkerSupplyIssueMenu.cs`
- 公共枚举：`Scripts/2D/Enum/WorkerSupplyIssueType.cs`
- 公共常量：`Scripts/2D/Constant/WorkerSupplyConstant.cs`
- 公共工具：`Scripts/2D/Tool/WorkerSupplyTool.cs`
- 接入点：`Scripts/2D/GlobalInit.cs`

## 静态检查

- 命名空间：新增 C# 文件均使用 `LAB2D`。
- Unity API 使用：运行时代码使用 `UnityEngine`、`UnityEngine.UI`、`Mathf`、`Time`、`Debug`；Editor API 只出现在 `Scripts/2D/Editor/WorkerSupplyIssueMenu.cs`。
- 运行时代码未引入 `using UnityEditor`，不会直接污染构建。
- `GlobalInit.WorkerUpdate()` 只新增 `WorkerSupplyIssueManager.Instance.Tick()`，原有饥饿、疲劳衰减和 `WorkerConditionManager` 刷新顺序未改变。
- `WorkerSupplyIssueManager` 只读访问 `WorkerManager`、`InventoryManager.TypeToResource`、`FurnitureManager.BedToWorker` 和 Worker 状态数据，不扣减食物、不预取资源、不分配床位。

## Tool 验证

- 路径：`Scripts/2D/Tool/WorkerSupplyTool.cs`
- 命名空间：`LAB2D`
- 是否误引 `UnityEditor`：否。
- 公共函数：
  - `NeedsFood()`：基于工人饥饿阈值和 `WorkerConditionConstant.WarningRatio` 判断是否需要食物。
  - `NeedsRest()`：基于工人疲劳阈值和 `WorkerConditionConstant.WarningRatio` 判断是否需要休息。
  - `GetHungryRecoverNeed()`：安全计算吃满缺口，最小为 0。
  - `GetWorkerPrimaryIssue()`：统一选择单个 Worker 的优先补给问题。
  - `GetIssueName()`、`GetIssueRichColor()`、`BuildWorkerIssueLine()`、`BuildTipText()`、`FormatPercent()`：统一 UI/Tip 文案。
- 空引用保护：对 `workerData == null` 返回安全值；格式化函数对比例使用 `Clamp01`。
- 中文注释：已覆盖用途、参数、返回值、使用边界和风险限制。
- 对已有调用方影响：新增文件，无修改旧 Tool 方法签名。

## Enum 验证

- 路径：`Scripts/2D/Enum/WorkerSupplyIssueType.cs`
- 新增枚举值：`None`、`FoodShortage`、`BedShortage`、`HungryWorker`、`TiredWorker`、`CriticalWorker`。
- 语义：补给提示类型，供 HUD、Tip、统计和后续目标提示复用。
- 是否重复或冲突：未发现已有补给缺口枚举；未修改 `WorkerConditionState`。
- 中文注释：包含用途、每个枚举值含义、使用场景和后续扩展限制。

## Constant 验证

- 路径：`Scripts/2D/Constant/WorkerSupplyConstant.cs`
- 新增常量：扫描间隔、Tip 冷却、食物恢复估算、HUD 热键、菜单路径、HUD Canvas/Root/Text 节点名、最大行数和默认文案。
- 复用常量：Editor 菜单复用 `WorkerConditionConstant.GameSceneName` 与 `WorkerConditionConstant.FontResourcePath`，避免重复硬编码 `Game` 场景名和字体路径。
- 是否重复或冲突：F014 节点名均带 `Feature_F014_` 前缀，避免与 F012/F013 HUD 冲突。
- 是否改变已有公共常量：否。
- 中文注释：说明了默认值含义、使用场景和修改风险。

## UI / Scene / Prefab 验证

- `Game.unity` 真实路径：扫描结果为 `Scenes/Game.unity`。
- 是否直接写入 `Game.unity`：否。
- 未直接写入原因：当前环境未运行 Unity Editor，手写 Scene YAML 容易破坏对象引用、Canvas 层级或 MonoScript 绑定。
- 是否创建 `ResourcesLocal` Prefab：否。
- 未创建 Prefab 原因：不直接手写 Prefab YAML，避免 UI 组件、字体、脚本 GUID 或 `.meta` 引用误配。
- 降级方案：新增 Editor 菜单 `工具/工人补给提示/创建补给缺口 HUD 到 Game 场景`，由 Unity Editor 安全创建独立 UI。
- UI 对象命名：
  - Canvas：`Feature_F014_WorkerSupply_Canvas`
  - HUD 根节点：`Feature_F014_WorkerSupplyHUD_Root`
  - 文本节点：`WorkerSupplyText`
- 组件层级：Canvas / CanvasScaler / GraphicRaycaster；HUD 根节点挂 `CanvasGroup`、`WorkerSupplyHUD`；背景挂 `Image`；文本挂 `Text`。
- 脚本挂载：`WorkerSupplyHUD` 挂在 HUD 根节点。
- 回滚方式：通过菜单 `工具/工人补给提示/从当前场景移除补给缺口 HUD` 删除 HUD 根节点，或手动删除上述 F014 节点。

## 数据模型与管理器验证

- `WorkerSupplyIssueManager` 默认启用监控和 Tip。
- `Tick()` 使用 `WorkerSupplyConstant.MonitorRefreshInterval` 节流，避免每帧遍历和刷 Tip。
- `Refresh()` 通过报告签名判断变化，变化后触发 `OnWorkerSupplyReportChanged`。
- `TryShowSupplyTip()` 通过 `WorkerSupplyConstant.TipCooldownSeconds` 防刷屏。
- `WorkerSupplyReport` 只保存统计值和快照，不持有 Worker 实例。
- `WorkerSupplyIssueSnapshot` 只保存显示数据，避免 UI 层引用运行时对象。

## 可执行验证结果

- 已执行：文件存在性检查、关键引用搜索、运行时代码 `using UnityEditor` 搜索。
- 结果：新增运行时代码未引用 `UnityEditor`；F014 `.cs` 与 `.meta` 均已创建；`GlobalInit` 接入点已添加。
- 未执行：Unity Editor 编译、Play Mode、实际菜单创建 HUD。
- 未执行原因：当前命令行环境未发现可用 Unity 可执行文件或项目 `.csproj/.sln`，无法在本回合运行 Unity 编译或 Play Mode。

## 剩余风险

- 需要在 Unity Editor 内验证菜单创建的 HUD 层级、字体加载和屏幕位置。
- 需要在 Play Mode 中验证低食物、缺床、临界状态下 Tip 冷却和 HUD 文案是否符合节奏。
- 食物缺口按当前吃饭任务的每份食物恢复 10 点估算；若后续食物恢复规则变更，应同步调整 `WorkerSupplyConstant.FoodRecoverValuePerItem`。
