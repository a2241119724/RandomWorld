# Agent Task Card — F006 玩家物品收集统计与里程碑提示

## 基本信息

- 任务 ID：F006
- 创建时间：2026-04-28
- 当前状态：Done
- 风险等级：Low
- 候选ID：F006
- 原始候选：玩家物品收集统计与里程碑提示
- 本次任务目录：Agent/Reports/2026-04-28/feature_F006_ItemCollectionMilestone/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 用户需求

> 物品拾取无统计；GameplaySessionStats 已有 RecordItemCollected 但从未调用。补齐 RecordItemCollected 死代码调用，增加收集里程碑即时反馈。

## 任务分类

- 任务分类：gameplay_feature（玩法业务功能）
- 游戏业务类型：收集反馈
- 目标模块：物品收集追踪、里程碑提示、Editor 调试菜单
- 玩家价值：提升收集成就感和目标感，每次达到里程碑时有即时正向反馈
- 开发价值：补齐 RecordItemCollected 死代码，为收集类成就/任务系统提供数据基础

## 主 Agent 分析

- 负责 Agent：ItemDataAgent / GameplayAgent
- 需要的 Skill：ScriptGenerateSkill、EditorToolSkill
- 主要影响路径：`Scripts/2D/Gameplay/`（新增）、`Scripts/2D/Map/ItemMap.cs`（微改）、`Scripts/2D/Editor/`（新增）
- 不应触碰的路径：Scenes、Resources/SO、ResourcesLocal/Prefabs、StreamingAssets、存档、Photon 同步

## Skill 调用计划

| Skill | 调用原因 | 输入 | 预期输出 |
| --- | --- | --- | --- |
| ScriptGenerateSkill | 生成 ItemCollectionTracker 业务脚本 | 里程碑阈值、现有 GameplaySessionStats API | 低侵入独立脚本 |
| EditorToolSkill | 生成 Editor 调试菜单 | 现有 Editor 菜单模式参考 | ItemCollectionMenu |
| TestSkill | 静态验证 | 代码文件 | 编译/逻辑/空引用/边界验证 |

## 上下文快照

- 相关脚本：
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — RecordItemCollected 已定义但零调用
  - `Scripts/2D/Map/ItemMap.cs:167-195` — OnTriggerEnter2D 玩家拾取入口
  - `Scripts/2D/GlobalInit.cs:96` — ShowTip 提示系统入口
  - `Scripts/2D/UI/TipUI.cs` — 自动淡出动画提示 UI
  - `Scripts/2D/Data/ItemDataManager.cs` — 物品名称查询
  - `Scripts/2D/Item/DropManager.cs:144` — ResourceInfo 数据结构
- 相关资源：无直接修改
- 相关场景：无直接修改
- 相关配置：无直接修改

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否（Tip 提示走已有 Prefab，降级保护）
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否
- 是否需要兼容旧数据：否

## 功能边界

1. ItemCollectionTracker 负责：
   - 封装 GameplaySessionStats.RecordItemCollected 调用
   - 维护收集里程碑阈值（1, 5, 10, 25, 50, 100, 200, 500, 1000, 2000, 5000, 10000）
   - 达到里程碑时通过 GlobalInit.ShowTip 给予即时反馈（降级保护）
   - 提供 MilestoneReached 事件供 UI 或其他系统订阅
   - 提供查询接口

2. ItemMap.OnTriggerEnter2D 接入点：
   - 在每次 AddItem 后调用 ItemCollectionTracker.Instance.RecordItemCollected
   - 传入 item.Id 和 Count=1

3. ItemCollectionMenu Editor 菜单：
   - Tools/Item Collection/Show Collection Stats
   - Tools/Item Collection/Show Milestones
   - Tools/Item Collection/Reset Milestones

## 业务规则

- 每次拾取计数 1（单格单物品）
- 里程碑不可逆（一旦达到不会因重置而消失，除非主动 Reset）
- Tip 缺失时不崩溃，降级为 Debug.Log

