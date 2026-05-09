# 游戏体验升级任务卡 — A008 主动技能系统

## 基本信息

- 候选ID：A008
- 原始候选：主动技能系统（技能冷却+技能效果+技能HUD+技能升级树）
- 当前状态：从 `[TODO]` 开始开发
- 本次任务目录：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：战斗体验升级 / UI与表现
- 玩家价值：玩家目前只有鼠标点击基础攻击，缺少主动技能释放、冷却管理和技能成长。本系统显著丰富战斗操作维度，增加策略深度和操作爽感。
- 开发价值：为技能树、职业、装备附加技能和后续扩展提供完整技能框架。
- 预计影响范围：`Scripts/2D/Enum`、`Scripts/2D/Constant`、`Scripts/2D/Tool`、`Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Editor`、`Scripts/2D/Character/Player`、`Scripts/2D/GlobalInit`、`Game.unity`（可选）
- 负责 Agent：GameplayAgent + UIAgent + ToolAgent
- 需要 Skill：ScriptGenerateSkill + EditorToolSkill + TestSkill

## 风险与约束

- 风险等级：中高
- 是否涉及 Scene：可选（优先运行时动态 UI + Editor 菜单）
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否（纯代码驱动，SkillData 为运行时类）
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否（技能等级仅内存，不持久化到存档）
- 是否涉及 Photon/网络同步：否（技能仅在本地客户端执行，不涉及网络权威）
- 是否需要兼容旧数据：否
- 不应触碰的路径：`NetworkConnect.cs`、`ArchiveManager.cs`、`Photon` 相关同步逻辑、`AssetBundle` 配置

## 功能边界

### 本次包含
1. 4个预定义主动技能：旋风斩(AOE)、冲刺(位移+无敌)、力量爆发(攻击Buff)、治疗之光(自愈)
2. 技能数据模型（SkillData）：名称、类型、效果类型、冷却、法力消耗、伤害倍率、等级
3. 技能管理器（SkillManager）：冷却追踪、法力校验、技能激活、等级升级
4. 技能 HUD：4个技能按钮栏，显示冷却覆盖、法力消耗、技能名称、等级、快捷键
5. 技能冷却系统：独立冷却计时、就绪/冷却中状态
6. 玩家 MP 消费：技能消耗法力，法力不足时技能不可用
7. 技能升级：消耗经验点数提升技能等级（最多5级）
8. Editor 菜单：安装/移除/验证工具
9. 快捷键绑定：Q/E/R/F 对应4个技能槽位

### 本次不包含
- 技能特效/粒子/动画（需要美术资源）
- 技能音效（需要音频资源）
- 技能树 UI（复杂的多分支技能树界面）
- 技能存档持久化
- 网络同步的技能释放

## UI 接入策略

- 优先级：运行时代码动态创建 UI（优先级4）+ Editor 菜单辅助（优先级3）
- 不直接写入 `Game.unity`：Scene YAML 复杂，已有大量 Canvas 节点，手写风险高
- 不创建 `ResourcesLocal` Prefab：项目无标准 UI Prefab 目录规范用于技能 HUD
- 运行时创建独立 Canvas `Ambitious_A008_SkillHUD_Canvas`（sortingOrder=80），不影响已有 UI
- Editor 菜单 `工具/智能体/主动技能系统/安装技能 HUD 到 Game 场景` 可安全落到场景中
- 快捷键绑定 Q/E/R/F 不与现有 WASD 移动、数字键工具菜单、F1-F8 HUD 切换冲突

## 资源修改清单

| 资源类型 | 路径 | 操作 | 方式 |
|---------|------|------|------|
| C# 脚本 | `Scripts/2D/Enum/SkillType.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Enum/SkillEffectType.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Constant/SkillConstant.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Tool/SkillTool.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Gameplay/SkillData.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Gameplay/SkillManager.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/UI/SkillHUD.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Editor/SkillMenu.cs` | 新增 | Write |
| C# 脚本 | `Scripts/2D/Character/Player/Player.cs` | 修改 | Edit（+技能激活调用） |
| C# 脚本 | `Scripts/2D/Constant/InputKeyConstant.cs` | 修改 | Edit（+技能快捷键常量） |
| C# 脚本 | `Scripts/2D/GlobalInit.cs` | 修改 | Edit（+技能系统初始化） |

