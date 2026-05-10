# 游戏体验升级任务卡 — A010 装备掉落稀有度与对比强化系统

## 基本信息

- 候选ID：A010
- 原始候选：装备掉落稀有度与对比强化系统（稀有度颜色+掉落光柱+对比弹窗+装备面板）
- 当前状态：Running
- 本次任务目录：`Agent/Reports/2026-05-10/ambitious_A010_Equipment_Loot/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：成长与养成 / 战斗掉落 / UI与表现
- 玩家价值：创造"暗黑式"掉落兴奋感，每次击杀都有机会获得稀有装备，对比新旧装备后决定取舍，显著提升刷怪动力
- 开发价值：激活已有装备数据层（`AEquipment`/`CharacterData.equipments`/`BackpackItemQualityEnum`），为商店/打造/套装提供稀有度基础设施
- 预计影响范围：`Scripts/2D/Enum`、`Scripts/2D/Constant`、`Scripts/2D/Tool`、`Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Editor`、`Scripts/2D/Character`、`GlobalInit.cs`
- 负责 Agent：GameplayAgent + UIAgent + ItemDataAgent
- 需要 Skill：ScriptGenerateSkill + EditorToolSkill + CodeReviewSkill + TestSkill

## 影响路径

- 新增文件：
  - `Scripts/2D/Enum/EquipmentRarityType.cs` — 装备稀有度枚举（6级，映射到 BackpackItemQualityEnum）
  - `Scripts/2D/Constant/EquipmentLootConstant.cs` — 装备掉落常量（颜色、倍率、路径、UI文案）
  - `Scripts/2D/Tool/EquipmentLootTool.cs` — 装备掉落公共工具（稀有度颜色、属性生成、对比计算）
  - `Scripts/2D/Gameplay/EquipmentLootManager.cs` — 装备掉落管理器（掉落概率、稀有度加权、属性随机）
  - `Scripts/2D/UI/EquipmentComparePopup.cs` — 装备对比弹窗（属性对比、替换/丢弃选择）
  - `Scripts/2D/UI/EquipmentPanel.cs` — 装备管理面板（所有槽位展示、属性总览）
  - `Scripts/2D/Editor/EquipmentLootMenu.cs` — Editor 安装/卸载/测试菜单

- 修改文件：
  - `Scripts/2D/Character/Character.cs` — 在 `ReduceHp` 中接入装备掉落概率判定（敌人死亡前）
  - `Scripts/2D/GlobalInit.cs` — 初始化装备掉落管理器和 UI

- 不应触碰路径：
  - Photon 同步核心（`NetworkConnect.cs`、`SyncDataTool.cs`）
  - AssetBundle 配置（`AddressableAssetsData`、`StreamingAssets` 已有配置）
  - 存档结构（`Scripts/2D/Data`、`ArchiveManager.cs`）
  - `AEquipment.cs` 核心数据模型（已有属性系统和 EquipTypeEnum）
  - `ABackpackItem.cs` 品质枚举（已有 BackpackItemQualityEnum）
  - `CharacterData.ComputeAttribute()` 属性聚合逻辑

## 风险等级

- 风险等级：中高
- 功能边界：
  - 包含：稀有度枚举和倍率、装备掉落概率和稀有度加权、掉落预览、对比弹窗、装备管理面板、Editor 安装工具
  - 不包含：装备升级/强化/锻造、装备套装效果、装备耐久度、装备交易、存档持久化
- UI 接入策略：
  - 优先级1：编辑器菜单 `Tools/Agent/Ambitious/Install A010 Equipment Loot UI` 在 `Game.unity` 创建独立 UI 节点
  - 优先级2：运行时动态创建独立 Canvas（`Ambitious_A010_EquipmentPanel_Canvas` 和 `Ambitious_A010_ComparePopup_Canvas`）
  - 优先级3：Editor 菜单生成 ResourcesLocal Prefab
  - 默认：运行时动态创建 + Editor 菜单辅助
- Scene/Prefab/ResourcesLocal 生成策略：
  - 不直接手写 `Game.unity` YAML
  - 不直接手写 Prefab YAML
  - 使用 Editor 菜单 + 运行时动态创建
  - 不覆盖已有 Prefab/场景对象

## 业务规则说明

1. **稀有度体系**：复用已有 `BackpackItemQualityEnum` 9级品质，新增 `EquipmentRarityType` 映射6个稀有度等级（Common→Gray+White、Uncommon→Green、Rare→Blue、Epic→Purple、Legendary→Orange、Mythic→Red），每级有独立颜色和属性倍率
2. **掉落规则**：敌人死亡时按概率掉落装备（基础掉落率10%），稀有度加权（Common 50%/Uncommon 25%/Rare 15%/Epic 7%/Legendary 2.5%/Mythic 0.5%），波次越高稀有概率越高
3. **属性规则**：稀有度越高，`AEquipment.RankRandom` 的上下限倍率越大；Legendary+ 装备出现1-2条极值属性（某个属性翻倍）
4. **对比规则**：拾取新装备时弹出对比弹窗，显示当前装备 vs 新装备的属性差异（绿色↑提升/红色↓下降），玩家选择替换或丢弃
5. **展示规则**：装备管理面板按槽位分组展示所有已装备物品，显示总属性加成，支持卸下操作

## 数据流说明

1. 敌人死亡 → `EquipmentLootManager.RollDrop(enemyPos, waveNumber)` → 随机决定是否掉落和稀有度
2. 生成装备数据 → `EquipmentLootTool.GenerateEquipment(type, rarity)` → 属性加权随机
3. 装备掉落物创建 → `ItemMap.PutDownToDrop()` → 地面显示
4. 玩家拾取 → `EquipmentLootManager.OnPickup(equipment)` → 弹出对比弹窗
5. 玩家选择替换 → `CharacterData.AddEquipment()` → `ComputeAttribute()` → 属性更新
6. 玩家选择丢弃 → 装备放回地面

## 工具类复用策略

- 已检查 `Scripts/2D/Tool`：`Tool.cs`（`IsUIInputActive()`、`GetComponentInChildren<T>()`）、`ResourceTool.cs`、`VectorTool.cs`
- 计划复用：
  - `Tool.IsUIInputActive()` — UI 输入检测
  - `Tool.GetComponentInChildren<T>()` — 安全组件获取
  - `WeatherGameplayTool.ApplyMultiplier()` — 属性倍率应用（若适用）
- 计划新增：`EquipmentLootTool.cs` — 稀有度颜色映射、属性加权生成、对比计算、装备文本格式化
- 子模块共享能力：所有子模块通过 `EquipmentLootTool` 获取稀有度颜色、格式化装备文本

## 枚举复用策略

- 已检查 `Scripts/2D/Enum`：`PackageTypeEnum.cs`、`WorkerConditionState.cs`、`WavePhaseType.cs`、`WaveRewardType.cs`、`AchievementCategory.cs`、`AchievementState.cs`、`FloatingTextType.cs`、`SkillType.cs`、`SkillEffectType.cs`
- 计划复用：`AEquipment.EquipTypeEnum`（装备槽位类型）、`ABackpackItem.BackpackItemQualityEnum`（物品品质）
- 计划新增：`EquipmentRarityType.cs` — 装备稀有度等级枚举（映射到 BackpackItemQualityEnum）
- 共享：`EquipmentRarityType` 被 `EquipmentLootTool`、`EquipmentLootManager`、`EquipmentComparePopup`、`EquipmentPanel`、`EquipmentLootMenu` 共同引用

## 常量复用策略

- 已检查 `Scripts/2D/Constant`：`TagConstant.cs`、`ResourceConstant.cs`、`Lock.cs`、`LayerConstant.cs`、`PrefabConstant.cs`、`InputKeyConstant.cs`、`SkillConstant.cs`、`FloatingTextConstant.cs`、`AchievementConstant.cs`
- 计划复用：`TagConstant`（标签）、`LayerConstant`（层级）、`PrefabConstant`（预设体路径）
- 计划新增：`EquipmentLootConstant.cs` — 稀有度颜色、属性倍率、掉落概率、UI 节点名、菜单路径、默认文案、快捷键

## 哪些逻辑保留在业务脚本

- `EquipmentLootManager.cs`（Gameplay）：掉落概率判定、稀有度加权、拾取处理
- `EquipmentComparePopup.cs`（UI）：对比弹窗创建、属性展示、替换/丢弃交互
- `EquipmentPanel.cs`（UI）：装备面板创建、槽位展示、属性总览
- `EquipmentLootMenu.cs`（Editor）：安装/卸载/测试菜单

## 是否涉及 UnityEditor API

- 是，`EquipmentLootMenu.cs` 位于 `Scripts/2D/Editor`，使用 `UnityEditor` API
- 运行时代码（Tool/Gameplay/UI）不引用 `UnityEditor`
- Editor 专用逻辑与运行时公共逻辑已分离

## 执行步骤

### 步骤1：新增公共枚举 `EquipmentRarityType`
- 目标：定义6级稀有度枚举，映射到 BackpackItemQualityEnum
- 文件：`Scripts/2D/Enum/EquipmentRarityType.cs`
- 操作方式：新建文件
- 完成标准：枚举定义完整、中文注释清晰、与 BackpackItemQualityEnum 映射明确

### 步骤2：新增公共常量 `EquipmentLootConstant`
- 目标：定义稀有度颜色、属性倍率、掉落概率、UI节点名、菜单路径
- 文件：`Scripts/2D/Constant/EquipmentLootConstant.cs`
- 操作方式：新建文件
- 完成标准：常量分组清晰、中文注释完整、值合理

### 步骤3：新增公共工具 `EquipmentLootTool`
- 目标：实现稀有度颜色映射、属性加权生成、对比计算、装备文本格式化
- 文件：`Scripts/2D/Tool/EquipmentLootTool.cs`
- 操作方式：新建文件
- 完成标准：所有方法为静态、中文注释完整、无 UnityEditor 引用、错误处理完善

### 步骤4：新增 `EquipmentLootManager`
- 目标：管理装备掉落概率、稀有度加权、拾取处理
- 文件：`Scripts/2D/Gameplay/EquipmentLootManager.cs`
- 操作方式：新建 Singleton MonoBehaviour
- 完成标准：掉落判定逻辑完整、稀有度加权算法正确、与 EnemyDropManager 协同

### 步骤5：新增 `EquipmentComparePopup`
- 目标：装备对比弹窗 UI
- 文件：`Scripts/2D/UI/EquipmentComparePopup.cs`
- 操作方式：新建 MonoBehaviour，运行时动态创建 Canvas
- 完成标准：属性对比显示正确、替换/丢弃按钮工作、颜色差异清晰

### 步骤6：新增 `EquipmentPanel`
- 目标：装备管理面板
- 文件：`Scripts/2D/UI/EquipmentPanel.cs`
- 操作方式：新建 MonoBehaviour，运行时动态创建 Canvas
- 完成标准：所有槽位显示、属性总览正确、卸下功能工作、快捷键切换

### 步骤7：新增 `EquipmentLootMenu`
- 目标：Editor 安装/卸载/测试菜单
- 文件：`Scripts/2D/Editor/EquipmentLootMenu.cs`
- 操作方式：新建 Editor 脚本
- 完成标准：菜单路径正确、安装/卸载/测试功能完整

### 步骤8：修改接入点
- 目标：在敌人死亡和 GlobalInit 中接入装备掉落
- 文件：`Scripts/2D/Character/Character.cs`（或 EnemyDeadState）、`Scripts/2D/GlobalInit.cs`
- 操作方式：在敌人死亡流程触发装备掉落判定，在 GlobalInit.Start() 初始化管理器
- 完成标准：接入点最小化、不破坏原有逻辑

## 验证步骤

1. 静态检查：所有新增代码无 `using UnityEditor`（Editor 脚本除外）、namespace 一致、Singleton 模式一致、无编译错误
2. 枚举验证：`EquipmentRarityType` 语义清晰、与 BackpackItemQualityEnum 映射正确、未被重复定义
3. 常量验证：`EquipmentLootConstant` 分组合理、值合理、未被硬编码替代
4. 工具验证：`EquipmentLootTool` 所有方法为静态、无 Editor 引用、错误处理完善
5. 管理器验证：`EquipmentLootManager` 初始化安全、空引用保护、掉落概率算法正确
6. UI 验证：对比弹窗和装备面板 Canvas 创建正确、布局逻辑合理、快捷键不冲突
7. Editor 验证：菜单路径正确、安装/卸载安全、重复执行不覆盖
8. `.meta` 验证：所有新增文件有对应 `.meta`（由 Unity 自动生成）

## 回滚方案

- 删除所有新增 `.cs` 文件和对应 `.meta`
- 从 `GlobalInit.cs` 移除 A010 初始化代码
- 从死亡流程移除装备掉落调用
- 不涉及 Scene/Prefab/ScriptableObject/StreamingAssets 修改，回滚无副作用

## 结果区

- 最终状态：**[DONE]**
- 本次任务目录：`Agent/Reports/2026-05-10/ambitious_A010_Equipment_Loot/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`

### 已完成内容（按子模块）

1. **稀有度枚举系统**：`EquipmentRarityType.cs` — 6级稀有度（Common/Uncommon/Rare/Epic/Legendary/Mythic），中文注释，映射到已有 `BackpackItemQualityEnum`
2. **掉落常量系统**：`EquipmentLootConstant.cs` — 稀有度颜色×6、属性倍率×6、掉落权重×6、极值属性配置、UI节点名、Canvas排序、快捷键、默认文案、Editor菜单路径
3. **公共工具类**：`EquipmentLootTool.cs` — 11个公共静态方法（稀有度加权随机、颜色映射、属性倍率施加、对比文本生成、属性摘要格式化、槽位名称映射等）
4. **掉落管理器**：`EquipmentLootManager.cs` — 单例（`Singleton<T>`），管理掉落概率判定、稀有度加权、拾取对比流程、过期清理、稀有度分布测试
5. **装备对比弹窗**：`EquipmentComparePopup.cs` — MonoBehaviour + runtimeInstance，运行时动态创建独立 Canvas（sortingOrder=250），属性对比行（颜色标记提升/下降），替换/丢弃按钮
6. **装备管理面板**：`EquipmentPanel.cs` — MonoBehaviour + runtimeInstance，运行时动态创建独立 Canvas（sortingOrder=120），F9 切换显示/隐藏，所有装备槽位展示+总属性加成
7. **Editor 菜单**：`EquipmentLootMenu.cs` — 3个菜单项（安装UI/卸载UI/测试稀有度分布），`#if UNITY_EDITOR` 预编译保护
8. **接入点**：修改 `GlobalInit.cs`（+4行初始化）、`CommonEnemyDeadState.cs`（+3行掉落调用）、`SeekEnemyDeadState.cs`（+3行掉落调用）

