# Agent Task Card — F003 玩家死亡惩罚与重生延迟系统

## 基本信息

- 任务 ID：feature_F003_DeathPenalty
- 候选ID：F003
- 创建时间：2026-04-27
- 提出人：ProjectDirectorAgent（自动发现）
- 当前状态：Done
- 风险等级：Low
- 本次任务目录：Agent/Reports/2026-04-27/feature_F003_DeathPenalty/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 |
|---|---|---|---|---|
| [TODO] | F003 | 玩家死亡惩罚与重生延迟系统 | 玩家体验 | Player.Death() 仅设 HP=100，无任何惩罚、延迟或反馈 |

## 用户需求

> 自动发现：Player.Death() 目前仅设置 HP=100 并记录统计，没有任何死亡惩罚（经验损失、重生延迟）或反馈。玩家死亡在游戏中无实际意义。需要新增死亡惩罚机制，使死亡有代价，增加生存压力感。

## 主 Agent 分析

- 任务分类：gameplay_feature（玩家体验/死亡惩罚）
- 游戏业务类型：玩家体验
- 玩家价值：增加生存压力和决策意义，使死亡有实际游戏意义
- 开发价值：为后续复活道具、安全区、死亡保护等扩展提供基础
- 目标模块：Player、DeathPenaltyManager（新增）、GameplaySessionStats
- 主要影响路径：
  - `Scripts/2D/Character/Player/Player.cs` — Death()、Update()、FixedUpdate()、ReduceHp()
  - `Scripts/2D/Gameplay/DeathPenaltyManager.cs` — 新增，死亡惩罚管理器
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — 只读引用（RecordPlayerDeath 已由 F001 接入）
  - `Scripts/2D/UI/Panel/PanelUI/PlayerStatusUI.cs` — 只读引用（UI 更新）
- 不应触碰的路径：
  - `Resources/SO`、`Resources/Tilemap`、`Resources/Images`
  - `Scenes`、`StreamingAssets`、`AddressableAssetsData`
  - `Scripts/2D/Data/`、`Scripts/2D/Manager/ArchiveManager.cs`
  - `Scripts/2D/NetworkConnect.cs`、Photon 同步逻辑
- 负责 Agent：GameplayAgent
- 需要的 Skill：ScriptGenerateSkill
- 需要确认的问题：无

## 子 Agent 分工

| 子 Agent | 职责 | 输入 | 输出 |
|---|---|---|---|
| GameplayAgent | 创建 DeathPenaltyManager，修改 Player 死亡逻辑 | Player.Death() 当前实现 | DeathPenaltyManager.cs + Player.cs 修改 |

## Skill 调用计划

| Skill | 调用原因 | 输入 | 预期输出 |
|---|---|---|---|
| ScriptGenerateSkill | 生成死亡惩罚管理器脚本 | 功能需求和现有 Player API | DeathPenaltyManager.cs |

## 上下文快照

- 相关脚本：
  - `Scripts/2D/Character/Player/Player.cs` — Death()（仅设HP=100）、Update()、FixedUpdate()、ReduceHp()
  - `Scripts/2D/Character/Character.cs` — ReduceHp() 基类实现、Death() 基类（Destroy）
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — RecordPlayerDeath()（已由 F001 接入）
  - `Scripts/2D/UI/Panel/PanelUI/PlayerStatusUI.cs` — UpdatePlayerState()
  - `Scripts/2D/Singleton.cs` — Singleton<T> 基类
- 相关资源：无
- 相关场景：无
- 相关配置：无

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否（DeathPenaltyManager 是运行时单例，不保存数据）
- 是否涉及 Photon/网络同步：否（死亡惩罚仅影响本地玩家）
- 是否需要兼容旧数据：否
- 风险等级：低

## 业务规则说明

1. **重生延迟**：玩家死亡后需等待 N 秒（默认 3 秒）才能重生，期间无法移动、攻击或受伤害
2. **经验惩罚**：死亡时损失当前经验值的一定比例（默认 10%），最低降至 0
3. **重生恢复**：重生时 HP 和 MP 恢复至满值
4. **无敌保护**：重生延迟期间玩家处于无敌状态，不受任何伤害
5. **死亡提示**：死亡和重生时通过全局 Tip 系统显示提示信息
6. **会话统计**：死亡次数统计由 GameplaySessionStats 负责（F001 已完成接入）

## 数据流说明

```
Enemy/Environment deals damage
  -> Character.ReduceHp(hp, attacker)     // 受击
  -> Player.ReduceHp() gate               // 重生期间免疫伤害
  -> HP <= 0 triggers Death()
  
Player.Death()
  -> GameplaySessionStats.RecordPlayerDeath()  // 统计死亡（F001）
  -> HP = 1                                    // 保底存活值，防重入
  -> DeathPenaltyManager.HandlePlayerDeath()   // 应用惩罚
  
DeathPenaltyManager.HandlePlayerDeath()
  -> 计算并扣除经验惩罚
  -> 设置重生截止时间 (Time.realtimeSinceStartup + delay)
  -> 显示 Tip 提示

Player.Update() / Player.FixedUpdate()
  -> 检查 IsRespawning -> 阻止攻击和移动
  -> 检查 TryCompleteRespawn() -> 完成重生时恢复 HP/MP 并更新 UI
```

