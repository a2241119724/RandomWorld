# A001 验证记录

## 验证对象

- 候选ID：A001
- 功能名称：沉浸式会话体验中枢（实时HUD+事件流+结算面板+可生成Prefab工具）
- 任务目录：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/`
- 任务卡：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/task_ambitious_A001_Experience_Hub.md`

## 静态验证

| 验证项 | 结果 | 说明 |
|---|---|---|
| `git diff --check` | PASS | 未发现尾随空格或补丁格式问题 |
| `Game.unity` 路径 | PASS | 已确认 `Scenes/Game.unity` 存在 |
| 场景未直接修改 | PASS | `git diff -- Scenes/Game.unity` 无输出 |
| ResourcesLocal 未直接修改 | PASS | `git diff -- ResourcesLocal` 无输出 |
| 已有核心 UI 未修改 | PASS | `ForegroundPanel`、`PlayerStatusUI`、`GameplayStatsUI` 无 diff |
| 运行时脚本 meta | PASS | `AmbitiousExperienceHub.cs.meta` 存在 |
| Editor 工具 meta | PASS | `AmbitiousExperienceHubInstaller.cs.meta` 存在 |
| 报告 meta | PASS | `ambitious_discovery.md.meta` 和任务目录 meta 存在 |
| 菜单路径 | PASS | `Tools/Agent/Ambitious/Experience Hub/` 已在 Editor 脚本中声明 |
| 运行时自动入口 | PASS | `RuntimeInitializeOnLoadMethod` 已存在，且只在 `Game` 场景自动创建 |
| 事件订阅 | PASS | 订阅 `GameplaySessionStats`、`ComboBonusManager`、`WaveEventFeedback`、`SessionResultManager`，并在禁用时取消订阅 |
| UI 射线边界 | PASS | HUD `CanvasGroup.blocksRaycasts=false`，结算面板显示时才阻挡底层点击 |
| Prefab 生成边界 | PASS | Editor 工具使用 `AssetDatabase.GenerateUniqueAssetPath`，不覆盖已有 Prefab |
| Scene 安装边界 | PASS | Editor 工具先查找 `Ambitious_A001_ExperienceHub_Root`，重复执行不重复创建 |

## 未执行验证

- Unity Editor 编译：当前命令行环境未运行 Unity Editor 编译流程。
- Play Mode：当前环境未启动 Unity Play Mode，HUD 实际渲染、按钮点击和事件流需在 Unity 内验证。
- Editor 菜单实操：未实际点击生成 Game 场景节点或 ResourcesLocal Prefab。

## 运行时问题修复记录

### 2026-05-07 内置字体兼容修复

- 问题：新版 Unity 中 `Resources.GetBuiltinResource<Font>("Arial.ttf")` 会抛出 `ArgumentException`，提示应改用 `LegacyRuntime.ttf`。
- 修复文件：`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`
- 修复方式：新增 `LoadDefaultFont()` 和 `TryLoadBuiltinFont()`，优先加载 `LegacyRuntime.ttf`，旧版本回退 `Arial.ttf`，并吞掉字体名不兼容导致的异常，避免打断 `Awake()`。
- 静态验证：已确认脚本中不再直接调用 `Arial.ttf`，`git diff --check` 无格式问题。

## UI 验证结论

- `Game.unity` 未被直接写入，避免手写复杂 Scene YAML 的破坏风险。
- `ResourcesLocal` 未被直接写入 Prefab YAML，避免脚本 GUID 和 UI 组件序列化风险。
- 已提供 Editor 菜单工具，可用 Unity 官方 API 安装场景节点或生成 Prefab。
- 已提供运行时代码动态创建完整 UI，默认进入 `Game` 场景即可生成独立 Canvas、HUD、事件流和结算面板。

## 回滚验证

- 由于未直接修改 Scene、Prefab、ScriptableObject、StreamingAssets，回滚只需删除新增脚本和可选生成物。
- 回滚路径见：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/rollback_ambitious_A001.md`
- 已确认当前没有 `Scenes/Game.unity` 或 `ResourcesLocal` 的 A001 diff。

## 最终判定

- 最终状态：`[DONE]`
- 验证级别：静态验证通过，Unity 编译与 Play Mode 待人工环境验证。
- 剩余风险：运行时 UI 排序、默认字体和实际屏幕布局需要在 Unity 中观察微调。