## 工具类复用策略

- 已检查 `Scripts/2D/Tool`：`Tool.IsUIInputActive()`、`Tool.GetComponentInChildren<T>()`
- 计划复用：`Tool.IsUIInputActive()` 用于技能快捷键守卫
- 计划新增：`Scripts/2D/Tool/SkillTool.cs` — 技能伤害计算、冷却格式化、法力消耗校验、范围敌人查询、技能升级成本计算
- 子模块共享：Manager 和 HUD 均通过 SkillTool 做计算

## 枚举复用策略

- 已检查 `Scripts/2D/Enum`：无现有技能相关枚举
- 计划新增：`SkillType.cs`（5个值：SelfAOE/Projectile/SelfBuff/Movement/SelfHeal）、`SkillEffectType.cs`（7个值：PhysicalDamage/MagicDamage/Heal/AttackBuff/DefenseBuff/SpeedBuff/Invincibility）
- 不抽取原因：无现有技能枚举可复用

## 常量复用策略

- 已检查 `Scripts/2D/Constant`：复用 `LayerConstant.ENEMY_LAYER` 用于 AOE 技能敌人查询
- 计划新增：`SkillConstant.cs` — 技能ID、默认参数、UI节点名、菜单路径、HUD参数、颜色
- 新增按键常量到 `InputKeyConstant.cs`：Skill1-4（Q/E/R/F）

## 哪些逻辑保留在业务脚本

- `SkillManager.cs`：技能生命周期、冷却管理、激活校验、等级升级
- `SkillHUD.cs`：UI 创建、刷新、交互表现
- `SkillData.cs`：技能数据模型
- `Player.cs`：技能快捷键输入检测

## 是否涉及 UnityEditor API

- 仅 `Scripts/2D/Editor/SkillMenu.cs` 使用 `UnityEditor` API
- 运行时脚本（Tool、Manager、HUD、Data）不引用 `UnityEditor`

## 执行步骤

### 步骤1：创建枚举（SkillType、SkillEffectType）
- 目标：定义技能类型和效果类型的稳定枚举
- 涉及文件：`Scripts/2D/Enum/SkillType.cs`、`Scripts/2D/Enum/SkillEffectType.cs`
- 完成标准：枚举定义完整，中文注释清晰

### 步骤2：创建常量（SkillConstant、扩展 InputKeyConstant）
- 目标：统一管理技能参数、UI节点名、菜单路径
- 涉及文件：`Scripts/2D/Constant/SkillConstant.cs`、修改 `Scripts/2D/Constant/InputKeyConstant.cs`
- 完成标准：常量分组清晰，无魔法数字

### 步骤3：创建工具类（SkillTool）
- 目标：提供技能伤害计算、冷却格式化、法力校验、范围查询等公共方法
- 涉及文件：`Scripts/2D/Tool/SkillTool.cs`
- 完成标准：静态方法，无 UnityEditor 引用，中文注释完整

### 步骤4：创建数据模型（SkillData）
- 目标：定义技能的运行时数据结构
- 涉及文件：`Scripts/2D/Gameplay/SkillData.cs`
- 完成标准：包含名称、类型、冷却、法力消耗、伤害倍率、等级等字段

### 步骤5：创建技能管理器（SkillManager）
- 目标：实现技能生命周期管理（冷却、激活、升级）
- 涉及文件：`Scripts/2D/Gameplay/SkillManager.cs`
- 完成标准：Singleton 模式，冷却追踪、MP 校验、技能激活、事件通知

### 步骤6：创建技能 HUD（SkillHUD）
- 目标：运行时动态创建技能按钮栏 UI
- 涉及文件：`Scripts/2D/UI/SkillHUD.cs`
- 完成标准：4个技能按钮，冷却覆盖、法力消耗显示、快捷键提示

