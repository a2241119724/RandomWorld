# F013 验证记录

## 验证结论

- 最终状态：[DONE]
- 静态验证：通过。
- Unity 编译：当前 shell 环境未启动 Unity Editor，未执行。
- Play Mode：当前 shell 环境未启动 Unity Editor，未执行。
- 风险判断：运行时代码低侵入，未触碰存档、Photon、AssetBundle、StreamingAssets 或现有资源引用。

## 运行时业务脚本验证

- `Scripts/2D/Gameplay/WorkerConditionManager.cs`
  - 命名空间：`LAB2D`
  - 类名：`WorkerConditionManager`、`WorkerConditionSnapshot`
  - Unity API 使用：`Time.time`、`Debug.Log`、`Debug.LogWarning`
  - 基础逻辑：缓存 Worker 状态，派发状态变化事件，提供移动/工作倍率，Tip 展示带冷却。
  - 空引用保护：通过 `WorkerConditionTool.TryGetWorkerData()` 保护 Worker 与 WorkerData。
  - 边界：不写存档，不同步网络，不操作资源。

- `Scripts/2D/UI/WorkerConditionHUD.cs`
  - 命名空间：`LAB2D`
  - 类名：`WorkerConditionHUD`
  - Unity API 使用：`MonoBehaviour`、`CanvasGroup`、`Text`、`Input.GetKeyDown`
  - 基础逻辑：定时读取 `WorkerConditionManager.BuildSummaryText()`，使用 F5 显示/隐藏。
  - 空引用保护：Text 缺失时跳过刷新，热键判断异常时降级为允许处理。

- `Scripts/2D/Core/Seek/ASeek.cs`
  - 修改点：Worker 移动先套用天气倍率，再套用 `WorkerConditionManager.GetAdjustedWorkerMoveSpeed()`。
  - 风险边界：只影响 `AWorker`，玩家与敌人不受 F013 状态倍率影响。

- `Scripts/2D/Character/Worker/Task/AWorkerTask.cs`
  - 修改点：任务进度倍率由天气倍率乘以 Worker 状态倍率。
  - 修改点：普通工作疲劳扣减增加 `Mathf.Max`，避免疲劳值跌成负数。
  - 风险边界：`WorkerConditionTool` 对 Eat/Sleep 返回 1，恢复任务不会被惩罚拖慢。

- `Scripts/2D/GlobalInit.cs`
  - 修改点：饥饿/疲劳自然衰减改用 `WorkerConditionConstant`，并刷新 Worker 状态。
  - 风险边界：保持原衰减默认值 0.1 和 0.01；新增 `Mathf.Max` 防止数值继续跌为负数。

## UI / Scene / Prefab 验证

- `Game.unity` 路径：`Scenes/Game.unity`
- 是否直接写入 `Game.unity`：否。
- 不直接写入原因：场景 YAML 体量大且已有复杂 UI 层级，手写节点和脚本引用容易破坏现有引用。
- 是否创建 `ResourcesLocal` Prefab：否。
- 不创建 Prefab 原因：当前环境无法通过 Unity Editor 可靠生成带脚本引用的 Prefab，手写 Prefab GUID 风险高。
- 降级方案：Editor 菜单生成独立 HUD。
- 菜单路径：`工具/工人状态/创建工人状态 HUD 到 Game 场景`
- HUD 根节点：`Feature_F013_WorkerConditionHUD_Root`
- HUD 文本节点：`WorkerConditionText`
- 回滚方式：执行 `工具/工人状态/从当前场景移除工人状态 HUD`，或删除新增脚本并回退三个接入点。

## Editor 工具验证

- `Scripts/2D/Editor/WorkerConditionMenu.cs`
  - 命名空间：`LAB2D`
  - 菜单路径：
    - `工具/工人状态/查看状态汇总`
    - `工具/工人状态/启用状态效果`
    - `工具/工人状态/禁用状态效果`
    - `工具/工人状态/启用状态提示`
    - `工具/工人状态/禁用状态提示`
    - `工具/工人状态/创建工人状态 HUD 到 Game 场景`
    - `工具/工人状态/从当前场景移除工人状态 HUD`
  - 输出路径：真实 `Game.unity` 场景内的独立 HUD 节点。
  - 基本生成逻辑：查找 `Game.unity`，复用 Canvas 或创建独立 Canvas，创建背景与 Text，并绑定 `WorkerConditionHUD`。
  - Editor 隔离：`UnityEditor` 仅出现在 `Scripts/2D/Editor` 下。

