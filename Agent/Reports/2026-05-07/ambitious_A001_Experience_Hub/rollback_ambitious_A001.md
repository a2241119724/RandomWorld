# A001 回滚方案

## 回滚目标

将本次“沉浸式会话体验中枢”相关脚本、可选场景节点、可选 ResourcesLocal Prefab 和报告状态恢复到执行前状态，不影响已有 F001-F011 功能。

## 回滚步骤

1. 删除新增运行时脚本：
   - `Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`
   - `Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs.meta`

2. 删除新增 Editor 工具：
   - `Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs`
   - `Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs.meta`

3. 如果已经通过菜单安装到场景：
   - 打开 `Scenes/Game.unity`
   - 删除 `Ambitious_A001_ExperienceHub_Root`
   - 删除运行时生成的 `Ambitious_A001_ExperienceHub_Canvas`（如果 Play Mode 后被保存到场景）
   - 保存场景

4. 如果已经通过菜单生成 Prefab：
   - 删除 `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/Ambitious_A001_ExperienceHub*.prefab`
   - 删除对应 `.meta`
   - 如果目录为空，可删除 `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/` 及目录 `.meta`

5. 回写报告：
   - 将 `Agent/Reports/ambitious_discovery.md` 中 A001 状态从 `[DONE]` 改为 `[BLOCKED]` 或恢复到 `[TODO]`
   - 保留本任务目录作为审计记录，避免覆盖历史

## 回滚后验证

- Unity 编译不再出现 `AmbitiousExperienceHub` 或 `AmbitiousExperienceHubInstaller` 类型。
- `Scenes/Game.unity` 中不存在 `Ambitious_A001_` 前缀对象。
- `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/` 不存在自动生成 Prefab。
- 原有 `PlayerStatusUI`、`GameplayStatsUI`、`SessionResultManager`、`WaveEventFeedback` 行为不受影响。
