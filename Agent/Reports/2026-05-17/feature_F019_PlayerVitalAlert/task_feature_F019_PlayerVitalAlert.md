# F019 玩家生命危险提示任务卡

## 基本信息

- 任务 ID：feature_F019_PlayerVitalAlert
- 候选 ID：F019
- 原始候选：玩家生命危险提示
- 当前状态：Done
- 本次任务目录：`Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/`
- 全局候选报告路径：`Agent/Reports/feature_discovery.md`
- 任务分类：游戏业务功能 / 玩家状态提示 / UI 反馈
- 游戏业务类型：玩家体验 / 状态变化提示
- 玩家价值：玩家在生命偏低、生命濒危或复活等待时能收到明确提示，降低突然死亡和误判战斗状态的挫败感。
- 开发价值：沉淀玩家生命状态枚举、阈值常量、只读计算工具和事件报告，后续可接入 HUD、教程目标或成就条件。
- 负责 Agent：GameplayAgent + UIAgent
- 需要的 Skill：ScriptGenerateSkill + CodeReviewSkill + TestSkill
- 风险等级：低

## 影响路径

- `Scripts/2D/Enum/PlayerVitalAlertLevel.cs`
- `Scripts/2D/Constant/PlayerVitalAlertConstant.cs`
- `Scripts/2D/Tool/PlayerVitalAlertTool.cs`
- `Scripts/2D/Gameplay/PlayerVitalAlertManager.cs`
- `Scripts/2D/Editor/PlayerVitalAlertMenu.cs`
- `Scripts/2D/GlobalInit.cs`
- `Agent/Reports/feature_discovery.md`
- `Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/`

## 不应触碰路径

- 不修改存档结构、`ArchiveManager`、`ASaveData`。
- 不修改 Photon、RPC、网络同步逻辑。
- 不修改 AssetBundle、Addressables、StreamingAssets 运行时资源结构。
- 不删除、重命名或覆盖已有 Scene、Prefab、ScriptableObject、材质、图片、动画、音效或配置资源。
- 不改变 `Player.ReduceHp()`、`DeathPenaltyManager` 的实际伤害、死亡、复活或恢复数值。

## 功能边界

- 只读取 `PlayerManager.Instance.Mine` 的当前生命值、最大生命值和 `DeathPenaltyManager.Instance.IsRespawning`。
- 只计算生命提示等级、摘要文案和 Tip 文案。
- 只通过现有 `GlobalInit.ShowTip()` 展示提示；不可用时降级为日志。
- 不新增、删除或调整玩家属性，不写存档，不同步网络状态。
- Editor 菜单只用于 Play Mode 查看报告、开关监控和手动触发一次 Tip。

## 业务规则说明

- 血量比例高于 `PlayerVitalAlertConstant.WarningRatio` 时为安全。
- 血量比例低于或等于 `WarningRatio` 时进入“生命偏低”提示。
- 血量比例低于或等于 `CriticalRatio` 时进入“生命濒危”提示。
- `DeathPenaltyManager.IsRespawning` 为 true 时进入“复活等待”提示。
- 从危险状态恢复到 `RecoveryRatio` 以上时允许显示一次恢复提示。
- 提示受 `TipCooldownSeconds` 节流；从偏低升级为濒危或复活等待时允许优先提示。

## 数据流说明

1. `GlobalInit.Update()` 每帧调用 `PlayerVitalAlertManager.Instance.Tick()`。
2. 管理器内部按 `PlayerVitalAlertConstant.MonitorRefreshInterval` 节流。
3. `PlayerVitalAlertManager` 读取本地玩家数据并调用 `PlayerVitalAlertTool.GetLevel()`。
4. `PlayerVitalAlertReport` 保存当前等级、血量比例、复活状态和建议文案。
5. 报告变化时派发 `OnPlayerVitalAlertChanged`。
6. 满足提示等级和冷却限制时，请求 `GlobalInit.ShowTip()` 展示现有 Tip UI。

## UI 接入策略

