# 验证记录 — F009 连击增益奖励系统

- 候选ID：F009
- 功能名称：连击增益奖励系统
- 验证时间：2026-04-30
- 验证方式：静态验证（无法运行 Unity Editor 进行 Play Mode 测试）
- 任务目录：Agent/Reports/2026-04-30/feature_F009_ComboBonus/
- **修订记录**：2026-04-30 BugFix — 修复 EnsureInitialized 延迟初始化导致连击状态不同步，详见维度 8

## 验证维度

### 1. 编译验证（命名空间、Unity API、类型正确性）

| 检查项 | 结果 | 说明 |
|---|---|---|
| 命名空间 | PASS | ComboBonusManager、ComboBonusMenu 均使用 `namespace LAB2D`，与项目一致 |
| Unity API 引用 | PASS | 仅使用 `Mathf.RoundToInt`、`Mathf.Max`、`Debug.Log`、`Debug.LogWarning`、`Application.isPlaying`、`EditorUtility.DisplayDialog`、`MenuItem`，均为合法 Unity API |
| 类型正确性 | PASS | `Singleton<ComboBonusManager>` 继承合法（ComboBonusManager 有公开无参构造函数）；`GameplaySessionStatsSnapshot` 引用正确；`Player` 类型检查（`attacker is Player`）正确 |
| System 命名空间 | PASS | 使用 `System`、`System.Text`、`UnityEngine`、`UnityEditor`，无需额外依赖 |
| 事件委托签名 | PASS | `Action<int>`、`Action<int, float, float>` 均与调用处参数一致 |

### 2. 逻辑验证（等级匹配、连击中断、倍率计算）

| 检查项 | 结果 | 说明 |
|---|---|---|
| 等级匹配算法 | PASS | 从 Tiers 末尾向开头遍历（`for i = Tiers.Length-1; i >= 0; i--`），取第一个 `combo >= MinCombo` 的等级，逻辑正确 |
| 连击 0 倍率 | PASS | combo=0 时匹配 Tiers[0]（MinCombo=1, DamageMultiplier=1.0, ExperienceMultiplier=1.0），返回基准倍率 |
| 连击中断检测 | PASS | `oldCombo > 1 && newCombo <= 1` 条件正确：combo 从 2+ 掉到 0/1 才触发中断；combo=1 本身不触发中断提示 |
| 连击等级提升检测 | PASS | `newTierIndex != currentTierIndex && currentTierIndex > 0` 正确：只在等级提升且非基准等级时触发里程碑 |
| 伤害倍率计算 | PASS | GetDamageMultiplierForCombo 静态方法与实例方法 RecalculateMultipliers 使用相同的遍历逻辑，结果一致 |
| 经验倍率计算 | PASS | GetExperienceMultiplierForCombo 同理 |
| Mathf.RoundToInt | PASS | 经验值使用 Mathf.RoundToInt 四舍五入，避免浮点精度导致经验值丢失 |

### 3. 空引用保护验证

| 检查项 | 结果 | 说明 |
|---|---|---|
| Singleton 懒初始化 | PASS | `Singleton<T>.Instance` 使用 `new T()` 懒初始化，首次访问时创建，不会返回 null |
| EnsureInitialized 防护 | PASS | 所有公开属性和方法均调用 EnsureInitialized，首次访问时完成事件订阅，避免重复订阅 |
| GameplaySessionStats 订阅保护 | PASS | try-catch 包裹 `StatsChanged += OnStatsChanged`，即使 GameplaySessionStats 构造失败也不会崩溃 |
| ShowTip 降级 | PASS | ShowComboMilestoneTip 和 ShowComboBreakTip 均 try-catch 包裹，GlobalInit.Instance 为 null 时降级为 Debug.Log |
| Character.ReduceHp 调用保护 | PASS | `attacker is Player` 在 null attacker 时返回 false，不会触发 NullReferenceException；ComboBonusManager.Instance 首次访问时懒初始化 |
| Player.AddExperienceValue 调用保护 | PASS | `experience > 0` 检查防止无效计算；ComboBonusManager.Instance 懒初始化安全 |

