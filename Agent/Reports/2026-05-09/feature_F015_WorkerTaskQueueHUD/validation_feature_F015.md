# F015 任务队列 HUD 摘要验证记录

## 验证对象

- 候选 ID：F015
- 功能名称：任务队列 HUD 摘要
- 任务目录：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/`
- 任务卡：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/task_feature_F015_WorkerTaskQueueHUD.md`
- 全局候选报告：`Agent/Reports/feature_discovery.md`

## 运行时业务脚本验证

- 数据模型：`Scripts/2D/Gameplay/WorkerTaskQueueSnapshot.cs`
  - 类名：`WorkerTaskQueueSnapshot`、`WorkerTaskTypeSummary`
  - 命名空间：`LAB2D`
  - 作用：保存任务总数、等待中数量、进行中数量和任务类型分布。
  - 边界：只保存统计结果，不持有任务引用，不修改任务状态。
- 任务管理器扩展：`Scripts/2D/Character/Worker/WorkerTaskManager.cs`
  - 新增 `CreateTaskQueueSnapshot()`：只读读取内部任务字典并交给工具类统计。
  - 新增 `GetTaskQueueSummaryText()`：返回 HUD RichText 摘要。
  - 未修改 `AddTask()`、`Update()`、`CompleteTask()`、`GiveUpTask()` 的调度语义。
- UI 脚本：`Scripts/2D/UI/WorkerTaskQueueHUD.cs`
  - 类名：`WorkerTaskQueueHUD`
  - 命名空间：`LAB2D`
  - Unity API：`MonoBehaviour`、`Text`、`CanvasGroup`、`KeyCode`、`Input.GetKeyDown()`、`Time.unscaledTime`
  - 绑定逻辑：优先使用公开 `queueText`，为空时通过 `Tool.GetComponentInChildren<Text>()` 查找 `WorkerTaskHudConstant.HudTextName`。
  - 空引用保护：`WorkerTaskManager.Instance` 为空时显示 `ManagerUnavailableText`；刷新异常时写 Warning 并保持兜底文案。
  - 输入边界：使用 `Tool.IsUIInputActive()` 避免 UI 输入框聚焦时触发 HUD 热键。

## Tool 验证

