# Agent Task Card — F001 玩家战斗数据统计与连击反馈系统

## 基本信息

- 任务 ID：feature_F001_CombatStats
- 候选ID：F001
- 创建时间：2026-04-26
- 提出人：ProjectDirectorAgent（自动发现）
- 当前状态：Running
- 风险等级：Low
- 本次任务目录：Agent/Reports/2026-04-26/feature_F001_CombatStats/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 |
|---|---|---|---|---|
| [TODO] | F001 | 玩家战斗数据统计与连击反馈系统 | 战斗反馈 | GameplaySessionStats 已完整实现但从未被调用 |

## 用户需求

> 自动发现：GameplaySessionStats 是完整的运行时会话统计单例，包含击杀计数、连击追踪、伤害统计、暴击统计、玩家死亡计数、经验值统计、物品收集统计等能力，但从未被任何游戏代码调用。需要在现有战斗流程中接入该统计系统，使玩家在战斗中获得即时反馈。

## 主 Agent 分析

- 任务分类：gameplay_feature（战斗反馈/数据统计）
- 游戏业务类型：战斗反馈
- 目标模块：Character、Enemy、Player、GameplaySessionStats
- 主要影响路径：
  - `Scripts/2D/Character/Character.cs` — ReduceHp、AddExperienceValue
  - `Scripts/2D/Character/Enemy/CommonEnemy/State/CommonEnemyDeadState.cs` — OnEnter
  - `Scripts/2D/Character/Enemy/SeekEnemy/State/SeekEnemyDeadState.cs` — OnEnter
  - `Scripts/2D/Character/Player/Player.cs` — Death
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — 已有完整实现，只读引用
  - `Scripts/2D/Editor/` — 新增 Editor 菜单用于验证
- 不应触碰的路径：
  - `Resources/SO`、`Resources/Tilemap`、`Resources/Images`
  - `Scenes`、`StreamingAssets`、`AddressableAssetsData`
  - `Scripts/2D/Data/`、`Scripts/2D/Manager/ArchiveManager.cs`
  - `Scripts/2D/NetworkConnect.cs`、Photon 同步逻辑
- 需要确认的问题：无（所有目标文件已读取并理解）

## 子 Agent 分工

| 子 Agent | 职责 | 输入 | 输出 |
|---|---|---|---|
| GameplayAgent | 在战斗流程中接入统计数据 | Character.ReduceHp、AddExperienceValue、死亡状态 | 修改后的脚本 |
| ToolAgent | 创建 Editor 菜单验证统计 | GameplaySessionStats.BuildSummaryText() | Editor 菜单项 |

## Skill 调用计划

| Skill | 调用原因 | 输入 | 预期输出 |
|---|---|---|---|
| ScriptGenerateSkill | 在现有脚本中添加统计调用 | 现有类和方法签名 | 修改后的调用代码 |

## 上下文快照

- 相关脚本：
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — 完整统计单例，包含 RecordDamageDealt、RecordDamageTaken、RecordEnemyDefeated、RecordExperienceGained、RecordPlayerDeath、RecordWorkerDeath、RecordItemCollected、RecordWorkerTaskCompleted、BuildSummaryText
  - `Scripts/2D/Character/Character.cs` — ReduceHp(hp, attacker, isCRT)、AddExperienceValue(experience)、Death()
  - `Scripts/2D/Character/Player/Player.cs` — Death() override（设HP=100，无惩罚）
  - `Scripts/2D/Character/Enemy/CommonEnemy/State/CommonEnemyDeadState.cs` — OnEnter 中 LastAttacker.AddExperienceValue(5)
  - `Scripts/2D/Character/Enemy/SeekEnemy/State/SeekEnemyDeadState.cs` — OnEnter 中 LastAttacker.AddExperienceValue(5)
- 相关资源：无
- 相关场景：无
- 相关配置：无

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否（GameplaySessionStats 是本地运行时统计）
- 是否需要兼容旧数据：否
- 风险等级：低

## 业务规则说明

1. **伤害统计**：每次角色受伤时，同时记录造成伤害者的伤害输出和受伤者的伤害承受
2. **击杀统计**：敌人死亡时记录击杀（含敌人类型、攻击者类型、经验值奖励）
3. **连击统计**：GameplaySessionStats 内置 4 秒连击超时，连续击杀自动递增 combo
4. **死亡统计**：玩家死亡时记录、重置连击
5. **经验统计**：每次获得经验时记录累计经验值
6. **所有统计仅在本地进行，不同步网络，不保存存档**

## 数据流说明

```
Character.ReduceHp(hp, attacker, isCRT)
  -> GameplaySessionStats.Instance.RecordDamageDealt(hp, isCRT)    // 攻击者输出
  -> GameplaySessionStats.Instance.RecordDamageTaken(actualDamage)  // 受伤者承受

CommonEnemyDeadState.OnEnter() / SeekEnemyDeadState.OnEnter()
  -> GameplaySessionStats.Instance.RecordEnemyDefeated(enemy, attacker, 5)

Player.Death()
  -> GameplaySessionStats.Instance.RecordPlayerDeath()

Character.AddExperienceValue(experience)
  -> GameplaySessionStats.Instance.RecordExperienceGained(experience)
```

