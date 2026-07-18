# Ambitious Task Card: A001 沉浸式会话体验中枢

## 基本信息

- 候选ID：A001
- 原始候选：沉浸式会话体验中枢（实时HUD+事件流+结算面板+可生成Prefab工具）
- 当前状态：Done
- 本次任务目录：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：UI与表现 / 战斗反馈 / 关卡结算
- 负责 Agent：UIAgent + GameplayAgent + SceneAgent
- 需要的 Skill：ScriptGenerateSkill + SceneAnalyzeSkill + EditorToolSkill + TestSkill
- 风险等级：Medium

## 玩家价值

玩家能在游戏过程中持续看到击杀、连击、波次、经验、收集、工人任务、评分预览等关键反馈；在死亡或通关结算时获得星级、评级和核心数据面板，战斗与关卡结果不再只停留在日志或 Editor 菜单里。

## 开发价值

将 F001/F002/F004/F005/F006/F009/F010/F011 已完成的数据系统汇聚成统一展示层，为后续成就、任务、奖励、关卡评分、主 HUD 美术替换和 ResourcesLocal Prefab 接入提供稳定入口。

## 修改前状态

- `Scenes/Game.unity` 真实路径已确认存在，且场景内已有大量 Canvas/UI 节点和多个 `Tip` 节点。
- `ResourcesLocal/Prefabs` 真实路径已确认存在，当前未发现 `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/`。
- 已有 UI：`PlayerStatusUI` 显示玩家血量/蓝量/经验；`GameplayStatsUI` 是 F1 调试统计文本，不是完整体验 HUD；`SessionResultData` 和 `SessionResultManager` 仅有数据和 Editor 菜单展示。
- 历史候选 F001/F002/F004/F005/F006/F009/F010/F011 均为 `[DONE]`，本任务不得重复实现其底层统计逻辑。

## 预计影响范围

- 新增运行时 UI 脚本：`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`
- 新增 Editor 安装工具：`Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs`
- 新增报告文件：本任务目录内任务卡、验证记录、回滚方案
- 更新全局候选报告：`Agent/Reports/ambitious_discovery.md`

## 不应触碰路径

- 不修改 `Scenes/Game.unity` 的 YAML 内容。
- 不修改已有 `ForegroundPanel`、`PlayerStatusUI`、`GameplayStatsUI` 的核心字段和层级绑定。
- 不修改 Photon 核心：`Scripts/2D/NetworkConnect.cs`、`Scripts/2D/Tool/SyncDataTool.cs`。
- 不修改 AssetBundle / Addressables 配置：`StreamingAssets`、`AddressableAssetsData`。
- 不修改存档结构：`Scripts/2D/Data`、`Scripts/2D/Manager/ArchiveManager.cs`。

## 功能边界

本次包含：

- 运行时自动创建独立 `Ambitious_A001_ExperienceHub_Canvas`。
- 实时 HUD：显示会话时长、击杀、当前连击、最高连击、伤害/承伤、经验、收集、工人任务、波次状态、难度倍率、实时评分预览、星级预览。
- 玩家生存条补充：独立显示本地玩家 HP、MP、环境灵气。
- 事件流：订阅连击里程碑、连击中断、波次提示、结算采集事件，显示最近事件。
- 结算面板：订阅 `SessionResultManager.OnResultCaptured`，显示评分、星级、评级、击杀、连击、伤害、经验、收集、死亡等统计。
- Editor 工具：提供在 `Game.unity` 安装根节点、在 `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/` 生成 Prefab、在当前场景预览/移除根节点的菜单。

本次不包含：

- 不新增评分算法，不改变 `SessionResultData` 权重。
- 不接入美术贴图、音效、动画控制器。
- 不修改敌人、波次、存档、Photon 同步规则。
- 不手写 `Game.unity` 或 Prefab YAML。

## 业务规则说明

- HUD 数据来自 `GameplaySessionStats.Instance.CreateSnapshot()`，只读消费，不改统计来源。
- 连击倍率来自 `ComboBonusManager`，HUD 只显示当前倍率和事件，不改伤害/经验公式。
- 波次状态来自 `WaveEventFeedback.CurrentState` 和 `WaveManager`，HUD 只展示当前波次/休息/难度，不启动或停止波次。
- 结算面板只订阅 `SessionResultManager.OnResultCaptured`，在死亡或通关触发采集后显示结果，不主动结束会话。
- 运行时 UI 仅在 `Game` 场景自动创建，避免污染登录/菜单场景。
- HUD 默认不拦截鼠标射线；结算面板显示时才允许按钮交互。

## 数据流说明

`GameplaySessionStats` → `AmbitiousExperienceHub.RefreshStats` → HUD 文本/条形进度  
`ComboBonusManager` → 连击事件 → 中央连击提示 + 事件流  
`WaveEventFeedback` → 波次状态/Tip 事件 → 波次文本 + 事件流  
`SessionResultManager.OnResultCaptured` → `AmbitiousExperienceHub.ShowResultPanel` → 结算面板  
`PlayerManager.Instance.Mine` / `EnvironmentManager.Instance` → 生存条补充显示

