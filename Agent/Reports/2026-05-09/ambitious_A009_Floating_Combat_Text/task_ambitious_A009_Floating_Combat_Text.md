# 任务卡 - A009 浮动战斗文字系统

## 基本信息

- 候选ID：A009
- 原始候选：浮动战斗文字系统（伤害/暴击/治疗/状态文字+动画+颜色分级）
- 当前状态：Running
- 本次任务目录：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：战斗体验升级 / UI与表现
- 玩家价值：显著提升战斗打击感和信息可读性，让每次伤害/暴击/治疗/连击可见
- 开发价值：为战斗反馈、BUFF/DEBUFF 提示和战斗教学提供统一文字层
- 预计影响范围：`Scripts/2D/Enum`、`Scripts/2D/Constant`、`Scripts/2D/Tool`、`Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Editor`、`Scripts/2D/Character/Character.cs`、`Scripts/2D/GlobalInit.cs`
- 负责 Agent：UIAgent + GameplayAgent
- 需要 Skill：ScriptGenerateSkill + EditorToolSkill + TestSkill

## 风险与约束

- 风险等级：中
- 是否涉及 Scene：否（不直接修改 Game.unity YAML）
- 是否涉及 Prefab：否（复用已有 Damage.prefab 实例化模式，新增运行时动态 UI）
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否（浮动文字仅本地表现，不参与同步）
- 功能边界：
  - 本次包含：浮动文字类型枚举、常量、工具类、对象池、管理器、动画表现、Editor 安装菜单、与 Character.ReduceHp 集成
  - 本次不包含：BUFF/DEBUFF 图标系统、屏幕震动、连击语音
- 不应触碰路径：`NetworkConnect.cs`、`ArchiveManager.cs`、`SyncDataTool.cs`、`StreamingAssets`、`AssetBundle 配置`

## UI 接入策略

优先级：运行时动态创建（第4优先级）+ Editor 菜单辅助

原因：
- Game.unity 已有复杂 Canvas 层级，手写 Scene YAML 风险高
- ResourcesLocal Prefab 无法安全手写 YAML
- 运行时动态创建独立 Canvas 最安全，与 A001/A004/A006/A007 策略一致
- 提供 Editor 菜单可在 Unity Editor 中一键安装到场景

## 资源修改清单

- 修改 `Scripts/2D/Character/Character.cs`：`ReduceHp` 方法中增加调用 `FloatingTextManager.SpawnDamageText()`
- 修改 `Scripts/2D/GlobalInit.cs`：`Start` 方法中初始化 `FloatingTextManager`

## 执行步骤

1. 创建 `FloatingTextType` 枚举 —— 定义伤害/暴击/治疗/连击/经验/闪避/状态七种文字类型
2. 创建 `FloatingTextConstant` 常量 —— 颜色、字号、动画参数、池大小、节点名、菜单路径
3. 创建 `FloatingTextTool` 工具类 —— 对象池管理、文字创建、颜色/字号获取、格式化
4. 创建 `FloatingTextUI` 表现组件 —— MonoBehaviour 挂载到每个浮动文字，驱动动画与自动回收
5. 创建 `FloatingTextManager` 管理器 —— Singleton，统一生成入口，协调池与表现
6. 创建 `FloatingTextMenu` Editor 菜单 —— 场景安装/移除/验证
7. 修改 `Character.cs` —— `ReduceHp` 接入 `FloatingTextManager`
8. 修改 `GlobalInit.cs` —— 初始化浮动文字系统
9. 确保 `.meta` 文件同步
10. 编写验证记录

## 验证步骤

1. 静态验证：检查所有新增文件 `.meta` 存在、namespace 一致、无 `UnityEditor` 运行时引用
2. 编译验证：`git diff --check` 通过
3. 逻辑验证：枚举不重复、常量不冲突、工具方法边界安全
4. 集成验证：Character.ReduceHp 调用链完整、GlobalInit 初始化路径正确

## 回滚方案

- 新增文件均为独立模块，不修改已有文件的核心逻辑
- Character.cs 修改仅在 `ReduceHp` 中新增一行调用，通过 `#if` 或条件判断可安全回退
- GlobalInit.cs 修改仅新增一行初始化
- 删除所有新增文件即可完全回滚
- 已有 DamageUI/Damage.prefab 表现不变，新旧系统并行

## 工具类复用策略

- 已检查 `Scripts/2D/Tool`：复用 `Tool.IsUIInputActive()`、`Tool.GetOrAddComponentInChildren<T>()`
- 计划新增：`FloatingTextTool.cs`（对象池操作、文字生成、颜色获取、格式化）
- 子模块共享：所有浮动文字类型共用 `FloatingTextTool` 的创建和池方法

## 枚举复用策略

- 已检查 `Scripts/2D/Enum`：现有枚举不涵盖浮动文字类型
- 计划新增：`FloatingTextType.cs`（Damage/Critical/Heal/Combo/Experience/Dodge/StatusEffect）

## 常量复用策略

