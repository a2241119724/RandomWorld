# A004 波次Boss与波间奖励系统任务卡

## 基本信息

- 候选ID：A004
- 原始候选：波次Boss与波间奖励系统（精英波+奖励选择+难度缩放接入）
- 当前状态：[DONE]
- 本次任务目录：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：关卡与玩法 / 波次挑战 / 波间成长奖励
- 玩家价值：让波次从“刷怪计数”升级为有阶段目标、Boss 压迫和局内成长选择的完整循环。
- 开发价值：在 F002/F010 波次基础上补齐 Boss、难度缩放和奖励选择模板，供后续关卡、无尽模式和成就继续扩展。
- 负责 Agent：AINPCAgent + GameplayAgent + UIAgent + ToolAgent
- 需要 Skill：ScriptGenerateSkill、ConfigGenerateSkill、SceneAnalyzeSkill、CodeReviewSkill、TestSkill
- 风险等级：高

## 影响路径

- 新增：`Scripts/2D/Enum/WavePhaseType.cs`
- 新增：`Scripts/2D/Enum/WaveRewardType.cs`
- 新增：`Scripts/2D/Constant/WaveBossRewardConstant.cs`
- 新增：`Scripts/2D/Tool/WaveBossRewardTool.cs`
- 新增：`Scripts/2D/Gameplay/WaveBossRewardManager.cs`
- 新增：`Scripts/2D/UI/WaveBossRewardPanel.cs`
- 新增：`Scripts/2D/Editor/WaveBossRewardMenu.cs`
- 修改：`Scripts/2D/Gameplay/WaveManager.cs`
- 修改：`Scripts/2D/Character/Character.cs`
- 修改：`Scripts/2D/Character/Player/Player.cs`
- 修改：`Agent/Reports/ambitious_discovery.md`

## 不应触碰路径

- `Scripts/2D/NetworkConnect.cs` 与 Photon RPC/同步核心。
- `StreamingAssets/`、`AddressableAssetsData/`、`Build/`。
- 现有 `Resources/SO`、Tile、道具、存档结构。
- 现有敌人 Prefab 变体层级和已有 Scene 核心对象属性。

## 功能边界

- 本次包含：Boss 波判定、Boss 属性放大、普通波难度缩放、波间三选一奖励、奖励 UI、Editor 安装/回滚菜单、运行时动态 UI 兜底。
- 本次不包含：新 Boss 美术资源、新敌人 AI 状态机、Photon 权威同步、掉落表重写、永久存档成长。
- Boss 使用现有敌人 Prefab 放大和属性缩放实现，避免新增高风险资源引用。
- 奖励为本局运行时效果，不新增存档字段；经验和回血即时应用到现有 Player 数据。

## 业务规则说明

- 每 3 波触发一次 Boss 波。
- Boss 波最后一名敌人会被标记为 Boss，体型放大，生命、攻击、防御随波次和难度倍率提升。
- 普通敌人也会套用温和难度缩放，解决 WaveManager 原有难度值只展示不生效的问题。
- 每波结束后生成 3 个奖励选项，Boss 波奖励更强。
- 玩家可通过 UI 按钮或数字键 1/2/3 选择奖励。
- 奖励包括：回血、经验、伤害强化、减伤强化、移动强化。
- 奖励选择 UI 独立于已有 HUD，不阻塞原有面板栈；缺少场景安装时运行时可自动创建独立 Canvas。

## 数据流说明

1. `WaveManager.WaveLoop()` 开始波次时调用 `WaveBossRewardManager.OnWaveStarted()`。
2. `WaveManager` 计算基础敌人数后，通过 `WaveBossRewardManager.GetEnemyCountForWave()` 允许 Boss 波追加守卫敌人。
3. 每个敌人生成后，`WaveBossRewardManager.ConfigureSpawnedEnemy()` 应用普通缩放或 Boss 缩放。
4. `WaveManager.OnWaveEnd` 触发后，`WaveBossRewardManager` 生成三选一奖励并通知 UI。
5. `WaveBossRewardPanel` 订阅奖励事件并展示按钮；玩家选择后调用 `SelectReward()`。
6. `Character.ReduceHp()` 调用奖励管理器应用玩家伤害强化和减伤强化。
7. `Player.Move()` 在天气倍率后继续套用奖励移动倍率。

## UI 接入策略

- 已确认真实 Game 场景：`Scenes/Game.unity`。
- 不直接手写 `Game.unity` 原因：当前环境无法运行 Unity Editor，大型 Scene YAML 与脚本 GUID 手写容易破坏已有 UI 引用。
- 不直接创建 `ResourcesLocal` Prefab 原因：当前环境无法可靠导入并校验带脚本引用的 Prefab YAML。
- 已规划 Editor 菜单：`工具/智能体/波次Boss奖励/创建奖励面板到 Game 场景`，通过 Unity Editor API 安装独立 Canvas/Panel。
- 已规划运行时动态 UI：首次出现奖励时，如果场景没有 `WaveBossRewardPanel`，自动创建独立 Canvas 和奖励面板。
- UI 节点名、菜单路径、文案、尺寸和热键全部放入 `WaveBossRewardConstant`。

