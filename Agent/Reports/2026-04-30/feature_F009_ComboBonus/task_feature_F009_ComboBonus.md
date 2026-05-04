# Agent Task Card — F009 连击增益奖励系统

## 基本信息

- 任务 ID：feature_F009_ComboBonus
- 候选ID：F009
- 创建时间：2026-04-30
- 提出人：ProjectDirectorAgent（自动发现）
- 当前状态：Running
- 风险等级：Low
- 本次任务目录：Agent/Reports/2026-04-30/feature_F009_ComboBonus/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill |
|---|---|---|---|---|---|---|---|---|---|---|---|
| [TODO] | F009 | 连击增益奖励系统 | 战斗反馈 | GameplaySessionStats 追踪连击但无任何游戏性收益 | 激励连续击杀，提升战斗深度和爽感 | 为后续技能/装备连击增益提供基础 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill |

## 用户需求

> 自动发现：GameplaySessionStats 已完整追踪连击计数（4 秒超时、CurrentCombo、MaxCombo），但连击数据仅用于 F004 的会话结算评分。在实时战斗中，连击没有任何游戏性收益——没有伤害加成、没有经验加成、没有视觉反馈。需要在战斗流程中接入连击增益，使玩家保持高连击时获得实质性奖励。

## 主 Agent 分析

- 任务分类：gameplay_feature（战斗反馈/连击增益）
- 游戏业务类型：战斗反馈
- 玩家价值：激励连续击杀，连击越高伤害和经验收益越大，提升战斗爽感和操作深度
- 开发价值：为后续技能/装备连击增益系统提供基础框架，OnComboMilestoneReached/OnComboBroken 事件可供成就、UI、音效系统订阅
- 目标模块：GameplaySessionStats（数据源）→ ComboBonusManager（增益计算）→ Character.ReduceHp / Player.AddExperienceValue（接入点）
- 主要影响路径：
  - `Scripts/2D/Gameplay/ComboBonusManager.cs` — 新增，连击增益管理器
  - `Scripts/2D/Editor/ComboBonusMenu.cs` — 新增，Editor 调试菜单
  - `Scripts/2D/Character/Character.cs` — ReduceHp 中新增连击伤害加成（仅当 attacker is Player）
  - `Scripts/2D/Character/Player/Player.cs` — AddExperienceValue 中新增连击经验加成
- 不应触碰的路径：
  - `Resources/SO`、`Resources/Tilemap`、`Resources/Images`
  - `Scenes`、`StreamingAssets`、`AddressableAssetsData`
  - `Scripts/2D/Data/`、`Scripts/2D/Manager/ArchiveManager.cs`
  - `Scripts/2D/NetworkConnect.cs`、Photon 同步逻辑
- 负责 Agent：GameplayAgent
- 需要的 Skill：ScriptGenerateSkill、EditorToolSkill
- 需要确认的问题：无

## 子 Agent 分工

| 子 Agent | 职责 | 输入 | 输出 |
|---|---|---|---|
| GameplayAgent | 创建 ComboBonusManager，修改 Character.ReduceHp 和 Player.AddExperienceValue | GameplaySessionStats.StatsChanged 事件、Character.ReduceHp 签名、Player.AddExperienceValue 签名 | ComboBonusManager.cs + 2 处最小修改 |
| ToolAgent | 创建 Editor 调试菜单 | 现有 Editor 菜单模式参考（GameplayStatsMenu） | ComboBonusMenu.cs |

## Skill 调用计划

| Skill | 调用原因 | 输入 | 预期输出 |
|---|---|---|---|
| ScriptGenerateSkill | 生成 ComboBonusManager 独立脚本 | 连击等级配置表、事件定义、StatsChanged 订阅 | 低侵入独立脚本 |
| EditorToolSkill | 生成 Editor 调试菜单 | 现有 Editor 菜单模式 | ComboBonusMenu |
| TestSkill | 静态验证 | 全部修改/新增文件 | 编译、逻辑、空引用、边界验证 |

## 上下文快照