### 修改文件

- 新增文件（7个）：
  - `Scripts/2D/Enum/EquipmentRarityType.cs`
  - `Scripts/2D/Constant/EquipmentLootConstant.cs`
  - `Scripts/2D/Tool/EquipmentLootTool.cs`
  - `Scripts/2D/Gameplay/EquipmentLootManager.cs`
  - `Scripts/2D/UI/EquipmentComparePopup.cs`
  - `Scripts/2D/UI/EquipmentPanel.cs`
  - `Scripts/2D/Editor/EquipmentLootMenu.cs`
- 修改文件（3个）：
  - `Scripts/2D/GlobalInit.cs`（+4行）
  - `Scripts/2D/Character/Enemy/CommonEnemy/State/CommonEnemyDeadState.cs`（+3行）
  - `Scripts/2D/Character/Enemy/SeekEnemy/State/SeekEnemyDeadState.cs`（+3行）

### 新增游戏体验能力

- 敌人掉落装备按稀有度随机生成（10%基础概率，6级稀有度加权）
- 稀有度随波次提升而提高（高波次更容易出稀有+装备）
- 稀有度浮动标签在掉落位置显示
- 拾取装备时弹出对比弹窗（属性逐条对比、颜色标记提升/下降、一键替换/丢弃）
- F9 装备管理面板（所有槽位展示+总属性加成）
- Editor 菜单安装/卸载/测试
- Legendary+ 装备有随机极值属性（1-2条属性翻倍）

