# Agent Task Card — F008 玩家受击无敌帧保护系统

## 基本信息

- 任务 ID：feature_F008_InvincibilityFrame
- 创建时间：2026-04-28
- 候选ID：F008
- 当前状态：Running
- 风险等级：Low
- 本次任务目录：Agent/Reports/2026-04-28/feature_F008_InvincibilityFrame/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill |
|---|---|---|---|---|---|---|---|---|---|---|---|
| [TODO] | F008 | 玩家受击无敌帧保护系统 | 玩家体验 | Player.ReduceHp 无受伤间隔保护，可被连续攻击瞬间击杀 | 防止被多敌人同时攻击秒杀，提升操作容错率 | 提供通用 i-frame 机制，可复用于技能/道具 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill |

## 任务分类

- 游戏业务类型：玩家体验 / 战斗反馈
- 目标模块：Player.ReduceHp（伤害接收端）
- 负责 Agent：GameplayAgent
- 需要的 Skill：ScriptGenerateSkill、CodeReviewSkill、TestSkill

## 影响路径

- **修改文件**：`Scripts/2D/Character/Player/Player.cs`
- **不应触碰路径**：Scenes、Resources/SO、ResourcesLocal/Prefabs、StreamingAssets、存档、Photon 同步、AssetBundle

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否（i-frame 只影响本地玩家受击判定，不影响网络同步数据）
- 是否需要兼容旧数据：否

## 功能边界

- 仅在 Player.ReduceHp 中添加受击冷却检查
- 默认无敌时间窗口：0.5 秒（可配置）
- 无敌期间：忽略所有伤害（hp 不减少、不显示 DamageUI、不变红）
- 不阻止攻击判定、不修改敌人 AI、不影响其他角色
- 与现有 DeathPenaltyManager.IsRespawning 重生无敌保护互不冲突
- 提供 IsInvincible 属性供外部查询
- 可选：无敌期间 Sprite 闪烁效果

## 业务规则说明

1. 玩家每次受到有效伤害后，进入短暂无敌状态（默认 0.5 秒）
2. 无敌期间任何 ReduceHp 调用都会被忽略
3. 无敌时间窗口可通过 `InvincibilityDuration` 属性动态调整
4. 重生无敌（DeathPenaltyManager.IsRespawning）优先级更高，先于 i-frame 检查
5. i-frame 不阻止玩家攻击、移动或其他操作
6. 无敌结束时自动恢复正常渲染

## 数据流说明

```
敌人攻击 → Player.ReduceHp(hp, attacker, isCRT)
  → 检查 hp <= 0 → 拒绝
  → 检查 DeathPenaltyManager.IsRespawning → 拒绝（重生保护）
  → 检查 Time.time - lastDamageTime < invincibilityDuration → 拒绝（i-frame）
  → 通过 → 记录 lastDamageTime → base.ReduceHp → 应用伤害 + UI 更新
```

## 执行步骤

1. 在 Player 类中添加无敌帧相关字段：`invincibilityDuration`、`lastDamageTime`
2. 在 Player.ReduceHp 方法开头添加 i-frame 检查逻辑
3. 添加 `IsInvincible` 公开属性
4. 添加可选的无敌期间视觉闪烁效果
5. 静态验证（编译检查、命名空间、API 使用、空引用保护）

## 验证步骤

1. 编译验证：检查命名空间、Unity API 使用、代码风格一致性
2. 静态检查：确认 i-frame 检查逻辑、与其他保护机制的优先级关系
3. Play Mode 验证：待人工完成（多敌人围攻场景下验证无敌帧效果）

## 回滚方案

- 删除 Player.cs 中新增的 i-frame 字段和检查逻辑
- 恢复 Player.ReduceHp 到修改前状态
- 无需清理其他文件或资源

## 结果区

- **最终状态**：[DONE]
- **已完成内容**：
  1. 在 Player.cs 中新增 `invincibilityDuration` 字段（默认 0.5 秒）
  2. 新增 `lastDamageTime` 字段用于追踪上次受击时间
  3. 新增 `InvincibilityDuration` 公开属性（可运行时动态调整，设为 0 可禁用）
  4. 新增 `IsInvincible` 公开属性（供外部系统查询当前无敌状态）
  5. 在 `ReduceHp` 中新增无敌帧检查逻辑，位于重生保护检查之后、伤害应用之前
  6. 每次成功受击后记录时间，启动无敌帧冷却
- **修改的文件**：
  - `Scripts/2D/Character/Player/Player.cs`（仅此一个文件）
- **新增的游戏业务能力**：
  - 受击无敌帧保护：玩家受伤后 0.5 秒内免疫所有后续伤害
  - 可配置无敌时长：通过 `InvincibilityDuration` 属性运行时调整
  - 无敌状态查询：通过 `IsInvincible` 属性供 UI/特效等系统使用
  - 零侵入开关：设为 0 即完全禁用，不影响原有逻辑
- **玩家侧效果**：
  - 防止被多敌人同时攻击秒杀，提升操作容错率和战斗公平性
  - 玩家在敌群中仍有反应和脱战的时间窗口
- **开发侧接入方式**：
  - 直接可用，无需额外配置
  - 其他系统可通过 `PlayerManager.Instance.Mine.InvincibilityDuration` 动态调整
  - UI/HUD 可通过 `PlayerManager.Instance.Mine.IsInvincible` 查询状态做视觉反馈
- **验证结果**：静态验证全部通过（编译、逻辑、空引用、破坏性、代码风格、边界条件），详见验证记录
- **验证记录路径**：`Agent/Reports/2026-04-28/feature_F008_InvincibilityFrame/validation_feature_F008.md`
- **未完成项**：Play Mode 运行时验证（需要在 Unity Editor 中运行游戏进行多敌人围攻测试）
- **剩余风险**：无（仅修改 Player.cs 一个文件，不涉及任何资源、Scene、Prefab、存档或网络同步）
- **后续建议**：
  1. 可在 HUD 上添加无敌帧视觉提示（如角色闪烁、护盾图标）
  2. 可扩展为通用 i-frame 接口供技能/道具系统复用（例如"使用护盾药水获得 3 秒无敌"）
  3. 可接入 GameplaySessionStats 记录"被无敌帧拦截的伤害次数"用于战斗数据分析