- 相关脚本：
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — StatsChanged 事件、CurrentCombo/MaxCombo、UpdateCombo 逻辑（4 秒超时）
  - `Scripts/2D/Character/Character.cs` — ReduceHp(hp, attacker, isCRT)、AddExperienceValue
  - `Scripts/2D/Character/Player/Player.cs` — ReduceHp 重写（含无敌帧和重生保护）、AddExperienceValue 重写、Death
  - `Scripts/2D/GlobalInit.cs` — ShowTip 提示系统
  - `Scripts/2D/Singleton.cs` — Singleton<T> 基类
- 相关资源：无
- 相关场景：无
- 相关配置：无

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否（ComboBonusManager 是运行时单例，不持久化）
- 是否涉及 Photon/网络同步：否（连击增益仅影响本地玩家伤害输出和经验获取）
- 是否需要兼容旧数据：否
- 风险等级：低

## 功能边界

1. ComboBonusManager 负责：
   - 订阅 GameplaySessionStats.StatsChanged，实时同步连击数
   - 根据连击等级配置表计算伤害倍率和经验倍率
   - 连击等级跨越时触发 OnComboMilestoneReached 事件和即时 Tip 提示
   - 连击中断时触发 OnComboBroken 事件和中断提示
   - 提供 DamageMultiplier / ExperienceMultiplier 公开属性供业务代码读取
   - 提供静态查询方法 GetDamageMultiplierForCombo / GetExperienceMultiplierForCombo

2. Character.ReduceHp 接入点：
   - 当 attacker is Player 时，对传入的 hp 乘以 ComboBonusManager.Instance.DamageMultiplier
   - 乘法在 DEF 减伤计算之前应用
   - 非 Player 攻击者不受影响

3. Player.AddExperienceValue 接入点：
   - 在调用 base.AddExperienceValue 之前，对 experience 乘以 ComboBonusManager.Instance.ExperienceMultiplier
   - Mathf.RoundToInt 保证整型经验值

4. ComboBonusMenu Editor 菜单：
   - Tools/Combo Bonus/Show Combo Status — 查看当前运行时连击状态
   - Tools/Combo Bonus/Show All Tiers — 查看完整等级配置表
   - Tools/Combo Bonus/Simulate Query (combo=5) — 查询连击 5 的倍率
   - Tools/Combo Bonus/Simulate Query (combo=50) — 查询连击 50 的倍率

## 业务规则说明

### 连击等级配置表

| 连击数 | 伤害倍率 | 经验倍率 | 提示文本 |
|---|---|---|---|
| 1-4 | 1.00x | 1.0x | (无) |
| 5-9 | 1.10x | 1.2x | 连击 x5! |
| 10-19 | 1.20x | 1.5x | 连击 x10! 伤害提升! |
| 20-49 | 1.35x | 2.0x | 连击 x20! 激增! |
| 50-99 | 1.50x | 3.0x | 连击 x50! 无双! |
| 100+ | 2.00x | 5.0x | 连击 x100! 传说! |

### 连击生命周期规则

1. 连击由 GameplaySessionStats 内部维护：每次 RecordEnemyDefeated 时 UpdateCombo
2. 4 秒内连续击杀 → combo 递增；超过 4 秒 → combo 重置为 1
3. 玩家死亡时 RecordPlayerDeath 会将 combo 归零
4. ComboBonusManager 在 combo 从 >1 变为 <=1 时判定为"连击中断"
5. 连击中断时显示 Tip 提示（"连击中断! 最高连击: X"）
6. 连击等级跨越（如 4→5、9→10）时显示里程碑 Tip 并触发事件

## 数据流说明

```
敌人死亡
  → CommonEnemyDeadState / SeekEnemyDeadState.OnEnter()
  → GameplaySessionStats.Instance.RecordEnemyDefeated()
  → GameplaySessionStats.UpdateCombo()        // 更新 currentCombo
  → GameplaySessionStats.NotifyStatsChanged() // 触发 StatsChanged 事件
  
ComboBonusManager.OnStatsChanged(snapshot)
  → 检测 combo 变化
  → 检测连击中断 (oldCombo > 1 && newCombo <= 1)
    → OnComboBroken 事件 + ShowTip("连击中断!")
  → RecalculateMultipliers()
    → 从高到低遍历 Tiers，匹配等级
    → 更新 damageMultiplier / experienceMultiplier
    → 等级提升时 → OnComboMilestoneReached 事件 + ShowTip(等级标签)

玩家攻击敌人
  → enemy.ReduceHp(baseDamage, player, isCRT)
  → if (attacker is Player) hp *= ComboBonusManager.Instance.DamageMultiplier
  → DEF 减伤
  → HP 扣除 + 统计记录

玩家获得经验
  → Player.AddExperienceValue(baseExp)
  → experience *= ComboBonusManager.Instance.ExperienceMultiplier
  → base.AddExperienceValue(modifiedExp)
  → 升级判定 + UI 更新
```

