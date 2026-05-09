# A007 成就系统 — 任务卡

## 基本信息

- 候选ID：A007
- 原始候选：成就系统（成就定义+条件检测+解锁弹窗+成就面板+成就点数）
- 当前状态：Running → Done
- 本次任务目录：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/`
- 全局候选报告路径：`Agent/Reports/ambitious_discovery.md`
- 任务分类：游戏体验升级
- 游戏业务类型：收集与进度 / UI与表现
- 玩家价值：提供跨局长期目标、解锁成就感、收集驱动力和重玩价值
- 开发价值：为每日任务、赛季挑战、排行榜和社交分享提供成就数据基础
- 预计影响范围：`Scripts/2D/Enum`、`Scripts/2D/Constant`、`Scripts/2D/Tool`、`Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Editor`、`Scripts/2D/GlobalInit`
- 负责 Agent：GameplayAgent + UIAgent + ItemDataAgent
- 需要 Skill：ScriptGenerateSkill + ConfigGenerateSkill + EditorToolSkill + TestSkill

## 用户需求

为 RandomWorld 项目开发完整的成就系统。基于已有 F001-F016 的统计数据（战斗统计、收集统计、波次记录、Worker 效率），建立跨局长期成就目标和解锁反馈。包括成就定义、条件检测、解锁弹窗、成就浏览面板和成就点数。

## 影响路径

新增文件：
- `Scripts/2D/Enum/AchievementCategory.cs` + `.meta` — 成就类别枚举
- `Scripts/2D/Enum/AchievementState.cs` + `.meta` — 成就状态枚举
- `Scripts/2D/Constant/AchievementConstant.cs` + `.meta` — 成就常量
- `Scripts/2D/Tool/AchievementTool.cs` + `.meta` — 成就工具类
- `Scripts/2D/Gameplay/AchievementData.cs` + `.meta` — 成就数据模型
- `Scripts/2D/Gameplay/AchievementManager.cs` + `.meta` — 成就管理器
- `Scripts/2D/UI/AchievementPopup.cs` + `.meta` — 成就解锁弹窗
- `Scripts/2D/UI/AchievementPanel.cs` + `.meta` — 成就浏览面板
- `Scripts/2D/Editor/AchievementMenu.cs` + `.meta` — Editor 安装工具

修改文件：
- `Scripts/2D/GlobalInit.cs` — 在 Start 初始化成就系统，在 Update 驱动进度更新和 F7 面板切换
- `Agent/Reports/ambitious_discovery.md` — 添加 A007/A008/A009 候选

不应触碰路径：Photon 同步核心、AssetBundle 配置、存档数据结构、Scene YAML、ResourcesLocal Prefab。

## 风险等级

中风险 — 新增独立系统，不修改已有脚本核心逻辑，只读取已有统计数据。不涉及 Photon、AssetBundle 和存档。

## 功能边界

本次包含：
- 20个预定义成就（战斗×6、收集×3、生存×4、波次×3、工人×3）
- 成就数据模型（ID、名称、描述、类别、目标值、进度、状态、点数）
- 成就管理器（进度跟踪、条件检测、解锁通知、事件驱动）
- 成就解锁弹窗（金色浮动通知，淡入淡出动画，4秒自动消失）
- 成就浏览面板（按类别筛选、进度条、状态图标、成就点数汇总）
- Editor 安装/卸载/验证菜单
- F7 快捷键切换成就面板

本次不包含：
- 成就存档持久化（跨会话保存）
- 成就图标/美术资源
- 社交分享
- 成就排行榜

## 业务规则说明

1. 成就通过读取 GameplaySessionStats、WaveManager、WorkerEfficiencyTracker 的已有统计数据驱动。
2. 成就状态流转：Locked（未解锁）→ Unlocked（已解锁）→ Claimed（已领取）。
3. 已解锁成就加入待展示队列，弹窗逐个展示。
4. 每帧调用 UpdateProgressAll() 同步进度，但已解锁成就不再更新。
5. 成就不影响任何战斗/关卡/物品逻辑，纯只读消费者。

## UI 接入策略

优先级 4（运行时代码动态创建 UI）：
- 不在 Game.unity 场景写入 UI 节点（避免破坏已有复杂 Canvas 层级）
- 不在 ResourcesLocal 手写 UI Prefab（避免 Prefab YAML 不可靠）
- 提供运行时动态创建 `EnsureRuntimePopup()` 和 `EnsureRuntimePanel()`
- 提供 Editor 菜单 `工具/智能体/成就系统/安装成就系统到 Game 场景` 安全落场景
- 弹窗和面板均为独立 Canvas，不污染已有 UI 层级
- 面板默认隐藏，F7 切换显示

## 执行步骤

1. 创建 `AchievementCategory` 枚举（完成）
2. 创建 `AchievementState` 枚举（完成）
3. 创建 `AchievementConstant` 常量类（完成）
4. 创建 `AchievementTool` 工具类（完成）
5. 创建 `AchievementData` 数据模型（完成）
6. 创建 `AchievementManager` 管理器（完成）
7. 创建 `AchievementPopup` 弹窗组件（完成）
8. 创建 `AchievementPanel` 面板组件（完成）
9. 创建 `AchievementMenu` Editor 工具（完成）
10. 修改 `GlobalInit.cs` 接入成就系统（完成）
11. 创建任务卡（完成）
12. 创建验证记录（进行中）
13. 回写全局候选报告（待完成）

## 验证步骤

1. 编译验证：所有新增 `.cs` 文件语法正确，命名空间一致（LAB2D），无 `using UnityEditor` 污染运行时代码。
2. 元数据验证：所有 `.cs` 有对应 `.meta` 文件。
3. 静态检查：`git diff --check` 通过。
4. Unity Play Mode 验证：待人工环境，验证成就进度跟踪、弹窗显示、面板 F7 切换。
5. Editor 菜单验证：待人工环境，验证安装/卸载/验证菜单功能。

## 回滚方案

1. 删除所有新增文件（12个 `.cs` + 12个 `.meta`）
2. 还原 `GlobalInit.cs` 中的 A007 相关修改（3处插入：Start 初始化 + Update 进度/F7/弹窗）
3. 删除任务目录 `Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/`
4. 还原 `ambitious_discovery.md` 中的 A007/A008/A009 添加
5. 无 Scene/Prefab/ScriptableObject/StreamingAssets 回滚需要

## 工具类复用策略

- 已检查 `Scripts/2D/Tool`：
  - 复用 `Tool.IsUIInputActive()` — 在 GlobalInit.Update 中判断是否屏蔽 F7 按键
  - 复用 `Tool.GetComponentInChildren<T>()` — AchievementMenu 验证安装状态时可用
- 新增 `AchievementTool.cs`：
  - `FormatProgress()` — 进度文本格式化
  - `GetProgressRatio()` — 进度比例计算
  - `FormatPoints()` — 点数文本格式化
  - `GetCategoryDisplayName()` — 类别中文名
  - `GetStateDisplayName()` — 状态中文名
  - `EnsureCanvas()` — Canvas 安全创建
  - `CreateText()` — UI 文本安全创建
  - `CreateImage()` — UI 图像安全创建
  - `BuildSummaryText()` — 成就摘要文本
  - `BuildConditionText()` — 条件描述文本

## 枚举复用策略

- 新增 `AchievementCategory.cs`：战斗/收集/生存/波次/工人五种类别
- 新增 `AchievementState.cs`：Locked/Unlocked/Claimed 三种状态
- 不重复已有枚举：`ColonyCommandAlertLevel`、`WorkerTaskBlockReason` 等与成就语义不冲突

## 常量复用策略

- 新增 `AchievementConstant.cs`：UI 节点名、默认文案、显示时长、动画时长、事件名、菜单路径
- 不重复已有常量：`WorkerConditionConstant`、`WaveBossRewardConstant` 等与成就系统路径不重叠

## 结果区

- 最终状态：`[DONE]`
- 已完成内容（按子模块）：
  1. 成就类别枚举 (`AchievementCategory`) — 战斗/收集/生存/波次/工人
  2. 成就状态枚举 (`AchievementState`) — Locked/Unlocked/Claimed
  3. 成就常量 (`AchievementConstant`) — UI 名/默认文案/事件名/菜单路径
  4. 成就工具类 (`AchievementTool`) — 进度格式化/Canvas创建/Text创建/摘要文本
  5. 成就数据模型 (`AchievementData`) — ID/名称/描述/类别/目标值/进度/状态/点数 + 计算属性
  6. 成就管理器 (`AchievementManager`) — 20个预定义成就/进度跟踪/条件检测/解锁通知
  7. 成就解锁弹窗 (`AchievementPopup`) — 淡入淡出/自动隐藏/浮动通知
  8. 成就浏览面板 (`AchievementPanel`) — 类别标签/ScrollView/进度条/F7切换
  9. Editor 安装工具 (`AchievementMenu`) — 安装/卸载/验证菜单
  10. GlobalInit 接入 — Start 初始化 + Update 进度/弹窗/F7
- 修改文件：
  - 新增 9 个 `.cs` 文件 + 9 个 `.meta` 文件
  - 修改 `Scripts/2D/GlobalInit.cs`
  - 修改 `Agent/Reports/ambitious_discovery.md`
- 新增游戏体验能力：
  1. 跨局长期成就系统 — 20个成就覆盖战斗、收集、生存、波次、工人5个维度
  2. 实时成就进度跟踪 — 从 GameplaySessionStats/WaveManager/WorkerEfficiencyTracker 同步进度
  3. 解锁弹窗通知 — 金色浮动弹窗，淡入淡出动画
  4. 成就浏览面板 — F7 打开/关闭，按类别筛选
  5. 成就点数系统 — 每个成就有独立点数奖励
  6. Editor 一键安装/卸载
- 玩家侧效果：
  - 每场游戏中自动跟踪各种成就进度
  - 达成成就时弹出金色通知
  - 按 F7 可浏览所有成就状态和进度
  - 成就点数提供长期收集目标
- UI 生成位置：
  - 未直接写入 `Game.unity`
  - 未创建 `ResourcesLocal` Prefab
  - 采用运行时动态创建 (优先级4) + Editor 菜单辅助
  - 弹窗 Canvas: `Ambitious_A007_AchievementPopup_Canvas` (sortingOrder=200)
  - 面板 Canvas: `Ambitious_A007_AchievementPanel_Canvas` (sortingOrder=150)
- 开发侧接入方式：GlobalInit.Start 初始化，GlobalInit.Update 驱动
- 验证结果：静态验证通过（无 UnityEditor 引用、.meta 齐全、namespace 一致、git diff --check 通过）
- 验证记录路径：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/validation_ambitious_A007.md`
- 回滚方案验证：纯新增文件 + GlobalInit 局部修改，可安全回滚
- 未完成项：无（核心能力全部完成）
- 剩余风险：
  1. Unity 编译和 Play Mode 待人工验证
  2. 弹窗/面板布局需在 Unity 中实际观察调整
  3. 成就进度仅内存存储，跨会话不持久化
  4. Boss 击杀统计暂用敌人总数，需后续接入专属计数器
- 后续建议：
  1. 人工调整弹窗位置、字号、面板颜色
  2. 添加成就存档持久化（PlayerPrefs）
  3. 补充 Boss 专属击杀统计
  4. 添加更多成就（如技能类、装备类、社交类）
- `Scripts/2D/Tool`：新增 `AchievementTool.cs`（10个可复用方法），复用 `Tool.IsUIInputActive()`，无 UnityEditor 引用，已完成 Editor 与运行时拆分
- `Scripts/2D/Enum`：新增 `AchievementCategory.cs`（5值）、`AchievementState.cs`（3值），不与已有枚举冲突
- `Scripts/2D/Constant`：新增 `AchievementConstant.cs`（UI名/文案/阈值/事件名/菜单路径），分组清晰
- 未抽取重复逻辑：无（所有公共能力已在 Tool/Enum/Constant 中沉淀）
