# Validation Record — F003 玩家死亡惩罚与重生延迟系统

## 验证概要

- 候选ID：F003
- 功能名称：玩家死亡惩罚与重生延迟系统（含随机位置重生、死亡界面、30%HP恢复）
- 验证时间：2026-04-27（更新）
- 验证方式：静态代码检查（本环境无法运行 Unity）

## 验证维度

### 1. 编译验证

- **Unity API 使用正确性**：确认
  - `Time.realtimeSinceStartup` — 用于死亡计时器，不受 Time.timeScale 影响
  - `Mathf.Max`、`Mathf.RoundToInt`、`Mathf.CeilToInt` — 标准 Mathf API
  - `TileMap.Instance.GenCanReachPos()` — 生成随机可到达地图位置，CharacterCreator.Create() 已有先例
  - `TileMap.Instance.MapPosToWorldPos()` — 地图坐标转世界坐标，全项目通用
  - `PanelController.Instance.Show()/Close()` — 面板栈管理，PauseMenuPanel 已有先例
  - `ABasePanel<DeathMenuPanel>` — 继承链正确，与 PauseMenuPanel 模式一致
- **命名空间一致性**：确认
  - 所有文件使用 `namespace LAB2D`
- **方法签名匹配**：确认
  - `TryCompleteRespawn(Player)` → 返回 bool，由 Player.Update() 每帧调用
  - `UpdateDeathScreen()` → void，由 Player.Update() 在死亡期间每帧调用
  - `HandlePlayerDeath(Player)` → void，由 Player.Death() 调用

### 2. 静态代码检查

#### DeathPenaltyManager.cs
- **随机位置重生**：
  - 使用 `TileMap.Instance.GenCanReachPos()` 获取随机可到达地图坐标
  - 使用 `TileMap.Instance.MapPosToWorldPos()` 转换为世界坐标
  - 直接设置 `player.transform.position` 移动玩家
  - 这两个方法在 CharacterCreator.Create() 中已有成熟使用先例
- **死亡界面管理**：
  - `ShowDeathScreen()` 尝试显示 DeathMenuPanel，失败时降级为 ShowTip
  - `HideDeathScreen()` 仅在面板是栈顶时安全关闭
  - `UpdateDeathScreen()` 每帧更新倒计时文本
  - `TryAccessDeathPanel()` 统一使用 try-catch 保护，防止 Prefab 缺失导致崩溃
  - `deathPanelState` 状态机：0=未检测, 1=可用, -1=不可用（降级模式）
- **HP 恢复**：`HpRestorePercent` 默认值改为 0.3f（30%）
- **空引用保护**：HandlePlayerDeath、TryCompleteRespawn 对 player 参数做 null 检查
- **边界条件**：经验值扣除后不低于 0；死亡截止时间基于 realtimeSinceStartup

#### DeathMenuPanel.cs
- **Panel 模式一致性**：
  - 继承 `ABasePanel<DeathMenuPanel>`，与 PauseMenuPanel 完全一致的模式
  - 构造函数设置 `this.Name = "DeathMenu"` 并调用 `this.Init()`
  - 使用 `Tool.GetComponentInChildren<Text>()` 查找子 Text 组件
- **组件查找**：在 Panel 非 null 时查找 "Countdown" 和 "DeathCount" Text
- **OnEnter**：显示时更新死亡计数文本
- **UpdateCountdown**：更新倒计时文本，对 null countdownText 做了保护
- **OnClick_Back**：空实现，阻止 ESC 关闭死亡界面
- **降级兼容**：当 Prefab 不存在时，Init() 可能失败，DeathPenaltyManager 的 TryAccessDeathPanel 会捕获异常并降级到 Tip 模式

#### Player.cs
- **Update()**：死亡期间调用 UpdateDeathScreen() + TryCompleteRespawn()，重生完成时更新 UI；非死亡期间正常攻击
- **FixedUpdate()**：死亡期间跳过 Move()，保留网络检查和材质设置
- **ReduceHp()**：死亡期间直接 return 免疫伤害
- **Death()**：HP=1 防重入 + HandlePlayerDeath

#### PrefabConstant.cs
- 添加 `DEATH_MENU = "DeathMenu"` 常量，遵循现有命名规范

### 3. 风险边界检查