- 已确认 `Scenes/Game.unity` 真实路径为 `Scenes/Game.unity`，但本功能只需要短时状态提示。
- 优先复用现有 `GlobalInit.ShowTip()`、`PrefabConstant.TIP` 和 `TipUI`，不手写 `Game.unity` YAML。
- 不创建新的 `ResourcesLocal` Prefab，因为项目已有统一 Tip 展示链路。
- 降级方案：当 `GlobalInit.ShowTip()` 不可用时输出 `Debug.Log` / `Debug.LogWarning`。

## Scene / Prefab / ResourcesLocal 生成策略

- `Game.unity`：不直接修改，避免破坏已有复杂 Canvas 与引用。
- `ResourcesLocal` Prefab：不新增，不覆盖；本次复用已有 Tip 资源链路。
- Editor 菜单：新增 `工具/玩家生命提示/`，支持查看报告、开关监控、开关 Tip 和立即触发。
- 运行时代码动态 UI：通过 `GlobalInit.ShowTip()` 触发现有 `TipUI`。

## Tool 复用策略

- 已检查 `Scripts/2D/Tool/Tool.cs`：本功能不需要 UI 输入焦点或组件查找。
- 已检查 `Scripts/2D/Tool/WorkerConditionTool.cs`：其安全比例与状态分层思路可复用为设计参考，但语义属于 Worker，不直接复用。
- 已检查 `Scripts/2D/Tool/WorkerTaskCongestionTool.cs`：报告、Tip 和颜色分层模式可复用为实现参考，但语义不同。
- 本次新增公共工具：`PlayerVitalAlertTool`，负责玩家血量比例、等级、颜色、建议和文案格式化。

## Enum 复用策略

- 已检查 `Scripts/2D/Enum`：没有玩家生命危险提示等级枚举。
- 不复用 `WorkerConditionState`，因为 Worker 饥饿/疲劳与玩家血量语义不同。
- 本次新增公共枚举：`PlayerVitalAlertLevel`，表达 Safe、Wounded、Critical、Respawning。

## Constant 复用策略

- 已检查 `PrefabConstant.cs`：继续通过 `GlobalInit.ShowTip()` 间接复用 `PrefabConstant.TIP`，不新增 Tip 资源名。
- 已检查 `InputKeyConstant.cs`：本功能不占用新热键，只在 Editor 菜单中提供调试入口。
- 本次新增公共常量：`PlayerVitalAlertConstant`，维护刷新间隔、Tip 冷却、血量阈值、菜单路径和默认文案。

## 分层说明

- 公共枚举沉淀到 `Scripts/2D/Enum`：玩家生命危险提示等级。
- 公共常量沉淀到 `Scripts/2D/Constant`：阈值、冷却、菜单路径和默认文案。
- 公共函数沉淀到 `Scripts/2D/Tool`：血量比例、等级、颜色和文案格式化。
- 业务状态管理放入 `Scripts/2D/Gameplay`：节流、事件、报告、Tip 请求和运行时开关。
- Editor 专用逻辑放入 `Scripts/2D/Editor`：运行时查看和开关菜单。

## 执行步骤

1. 新增玩家生命危险提示枚举。
2. 新增玩家生命提示常量。
3. 新增玩家生命提示工具。
4. 新增运行时 `PlayerVitalAlertManager` 与 `PlayerVitalAlertReport`。
5. 修改 `GlobalInit.Update()` 接入只读 Tick。
6. 新增 Editor 菜单。
7. 完成静态检查、验证记录和全局候选回写。

## 验证步骤

1. 检查新增运行时代码不引用 `UnityEditor`。
2. 检查新增 Tool / Enum / Constant 注释完整且无重复状态定义。
3. 检查 `GlobalInit` 仅新增只读 Tick，不改变玩家移动、攻击、死亡和复活流程。
4. 检查 `Scenes/Game.unity` 和 `ResourcesLocal` 未被直接修改。
5. 尝试执行可用的静态语法或文本检查；Unity 编译和 Play Mode 若无法运行，则记录原因。

## 回滚方案

- 删除本次新增脚本及 `.meta`：
  - `Scripts/2D/Enum/PlayerVitalAlertLevel.cs`
  - `Scripts/2D/Constant/PlayerVitalAlertConstant.cs`
  - `Scripts/2D/Tool/PlayerVitalAlertTool.cs`
  - `Scripts/2D/Gameplay/PlayerVitalAlertManager.cs`
  - `Scripts/2D/Editor/PlayerVitalAlertMenu.cs`
