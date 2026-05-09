# F016 任务队列拥堵 Tip 与优先级建议验证记录

- 验证日期：2026-05-09
- 最终状态建议：`[DONE]`
- 功能名称：任务队列拥堵 Tip 与优先级建议
- 任务目录：`Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/`
- 任务卡：`Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/task_feature_F016_WorkerTaskCongestionTip.md`

## 运行时业务脚本验证

- 管理器路径：`Scripts/2D/Gameplay/WorkerTaskCongestionAdvisor.cs`
  - 命名空间：`LAB2D`
  - 类名：`WorkerTaskCongestionAdvisor`
  - 基类：`Singleton<WorkerTaskCongestionAdvisor>`
  - Unity API：使用 `Time.time`、`Mathf.Max`、`Debug.Log`、`Debug.LogWarning`，不引用编辑器 API。
  - 基础逻辑：按 `MonitorRefreshInterval` 节流读取 `WorkerTaskManager.CreateTaskQueueSnapshot()`，生成 `WorkerTaskCongestionReport`，报告变化时派发事件，达到拥堵等级且冷却结束后请求 Tip。
  - 空引用保护：`WorkerTaskManager.Instance` 为空时返回 `ManagerUnavailableText`；扫描异常时返回带错误文案的报告。
  - 调用边界：只读读取任务快照，不新增任务，不取消任务，不修改任务优先级。

- 接入点路径：`Scripts/2D/GlobalInit.cs`
  - 新增调用：`WorkerTaskCongestionAdvisor.Instance.Tick()`
  - 位置：`WorkerUpdate()` 中，在 `WorkerSupplyIssueManager.Instance.Tick()` 之后。
  - 风险控制：仅新增只读节流刷新，不改变 Worker 饥饿/疲劳衰减、补给缺口提示、任务调度顺序。

## UI / Scene / Prefab 验证

- `Game.unity` 真实路径：`Scenes/Game.unity`
- 是否直接写入 `Game.unity`：否。
  - 原因：本功能是短时 Tip 反馈，已存在 `GlobalInit.ShowTip()` 和 `Tip.prefab`，直接手写大型 Scene YAML 风险高且没有必要。
  - 回滚方式：删除新增脚本与 `GlobalInit` Tick 调用即可，无需清理场景节点。

- `ResourcesLocal` Prefab：
  - 复用路径：`ResourcesLocal/Prefabs/Tip.prefab`
  - `.meta`：`ResourcesLocal/Prefabs/Tip.prefab.meta` 已存在。
  - 脚本引用链路：`WorkerTaskCongestionAdvisor.ShowTip()` -> `GlobalInit.ShowTip()` -> `ResourceManager.Instance.Instantiate(PrefabConstant.TIP)` -> `TipUI.SetText()`。
  - 是否新增 Prefab：否。
  - 原因：项目已有 Tip UI 预制体和 `PrefabConstant.TIP` 常量，本次复用可避免重复资源和额外绑定风险。

## Editor 工具验证

- Editor 路径：`Scripts/2D/Editor/WorkerTaskCongestionAdvisorMenu.cs`
- 命名空间：`LAB2D`
- 菜单路径：
  - `工具/任务队列拥堵提示/查看拥堵建议`
  - `工具/任务队列拥堵提示/启用拥堵监控`
  - `工具/任务队列拥堵提示/禁用拥堵监控`
  - `工具/任务队列拥堵提示/启用拥堵 Tip`
  - `工具/任务队列拥堵提示/禁用拥堵 Tip`
  - `工具/任务队列拥堵提示/立即触发一次拥堵 Tip`
- 输出方式：Play Mode 中显示 Dialog 并向 Console 输出当前拥堵建议；手动触发时复用现有 Tip UI。
- 运行时隔离：编辑器脚本位于 `Scripts/2D/Editor`，运行时代码未引用 `UnityEditor`。

## Tool 验证

- 新增路径：`Scripts/2D/Tool/WorkerTaskCongestionTool.cs`
- 命名空间：`LAB2D`
- 是否误引编辑器 API：否。
- 公共函数：
  - `BuildReport()`：将 `WorkerTaskQueueSnapshot` 转为拥堵报告，快照为空时返回未初始化报告。
  - `GetCongestionLevel()`：按等待任务数返回拥堵等级。
  - `GetLevelName()`：返回中文等级名。
  - `GetLevelRichColor()`：返回 RichText 颜色，供后续 HUD 或面板复用。
  - `GetPrimaryWaitingSummary()`：选择等待数量最高的任务类型。
  - `HasDominantTaskType()`：判断主积压类型是否明显。
  - `BuildAdviceText()`：生成玩家建议文案。