| 风险项 | 结论 | 说明 |
|---|---|---|
| 是否修改 Scene | 否 | 未修改任何 .unity 文件 |
| 是否修改 Prefab | 否 | DeathMenuPanel 需要新 Prefab，但缺失时自动降级，不修改现有 Prefab |
| 是否修改 SO | 否 | 未修改任何 ScriptableObject |
| 是否修改存档结构 | 否 | DeathPenaltyManager 是运行时单例，不序列化 |
| 是否修改 Photon 同步 | 否 | 死亡惩罚仅影响本地玩家 |
| 是否修改 AB/StreamingAssets | 否 | 仅添加 PrefabConstant，不修改 AB 配置 |
| 是否引入循环引用 | 否 | Player → DeathPenaltyManager → TileMap（单向） |
| 是否影响旧档兼容 | 否 | 无存档字段变更 |
| 是否影响网络同步 | 否 | 重生位置仅本地计算，其他玩家位置由 Photon 各自管理 |

### 4. 功能边界检查

| 功能点 | 实现状态 | 说明 |
|---|---|---|
| 重生延迟 | 已实现 | 默认 3 秒，可配置 RespawnDelaySeconds |
| 随机位置重生 | 已实现 | 通过 TileMap.GenCanReachPos() 在地图全范围随机选择可到达空位 |
| 经验惩罚 | 已实现 | 默认 10%，可配置 ExperienceLossPercent |
| 30% HP 恢复 | 已实现 | HpRestorePercent 默认 0.3f，可配置 |
| MP 完全恢复 | 已实现 | 重生时恢复至 MaxMp |
| 死亡界面显示 | 已实现 | DeathMenuPanel（需 Prefab），降级为 ShowTip |
| 死亡界面倒计时 | 已实现 | UpdateDeathScreen 每帧更新 |
| 死亡计数显示 | 已实现 | DeathMenuPanel.OnEnter 更新死亡计数 |
| ESC 阻止关闭 | 已实现 | DeathMenuPanel.OnClick_Back 空实现 |
| 重生无敌 | 已实现 | Player.ReduceHp 门控 |
| 行动限制 | 已实现 | Update + FixedUpdate 门控 |
| UI 状态更新 | 已实现 | 重生完成时更新 PlayerStatusUI |

### 5. 降级行为验证

当 DeathMenu Prefab 不存在时：
1. `ShowDeathScreen()` 调用 `TryAccessDeathPanel()`，构造函数异常被 catch → `deathPanelState = -1` → 降级为 `ShowTip("Died. Respawning in 3s...")`
2. `UpdateDeathScreen()` 检测到 `deathPanelState = -1` → 跳过 DeathMenuPanel 更新
3. `TryCompleteRespawn()` 调用 `HideDeathScreen()` → `deathPanelState` 重置为 0 → `ShowTip("Respawned!")`
4. **结论**：降级路径完整，游戏不会因缺失 Prefab 而崩溃

## 验证结果

- **静态验证**：通过
  - 命名空间正确
  - Unity API 使用正确
  - 继承链正确（ABasePanel → DeathMenuPanel 与 PauseMenuPanel 模式一致）
  - 空引用保护到位
  - 降级路径完整
  - 与现有代码风格一致
- **编译验证**：待人工在 Unity 中完成
- **Play Mode 验证**：待人工在 Unity 中完成

## 未验证项

1. Unity 编译：本环境无法运行 Unity Editor，需人工编译确认
2. Play Mode 端到端：需人工测试死亡→死亡界面显示→3秒倒计时→随机位置重生→HP30%恢复→UI更新
3. DeathMenu Prefab：需人工在 Unity 中创建并加入 AssetBundle
4. GenCanReachPos 在极小地图上的边界行为
5. 联机时其他玩家死亡不受惩罚影响

## 人工接入清单

1. **创建 DeathMenu Prefab**（必须）：
   - 在 Unity 中创建 Canvas Prefab，命名为 "DeathMenu"
   - 添加深色半透明 Image 作为背景遮罩
   - 添加 Text 子对象命名为 "Countdown"（用于显示倒计时）
   - 添加 Text 子对象命名为 "DeathCount"（用于显示死亡次数）
   - 将 Prefab 加入 AssetBundle（与 PauseMenu Prefab 同级）
2. **验证编译通过**
3. **Play Mode 测试死亡流程**
