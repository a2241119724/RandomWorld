# Agent Task Card — F004 会话结束统计数据模型与报告

## 基本信息

- 任务 ID：feature_F004_SessionResult
- 候选ID：F004
- 创建时间：2026-04-28
- 提出人：ProjectDirectorAgent（自动发现）
- 当前状态：Running
- 风险等级：Low
- 本次任务目录：Agent/Reports/2026-04-28/feature_F004_SessionResult/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill |
|---|---|---|---|---|---|---|---|---|---|---|---|
| [TODO] | F004 | 会话结束统计数据模型与报告 | 关卡结算 | 关卡流程有胜负结果但缺少统一统计数据；GameplaySessionStats 已有数据但无保存/展示 | 提升结算反馈和成长感 | 为 UI 面板和数据分析提供基础 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill |

## 任务分类

- 游戏业务类型：关卡结算 / 数据反馈
- 目标模块：GameplaySessionStats → SessionResultData → SessionResultManager
- 负责 Agent：GameplayAgent
- 需要的 Skill：ScriptGenerateSkill、CodeReviewSkill、TestSkill、EditorToolSkill

## 影响路径

- **新增文件**：
  - `Scripts/2D/Gameplay/SessionResultData.cs` — 结算数据模型
  - `Scripts/2D/Gameplay/SessionResultManager.cs` — 结算管理器单例
  - `Scripts/2D/Editor/SessionResultMenu.cs` — Editor 菜单工具
- **不应触碰路径**：Scenes、Resources/SO、ResourcesLocal/Prefabs、StreamingAssets、存档、Photon 同步、AssetBundle

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否（SessionResultManager 仅运行时内存存储，不写存档）
- 是否涉及 Photon/网络同步：否
- 是否需要兼容旧数据：否
- 风险等级：低

## 功能边界

1. 新增 SessionResultData 数据类：从 GameplaySessionStatsSnapshot 创建结构化的结算数据
2. 新增 SessionResultManager 运行时单例：管理结算数据的采集、存储和历史查询
3. 新增 SessionResultMenu Editor 工具：提供采集、查看、历史管理和清空功能
4. 不修改任何已有文件
5. 不涉及 Scene/Prefab/SO/存档/Photon
6. 提供战斗评分计算（0-10000）、星级评价（1-5）、等级评级（S/A/B/C/D）
7. 提供格式化文本报告生成

## 业务规则说明

### 评分计算规则

战斗评分（满分 10000）按以下权重加权：

1. **击杀分 (35%, 上限 3500)**：每击杀一只敌人得 100 分
2. **连击分 (25%, 上限 2500)**：最高连击数 × 50 分
3. **生存分 (20%, 上限 2000)**：存活通关（零死亡）+2000；每次死亡扣 500
4. **效率分 (15%, 上限 1500)**：伤害效率（输出/承受比）× 300
5. **收集分 (5%, 上限 500)**：收集物品数 × 5

### 评级规则

- S 级：评分 ≥ 8000，5 星
- A 级：评分 ≥ 6000，4 星
- B 级：评分 ≥ 4000，3 星
- C 级：评分 ≥ 2000，2 星
- D 级：评分 < 2000，1 星

## 数据流说明

```
GameplaySessionStats.CreateSnapshot()
  → SessionResultData.FromSnapshot(snapshot)  // 数据转换 + 衍生计算
  → SessionResultManager.CaptureResult()      // 存入历史 + 触发事件
  → OnResultCaptured 事件                     // 通知 UI/日志/分析系统
  → LatestResult / GetResultAt() / GetAllResults()  // 查询接口
  → GetReportText()                           // 格式化报告输出
```

## 执行步骤

1. 创建 `Scripts/2D/Gameplay/SessionResultData.cs` — 结算数据模型，含 FromSnapshot、CalculateDerivedStats、GetReportText
2. 创建 `Scripts/2D/Gameplay/SessionResultManager.cs` — 结算管理器单例，含 CaptureResult、历史管理、事件通知
3. 创建 `Scripts/2D/Editor/SessionResultMenu.cs` — Editor 菜单工具，含 Capture Now / Show Latest / Show History / Clear History
4. 静态验证
5. 生成验证记录
6. 回写 feature_discovery.md

