# F019 玩家生命危险提示验证记录

## 基本信息

- 候选 ID：F019
- 功能名称：玩家生命危险提示
- 验证日期：2026-05-17
- 任务目录：`Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/`
- 任务卡路径：`Agent/Reports/2026-05-17/feature_F019_PlayerVitalAlert/task_feature_F019_PlayerVitalAlert.md`
- 全局候选报告路径：`Agent/Reports/feature_discovery.md`
- 最终状态：`[DONE]`

## 验证命令与结果

- `git diff --check`
  - 结果：通过；仅提示 `Scripts/2D/GlobalInit.cs` 在 Git 触碰时可能按仓库配置转换为 CRLF，不属于语法或空白错误。
- `rg "using UnityEditor|UnityEditor\\." Scripts/2D/Enum/PlayerVitalAlertLevel.cs Scripts/2D/Constant/PlayerVitalAlertConstant.cs Scripts/2D/Tool/PlayerVitalAlertTool.cs Scripts/2D/Gameplay/PlayerVitalAlertManager.cs`
  - 结果：无匹配，运行时代码未引用 Editor API。
- `rg "PlayerVitalAlert" Scripts/2D -g "*.cs"`
  - 结果：可定位新增枚举、常量、工具、管理器、Editor 菜单和 `GlobalInit` 接入点。
- `rg --files -g "*.csproj" -g "*.sln"`
  - 结果：未找到可用工程文件，当前命令行环境无法执行 Unity C# 编译。

## 运行时业务脚本验证

- 类名：`PlayerVitalAlertManager`
- 命名空间：`LAB2D`
- 脚本路径：`Scripts/2D/Gameplay/PlayerVitalAlertManager.cs`
- Unity API 使用：
  - `Time.time`：用于刷新节流与 Tip 冷却。
  - `Mathf.Max`：保护刷新间隔下限。
  - `Debug.Log` / `Debug.LogWarning`：Tip UI 不可用时的安全降级。
- 基础逻辑：
  - `Tick()` 按 `PlayerVitalAlertConstant.MonitorRefreshInterval` 节流。
  - `Refresh()` 构建 `PlayerVitalAlertReport`，报告变化时派发事件。
  - `TryShowVitalTip()` 只在危险状态、升级状态或恢复状态下请求 Tip。
  - `BuildReport()` 只读 `PlayerManager.Instance.Mine` 和 `DeathPenaltyManager.Instance.IsRespawning`。
- 空引用保护：
  - 本地玩家为空时返回 `PlayerUnavailableText` 降级报告。
  - 玩家数据不可读时返回 `PlayerDataUnavailableText` 降级报告。
  - `GlobalInit.ShowTip()` 异常时降级为日志。
- 调用边界：
  - 不修改玩家血量、最大血量、经验、死亡惩罚、复活时间或移动/攻击逻辑。
  - 不写入存档，不访问 Photon，不访问 AssetBundle 或 StreamingAssets。

## UI / Scene / Prefab 验证

- `Game.unity` 路径：`Scenes/Game.unity`
- 是否写入 `Game.unity`：否。
- 未写入原因：本功能为短时状态提示，已有 `GlobalInit.ShowTip()` 与 `TipUI` 可安全复用；手写大型 Scene YAML 风险高且没有必要。
- UI Prefab 路径：未新增。
- 是否创建 `ResourcesLocal` Prefab：否。
- 未创建原因：项目已有统一 Tip 展示链路，新增 Prefab 会增加接入和 AssetBundle 验证成本。
- 实际 UI 生成方式：运行时复用 `GlobalInit.ShowTip()`，由现有 `PrefabConstant.TIP` 和 `TipUI` 展示。
- 脚本挂载：未新增 Scene 挂载脚本，`PlayerVitalAlertManager` 是普通单例，由 `GlobalInit.Update()` 调用。
- 回滚方式：移除 `GlobalInit.Update()` 中 `PlayerVitalAlertManager.Instance.Tick()`，删除 F019 新增脚本及 `.meta`。

## Editor 工具验证

- 类名：`PlayerVitalAlertMenu`
- 命名空间：`LAB2D`
- 脚本路径：`Scripts/2D/Editor/PlayerVitalAlertMenu.cs`
- 菜单路径：
  - `工具/玩家生命提示/查看生命提示报告`
  - `工具/玩家生命提示/启用生命监控`
  - `工具/玩家生命提示/禁用生命监控`
  - `工具/玩家生命提示/启用生命 Tip`
  - `工具/玩家生命提示/禁用生命 Tip`
  - `工具/玩家生命提示/立即触发一次生命 Tip`