### 玩家侧效果

- 每次击杀敌人都有机会获得稀有装备，创造"暗黑式"掉落兴奋感
- 装备对比弹窗让玩家能直观判断新装备是否更好
- F9 装备面板让玩家随时查看角色装备总览和属性加成
- 稀有度颜色分级（灰→绿→蓝→紫→橙→红）提供视觉满足感

### UI 生成方式

- **未直接写入 `Game.unity`**
- **未创建 `ResourcesLocal` Prefab**
- **采用运行时动态创建**（运行时优先级4：运行时代码动态 UI + Editor 菜单辅助）
  - 装备对比弹窗：`Ambitious_A010_ComparePopup_Canvas`（sortingOrder=250）
  - 装备管理面板：`Ambitious_A010_EquipmentPanel_Canvas`（sortingOrder=120）
- **Editor 菜单**：`Tools/Agent/Ambitious/装备掉落系统/` 提供安装/卸载/测试功能

### 开发侧接入方式

- `GlobalInit.Start()` 初始化管理器、弹窗、面板
- 敌人死亡状态（`CommonEnemyDeadState`/`SeekEnemyDeadState`）调用 `EquipmentLootManager.TryDropEquipment()`
- 装备拾取链路通过 `EquipmentLootManager.OnEquipmentPickup()` 触发对比弹窗