## 验证步骤

1. 编译验证：确认命名空间、Unity API 使用、类型正确性
2. 静态检查：验证评分计算公式、边界条件、空引用保护
3. Play Mode 验证：待人工完成（需要 Unity Editor 中进入 Play Mode，触发战斗后采集结算数据）

## 回滚方案

- 删除三个新增文件即可完全回滚
- 无其他文件受影响
- 无需保留数据

## 结果区

- **最终状态**：[DONE]
- **已完成内容**：
  1. 创建 SessionResultData 数据模型，支持从 GameplaySessionStatsSnapshot 生成结构化结算数据
  2. 实现多维度战斗评分计算（击杀 35% + 连击 25% + 生存 20% + 效率 15% + 收集 5%）
  3. 实现星级评价（1-5 星）和 S/A/B/C/D 等级评级
  4. 创建 SessionResultManager 运行时单例，管理结算采集、历史存储（最多 20 条）和事件通知
  5. 创建 SessionResultMenu Editor 工具，提供 4 个菜单项（Capture/Show Latest/Show History/Clear History）
  6. 提供格式化文本报告生成（GetReportText/GetHistorySummaryText）
- **修改的文件**：
  - `Scripts/2D/Gameplay/SessionResultData.cs`（新增）
  - `Scripts/2D/Gameplay/SessionResultManager.cs`（新增）
  - `Scripts/2D/Editor/SessionResultMenu.cs`（新增）
- **新增的游戏业务能力**：
  - **会话结算数据模型**：结构化存储战斗、生存、经济三维度的完整结算数据
  - **战斗评分系统**：加权多维度评分（0-10000），含星级和字母等级
  - **结算数据采集**：一键从 GameplaySessionStats 快照生成结算报告
  - **结算历史管理**：运行时内存中保留最近 20 次结算记录
  - **Editor 菜单集成**：Tools > Session Result 系列菜单，支持采集/查看/管理
  - **事件通知**：OnResultCaptured 事件供 UI 面板订阅刷新
- **玩家侧效果**：
  - 游戏会话结束后可获得结构化的战斗评价（评分 + 星级 + 等级）
  - 结算报告包含击杀明细、连击记录、伤害效率、生存状态等完整数据
- **开发侧接入方式**：
  - `SessionResultManager.Instance.CaptureResult()` 采集结算数据
  - `SessionResultManager.Instance.LatestResult` 获取最新结算
  - `SessionResultManager.Instance.OnResultCaptured += ...` 订阅事件
  - Editor 菜单：Tools > Session Result > ...
  - 结算数据可直接序列化为 JSON 供存档或后端上报
- **验证结果**：静态验证全部通过（详见验证记录）
- **验证记录路径**：`Agent/Reports/2026-04-28/feature_F004_SessionResult/validation_feature_F004.md`
- **未完成项**：
  - Play Mode 运行时验证（需要在 Unity Editor 中运行游戏后采集结算数据）
  - 自动采集逻辑（当前为手动采集，后续可在 Player.Death/波次结束等关键节点接入自动采集）
- **剩余风险**：无（仅新增 3 个独立文件，不修改任何已有代码或资源）
- **后续建议**：
  1. 在 Play Mode 中验证结算数据采集和报告展示
  2. 在 Player.Death() 或 WaveManager.OnAllWavesCleared 中添加自动调用 CaptureResult()
  3. 基于 SessionResultData 创建结算 UI 面板（SessionResultPanel），展示评分、星级和详细数据
  4. 接入存档系统，将结算数据保存到存档中用于跨会话查看
  5. 基于评分/星级设计解锁奖励机制（如 S 级解锁特殊道具）
  6. 可扩展为多关卡分别结算（每个关卡独立评分）