- 空引用保护：对空快照、空报告、空任务统计列表均有保护。
- 兼容性：未修改 `WorkerTaskSummaryTool` 既有方法签名和行为；继续复用 F015 快照和任务类型中文名。
- 中文注释：已覆盖用途、参数、返回值、边界和风险限制。

## Enum 验证

- 新增路径：`Scripts/2D/Enum/WorkerTaskCongestionLevel.cs`
- 命名空间：`LAB2D`
- 枚举值：
  - `None`：无数据或不可判断。
  - `Smooth`：队列平稳。
  - `Busy`：达到繁忙阈值。
  - `Congested`：达到拥堵阈值，可触发 Tip。
  - `Critical`：严重积压。
- 是否重复或冲突：未发现现有任务拥堵等级枚举；任务类型继续复用 `AWorkerTask.WorkerTaskTypeEnum`，没有重复定义任务类型。
- 兼容性：新增独立枚举，未删除、重命名或改变已有枚举值。
- 中文注释：已说明用途、每个枚举值含义、使用场景和扩展边界。

## Constant 验证

- 新增路径：`Scripts/2D/Constant/WorkerTaskCongestionConstant.cs`
- 命名空间：`LAB2D`
- 常量分组：
  - 扫描节奏：`MonitorRefreshInterval`
  - Tip 冷却：`TipCooldownSeconds`
  - 阈值：`BusyWaitingTaskThreshold`、`CongestedWaitingTaskThreshold`、`CriticalWaitingTaskThreshold`
  - 主积压判断：`DominantTaskWaitingThreshold`、`DominantTaskWaitingRatio`
  - 菜单路径：`MenuRoot`
  - 默认文案与日志：`NoCongestionText`、`ManagerUnavailableText`、`LogPrefix`
- 是否重复或冲突：繁忙/拥堵阈值复用 `WorkerTaskHudConstant`，避免 F015 HUD 与 F016 Tip 判断分叉。
- 兼容性：新增独立常量类，未修改或删除已有公共常量。
- 中文注释：已说明默认值含义、使用场景和修改风险。

## 静态检查

- 已执行：`git diff --check`
  - 结果：无空白错误；仅提示 `Scripts/2D/GlobalInit.cs` 在当前 Git 换行配置下未来会被转换为 CRLF。
- 已执行：运行时代码中搜索 `UnityEditor`
  - 范围：`WorkerTaskCongestionLevel.cs`、`WorkerTaskCongestionConstant.cs`、`WorkerTaskCongestionTool.cs`、`WorkerTaskCongestionAdvisor.cs`、`GlobalInit.cs`
  - 结果：无命中。
- 已执行：检查新增脚本 `.meta`
  - 结果：`WorkerTaskCongestionLevel.cs.meta`、`WorkerTaskCongestionConstant.cs.meta`、`WorkerTaskCongestionTool.cs.meta`、`WorkerTaskCongestionAdvisor.cs.meta`、`WorkerTaskCongestionAdvisorMenu.cs.meta` 均存在。
- 已执行：`Scenes` 和 `ResourcesLocal` 中搜索 `Feature_F016` / `WorkerTaskCongestion`
  - 结果：无命中，确认未直接写入场景或资源预制体。

## 未执行验证

- 未执行 Unity Editor 编译：当前环境未启动 Unity Editor，无法获得 Unity 编译控制台结果。
- 未执行 Play Mode：无法实际制造任务队列拥堵、观察 Tip 展示、确认 `Tip.prefab` 屏幕位置和文本长度。

## 结论

- 本次新增运行时逻辑低侵入，只读读取 F015 快照，不改变任务调度。
- UI 反馈复用现有 Tip 预制体和资源常量，未新增或覆盖 Prefab。
- Tool / Enum / Constant 分层符合公共代码优先规则。
- 建议最终状态：`[DONE]`，剩余风险为 Unity Editor 编译和 Play Mode 体验验证待人工执行。