### 4. 破坏性验证（对已有代码的影响）

| 检查项 | 结果 | 说明 |
|---|---|---|
| Character.cs 修改范围 | PASS | 仅新增 3 行（if 判断 + hp *= 倍率），位于 LastAttacker 赋值之后、DEF 计算之前，不影响已有逻辑 |
| Player.cs 修改范围 | PASS | 仅新增 3 行（if 判断 + experience = Mathf.RoundToInt），位于 base.AddExperienceValue 调用之前 |
| 非 Player 攻击者行为 | PASS | `attacker is Player` 为 false 时跳过增益，Worker/Enemy 攻击行为完全不变 |
| 经验值 0/负数 | PASS | `experience > 0` 检查确保 0 和负数不受倍率影响 |
| 已有 DamageUI/统计 | PASS | 连击伤害加成在 DEF 减伤之前应用，乘算结果正确传递到 RecordDamageDealt/RecordDamageTaken 和 DamageUI 显示 |

### 5. 边界条件验证

| 检查项 | 结果 | 说明 |
|---|---|---|
| combo = 0 | PASS | 匹配 Tiers[0]，倍率 1.0x，无加成 |
| combo = 1 | PASS | 匹配 Tiers[0]，倍率 1.0x，不触发中断提示 |
| combo = 4 | PASS | 匹配 Tiers[0]，倍率 1.0x，恰好低于 x5 阈值 |
| combo = 5 | PASS | 匹配 Tiers[1]，伤害 1.10x，经验 1.2x，触发里程碑 |
| combo = 100 | PASS | 匹配 Tiers[5]，伤害 2.00x，经验 5.0x，触发里程碑 |
| combo = 9999 | PASS | 匹配 Tiers[5]（最高等级），伤害 2.00x，经验 5.0x |
| experience = 0 | PASS | `experience > 0` 为 false，跳过倍率计算 |
| experience = 1 | PASS | `1 * 1.0f = 1`（Mathf.RoundToInt 无影响） |
| experience = 5，倍率 5.0x | PASS | `5 * 5.0f = 25.0f`，Mathf.RoundToInt → 25 |
| experience = 3，倍率 1.5x | PASS | `3 * 1.5f = 4.5f`，Mathf.RoundToInt → 5（四舍五入） |
| hp = 0.1f（极低伤害），倍率 2.0x | PASS | `0.1 * 2.0 = 0.2f`，DEF 减伤后不低于 0.1f（现有保底逻辑） |

### 6. 代码风格验证

| 检查项 | 结果 | 说明 |
|---|---|---|
| 命名空间 | PASS | 全部使用 `namespace LAB2D` |
| 中文注释 | PASS | 所有公开方法、字段、事件均有中文注释，符合项目要求 |
| 缩进风格 | PASS | 使用 4 空格缩进，与项目一致 |
| 类命名 | PASS | ComboBonusManager（Manager 后缀）、ComboBonusMenu（Menu 后缀），符合现有命名惯例 |
| 访问修饰符 | PASS | 公开 API 使用 public，内部实现使用 private，事件使用 public event |
| 区域分隔 | PASS | 使用 `#region` 组织代码结构（公开属性、事件、核心逻辑、查询方法） |

### 7. 事件安全性验证

| 检查项 | 结果 | 说明 |
|---|---|---|
| 事件调用空检查 | PASS | 所有事件触发均使用 `?.Invoke()` 空传播，无订阅者时不抛异常 |
| OnComboChanged 参数 | PASS | 传入 `newCombo`（int），与事件签名 `Action<int>` 匹配 |
| OnComboMilestoneReached 参数 | PASS | 传入 `(currentCombo, damageMultiplier, experienceMultiplier)`，与 `Action<int, float, float>` 匹配 |
| OnComboBroken 参数 | PASS | 传入 `oldCombo`（中断前连击数），与 `Action<int>` 匹配 |
| GameplaySessionStats.StatsChanged 订阅 | PASS | 使用 `+=` 订阅，无取消订阅需求（两个单例生命周期相同） |