## 执行步骤

1. 创建 `Scripts/2D/Gameplay/DeathPenaltyManager.cs` — 非 MonoBehaviour 单例，管理重生延迟、经验惩罚和状态查询
2. 修改 `Player.Death()` — 调用 DeathPenaltyManager.HandlePlayerDeath(this)，设置 HP=1 防重入
3. 修改 `Player.Update()` — 检查 TryCompleteRespawn 并在重生完成时更新 UI；重生期间阻止攻击
4. 修改 `Player.FixedUpdate()` — 重生期间阻止移动
5. 修改 `Player.ReduceHp()` — 重生期间免疫伤害

## 验证步骤

1. 编译验证：通过静态代码检查确认 Unity API 使用正确、命名空间一致、方法签名匹配
2. 静态检查：验证 DeathPenaltyManager 的 Singleton 继承、属性访问、Player 调用一致性
3. Play Mode 验证：需要人工在 Unity 中测试死亡→等待重生→重生完成流程

## 回滚方案

- 回滚路径：删除 DeathPenaltyManager.cs，还原 Player.cs 的 Death()、Update()、FixedUpdate()、ReduceHp() 修改
- 回滚顺序：直接 revert 2 个文件
- 需要保留的数据：无
- 回滚后验证：编译通过，Player.Death() 恢复为原 HP=100 行为

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  1. 创建 DeathPenaltyManager 运行时单例，管理重生延迟、经验惩罚、随机位置重生、死亡界面和状态查询
  2. 创建 DeathMenuPanel（遵循 ABasePanel 模式），显示死亡界面、倒计时和死亡计数
  3. 修改 Player.Death() 接入死亡惩罚，HP 设为 1 防止重入
  4. 修改 Player.Update() 在重生期间显示死亡界面倒计时、阻止攻击，重生完成时恢复 HP/MP 并更新 UI
  5. 修改 Player.FixedUpdate() 在重生期间阻止移动
  6. 修改 Player.ReduceHp() 在重生期间免疫伤害
  7. 添加 DEATH_MENU 常量到 PrefabConstant
- 修改的文件：
  - `Scripts/2D/Gameplay/DeathPenaltyManager.cs` — 新增（随机位置重生 + 死亡界面管理 + HP30%恢复）
  - `Scripts/2D/UI/Panel/DeathMenuPanel.cs` — 新增（死亡界面面板）
  - `Scripts/2D/Constant/PrefabConstant.cs` — 添加 DEATH_MENU 常量
  - `Scripts/2D/Character/Player/Player.cs` — Death()、Update()、FixedUpdate()、ReduceHp()
- 新增的游戏业务能力：
  - **重生延迟**：玩家死亡后等待可配置秒数（默认 3 秒）才能重生
  - **随机位置重生**：重生时在地图上随机选择一个可到达的空位作为出生点（通过 TileMap.GenCanReachPos）
  - **经验惩罚**：死亡时损失当前经验值百分比（默认 10%）
  - **死亡界面**：死亡时显示 DeathMenu 面板（如 Prefab 存在），展示倒计时和死亡计数，ESC 无法关闭；如 Prefab 不存在则降级为 ShowTip 提示
  - **重生无敌**：等待期间免疫所有伤害
  - **行动限制**：等待期间禁止移动和攻击
  - **30% HP 恢复**：重生时 HP 恢复至最大值的 30%（MP 完全恢复）
  - **死亡提示**：通过死亡界面或全局 Tip 显示死亡和重生信息
- 玩家侧效果：
  - 死亡后进入死亡界面等待 3 秒倒计时
  - 重生后随机出现在地图上新的可到达位置
  - 死亡损失 10% 当前经验值，重生仅恢复 30% HP
  - 死亡界面无法通过 ESC 跳过
  - 等待期间完全无敌，无法移动和攻击
- 开发侧接入方式：
  - DeathPenaltyManager.Instance 是全局单例，可配置 RespawnDelaySeconds、ExperienceLossPercent、HpRestorePercent
  - UpdateDeathScreen() 每帧更新死亡界面倒计时，自动降级到 Tip 模式
  - DeathMenuPanel 需在 Unity 中创建名为 "DeathMenu" 的 Prefab（含 Countdown/DeathCount Text 子对象），并加入 AssetBundle
  - 无需场景挂载、无需修改 SO/存档/Photon
- 验证结果：静态验证通过
- 验证记录路径：Agent/Reports/2026-04-27/feature_F003_DeathPenalty/validation_feature_F003.md
- 未完成项：
  - DeathMenu Prefab 需人工在 Unity 中创建并加入 AssetBundle
- 剩余风险：
  - Play Mode 端到端验证需人工在 Unity 中完成
  - DeathMenu Prefab 创建前，死亡界面降级为 Tip 文本提示
  - 联机模式下其他玩家的死亡不受此系统影响（仅本地玩家死亡有惩罚）
  - GenCanReachPos() 在极小地图上可能找不到可用位置（极端情况）
- 后续建议：
  - 创建 DeathMenu Prefab：需包含 Canvas、深色遮罩 Image、"Countdown" Text、"DeathCount" Text 子对象
  - 可在 DeathMenu Prefab 中添加"立即复活"按钮（消耗道具）
  - 可增加死亡位置墓碑标记
  - 可基于死亡次数接入成就系统