## UI 接入策略

- 第 1 优先级：`Game.unity` 已存在，但场景 YAML 体量大、Canvas 节点复杂，直接手写新增 UI 节点风险不可控，因此本次不直接修改场景文件。
- 第 2 优先级：不直接手写 `ResourcesLocal` Prefab YAML，避免脚本 GUID、UI 组件引用、CanvasRenderer 和 Text/Image 组件序列化不一致。
- 第 3 优先级：提供 Editor 菜单工具，可在 Unity 中用官方 API 安装 Game 场景根节点或生成 ResourcesLocal Prefab。
- 第 4 优先级：提供运行时代码动态创建完整 UI；本次实际采用该方案作为无需人工接入的默认路径。

## Scene / Prefab / ResourcesLocal 生成策略

- 不直接写入 `Scenes/Game.unity`。
- 不覆盖任何已有 Prefab。
- Editor 菜单生成 Prefab 时使用 `AssetDatabase.GenerateUniqueAssetPath`，避免覆盖。
- Editor 菜单安装场景节点时先检查 `Ambitious_A001_ExperienceHub_Root` 是否已存在，重复执行时安全退出并选中已有节点。

## 资源修改清单

- Scene：不直接修改；提供 Editor 菜单按需安装独立根节点。
- Prefab：不直接新增 YAML Prefab；提供 Editor 菜单按需生成 `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/Ambitious_A001_ExperienceHub.prefab`。
- ScriptableObject：不修改。
- StreamingAssets：不修改。
- `.meta`：新增脚本和报告文件同步新增 `.meta`。

## 执行步骤

1. 生成全局候选报告  
   - 涉及文件：`Agent/Reports/ambitious_discovery.md`  
   - 操作方式：新增 A001-A005 候选并保留状态  
   - 完成标准：A001 为 `[TODO]`，A005 为 `[SKIPPED]`

2. 新增运行时体验中枢  
   - 涉及文件：`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`  
   - 操作方式：新增独立 MonoBehaviour，运行时生成 Canvas、HUD、事件流、结算面板  
   - 完成标准：不修改已有 UI 脚本即可在 Game 场景自动显示

3. 新增 Editor 安装工具  
   - 涉及文件：`Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs`  
   - 操作方式：新增 `Tools/Agent/Ambitious/Experience Hub/` 菜单  
   - 完成标准：可安装 Game 场景根节点、生成 ResourcesLocal Prefab、预览和移除当前场景根节点

4. 静态验证  
   - 涉及文件：新增 C# 脚本和报告  
   - 操作方式：检查命名空间、Unity API、事件订阅/取消、空引用保护、菜单路径、meta 文件  
   - 完成标准：无明显编译错误和资源覆盖风险

5. 回写报告  
   - 涉及文件：`Agent/Reports/ambitious_discovery.md`  
   - 操作方式：将 A001 状态更新为最终状态，并补充任务卡、验证记录、修改文件和 UI 生成方式
   - 完成标准：不在任务目录创建重复的 `ambitious_discovery.md`

## 验证步骤

1. 检查 `Game.unity` 路径是否正确且未被直接修改。
2. 检查新增脚本类名与文件名一致，命名空间为 `LAB2D`。
3. 检查运行时 UI 创建逻辑具备 Canvas/EventSystem 兜底。
4. 检查 HUD 默认不阻塞原游戏点击。
5. 检查结算面板只在捕获结果后显示，可关闭。
6. 检查 Editor 菜单不覆盖已有场景对象或 Prefab。
7. 检查 `.meta` 文件存在。

## 回滚方案

- 删除新增脚本及 meta：
  - `Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`
  - `Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs.meta`
  - `Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs`
  - `Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs.meta`
- 如果已通过 Editor 菜单安装 Game 场景根节点，在 Unity Hierarchy 中删除 `Ambitious_A001_ExperienceHub_Root`，或执行 `Tools/Agent/Ambitious/Experience Hub/Remove Runtime Root From Current Scene`。
- 如果已通过 Editor 菜单生成 ResourcesLocal Prefab，删除 `ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub/` 下生成的 `Ambitious_A001_ExperienceHub*.prefab` 及 `.meta`。
- 恢复 `Agent/Reports/ambitious_discovery.md` 中 A001 处理说明到本次任务前状态，或保留历史但标记为 `[BLOCKED]`。
- 回滚后验证：Unity 编译不再引用 `AmbitiousExperienceHub`，Game 场景无 `Ambitious_A001_` 前缀对象，ResourcesLocal 无 A001 Prefab。

## 结果区