## 执行步骤

1. 新增 `Scripts/2D/Gameplay/ItemCollectionTracker.cs`
2. 修改 `Scripts/2D/Map/ItemMap.cs` — 在 OnTriggerEnter2D 中添加 RecordItemCollected 调用
3. 新增 `Scripts/2D/Editor/ItemCollectionMenu.cs`
4. 静态验证
5. 回写全局候选报告

## 验证步骤

1. 编译验证：检查命名空间、类名、Unity API 使用、语法
2. 逻辑验证：检查里程碑阈值逻辑、去重、事件触发
3. 空引用验证：检查 Singleton 懒初始化、GlobalInit 降级、Tip 降级
4. 破坏性验证：确认只修改 ItemMap.cs 一处已有文件，不改变已有逻辑
5. 代码风格验证：确认与现有代码风格一致

## 回滚方案

- 删除 `Scripts/2D/Gameplay/ItemCollectionTracker.cs` 及 .meta
- 删除 `Scripts/2D/Editor/ItemCollectionMenu.cs` 及 .meta
- 还原 `Scripts/2D/Map/ItemMap.cs` 中 OnTriggerEnter2D 的修改（删除 RecordItemCollected 调用）

## 结果区

- 最终状态：**[DONE]**
- 已完成内容：
  - 新增 ItemCollectionTracker 物品收集追踪器（独立单例）
  - 在 ItemMap.OnTriggerEnter2D 中接入物品收集统计（补齐 RecordItemCollected 死代码调用）
  - 新增 ItemCollectionMenu Editor 调试菜单
  - 完成静态验证（编译/逻辑/空引用/破坏性/边界条件全部通过）
- 修改的文件：
  - `Scripts/2D/Map/ItemMap.cs` — OnTriggerEnter2D 中新增 RecordItemCollected 调用（最小修改）
- 新增的文件：
  - `Scripts/2D/Gameplay/ItemCollectionTracker.cs` — 收集统计与里程碑追踪器
  - `Scripts/2D/Editor/ItemCollectionMenu.cs` — Editor 调试菜单
- 新增的游戏业务能力：
  - 物品收集自动统计（通过 ItemCollectionTracker → GameplaySessionStats）
  - 收集数量里程碑即时反馈（1, 5, 10, 25, 50, 100, 200, 500, 1000, 2000, 5000, 10000 个物品）
  - 里程碑提示通过 Tip 系统展示（降级保护）
  - MilestoneReached 事件供 UI/成就系统订阅
  - Editor 菜单查看收集统计、里程碑状态、重置里程碑
- 玩家侧效果：
  - 拾取物品时，达到里程碑阈值会弹出 Tip 提示（如"收集里程碑达成: 已收集 50 个物品!"）
  - 可在 F1 统计面板中看到物品收集数量
- 开发侧接入方式：
  - 自动接入：ItemMap.OnTriggerEnter2D 已自动调用 RecordItemCollected
  - 扩展接入：其他物品获取入口（如工人搬运完成）可同样调用 ItemCollectionTracker.Instance.RecordItemCollected
  - UI 接入：订阅 ItemCollectionTracker.Instance.MilestoneReached 事件
- 验证结果：静态验证全部通过（7 个验证维度），Play Mode 待人工完成
- 验证记录路径：Agent/Reports/2026-04-28/feature_F006_ItemCollectionMilestone/validation_feature_F006.md
- 未完成项：无
- 剩余风险：无（仅新增 2 个独立文件 + 修改 1 处已有文件，零侵入资源/场景/存档）
- 后续建议：
  - 在 Unity Editor 中 Play Mode 验证里程碑 Tip 弹窗效果
  - 后续可接入成就系统（基于 MilestoneReached 事件解锁收集成就）
  - 后续可接入收集图鉴 UI（基于 GameplaySessionStats.CollectedItemsById）
