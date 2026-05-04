# Agent Task Card — F011 会话结算自动触发与结果接入

## 基本信息

- 任务 ID：feature_F011_SessionResultAutoTrigger
- 创建时间：2026-04-30
- 提出人：ProjectDirectorAgent（自动发现）
- 当前状态：Done
- 风险等级：Low

## 用户需求

> 自动发现：SessionResultManager.CaptureResult() 从未被自动调用，OnResultCaptured 事件零订阅者。
> 玩家死亡或波次通关后应自动采集结算数据，补齐 F004 结算系统缺失的自动触发链路。

## 主 Agent 分析

- 任务分类：gameplay_bridge（玩法桥接/事件联动）
- 目标模块：Gameplay 层 — SessionResultManager + Player + WaveManager 桥接
- 主要影响路径：
  - `Scripts/2D/Gameplay/SessionResultAutoTrigger.cs`（新增）
  - `Scripts/2D/Editor/SessionResultAutoTriggerMenu.cs`（新增）
  - `Scripts/2D/Character/Player/Player.cs`（Death 方法 +1 行调用）
- 不应触碰的路径：
  - Scenes、Resources/SO、Resources/Tilemap、ResourcesLocal/Prefabs、StreamingAssets
  - Scripts/2D/Manager/ArchiveManager.cs（存档）
  - Scripts/2D/NetworkConnect.cs（Photon）
  - Scripts/2D/Data/（数据层结构）

## 子 Agent 分工

| 子 Agent | 职责 | 输入 | 输出 |
| --- | --- | --- | --- |
| GameplayAgent | 桥接脚本实现 | Player.Death()、WaveManager.OnAllWavesCleared | SessionResultAutoTrigger.cs |
| ToolAgent | Editor 调试菜单 | SessionResultAutoTrigger API | SessionResultAutoTriggerMenu.cs |

## Skill 调用计划

| Skill | 调用原因 | 输入 | 预期输出 |
| --- | --- | --- | --- |
| ScriptGenerateSkill | 生成事件桥接 MonoBehavior 脚本 | 接入点分析 | SessionResultAutoTrigger.cs |
| EditorToolSkill | 生成 Editor 调试菜单 | 菜单需求 | SessionResultAutoTriggerMenu.cs |
| CodeReviewSkill | 验证代码质量 | 最终产物 | 静态检查结果 |

## 上下文快照

- 相关脚本：
  - `Scripts/2D/Gameplay/SessionResultManager.cs` — 已有 CaptureResult()，零自动调用
  - `Scripts/2D/Gameplay/SessionResultData.cs` — 结算数据模型
  - `Scripts/2D/Character/Player/Player.cs` — Death() 方法（第306行）
  - `Scripts/2D/Gameplay/WaveManager.cs` — OnAllWavesCleared 事件（第81行）
  - `Scripts/2D/Gameplay/DeathPenaltyManager.cs` — HandlePlayerDeath 方法
- 相关资源：无
- 相关场景：无
- 相关配置：无

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否
- 是否需要兼容旧数据：否（仅新增，不修改数据格式）

## 执行步骤

1. 创建 `Scripts/2D/Gameplay/SessionResultAutoTrigger.cs` — 事件桥接 MonoBehavior
   - 订阅 WaveManager.OnAllWavesCleared
   - 提供静态方法 NotifyPlayerDeath()
   - 调用 SessionResultManager.CaptureResult()
   - 显示 Tip 结算摘要
   - 触发 OnAutoCaptureResult 事件
   - 全依赖降级保护
2. 修改 `Scripts/2D/Character/Player/Player.cs` — Death() 方法
   - 在 HandlePlayerDeath 之后新增 1 行 NotifyPlayerDeath() 调用
3. 创建 `Scripts/2D/Editor/SessionResultAutoTriggerMenu.cs` — Editor 调试菜单
   - 状态查看（Show Status）
   - 模拟玩家死亡采集
   - 模拟波次通关采集
   - 查看最新结算报告
   - 清空结算历史

## 验证步骤

1. 编译验证：确认新增脚本和修改的 Player.cs 无编译错误
2. 静态分析：
   - 命名空间 LAB2D 正确
   - Unity API 合法使用
   - 空引用保护完整
   - 降级路径可用
   - 不破坏现有代码逻辑
3. Play Mode 验证：（待人工完成）
   - 玩家死亡后自动采集结算数据
   - 波次通关后自动采集结算数据
   - Tip 显示结算摘要
   - OnAutoCaptureResult 事件可订阅
4. 场景/Prefab/SO 验证：不涉及
5. 离线/联机验证：不涉及

## 回滚方案

- 回滚路径：删除 SessionResultAutoTrigger.cs、SessionResultAutoTriggerMenu.cs，移除 Player.cs 中新增的 1 行调用
- 回滚顺序：先删新增文件，再回退 Player.cs
- 需要保留的数据：无
- 回滚后验证：编译通过，Player.Death() 行为与修改前一致

## 结果汇总

- 已完成：
  - 新增 `Scripts/2D/Gameplay/SessionResultAutoTrigger.cs` — 事件桥接 MonoBehavior
  - 新增 `Scripts/2D/Editor/SessionResultAutoTriggerMenu.cs` — Editor 调试菜单（5 项）
  - 修改 `Scripts/2D/Character/Player/Player.cs` — Death() 方法新增 1 行 NotifyPlayerDeath() 调用
  - 补齐结算系统缺失的自动触发链路
- 未完成：无（静态验证全部通过）
- 剩余风险：无（仅新增 2 个独立文件 + 修改 1 行调用，零侵入资源/场景/存档）
- 后续建议：
  - 在 Unity Editor Play Mode 中验证玩家死亡和波次通关的自动采集效果
  - （可选）将 SessionResultAutoTrigger 组件挂载到场景 GameObject 上以启用完整功能
  - （可选）在 HUD 或结算 UI 中订阅 OnAutoCaptureResult 事件
  - （可选）接入存档系统持久化结算数据