- 输出路径：无资源输出。
- 基本生成逻辑：无 Scene / Prefab 写入逻辑；仅在 Play Mode 查看报告或触发当前 Tip。
- Editor 隔离：`UnityEditor` 仅出现在 `Scripts/2D/Editor/PlayerVitalAlertMenu.cs`。

## Tool 验证

- 新增路径：`Scripts/2D/Tool/PlayerVitalAlertTool.cs`
- 命名空间：`LAB2D`
- 是否误引 `UnityEditor`：否。
- 是否影响运行时构建：运行时代码只引用 `UnityEngine`、`Player`、`PlayerVitalAlertLevel`、`PixelUITheme` 和常量类。
- 是否破坏已有调用方：新增类，不修改已有工具方法签名。
- 公共函数空引用保护：
  - `TryGetPlayerData()` 对 `player` 和 `CharacterDataLAB` 为空做保护。
  - `GetSafeRatio()` 对 `max <= 0` 做保护。
  - 文案构建函数对玩家名为空做默认值处理。
- 中文注释：已覆盖类、用途、参数、返回值、使用边界和风险限制。

## Enum 验证

- 新增路径：`Scripts/2D/Enum/PlayerVitalAlertLevel.cs`
- 命名：清晰表达玩家生命提示等级。
- 枚举值：`Safe`、`Wounded`、`Critical`、`Respawning`。
- 是否重复或冲突：未发现已有玩家血量提示枚举；未复用 `WorkerConditionState`，避免 Worker 生存状态和玩家生命状态语义混淆。
- 是否错误修改旧值：无旧值被修改、删除或重命名。
- 中文注释：枚举和每个枚举值均有中文注释。
- 业务脚本引用：`PlayerVitalAlertTool`、`PlayerVitalAlertManager`、`PlayerVitalAlertReport` 均引用该公共枚举，未在业务脚本中重复定义。

## Constant 验证

- 新增路径：`Scripts/2D/Constant/PlayerVitalAlertConstant.cs`
- 类命名：`PlayerVitalAlertConstant`，按功能语义分组。
- 常量内容：
  - 候选编号、菜单路径、刷新间隔、Tip 冷却、血量阈值、恢复阈值、默认玩家名、摘要标签、异常前缀、默认文案和日志前缀。
- 是否重复或冲突：未发现已有玩家生命提示常量；Tip 资源名继续走现有 `PrefabConstant.TIP` 链路，不重复定义。
- 是否错误修改旧值：无旧常量被修改、删除或重命名。
- 中文注释：所有公共常量均有中文注释，说明用途、使用场景和修改风险。
- 业务脚本引用：`PlayerVitalAlertTool`、`PlayerVitalAlertManager`、`PlayerVitalAlertMenu` 均引用该常量类，未继续硬编码阈值或菜单路径。

## GlobalInit 接入验证

- 修改路径：`Scripts/2D/GlobalInit.cs`
- 修改内容：在 `EnvironmentManager.Instance.UpdateEnergy()` 后新增 `PlayerVitalAlertManager.Instance.Tick()`。
- 风险判断：
  - 只读 Tick，有内部节流。
  - 不改变原 ESC 面板、鼠标关闭面板、Worker 更新、成就、天气、技能或附近拾取逻辑。
  - 不依赖 Scene 新节点，玩家未初始化时安全降级。

## 未使用 / 未新增资源说明

- 未直接修改 `Scenes/Game.unity`：因为现有 Tip 链路已满足短时状态提示需求。
- 未创建 `ResourcesLocal` Prefab：因为新增 Prefab 会带来资源接入和 AssetBundle 验证成本，本次复用现有 Tip UI 更低风险。
- 未修改存档、Photon、AssetBundle、StreamingAssets：本功能不需要持久化或同步。

## 未完成项与剩余风险

- 未在 Unity Editor 中执行 Play Mode 编译和真实低血量场景验证，原因是当前命令行环境没有 Unity Editor 编译入口。
- 未验证 Tip 文本在所有分辨率下的实际长度与显示位置，需要人工 Play Mode 观察。
- 低血量阈值 `35% / 18%` 和恢复阈值 `60%` 需要结合战斗节奏继续调手感。

## 验证结论

- 静态验证通过。
- 运行时代码和 Editor 代码分离清晰。
- Tool / Enum / Constant 分层符合项目约束。
- UI 采用现有 Tip 链路，无需后续人工接入才能生效；Play Mode 编译与体感验证仍建议人工补充。
