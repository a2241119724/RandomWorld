# A006 回滚方案

## 回滚目标

撤销殖民地运营指挥中心的所有代码、报告和可选 UI 安装结果，恢复到 A006 开发前状态。

## 代码回滚

删除以下新增脚本及对应 `.meta`：

- `Scripts/2D/Enum/ColonyCommandAlertLevel.cs`
- `Scripts/2D/Enum/WorkerTaskBlockReason.cs`
- `Scripts/2D/Constant/ColonyCommandCenterConstant.cs`
- `Scripts/2D/Tool/ColonyCommandCenterTool.cs`
- `Scripts/2D/Gameplay/ColonyCommandCenterReport.cs`
- `Scripts/2D/Gameplay/ColonyCommandCenterManager.cs`
- `Scripts/2D/UI/ColonyCommandCenterHUD.cs`
- `Scripts/2D/Editor/ColonyCommandCenterMenu.cs`

修改回滚：

1. 从 `Scripts/2D/Character/Worker/WorkerTaskManager.cs` 删除 `CreateTaskAssignmentReport()` 方法。
2. 从 `Scripts/2D/GlobalInit.cs` 删除 `ColonyCommandCenterHUD.EnsureRuntimePanel()` 调用。
3. 从 `Scripts/2D/GlobalInit.cs` 删除 `ColonyCommandCenterManager.Instance.Tick()` 调用。

## Scene / Prefab 回滚

本次未直接写入 `Scenes/Game.unity`，未直接生成 `ResourcesLocal` Prefab。

如果后续在 Unity 中执行了 Editor 菜单：

1. 打开 Game 场景。
2. 执行菜单 `工具/智能体/A006 殖民地指挥中心/从当前场景移除指挥中心 UI`。
3. 确认场景中不存在：
   - `Ambitious_A006_ColonyCommandCenter_Canvas`
   - `Ambitious_A006_ColonyCommandCenterHUD_Root`
4. 如果执行过 Prefab 生成菜单，删除：
   - `Assets/ResourcesLocal/Prefabs/UI/ColonyCommandCenter/Ambitious_A006_ColonyCommandCenterHUD.prefab`
   - 对应 `.meta`
5. 删除空目录 `Assets/ResourcesLocal/Prefabs/UI/ColonyCommandCenter/` 及 `.meta`。

## 报告回滚

1. 在 `Agent/Reports/ambitious_discovery.md` 中将 A006 状态从 `[DONE]` 改回 `[TODO]` 或删除 A006 行。
2. 删除本任务目录：
   - `Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/`
   - 对应 `.meta`

## 回滚后验证

1. 搜索 `A006`、`ColonyCommandCenter`、`WorkerTaskBlockReason`，确认仅剩必要历史记录或已全部删除。
2. 运行 `git diff --check`，确认没有空白错误。
3. 在 Unity 中重新编译，确认无缺失脚本和无编译错误。
4. 进入 Game 场景，确认不再自动出现指挥中心 HUD。