## 执行步骤

1. 创建 `Scripts/2D/Gameplay/ComboBonusManager.cs` — 连击增益管理器单例
2. 创建 `Scripts/2D/Editor/ComboBonusMenu.cs` — Editor 调试菜单
3. 修改 `Scripts/2D/Character/Character.cs` — ReduceHp 中添加连击伤害加成（仅 Player 攻击者）
4. 修改 `Scripts/2D/Character/Player/Player.cs` — AddExperienceValue 中添加连击经验加成
5. 静态验证
6. 生成验证记录
7. 回写 feature_discovery.md

## 验证步骤

1. 编译验证：检查命名空间、Unity API 使用、类型正确性、System.Linq 依赖
2. 逻辑验证：检查等级匹配算法、连击中断判定、倍率计算正确性
3. 空引用验证：检查 Singleton 懒初始化、EnsureInitialized 延迟订阅、降级路径
4. 破坏性验证：确认对 Character.cs 仅新增 3 行、对 Player.cs 仅新增 3 行，不改变已有逻辑
5. 边界条件验证：连击=0、连击=1、连击=999、经验=0、经验=负数
6. 代码风格验证：确认命名空间、中文注释、缩进与现有代码一致

## 回滚方案

- 回滚路径：
  1. 删除 `Scripts/2D/Gameplay/ComboBonusManager.cs` 及 .meta
  2. 删除 `Scripts/2D/Editor/ComboBonusMenu.cs` 及 .meta
  3. 还原 `Scripts/2D/Character/Character.cs` 中 ReduceHp 的 3 行新增代码
  4. 还原 `Scripts/2D/Character/Player/Player.cs` 中 AddExperienceValue 的 3 行新增代码
- 回滚顺序：删除新增文件 → 还原已有文件修改
- 需要保留的数据：无（ComboBonusManager 不持久化任何数据）
- 回滚后验证：编译通过，Character.ReduceHp 和 Player.AddExperienceValue 恢复原有行为

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  1. 创建 ComboBonusManager 运行时单例，基于 GameplaySessionStats.StatsChanged 实时追踪连击
  2. 实现 6 级连击等级配置表（1/5/10/20/50/100），提供递进式伤害倍率（1.0x~2.0x）和经验倍率（1.0x~5.0x）
  3. 在 Character.ReduceHp 中接入连击伤害加成（仅对 Player 攻击者生效，DEF 减伤前应用）
  4. 在 Player.AddExperienceValue 中接入连击经验加成
  5. 实现连击里程碑事件（OnComboMilestoneReached）和即时 Tip 提示
  6. 实现连击中断检测（OnComboBroken）和中断提示
  7. 实现 OnComboChanged 事件供 UI/HUD 订阅
  8. 提供静态查询方法 GetDamageMultiplierForCombo / GetExperienceMultiplierForCombo
  9. 创建 ComboBonusMenu Editor 调试菜单（4 个菜单项）
  10. 降级保护：GlobalInit/Tip Prefab 不可用时自动降级为 Debug.Log
- 修改的文件：
  - `Scripts/2D/Gameplay/ComboBonusManager.cs` — 新增（连击增益管理器，约 260 行）
  - `Scripts/2D/Editor/ComboBonusMenu.cs` — 新增（Editor 调试菜单，约 100 行）
  - `Scripts/2D/Character/Character.cs` — ReduceHp 新增 3 行（连击伤害加成）
  - `Scripts/2D/Character/Player/Player.cs` — AddExperienceValue 新增 3 行（连击经验加成）