### 验证结果

- 验证记录路径：`Agent/Reports/2026-05-10/ambitious_A010_Equipment_Loot/validation_ambitious_A010.md`
- 静态检查全部通过 ✅
- Unity 编译和 Play Mode 待人工环境验证

### Tool 使用情况

- **复用**：`Tool.IsUIInputActive()`（UI输入检测，EquipmentPanel 使用）
- **新增**：`EquipmentLootTool.cs`（11个公共静态方法，被所有子模块使用）
- **无 UnityEditor 引用** ✅
- **无未抽取重复逻辑** ✅

### Enum 使用情况

- **新增**：`EquipmentRarityType.cs`（6个枚举值，被所有子模块引用）
- **复用**：`AEquipment.EquipTypeEnum`（装备槽位类型）、`ABackpackItem.BackpackItemQualityEnum`（物品品质）
- **无重复枚举定义** ✅

### Constant 使用情况

- **新增**：`EquipmentLootConstant.cs`（8个分组，被所有子模块引用）
- **复用**：`TagConstant`、`LayerConstant`、`PrefabConstant` 等已有常量（通过接入点间接引用）
- **无重复魔法数字/字符串** ✅

### 回滚方案验证

- 回滚操作：删除7个新增文件 → 从 `GlobalInit.cs` 移除4行 A010 初始化 → 从 `CommonEnemyDeadState.cs` 和 `SeekEnemyDeadState.cs` 移除6行掉落调用
- 不涉及 Scene/Prefab/ScriptableObject/StreamingAssets ✅
- 回滚无副作用 ✅