### 步骤7：创建 Editor 菜单（SkillMenu）
- 目标：提供安装/移除/验证 Editor 工具
- 涉及文件：`Scripts/2D/Editor/SkillMenu.cs`
- 完成标准：3个菜单项，安全安装到 Game 场景

### 步骤8：修改 Player.cs
- 目标：在 Update 中检测技能快捷键并激活
- 涉及文件：`Scripts/2D/Character/Player/Player.cs`
- 完成标准：Q/E/R/F 键激活技能，不破坏现有攻击和移动逻辑

### 步骤9：修改 GlobalInit.cs
- 目标：初始化 SkillManager 和 SkillHUD
- 涉及文件：`Scripts/2D/GlobalInit.cs`
- 完成标准：Start 中调用初始化，不破坏现有初始化顺序

## 验证步骤

1. 静态验证：检查所有新增文件 .meta 完整性、namespace 一致性、无 UnityEditor 运行时引用
2. 逻辑验证：检查 SkillManager 冷却/法力/激活逻辑、SkillHUD UI 创建逻辑
3. 冲突验证：检查快捷键不与现有按键冲突、Canvas sortingOrder 不冲突
4. Unity 编译和 Play Mode 待人工环境验证

## 回滚方案

- 本次为纯新增代码 + 最小侵入修改（Player.cs 约+15行、GlobalInit.cs 约+5行、InputKeyConstant.cs 约+5行常量）
- 回滚路径：
  1. 删除所有新增文件（SkillType.cs、SkillEffectType.cs、SkillConstant.cs、SkillTool.cs、SkillData.cs、SkillManager.cs、SkillHUD.cs、SkillMenu.cs 及 .meta）
  2. 还原 Player.cs 中新增的技能激活代码
  3. 还原 GlobalInit.cs 中新增的初始化代码
  4. 还原 InputKeyConstant.cs 中新增的快捷键常量
- 回滚验证：`git diff --check` 确认仅目标文件变更

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  1. 技能类型与效果类型枚举（SkillType 5值、SkillEffectType 7值）
  2. 技能公共常量（SkillConstant，7个分组涵盖ID/参数/UI/菜单/颜色）
  3. 技能公共工具类（SkillTool，10个静态方法：伤害计算、冷却格式化、法力校验、AOE查询、升级成本等）
  4. 技能数据模型（SkillData，4个工厂方法创建预定义技能）
  5. 技能管理器（SkillManager，Singleton，冷却追踪+法力校验+技能激活+Buff计时+冲刺无敌）
  6. 技能 HUD（SkillHUD，运行时动态4按钮栏+冷却覆盖层+法力+等级+快捷键）
  7. Editor 菜单工具（SkillMenu，安装/移除/验证三项菜单）
  8. 玩家快捷键接入（Q/E/R/F 激活技能，HandleSkillInput 方法）
  9. 系统初始化接入（GlobalInit.Start + GlobalInit.Update Tick）
- 修改文件：
  - 新增：`Scripts/2D/Enum/SkillType.cs`、`Scripts/2D/Enum/SkillEffectType.cs`、`Scripts/2D/Constant/SkillConstant.cs`、`Scripts/2D/Tool/SkillTool.cs`、`Scripts/2D/Gameplay/SkillData.cs`、`Scripts/2D/Gameplay/SkillManager.cs`、`Scripts/2D/UI/SkillHUD.cs`、`Scripts/2D/Editor/SkillMenu.cs` 及对应 `.meta`（由 Unity 生成）
  - 修改：`Scripts/2D/Character/Player/Player.cs`（+39行）、`Scripts/2D/Constant/InputKeyConstant.cs`（+29行）、`Scripts/2D/GlobalInit.cs`（+5行）