- 新增路径：`Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
- 命名空间：`LAB2D`
- 是否误引 Editor API：未发现 `using UnityEditor` 或 `UnityEditor.`。
- 公共函数：
  - `BuildSnapshot()`：对 `WorkerTaskManager` 任务字典做只读聚合，跳过空任务和空分组。
  - `BuildHudText()`：生成 HUD RichText 摘要。
  - `BuildPlainText()`：生成 Editor 弹窗和日志用纯文本摘要。
  - `GetTaskDisplayName()`：复用 `AWorkerTask.WorkerTaskTypeEnum` 并映射中文名称。
  - `GetPressureLabel()`：根据等待中任务数量返回压力标签。
- 空引用保护：输入快照为空时返回 `ManagerUnavailableText`，任务组或任务为空时跳过。
- 风险限制：不新增、取消、重排任务，不访问 Scene、Prefab、存档、Photon 或 AssetBundle。
- 是否破坏已有调用方：新增工具类，无旧调用方。

## Enum 验证

- 本次未新增或修改 `Scripts/2D/Enum`。
- 复用枚举：`AWorkerTask.WorkerTaskTypeEnum`
  - 复用原因：它是当前 Worker 任务系统真实使用的任务类型定义，新增公共任务类型枚举会造成重复语义和映射风险。
  - 使用方式：`WorkerTaskQueueSnapshot` 和 `WorkerTaskSummaryTool` 直接引用该枚举做统计与显示。
- 未抽取原因：该枚举当前内嵌在 `AWorkerTask`，全项目已有调用依赖该位置；迁移到 `Scripts/2D/Enum` 会带来大范围改动，不适合本次低风险 UI 摘要任务。

## Constant 验证

- 新增路径：`Scripts/2D/Constant/WorkerTaskHudConstant.cs`
- 命名空间：`LAB2D`
- 新增常量：刷新间隔、HUD 热键、菜单根路径、Canvas/Root/Text 节点名、默认文案、压力阈值、HUD 尺寸与位置。
- 是否重复或冲突：节点名均带 `Feature_F015_` 前缀，避免与 F012/F013/F014 HUD 冲突。
- 复用常量：
  - `WorkerConditionConstant.GameSceneName`：Editor 菜单查找真实 `Game.unity`。
  - `WorkerConditionConstant.FontResourcePath`：Editor 菜单加载项目像素字体。
- 是否误改公共值：未修改任何已有常量值。
- 业务脚本引用：`WorkerTaskQueueHUD`、`WorkerTaskQueueHUDMenu`、`WorkerTaskSummaryTool` 均引用常量，未散落重复节点名和菜单路径。

## UI / Scene / Prefab 验证

- `Game.unity` 真实路径：`Scenes/Game.unity`
- 是否直接写入 `Game.unity`：否。
- 未直接写入原因：场景文件已有大量 UI 序列化引用，手写 YAML 容易破坏已有 Canvas、Panel、脚本绑定和对象引用。
- 是否创建 `ResourcesLocal` Prefab：否。
- 未创建原因：当前 `ResourcesLocal/Prefabs` 主要包含 Character/Item 目录，没有稳定 UI Prefab 规范；手写 Prefab YAML 需要脚本 GUID 和组件引用，自动修改风险高。
- 降级方案：新增 Editor 菜单 `工具/任务队列 HUD/创建任务队列 HUD 到 Game 场景`。
- Editor 生成节点：
  - Canvas：`Feature_F015_WorkerTaskQueue_Canvas`，仅当场景没有 Canvas 时创建。
  - HUD 根节点：`Feature_F015_WorkerTaskQueueHUD_Root`
  - 文本节点：`WorkerTaskQueueText`
  - 组件层级：Canvas / CanvasScaler / GraphicRaycaster；HUD 根节点挂 `CanvasGroup`、`WorkerTaskQueueHUD`；背景挂 `Image`；文本挂 `Text`。
  - 回滚方式：执行 `工具/任务队列 HUD/从当前场景移除任务队列 HUD`，或手动删除上述 F015 节点。

## Editor 工具验证

- 新增路径：`Scripts/2D/Editor/WorkerTaskQueueHUDMenu.cs`
- 菜单路径：
  - `工具/任务队列 HUD/查看任务队列摘要`
  - `工具/任务队列 HUD/创建任务队列 HUD 到 Game 场景`
  - `工具/任务队列 HUD/从当前场景移除任务队列 HUD`
- 输出路径：真实 `Game.unity` 场景内的独立 HUD 节点。
- 基本生成逻辑：通过 `AssetDatabase.FindAssets()` 定位 `Game.unity`，复用已有 Canvas 或创建独立 Canvas，再创建背景、Text 和 `WorkerTaskQueueHUD`。
- 运行时隔离：Editor API 只存在于 `Scripts/2D/Editor`。

## 静态检查记录

- 已执行：`rg -n "using UnityEditor|UnityEditor\\."` 检查运行时新增/修改文件。
  - 结果：运行时代码无 `UnityEditor` 引用。
- 已执行：`git diff --check`
  - 结果：无空白错误；仅提示 `WorkerTaskManager.cs` 在当前 Git 换行配置下未来会被转换为 CRLF。
- 已执行：新增脚本与任务卡 `.meta` 检查。
  - 结果：新增脚本和任务卡均存在 `.meta`。
- 已执行：`Scenes/Game.unity` 与 `ResourcesLocal` 中搜索 `Feature_F015` / `WorkerTaskQueueHUD`。
  - 结果：未直接修改场景或 Prefab 资源。

## 未执行验证

- 未执行 Unity Editor 编译：当前 shell 环境无法刷新 Unity 工程并重新生成包含新脚本的 `.csproj`，直接 `dotnet build` 会因工程文件未包含新脚本产生误报。
- 未执行 Play Mode：当前环境未启动 Unity Editor，无法实际创建 HUD、观察 Canvas 层级、字体加载和任务统计刷新。

## 验证结论

- 静态检查通过。
- 新增运行时代码未引用 Editor API。
- `WorkerTaskManager` 只增加只读查询接口，未改变任务调度语义。
- UI 未直接写入 `Game.unity`，未创建 `ResourcesLocal` Prefab；已提供低风险 Editor 菜单生成方案。
- 最终状态建议：`[DONE]`，剩余风险为 Unity Editor 编译和 Play Mode 体验验证待人工执行。