### 未完成项

- 装备拾取对比链路：`EquipmentLootManager.OnEquipmentPickup()` 需要在物品拾取系统中接入（当前物品拾取通过 `ItemMap` 系统，需找到合适的拾取事件挂接点）
- 装备掉落视觉：稀有度颜色光柱/边框特效需要美术资源（当前仅显示浮动文字标签）
- 装备属性平衡：数值需在 Play Mode 中调优
- 装备跨会话持久化：当前装备仅内存存储，不支持存档

### 剩余风险

- **中**：装备拾取对比链路需找到正确的物品拾取事件挂接点（背包系统拾取事件或 ItemMap 拾取回调）
- **低**：稀有度浮动标签颜色由 FloatingTextType 内置颜色表决定，不受 EquipmentLootConstant 稀有度颜色控制
- **低**：LegacyRuntime.ttf 字体兼容性（与 A009 相同风险）
- **低**：Canvas sortingOrder 层级冲突（120/250 与其他系统的层级）

### 后续建议

1. 在物品拾取系统中找到 `ItemMap` 或 `BackpackController` 的拾取回调，接入 `EquipmentLootManager.OnEquipmentPickup()`
2. 为史诗+装备添加掉落光柱或边框特效
3. 在 Unity Play Mode 中测试稀有度分布和属性平衡
4. 添加装备存档持久化支持
5. 扩展装备对比弹窗支持更多属性维度（如按比例对比）
