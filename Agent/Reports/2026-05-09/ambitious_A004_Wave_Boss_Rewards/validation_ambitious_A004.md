# A004 波次Boss与波间奖励系统验证记录

## 验证结论

- 最终状态：[DONE]
- 静态验证：通过。
- Unity 编译：未执行，当前命令行环境只有 .NET Runtime，没有 .NET SDK，无法替代 Unity Editor 编译。
- Play Mode：未执行，当前 shell 环境未启动 Unity Editor。
- 风险判断：功能高价值但运行时代码低侵入；未触碰 Photon、存档结构、ScriptableObject、StreamingAssets、Addressables 或既有 Prefab 本体。

## 新增运行时业务脚本验证

- `Scripts/2D/Gameplay/WaveBossRewardManager.cs`
  - 命名空间：`LAB2D`
  - 类名：`WaveBossRewardManager`、`WaveRewardOption`、`WaveBossRewardState`
  - 单例模式：`Singleton<WaveBossRewardManager>`，与 `WaveManager`、`ComboBonusManager` 一致。
  - 基础逻辑：订阅 `WaveManager.OnWaveEnd`、`OnRestStart`、`OnAllWavesCleared`；由 `WaveManager` 在波次开始和敌人生成后调用 Boss/缩放接入点。
  - 空引用保护：`ConfigureSpawnedEnemy()` 检查 `GameObject`、`AEnemy`、`EnemyData`；奖励应用检查 `PlayerManager.Instance?.Mine`。
  - 调用边界：只写运行时内存 Buff，不写存档，不同步网络，不修改资源。

- `Scripts/2D/UI/WaveBossRewardPanel.cs`
  - 命名空间：`LAB2D`
  - 类名：`WaveBossRewardPanel`
  - Unity API 使用：`MonoBehaviour`、`CanvasGroup`、`Canvas`、`CanvasScaler`、`GraphicRaycaster`、`EventSystem`、`Text`、`Button`。
  - 基础逻辑：奖励出现时运行时兜底创建独立 Canvas；订阅奖励选项和状态事件；支持按钮与数字键 1/2/3 选择奖励。
  - 空引用保护：缺少字体时回退 Unity 内置字体；缺少面板时自动创建；没有奖励时隐藏面板。

## 修改接入点验证

- `Scripts/2D/Gameplay/WaveManager.cs`
  - 新增 `WaveBossRewardManager.Instance.OnWaveStarted()`：只在波次开始后通知 A004，不改变原事件签名。
  - 新增 `GetEnemyCountForWave()` 委托：保留原基础敌人数计算，A004 只在 Boss 波追加护卫数量。
  - 新增 `ConfigureSpawnedEnemy()`：敌人生成后应用普通难度或 Boss 缩放，不修改 Prefab 本体。

- `Scripts/2D/Character/Character.cs`
  - 新增玩家输出伤害奖励倍率：只在 `attacker is Player` 时生效。
  - 新增玩家受击减伤奖励倍率：只在 `target is Player` 时生效。
  - 原连击倍率和防御计算顺序保留，奖励倍率插入点清晰。

- `Scripts/2D/Character/Player/Player.cs`
  - 新增移动奖励倍率：在天气倍率之后应用，避免覆盖 F012 天气效果。
  - 只影响本地 Player 移动速度计算，不触碰 Photon 同步核心。

## UI / Scene / Prefab 验证

- `Game.unity` 路径：`Scenes/Game.unity`
- 是否直接写入 `Game.unity`：否。
- 不直接写入原因：当前环境无法运行 Unity Editor，手写大型 Scene YAML 和脚本引用风险高。
- 是否创建 `ResourcesLocal` Prefab：否。
- 不创建 Prefab 原因：当前环境无法可靠导入并验证带脚本引用的 Prefab YAML。
- 降级与替代方案：
  - Editor 菜单：`工具/智能体/波次Boss奖励/创建奖励面板到 Game 场景`
  - 运行时动态 UI：奖励出现时自动创建 `Ambitious_A004_WaveBossReward_Canvas` 与 `Ambitious_A004_WaveBossReward_Root`
  - 回滚菜单：`工具/智能体/波次Boss奖励/从当前场景移除奖励面板`

## Tool 验证

- 新增路径：`Scripts/2D/Tool/WaveBossRewardTool.cs`
- 命名空间：`LAB2D`
- 是否引用 `UnityEditor`：否。
- 用途：Boss 波判定、敌人数量修正、普通/Boss 属性倍率、奖励数值、奖励文案、Buff 摘要、上限累加。
- 复用：`WaveBossRewardManager` 调用该工具；移动/伤害倍率继续复用 `WeatherGameplayTool.ApplyMultiplier()`。
- 中文注释：已覆盖用途、参数、返回值和边界。

## Enum 验证