- 最终状态：`[DONE]`
- 已完成内容：
  - 运行时自动体验中枢：进入 `Game` 场景后自动创建 `Ambitious_A001_ExperienceHub_Runtime` 和独立 Canvas。
  - 实时 HUD：显示时长、击杀、当前连击、最高连击、暴击、伤害、承伤、经验、收集、工人任务、玩家死亡、工人死亡。
  - 状态条：显示玩家 HP、MP 和环境灵气。
  - 波次状态：显示当前波次、休息/战斗状态、完成波次、存活敌人、难度倍率。
  - 实时评分预览：复用 `SessionResultData.FromSnapshot` 显示评分、星级、评级和连击倍率。
  - 事件流：订阅连击里程碑、连击中断、波次 Tip、结算采集事件。
  - 结算面板：订阅 `SessionResultManager.OnResultCaptured`，死亡或通关采集后自动显示结算面板。
  - Editor 菜单：`Tools/Agent/Ambitious/Experience Hub/` 支持安装 Game 场景根节点、生成 ResourcesLocal Prefab、当前场景预览和移除根节点。
- 修改文件：
  - 新增：`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`
  - 新增：`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs.meta`
  - 新增：`Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs`
  - 新增：`Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs.meta`
  - 新增：`Agent/Reports/ambitious_discovery.md`
  - 新增：`Agent/Reports/ambitious_discovery.md.meta`
  - 新增：`Agent/Reports/2026-05-07.meta`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub.meta`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/task_ambitious_A001_Experience_Hub.md`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/task_ambitious_A001_Experience_Hub.md.meta`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/rollback_ambitious_A001.md`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/rollback_ambitious_A001.md.meta`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/validation_ambitious_A001.md`
  - 新增：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/validation_ambitious_A001.md.meta`
  - 修改：`Agent/Reports/ambitious_discovery.md`（A001 状态回写）
- 新增游戏体验能力：
  - 玩家能持续看到战斗/收集/工人/波次/评分的统一态势。
  - 连击和波次事件会进入最近反馈流，并有中央连击强调文字。
  - 自动结算结果不再只停留在日志或 Editor 菜单，而是以游戏内面板展示。
  - 后续可用 Editor 菜单生成完整 ResourcesLocal UI Prefab。
- 玩家侧效果：战斗中目标更清晰，连击收益更有存在感，波次状态和结算评价更直观。
- UI 生成位置：
  - 是否已写入 `Game.unity`：否。原因是场景 YAML 和 Canvas 层级复杂，直接手写风险高。
  - 是否已创建 `ResourcesLocal` Prefab：否。原因是不手写 Prefab YAML，避免脚本 GUID 和 UI 组件序列化风险。
  - 是否改用 Editor 工具：是。Unity 中执行菜单即可生成场景根节点或完整 Prefab。
  - 是否改用运行时代码动态创建：是。默认在 `Game` 场景自动创建完整 UI，不需要人工接入。
  - 哪些 UI 部分仍需人工接入：无必需项；仅美术资源替换、布局微调、Prefab 固化为可选后续。
- 开发侧接入方式：
  - 默认无需挂载，运行时通过 `RuntimeInitializeOnLoadMethod` 在 `Game` 场景自动创建。
  - 如需显式场景节点，使用 `Tools/Agent/Ambitious/Experience Hub/Install Runtime Root In Game Scene`。
  - 如需资源化 Prefab，使用 `Tools/Agent/Ambitious/Experience Hub/Create Prefab In ResourcesLocal`。
- 验证结果：
  - `git diff --check` 通过。
  - `Scenes/Game.unity`、`ResourcesLocal`、已有 `ForegroundPanel`、`PlayerStatusUI`、`GameplayStatsUI` 无 diff。
  - 新增脚本和报告 `.meta` 均存在。
  - `rg` 静态检查确认菜单路径、运行时初始化、事件订阅和 A001 报告路径存在。
  - Unity 编译和 Play Mode 未在当前环境运行。
- 验证记录路径：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/validation_ambitious_A001.md`
- 回滚方案验证：已静态验证回滚路径；由于未直接改 Scene/Prefab，删除新增脚本和可选 A001 根节点/Prefab 即可回滚。
- 未完成项：
  - 未执行 Unity Editor 编译。
  - 未执行 Play Mode 验证。
  - 未实际点击 Editor 菜单生成 ResourcesLocal Prefab。
- 剩余风险：
  - 运行时自动 Canvas 排序值为 900，可能需按实际项目 UI 层级微调。
  - 动态 UI 使用 Unity 默认 Arial 字体，正式美术表现可后续替换为项目像素字体。
  - 首次 Play Mode 需确认旧版 Unity UI 包和输入模块可用。
- 后续建议：
  - 在 Unity Play Mode 中验证 HUD 与结算面板显示。
  - 使用 Editor 菜单生成 ResourcesLocal Prefab 后，将其加入团队资源流程。
  - 后续可接入音效、动画、成就红点和美术图标。