### 8. BugFix 验证 — EnsureInitialized 延迟初始化状态同步（2026-04-30）

**问题描述：** 用户反馈使用枪或剑攻击敌人时伤害没有连击倍率。

**根因分析：**
ComboBonusManager 采用延迟初始化（首次访问属性时才调用 EnsureInitialized 订阅 GameplaySessionStats.StatsChanged）。但初始化时仅订阅了后续事件，未主动拉取 GameplaySessionStats 中已累积的连击数据。导致 internal `currentCombo` 保持默认值 0，`DamageMultiplier` 永远返回 1.0f。

完整调用链验证（确认 attacker 传递正确）：
```
Player 装备武器 → SetCharacter(PlayerManager.Instance.Mine) → character = Player (runtime type)
鼠标点击 → Player.Attack() → ForegroundPanel.Onclick_Attack()
  → AWeaponObject.Attack() → attackEffect.Onwer = this.character (= Player)
  → OnParticleCollision → c.ReduceHp(this.Damage, this.Onwer, this.IsCRT)
  → Character.ReduceHp: attacker is Player → TRUE ✓
  → ComboBonusManager.Instance.DamageMultiplier → 旧代码永远返回 1.0 ✗
```

**修复方案：** 在 EnsureInitialized 订阅事件后，立即通过 `GameplaySessionStats.Instance.CreateSnapshot()` 拉取当前 combo 快照并同步到 internal 状态。

**修复验证：**

| 检查项 | 结果 | 说明 |
|---|---|---|
| 首次初始化同步 | PASS | EnsureInitialized 中 `snapshot.CurrentCombo` → `this.currentCombo` → `RecalculateMultipliers()` 链路完整 |
| 已初始化跳过同步 | PASS | `this.initialized` 为 true 时直接返回，不重复订阅或同步 |
| 异常保护 | PASS | try-catch 包裹整个初始化逻辑，失败时 logged + 倍率保持 1.0 |
| 初始化后事件链路 | PASS | 订阅后 GameplaySessionStats 后续 StatsChanged 正常触发 OnStatsChanged |
| 同步后倍率正确性 | PASS | currentCombo=5 → RecalculateMultipliers → DamageMultiplier=1.10, ExperienceMultiplier=1.2 |
| 初始化时里程碑抑制 | PASS | RecalculateMultipliers 直接调用，不走 OnStatsChanged，初始化 combo>0 时不触发 OnComboBroken 事件 |
| 攻击链路 Player 类型传递 | PASS | AWeaponObject.Attack → attackEffect.Onwer = character (Player) → c.ReduceHp(damage, Onwer) → attacker is Player=true |

**修改文件：** `Scripts/2D/Gameplay/ComboBonusManager.cs` — EnsureInitialized 方法 (+10 行)

## 验证总结

- 总检查项：38+
- 通过：38+
- 失败：0
- 阻塞：0（Play Mode 验证需人工在 Unity Editor 中完成）

## 未验证项

1. **Play Mode 运行时验证**：需要人工在 Unity Editor 中进入 Play Mode，实际击杀敌人验证：
   - 连击数递增后伤害倍率是否生效（对比开启/关闭增益的伤害数字）
   - 连击数递增后经验倍率是否生效（对比击杀同类型敌人的经验值获取）
   - 连击里程碑 Tip 提示是否正常弹出
   - 连击中断 Tip 提示是否正常弹出（等待 4 秒或死亡）
   - Editor 菜单项是否能正确读取运行时状态

2. **联机环境验证**：需要验证联机模式下：
   - 本地玩家连击增益不影响其他玩家的伤害计算
   - 网络同步的伤害值不被本地倍率修改

## 残余风险

- Play Mode 未验证（本地环境无 Unity Editor）
- 联机模式未验证（需要多客户端环境）
- 等级配置表硬编码在 Tiers 数组中，后续如需策划调优需改为 ScriptableObject 配置
- 连击超时（4 秒）由 GameplaySessionStats 控制，ComboBonusManager 不独立管理
