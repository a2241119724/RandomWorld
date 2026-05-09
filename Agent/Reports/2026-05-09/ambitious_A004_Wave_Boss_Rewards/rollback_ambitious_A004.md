# A004 回滚方案

## 回滚目标

撤销“波次Boss与波间奖励系统”全部新增能力，恢复到 F002/F010 后的普通波次系统状态。

## 文件级回滚

删除以下新增文件及对应 `.meta`：

- `Scripts/2D/Enum/WavePhaseType.cs`
- `Scripts/2D/Enum/WaveRewardType.cs`
- `Scripts/2D/Constant/WaveBossRewardConstant.cs`
- `Scripts/2D/Tool/WaveBossRewardTool.cs`
- `Scripts/2D/Gameplay/WaveBossRewardManager.cs`
- `Scripts/2D/UI/WaveBossRewardPanel.cs`
- `Scripts/2D/Editor/WaveBossRewardMenu.cs`

回退以下已有文件中的 A004 接入点：

- `Scripts/2D/Gameplay/WaveManager.cs`
- `Scripts/2D/Character/Character.cs`
- `Scripts/2D/Character/Player/Player.cs`

## Scene / UI 回滚

- 本次不直接手写 `Scenes/Game.unity`。
- 若在 Unity 中通过菜单安装过 UI，执行：
  - `工具/智能体/波次Boss奖励/从当前场景移除奖励面板`
- 也可手动删除场景对象：
  - `Ambitious_A004_WaveBossReward_Canvas`
  - `Ambitious_A004_WaveBossReward_Root`

## 资源与存档回滚

- 本次不修改 `Resources/SO`、`ResourcesLocal` Prefab、`StreamingAssets`、Addressables 或存档结构。
- 回滚不需要迁移旧档。
- 奖励 Buff 为运行时内存状态，退出 Play Mode 后自动消失。

## 回滚后验证

1. 执行静态检查：`git diff --check`。
2. 在 Unity 中进入 Play Mode。
3. 通过 `工具/波次管理/开始波次` 启动普通波次。
4. 确认敌人正常生成，波次提示仍由 `WaveEventFeedback` 提供。
5. 确认没有 `WaveBossReward` 相关 UI、Tip 或日志残留。