## Tool 验证

- 新增路径：`Scripts/2D/Tool/WorkerConditionTool.cs`
- 命名空间：`LAB2D`
- 是否误引 `UnityEditor`：否。
- 是否影响运行时构建：未发现 Editor API 引用。
- 是否破坏已有调用方：新增文件，无旧调用方签名变更。
- 公共函数：
  - `TryGetWorkerData`
  - `GetSafeRatio`
  - `GetState`
  - `GetStateName`
  - `GetStateRichColor`
  - `GetMoveSpeedMultiplier`
  - `GetTaskProgressMultiplier`
  - `BuildConditionLine`
  - `BuildTipText`
  - `FormatPercent`
- 空引用保护：`TryGetWorkerData` 和 `GetState` 已处理空 WorkerData。
- 中文注释：已覆盖类、方法、参数、返回值、使用边界。
- 复用情况：复用 `WeatherGameplayTool.ApplyMultiplier()`、`Tool.GetComponentInChildren<T>()`、`Tool.IsUIInputActive()`。

## Enum 验证

- 新增路径：`Scripts/2D/Enum/WorkerConditionState.cs`
- 命名：`WorkerConditionState`
- 语义：Worker 饥饿与疲劳组合状态。
- 是否重复或冲突：已检查 `PackageTypeEnum`，语义不重合。
- 是否修改旧枚举：否。
- 中文注释：已说明用途、每个枚举值含义和后续扩展约束。
- 业务脚本引用：`WorkerConditionTool`、`WorkerConditionManager` 引用该枚举，未在业务脚本内重复定义。

## Constant 验证

- 新增路径：`Scripts/2D/Constant/WorkerConditionConstant.cs`
- 类命名：`WorkerConditionConstant`
- 分组：衰减速度、阈值、倍率、Tip 冷却、HUD 节点、菜单路径、字体路径。
- 是否重复或冲突：已检查 `PrefabConstant`、`ResourceConstant`、`TagConstant`、`LayerConstant`，未发现语义一致常量。
- 是否修改旧常量：否。
- 中文注释：已说明用途、默认值含义和修改风险。
- 业务脚本引用：
  - `GlobalInit` 引用衰减常量。
  - `WorkerConditionTool` 引用阈值与倍率。
  - `WorkerConditionHUD` 引用 HUD 默认值。
  - `WorkerConditionMenu` 引用菜单路径、节点名和字体路径。

## 静态命令记录

- `rg -n "WorkerCondition|CurHungry|CurTired|GetAdjustedWorkerMoveSpeed|GetWorkerTaskProgressMultiplier" Scripts\2D`
  - 结果：确认新增类、接入点和 Worker 原有数据字段。
- `rg -n "using UnityEditor|UnityEditor\." Scripts\2D\Enum Scripts\2D\Constant Scripts\2D\Tool Scripts\2D\Gameplay Scripts\2D\UI`
  - 结果：F013 新增运行时代码未引用 `UnityEditor` API；项目既有 `PauseMenuPanel.cs` 存在旧 UnityEditor 引用，不属于本次改动。
- `git diff -- Scripts\2D\Core\Seek\ASeek.cs Scripts\2D\Character\Worker\Task\AWorkerTask.cs Scripts\2D\GlobalInit.cs`
  - 结果：确认已有文件只做最小接入。

## 未使用或未抽取说明

- 未直接复用旧枚举：旧枚举只覆盖背包类型。
- 未复用旧常量类承载新常量：避免把 F013 状态倍率与 HUD 节点塞入 Prefab/Resource/Tag/Layer 常量。
- 未创建 ResourcesLocal Prefab：需要 Unity Editor 可靠写入脚本引用，当前环境不适合手写。
- 未手写 Game.unity：避免破坏复杂 UI 层级和脚本绑定。
- 未抽取进一步重复逻辑：当前仅发现原有饥饿/疲劳衰减魔法值，已抽到 `WorkerConditionConstant`。

## 剩余风险

- 倍率手感需要 Play Mode 验证后微调。
- HUD 菜单需要在 Unity Editor 中实际执行，确认字体、Canvas 层级和屏幕位置。
- 项目既有 `PauseMenuPanel.cs` 在运行时 UI 路径引用 `UnityEditor`，不是本次新增问题，但未来打包前建议单独处理。