## 资源修改清单

- `Game.unity`：不直接修改；仅提供 Editor 菜单在 Unity 内创建或移除独立 UI 根节点。
- `ResourcesLocal` Prefab：不直接创建；保留后续可由 Unity Editor 菜单另存 Prefab 的路径。
- `ScriptableObject`：不修改。
- `StreamingAssets`：不修改。
- `.meta`：所有新增脚本和报告文件同步新增 `.meta`。

## 执行步骤

1. 新增公共枚举：波次阶段、奖励类型。
   - 涉及文件：`Scripts/2D/Enum/WavePhaseType.cs`、`WaveRewardType.cs`
   - 完成标准：枚举中文注释完整，不与现有枚举冲突。
2. 新增公共常量：Boss/奖励数值、菜单、节点、UI 文案。
   - 涉及文件：`Scripts/2D/Constant/WaveBossRewardConstant.cs`
   - 完成标准：业务常量集中，不散落魔法值。
3. 新增公共工具：Boss 波判定、倍率、奖励文案格式化。
   - 涉及文件：`Scripts/2D/Tool/WaveBossRewardTool.cs`
   - 完成标准：不引用 `UnityEditor`，不直接访问场景和存档。
4. 新增业务管理器：Boss 缩放、奖励生成、奖励应用、事件通知。
   - 涉及文件：`Scripts/2D/Gameplay/WaveBossRewardManager.cs`
   - 完成标准：可独立启停，空引用安全，运行时效果不写存档。
5. 修改波次接入点。
   - 涉及文件：`Scripts/2D/Gameplay/WaveManager.cs`
   - 完成标准：只追加委托调用，不重写原波次循环。
6. 修改玩家战斗与移动接入点。
   - 涉及文件：`Scripts/2D/Character/Character.cs`、`Scripts/2D/Character/Player/Player.cs`
   - 完成标准：玩家伤害、减伤、移动奖励生效；禁用管理器后回到 1 倍。
7. 新增 UI 面板和 Editor 菜单。
   - 涉及文件：`Scripts/2D/UI/WaveBossRewardPanel.cs`、`Scripts/2D/Editor/WaveBossRewardMenu.cs`
   - 完成标准：可动态创建、可菜单安装、可回滚移除。
8. 写入验证记录、更新任务卡结果区、回写全局候选报告。

## 验证步骤

- 静态检查新增运行时代码不引用 `UnityEditor`。
- 检查 `WaveManager` 只新增 Boss/奖励接入点，原事件签名保持兼容。
- 检查 `Character` 和 `Player` 接入点只对 Player 奖励生效，不影响敌人/工人默认逻辑。
- 检查 UI 动态创建含 Canvas、EventSystem、CanvasGroup、Text、Button，缺少字体时使用 Unity 默认字体。
- 检查 Editor 菜单仅位于 `Scripts/2D/Editor`。
- 执行 `git diff --check`。
- 若无法运行 Unity 编译/Play Mode，在验证记录说明原因和人工复验步骤。

## 回滚方案

- 删除新增脚本和 `.meta`：
  - `Scripts/2D/Enum/WavePhaseType.cs`
  - `Scripts/2D/Enum/WaveRewardType.cs`
  - `Scripts/2D/Constant/WaveBossRewardConstant.cs`
  - `Scripts/2D/Tool/WaveBossRewardTool.cs`
  - `Scripts/2D/Gameplay/WaveBossRewardManager.cs`
  - `Scripts/2D/UI/WaveBossRewardPanel.cs`
  - `Scripts/2D/Editor/WaveBossRewardMenu.cs`
- 回退 `WaveManager.cs` 中 `WaveBossRewardManager` 的四处接入调用。
- 回退 `Character.cs` 中玩家伤害/减伤奖励倍率调用。
- 回退 `Player.cs` 中移动奖励倍率调用。
- 若已通过菜单创建 UI，在 Unity 中执行 `工具/智能体/波次Boss奖励/从当前场景移除奖励面板`。
- 回滚后运行 `git diff --check` 并在 Unity 中确认普通波次仍可启动。

## Tool 复用策略

- 已检查：`Tool.cs`、`WeatherGameplayTool.cs`、`WorkerConditionTool.cs`、`ResourceTool.cs`、`DateTool.cs`、`DataTool.cs`、`VectorTool.cs`、`SyncDataTool.cs`。
- 计划复用：`Tool.IsUIInputActive()`、`WeatherGameplayTool.ApplyMultiplier()`。
- 计划新增：`WaveBossRewardTool`，承载 Boss 波判定、倍率计算和奖励文案格式化。
- 保留在业务层：敌人实例配置、奖励选项状态、玩家奖励应用、事件派发。

## Enum 复用策略

- 已检查：`PackageTypeEnum.cs`、`WorkerConditionState.cs`。
- 未复用原因：现有枚举语义分别为背包类型、工人状态，与波次阶段和奖励类型不重合。
- 计划新增：`WavePhaseType`、`WaveRewardType`。

## Constant 复用策略