- 已检查 `Scripts/2D/Constant`：复用 `PrefabConstant.DAMAGE`（保留原有 Damage 预制体实例化）
- 计划新增：`FloatingTextConstant.cs`（颜色、字号、动画参数、池大小、节点名、菜单路径）

## 结果区

- **最终状态**：`[DONE]`
- **已完成内容**：
  1. **枚举层**：`FloatingTextType.cs` — 7种浮动文字类型（Damage/Critical/Heal/Combo/Experience/Dodge/StatusEffect）
  2. **常量层**：`FloatingTextConstant.cs` — 颜色、字号、动画参数、对象池配置、节点名、菜单路径
  3. **工具层**：`FloatingTextTool.cs` — 12个公共方法（颜色/字号/速度/缩放/存活时间查询、伤害/治疗/经验/连击文本格式化、随机偏移、GameObject创建、Canvas创建）
  4. **表现层**：`FloatingTextUI.cs` — MonoBehaviour 驱动弹出缩放→上浮→淡出动画，自动回收到对象池
  5. **管理层**：`FloatingTextManager.cs` — Singleton，统一生成入口（6种Spawn方法），对象池管理，世界→屏幕坐标转换
  6. **Editor层**：`FloatingTextMenu.cs` — 安装到Game场景 / 从Game场景移除 / 验证配置
  7. **集成层**：修改 `Character.cs` ReduceHp() 接入浮动文字，修改 `GlobalInit.cs` 初始化系统
- **修改文件**：
  - 新增8个文件：`FloatingTextType.cs`、`FloatingTextConstant.cs`、`FloatingTextTool.cs`、`FloatingTextUI.cs`、`FloatingTextManager.cs`、`FloatingTextMenu.cs`（及对应 `.meta`）
  - 修改2个文件：`Character.cs`（+4行）、`GlobalInit.cs`（+2行）
- **新增游戏体验能力**：
  - 7种区分颜色和大小的浮动文字类型（伤害/暴击/治疗/连击/经验/闪避/状态）
  - 暴击和连击专属弹出缩放动画
  - 对象池复用机制（默认30个，最大60个）
  - 世界坐标自动转屏幕坐标
  - Editor 一键安装/移除/验证菜单
- **玩家侧效果**：
  - 每次造成伤害时在屏幕上看到浮动数字，颜色区分普通/暴击
  - 连击时伤害数字更大更醒目
  - 获得经验、治疗时看到对应颜色浮动文字
  - 闪避时看到灰色"MISS"文字
- **UI 生成方式**：运行时动态创建独立 Canvas（第4优先级）+ Editor 菜单辅助安装
  - Canvas 名称：`Ambitious_A009_FloatingText_Canvas`（sortingOrder=100）
  - 未直接写入 `Game.unity` YAML
  - 未创建 `ResourcesLocal` Prefab
  - 提供 Editor 菜单 `工具/智能体/浮动战斗文字/安装浮动文字系统到 Game 场景`
- **开发侧接入方式**：
  - `Character.ReduceHp()` 自动触发，无需额外调用
  - `FloatingTextManager.Instance.SpawnHealText/SpawnExpText/SpawnDodgeText/SpawnStatusText` 可供其他系统直接调用
- **验证结果**：静态验证通过；Unity 编译/Play Mode 待人工环境验证
- **验证记录路径**：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/validation_ambitious_A009.md`
- **回滚方案**：删除所有新增文件 + 删除 Character.cs/GlobalInit.cs 中新增的 A009 行即可完全回滚
- **未完成项**：无
- **残馀风险**：
  - 字体使用 `LegacyRuntime.ttf`，高版本 Unity 可能不可用（有默认字体兜底）
  - Canvas sortingOrder=100 可能与其他 UI 层冲突，需在 Unity 中观察
  - 坐标转换依赖 Camera.main，缺失时文字会显示在屏幕左下角
  - 数值/字号/动画参数需在 Unity Play Mode 中手感调优
- **后续建议**：
  - 在 Unity Editor 中运行验证菜单检查系统配置
  - 进入 Play Mode 测试伤害文字显示效果
  - 根据实际视觉调整颜色、字号、动画速度
  - 后续可扩展：BUFF/DEBUFF 图标、屏幕震动搭配、伤害数字累积显示
- **`Scripts/2D/Tool`**：
  - 新增：`FloatingTextTool.cs`（12个公共静态方法）
  - 复用：无直接调用已有 Tool 方法（独立系统）
  - Editor/运行时分离：FloatingTextTool 无 UnityEditor 引用
  - 未抽取：`EnsureCanvas` 与 `AchievementTool.EnsureCanvas` 功能相似但不同系统独立，后续可合并
- **`Scripts/2D/Enum`**：
  - 新增：`FloatingTextType.cs`（7个枚举值）
  - 复用：无（全新语义域）
  - 未抽取重复枚举：无
- **`Scripts/2D/Constant`**：
  - 新增：`FloatingTextConstant.cs`（颜色×7、字号×7、动画参数×8、池配置×2、节点名×4、文案×5、菜单路径×3）
  - 复用：`PrefabConstant.DAMAGE`（保留原有世界空间伤害预制体，不替代）
  - 未抽取重复常量：无