- 新增游戏体验能力：
  1. 旋风斩（Q键）：范围AOE伤害，半径3.0，伤害倍率2.0×ATN，冷却5秒，消耗20MP
  2. 冲刺（E键）：朝向位移4.0单位+短暂无敌（0.3秒），冷却3秒，消耗15MP
  3. 力量爆发（R键）：攻击力Buff 1.5倍持续8秒，冷却15秒，消耗30MP
  4. 治疗之光（F键）：回复30HP，冷却12秒，消耗25MP
  5. 技能升级系统：消耗升级点数提升等级（1-5级），每级+15%效果/-10%冷却
  6. Buff系统：力量爆发激活期间所有伤害（含普通攻击和旋风斩）享受倍率加成
- 玩家侧效果：
  - 战斗操作从纯鼠标左键扩展为 Q/E/R/F 四键技能释放
  - MP 系统从闲置变为此消彼长的资源管理
  - 冲刺提供走位和逃生能力
  - 力量爆发+旋风斩形成连招组合
  - 技能冷却提供策略节奏
- UI 生成位置：未写入 `Game.unity`，未创建 `ResourcesLocal` Prefab
  - 运行时动态创建：`SkillHUD.EnsureRuntimePanel()` 在 GlobalInit.Start 中自动创建独立 Canvas（sortingOrder=80）
  - Editor 菜单辅助：`工具/智能体/主动技能系统/安装技能HUD到Game场景` 可在 Unity Editor 中安全落场景
  - HUD 位于屏幕底部中央，4个技能按钮横向排列，显示冷却覆盖、法力消耗、技能名称、等级和快捷键
- 开发侧接入方式：
  - 自动接入：GlobalInit.Start 中调用 `SkillManager.Instance.Initialize()` 和 `SkillHUD.EnsureRuntimePanel()`
  - 自动刷新：GlobalInit.Update 中调用 `SkillManager.Instance.Tick()`
  - 无需手动挂载任何组件或 Prefab
- 验证结果：静态验证通过（见 `validation_ambitious_A008.md`）；Unity 编译和 Play Mode 待人工复验
- 验证记录路径：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/validation_ambitious_A008.md`
- 回滚方案验证：回滚路径明确（删除8个新文件+还原3个修改文件diff），静态验证通过
- 未完成项：
  1. 技能特效/粒子/动画（需美术资源，已在功能边界中排除）
  2. 技能音效（需音频资源）
  3. 技能升级点数获取途径（当前只有 `AddUpgradePoints()` API，未接入波次/Boss 奖励）
  4. 技能存档持久化（技能等级仅在内存中）
- 剩余风险：
  1. 技能数值平衡需在 Play Mode 中手感调优（冷却/伤害/消耗配比）
  2. HUD 布局（字号/按钮尺寸/位置）需在 Unity 中观察
  3. 冲刺方向从 Animator Direction 参数推断，若动画未初始化方向可能默认为右
  4. Canvas sortingOrder=80 可能与已有 UI 层级冲突
- 后续建议：
  1. 在波次完成或 Boss 击杀时调用 `SkillManager.Instance.AddUpgradePoints(1)` 接入升级点数获取
  2. 实现技能存档（将技能等级写入 PlayerData 或 ArchiveData）
  3. 添加技能特效（粒子系统或动画事件）提升视觉反馈
  4. 接入 Photon 同步（技能释放 RPC）以支持多人模式
  5. 扩展更多技能（投射物、召唤物、光环等）和技能树 UI
- Scripts/2D/Tool：
  - 复用：`Tool.IsUIInputActive()` 用于技能快捷键守卫
  - 新增：`SkillTool.cs` — 10个公共静态方法，供 SkillManager 和 SkillHUD 共享
  - 不涉及 UnityEditor
  - 无未抽取重复逻辑
- Scripts/2D/Enum：
  - 无现有枚举可复用
  - 新增：`SkillType.cs`、`SkillEffectType.cs` — 供 SkillData、SkillManager、SkillTool 引用
  - 无未抽取重复枚举
- Scripts/2D/Constant：
  - 复用：`LayerConstant.ENEMY_LAYER`（SkillTool AOE查询间接使用，通过 EnemyManager）
  - 新增：`SkillConstant.cs` — 所有技能魔法值统一管理
  - 修改：`InputKeyConstant.cs` — 追加4个技能快捷键常量
  - 无未抽取重复常量或魔法值