- 已检查：`PrefabConstant.cs`、`ResourceConstant.cs`、`TagConstant.cs`、`LayerConstant.cs`、`WorkerConditionConstant.cs`。
- 计划复用：字体路径风格、像素 UI 主题色。
- 计划新增：`WaveBossRewardConstant`，集中维护菜单路径、节点名、默认数值、奖励文案、热键。

## UnityEditor API 边界

- `Scripts/2D/Tool`、`Scripts/2D/Gameplay`、`Scripts/2D/UI` 不引用 `UnityEditor`。
- 仅 `Scripts/2D/Editor/WaveBossRewardMenu.cs` 使用 `UnityEditor` 和 `UnityEditor.SceneManagement`。

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  - Boss 波判定：每 3 波触发一次 Boss 波。
  - 敌人难度缩放：普通敌人按波次和 `WaveManager.CurrentDifficultyScale` 提升生命、攻击和防御。
  - Boss 生成：Boss 波最后一名敌人放大体型、强化属性、调整颜色和名称。
  - 波间奖励：每波结束生成 3 个不重复奖励选项。
  - 奖励应用：回血、经验、伤害强化、减伤强化、移动强化。
  - UI 表现：新增运行时动态奖励面板，支持按钮和数字键 1/2/3。
  - Editor 工具：新增 Game 场景奖励面板安装、移除、调试生成奖励和系统开关菜单。
- 新增文件：
  - `Scripts/2D/Enum/WavePhaseType.cs`
  - `Scripts/2D/Enum/WaveRewardType.cs`
  - `Scripts/2D/Constant/WaveBossRewardConstant.cs`
  - `Scripts/2D/Tool/WaveBossRewardTool.cs`
  - `Scripts/2D/Gameplay/WaveBossRewardManager.cs`
  - `Scripts/2D/UI/WaveBossRewardPanel.cs`
  - `Scripts/2D/Editor/WaveBossRewardMenu.cs`
  - 上述 `.meta`
  - `Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/validation_ambitious_A004.md`
  - `Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/rollback_ambitious_A004.md`
- 修改文件：
  - `Scripts/2D/Gameplay/WaveManager.cs`
  - `Scripts/2D/Character/Character.cs`
  - `Scripts/2D/Character/Player/Player.cs`
  - `Agent/Reports/ambitious_discovery.md`
- 新增游戏体验能力：波次阶段目标、Boss 压迫、波间成长选择、本局 Buff 叠加、可见奖励面板。
- 玩家侧效果：玩家会在每波结束后选择奖励，第 3/6/9 波等遇到更强 Boss，击败后获得更高价值奖励。
- UI 生成位置：
  - 是否写入 `Game.unity`：未直接手写。
  - 是否创建 `ResourcesLocal` Prefab：否。
  - 是否改用 Editor 工具：是，菜单 `工具/智能体/波次Boss奖励/创建奖励面板到 Game 场景`。
  - 是否改用运行时代码动态创建：是，奖励出现时自动创建独立 Canvas。
  - 仍需人工接入：不强制；建议在 Unity 中通过菜单生成场景面板并调整位置。
- 开发侧接入方式：
  - 使用 `WaveManager.Instance.StartWaves()` 启动波次后自动生效。
  - 可通过 `WaveBossRewardManager.Instance.Enable()` / `Disable()` 开关系统。
  - 可通过 Editor 菜单调试生成普通波或 Boss 波奖励。
- 验证结果：静态验证通过；`.meta` 已同步；运行时代码未新增 `UnityEditor` 引用；`git diff --check` 通过但有 CRLF 提醒；Unity 编译和 Play Mode 未运行。
- 验证记录路径：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/validation_ambitious_A004.md`
- 回滚方案验证：已静态验证回滚路径，未执行实际回滚。
- 未完成项：未在 Unity Editor 中执行菜单安装和 Play Mode 行为验证。
- 剩余风险：Boss 数值、奖励上限、UI 尺寸和联机场景同步表现需要人工 Play Mode 调优。
- `Scripts/2D/Tool`：
  - 复用：`Tool.IsUIInputActive()`、`WeatherGameplayTool.ApplyMultiplier()`。
  - 新增：`Scripts/2D/Tool/WaveBossRewardTool.cs`，用于 Boss 波判定、倍率计算、奖励文案和 Buff 上限。
  - 是否涉及 `UnityEditor`：否。
  - 未抽取重复逻辑：未发现本次必须额外抽取的重复工具逻辑。
- `Scripts/2D/Enum`：
  - 新增：`Scripts/2D/Enum/WavePhaseType.cs`、`Scripts/2D/Enum/WaveRewardType.cs`。
  - 复用说明：既有 `PackageTypeEnum`、`WorkerConditionState` 语义不重合。
  - 未抽取重复枚举：未发现。
- `Scripts/2D/Constant`：
  - 新增：`Scripts/2D/Constant/WaveBossRewardConstant.cs`。
  - 复用说明：沿用项目字体路径和 `PixelUITheme`，Boss/奖励常量独立分组。
  - 未抽取重复常量：未发现。