## 执行步骤

1. 修改 `Character.cs` 的 `ReduceHp` 方法，在伤害计算后添加 RecordDamageDealt 和 RecordDamageTaken 调用
2. 修改 `Character.cs` 的 `AddExperienceValue` 方法，添加 RecordExperienceGained 调用
3. 修改 `CommonEnemyDeadState.cs` 的 `OnEnter`，添加 RecordEnemyDefeated 调用
4. 修改 `SeekEnemyDeadState.cs` 的 `OnEnter`，添加 RecordEnemyDefeated 调用
5. 修改 `Player.cs` 的 `Death` 方法，添加 RecordPlayerDeath 调用
6. 新增 `Scripts/2D/Editor/GameplayStatsMenu.cs`，添加 Editor 菜单项用于显示会话统计

## 验证步骤

1. 编译验证：确认 Unity 编译无错误（本环境无法运行 Unity，通过静态代码检查验证）
2. 静态检查：验证类名、命名空间、方法签名正确
3. Play Mode 验证：需要人工在 Unity 中进入 Play Mode，击杀敌人后通过 Editor 菜单查看统计

## 回滚方案

- 回滚路径：删除各文件中新增的 GameplaySessionStats 调用行
- 回滚顺序：直接 revert 修改的 5 个文件
- 需要保留的数据：无
- 回滚后验证：编译通过即可

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  1. 在 Character.ReduceHp 中接入 GameplaySessionStats，记录伤害输出和伤害承受
  2. 在 Character.AddExperienceValue 中接入 GameplaySessionStats，记录经验值获取
  3. 在 CommonEnemyDeadState.OnEnter 中接入 RecordEnemyDefeated，记录普通敌人击杀和连击
  4. 在 SeekEnemyDeadState.OnEnter 中接入 RecordEnemyDefeated，记录追踪敌人击杀和连击
  5. 在 Player.Death 中接入 RecordPlayerDeath，记录玩家死亡并重置连击
  6. 新增 Editor 菜单 Tools > Gameplay Stats，可查看和重置会话统计
  7. 修复经验值重复统计问题（RecordEnemyDefeated 的 experienceReward 设为 0）
- 修改的文件：
  - `Scripts/2D/Character/Character.cs` — ReduceHp 添加 RecordDamageDealt/RecordDamageTaken；AddExperienceValue 添加 RecordExperienceGained
  - `Scripts/2D/Character/Player/Player.cs` — Death 添加 RecordPlayerDeath
  - `Scripts/2D/Character/Enemy/CommonEnemy/State/CommonEnemyDeadState.cs` — OnEnter 添加 RecordEnemyDefeated
  - `Scripts/2D/Character/Enemy/SeekEnemy/State/SeekEnemyDeadState.cs` — OnEnter 添加 RecordEnemyDefeated
  - `Scripts/2D/Editor/GameplayStatsMenu.cs` — 新增，Editor 菜单工具
- 新增的游戏业务能力：
  - **战斗伤害统计**：每次攻击造成/承受的伤害自动记录，含暴击标记
  - **敌人击杀计数**：每次击杀自动记录敌人类型、攻击者类型，按类型分组统计
  - **连击追踪系统**：4 秒内连续击杀自动递增 combo，记录最大 combo
  - **暴击统计**：自动记录暴击次数
  - **玩家死亡统计**：自动记录玩家死亡次数，死亡时重置连击
  - **经验值统计**：自动记录累计获得经验值
  - **会话统计查看**：通过 Editor 菜单 Tools > Gameplay Stats 查看完整统计
- 玩家侧效果：
  - 所有战斗行为（攻击、受击、暴击、击杀、死亡）均被自动追踪
  - 连击系统通过 GameplaySessionStats 内部维护，数据已就绪，可随时接入 UI 展示
  - 会话统计数据可通过 Editor 菜单实时查看
- 开发侧接入方式：
  - GameplaySessionStats.Instance 是全局单例，任何脚本可直接调用其 Record* 方法
  - StatsChanged 事件在每次数据变更时触发，可订阅用于 UI 更新
  - BuildSummaryText() 返回格式化文本，可用于 Debug 或日志
  - CreateSnapshot() 返回数据快照，可用于存档、上报或 UI 绑定
- 验证结果：静态验证通过（方法签名、命名空间、空引用保护、边界条件、风险边界全部确认）
- 验证记录路径：Agent/Reports/2026-04-26/feature_F001_CombatStats/validation_feature_F001.md
- 未完成项：无
- 剩余风险：
  - Play Mode 端到端验证需人工在 Unity 中完成
  - 连击超时 4 秒为默认值，如需要调整可通过 GameplaySessionStats.Instance.ComboTimeout 修改
- 后续建议：
  - 可基于 StatsChanged 事件接入 HUD UI 展示实时 combo/hit 数
  - 可基于 CreateSnapshot 实现关卡结束结算面板
  - 可实现 combo 达到阈值时的战斗增益（如伤害加成）
  - 可将统计数据接入存档系统用于跨会话追踪