- 移除 `GlobalInit.Update()` 中新增的 `PlayerVitalAlertManager.Instance.Tick()` 调用。
- 将 `Agent/Reports/feature_discovery.md` 中 F019 状态恢复为 `[TODO]` 或删除新增候选行。

## 结果区

- 最终状态：`[DONE]`
- 已完成内容：新增玩家生命等级枚举、阈值常量、生命提示工具、运行时生命提示管理器和 Editor 菜单，并在 `GlobalInit.Update()` 接入只读 Tick。
- 修改文件：
  - `Scripts/2D/Enum/PlayerVitalAlertLevel.cs`
  - `Scripts/2D/Constant/PlayerVitalAlertConstant.cs`
  - `Scripts/2D/Tool/PlayerVitalAlertTool.cs`
  - `Scripts/2D/Gameplay/PlayerVitalAlertManager.cs`
  - `Scripts/2D/Editor/PlayerVitalAlertMenu.cs`
  - `Scripts/2D/GlobalInit.cs`
  - `Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/task_feature_F019_PlayerVitalAlert.md`
  - `Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/validation_feature_F019.md`
- 新增游戏业务能力：玩家生命偏低、生命濒危、复活等待和恢复后会生成可读提示。
- 玩家侧效果：玩家在战斗压力下更容易意识到生命危险并及时脱战或恢复。
- UI 生成位置：未直接写入 `Game.unity`；未创建新的 `ResourcesLocal` Prefab；复用现有 Tip UI 链路。
- 开发侧接入方式：自动接入 `GlobalInit.Update()`；可在 Play Mode 使用 `工具/玩家生命提示/查看生命提示报告` 和 `立即触发一次生命 Tip`。
- 验证结果：静态检查通过；运行时代码无编辑器 API 引用；新增 `.meta` 齐全；`Scenes` 和 `ResourcesLocal` 未被写入 F019 内容；Unity 编译和 Play Mode 待人工验证。
- 验证记录路径：`Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/validation_feature_F019.md`
- 未完成项：未在 Unity Editor 内执行菜单与 Play Mode 低血量场景验证。
- 剩余风险：Tip 文本长度、阈值体感和死亡/复活边界提示频率需要在 Unity Editor / Play Mode 中观察。
- 是否复用 `Scripts/2D/Tool`：已检查并参考现有工具分层；本功能语义独立，未直接复用 Worker 工具。
- 是否新增或修改 Tool：新增 `Scripts/2D/Tool/PlayerVitalAlertTool.cs`。
- 新增公共工具类或函数路径及用途：`PlayerVitalAlertTool.TryGetPlayerData()`、`GetLevel()`、`GetAdviceText()`、`BuildTipText()`、`BuildSummaryText()` 供运行时管理器、Editor 菜单和后续 HUD 复用。
- 是否复用 `Scripts/2D/Enum`：已检查，未复用 Worker 枚举，避免语义混淆。
- 是否新增或修改 Enum：新增 `Scripts/2D/Enum/PlayerVitalAlertLevel.cs`。
- 新增公共枚举路径及用途：`PlayerVitalAlertLevel` 用于统一 Tip、Editor 菜单和后续玩家目标提示中的生命状态。
- 是否复用 `Scripts/2D/Constant`：通过现有 `GlobalInit.ShowTip()` 继续复用 `PrefabConstant.TIP` 链路。
- 是否新增或修改 Constant：新增 `Scripts/2D/Constant/PlayerVitalAlertConstant.cs`。
- 新增公共常量路径及用途：`PlayerVitalAlertConstant` 供运行时管理器、工具和 Editor 菜单统一引用。
- 后续建议：若 Play Mode 中玩家需要更强反馈，可再新增常驻生命危险 HUD 边框或音效提示候选。
- 是否存在未抽取的重复逻辑、重复枚举、重复常量或魔法值：未发现；血量阈值和文案已沉淀到 Constant，状态语义已沉淀到 Enum。