- 新增的游戏业务能力：
  - **连击伤害增益**：玩家维持连击时，每次攻击伤害获得倍率加成（最高 2.0x at 100 combo）
  - **连击经验增益**：玩家维持连击时，每次击杀经验获得倍率加成（最高 5.0x at 100 combo）
  - **连击里程碑即时反馈**：连击达到 5/10/20/50/100 时弹出 Tip 提示，触发 OnComboMilestoneReached 事件
  - **连击中断反馈**：连击断链时弹出 Tip 提示（"连击中断! 最高连击: X"），触发 OnComboBroken 事件
  - **实时状态查询**：DamageMultiplier / ExperienceMultiplier / CurrentCombo / CurrentTierIndex 属性
  - **静态倍率查询**：GetDamageMultiplierForCombo / GetExperienceMultiplierForCombo 供 UI 预览使用
  - **Editor 调试菜单**：Tools > Combo Bonus > 查看状态 / 查看等级表 / 模拟查询
- 玩家侧效果：
  - 连续杀敌时伤害逐渐提升（5 连击 +10%，10 连击 +20%，100 连击翻倍）
  - 连续杀敌时经验获取大幅增加（最高 5 倍经验），加速升级
  - 达到连击里程碑时屏幕弹出激励性提示（如"连击 x50! 无双!"）
  - 连击中断时收到反馈，激励玩家重新建立连击链
  - 鼓励激进战斗风格，高风险高回报
- 开发侧接入方式：
  - 自动接入：Character.ReduceHp 和 Player.AddExperienceValue 已自动读取 ComboBonusManager 倍率
  - UI 接入：订阅 ComboBonusManager.Instance.OnComboChanged 实时更新 HUD 连击显示
  - 特效接入：订阅 OnComboMilestoneReached 触发屏幕特效（如全屏闪光、震屏）
  - 成就接入：监听 OnComboMilestoneReached 解锁"达成 50 连击"等成就
  - Editor 调试：Tools > Combo Bonus > 各菜单项
  - 倍率查询：ComboBonusManager.GetDamageMultiplierForCombo(combo) 静态方法
- 验证结果：静态验证全部通过（详见验证记录）
- 验证记录路径：Agent/Reports/2026-04-30/feature_F009_ComboBonus/validation_feature_F009.md
- 未完成项：无
- 剩余风险：
  - Play Mode 端到端验证需人工在 Unity 中完成（需要在实际战斗中验证连击增益倍率效果）
  - 连击超时 4 秒为 GameplaySessionStats 默认值，如需调整可通过 `GameplaySessionStats.Instance.ComboTimeout` 修改
  - 联机模式下其他玩家的攻击不受 ComboBonusManager 影响（仅本地玩家受益）
  - 极端高连击（100+）的经验倍率 5.0x 可能导致快速升级，后续可根据实际数据调整等级配置表
- **BugFix (2026-04-30)**：修复 EnsureInitialized 中延迟初始化导致连击状态不同步的问题。
  - 根因：ComboBonusManager 首次被访问时（第一次攻击）才订阅 GameplaySessionStats.StatsChanged，但 internal `currentCombo` 保持默认值 0，错过已累积的连击数据
  - 修复：在 EnsureInitialized 中订阅事件后，立即通过 `GameplaySessionStats.Instance.CreateSnapshot()` 拉取当前 combo 并调用 RecalculateMultipliers
  - 修复文件：`Scripts/2D/Gameplay/ComboBonusManager.cs`（EnsureInitialized 方法 +10 行同步逻辑）
- 后续建议：
  1. 在 Unity Editor Play Mode 中验证连击增益的实际战斗效果
  2. 基于 OnComboChanged 事件在 HUD 上显示实时连击数和当前倍率
  3. 基于 OnComboMilestoneReached 事件添加屏幕特效（连击闪光、震屏、音效）
  4. 基于 OnComboBroken 事件添加连击中断视觉反馈（如屏幕闪红）
  5. 接入成就系统：达成 50 连击 / 100 连击解锁成就
  6. 可扩展为技能/装备系统：某些武器或技能可提供额外连击时间或更高的连击倍率
  7. 等级配置表（Tiers）当前为硬编码，后续可考虑迁移到 ScriptableObject 配置以支持策划调优