- 新增路径：`Scripts/2D/Enum/WavePhaseType.cs`
  - 语义：Idle、NormalWave、BossWave、RewardSelection、Resting、Completed。
  - 使用方：`WaveBossRewardManager`、`WaveBossRewardState`、UI/Editor 状态展示。
- 新增路径：`Scripts/2D/Enum/WaveRewardType.cs`
  - 语义：Heal、Experience、DamageBoost、DefenseBoost、MoveSpeedBoost。
  - 使用方：`WaveBossRewardTool`、`WaveBossRewardManager`、`WaveRewardOption`。
- 是否重复/冲突：已检查 `PackageTypeEnum.cs`、`WorkerConditionState.cs`，语义不重复。
- 是否修改旧枚举：否。

## Constant 验证

- 新增路径：`Scripts/2D/Constant/WaveBossRewardConstant.cs`
- 分组：菜单路径、Game 场景名、Canvas/Panel 节点名、字体路径、Boss 间隔、属性倍率、奖励数值、Buff 上限、热键。
- 是否重复/冲突：已检查 `PrefabConstant.cs`、`ResourceConstant.cs`、`TagConstant.cs`、`LayerConstant.cs`、`WorkerConditionConstant.cs`，无语义一致常量。
- 是否修改旧常量：否。
- 业务脚本引用：`WaveBossRewardTool`、`WaveBossRewardManager`、`WaveBossRewardPanel`、`WaveBossRewardMenu`。

## Editor 工具验证

- 新增路径：`Scripts/2D/Editor/WaveBossRewardMenu.cs`
- 菜单路径：
  - `工具/智能体/波次Boss奖励/查看当前状态`
  - `工具/智能体/波次Boss奖励/启用系统`
  - `工具/智能体/波次Boss奖励/禁用系统`
  - `工具/智能体/波次Boss奖励/启用提示`
  - `工具/智能体/波次Boss奖励/禁用提示`
  - `工具/智能体/波次Boss奖励/调试/生成普通波奖励`
  - `工具/智能体/波次Boss奖励/调试/生成Boss波奖励`
  - `工具/智能体/波次Boss奖励/创建奖励面板到 Game 场景`
  - `工具/智能体/波次Boss奖励/从当前场景移除奖励面板`
- Editor 隔离：`UnityEditor` 仅出现在 `Scripts/2D/Editor/WaveBossRewardMenu.cs`。

## 静态命令记录

- `rg -n "WaveBossReward|WavePhaseType|WaveRewardType" Scripts/2D Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards`
  - 结果：确认新增脚本、任务卡、回滚方案和接入点可定位。
- `rg -n "using UnityEditor|UnityEditor\\." Scripts/2D/Enum Scripts/2D/Constant Scripts/2D/Tool Scripts/2D/Gameplay Scripts/2D/UI`
  - 结果：A004 新增运行时代码未引用 `UnityEditor`；项目既有 `PauseMenuPanel.cs` 存在旧引用，不属于本次改动。
- `git diff --check`
  - 结果：未发现空白错误；输出了既有 CRLF 提醒。
- `dotnet build ..\Assembly-CSharp.csproj --no-restore`
  - 结果：失败，原因是当前环境未安装 .NET SDK，仅有 .NET Runtime。

## `.meta` 验证

以下新增文件均已同步 `.meta`：

- `WavePhaseType.cs.meta`
- `WaveRewardType.cs.meta`
- `WaveBossRewardConstant.cs.meta`
- `WaveBossRewardTool.cs.meta`
- `WaveBossRewardManager.cs.meta`
- `WaveBossRewardPanel.cs.meta`
- `WaveBossRewardMenu.cs.meta`
- 任务卡、验证记录、回滚方案及任务目录 `.meta`

## 人工复验步骤

1. 打开 Unity Editor。
2. 进入 `Scenes/Game.unity`。
3. 通过 `工具/智能体/波次Boss奖励/创建奖励面板到 Game 场景` 安装面板，或跳过安装验证运行时动态 UI。
4. 进入 Play Mode。
5. 通过 `工具/波次管理/开始波次` 启动波次。
6. 清理第 1、2 波，确认普通波奖励出现，按钮和数字键可选择。
7. 第 3 波确认 Boss 敌人名称、体型、颜色和属性明显变化。
8. 击败 Boss 后确认 Boss 奖励选项出现，选择后观察伤害、减伤或移动 Buff 摘要变化。
9. 通过 `工具/智能体/波次Boss奖励/从当前场景移除奖励面板` 验证 UI 回滚。

## 剩余风险

- Boss 生命/攻击/防御倍率需要 Play Mode 手感微调。
- 奖励上限与波间选择节奏需要结合实际波间休息时间验证。
- 运行时动态 UI 的屏幕位置、字体大小和按钮尺寸需要在不同分辨率下观察。
- 联机房间下 Boss 属性缩放未做 Photon 权威同步验证，本次刻意不改 Photon 核心。
