# 任务卡 — F010 波次事件反馈与波间提示系统

## 基本信息

- 任务 ID：feature_F010_WaveEventFeedback
- 创建时间：2026-04-30
- 候选ID：F010
- 当前状态：Running
- 风险等级：Low
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill |
|---|---|---|---|---|---|---|---|---|---|---|---|
| [TODO] | F010 | 波次事件反馈与波间提示系统 | 关卡玩法 | WaveManager 有5个公开事件但零订阅者；波次开始/结束/清空/休息无任何玩家反馈 | 让玩家感知波次节奏，提升关卡沉浸感 | 补齐波次系统缺失的玩家反馈层 | 低 | 低 | P1 | AINPCAgent + UIAgent | ScriptGenerateSkill |

## 本次任务目录

Agent/Reports/2026-04-30/feature_F010_WaveEventFeedback/

## 任务分类

- 任务类型：gameplay_feedback_feature
- 游戏业务类型：关卡玩法 / 波次反馈
- 负责 Agent：GameplayAgent
- 需要的 Skill：ScriptGenerateSkill、EditorToolSkill

## 玩家价值

- 波次开始/结束/清空时获得即时文字提示反馈，感知关卡节奏
- 波间休息倒计时提示，让玩家有时间准备
- 所有波次完成后的成就感反馈
- 为后续 HUD 波次状态显示提供数据层

## 开发价值

- 补齐 WaveManager 5个事件零订阅者的空缺
- 为 HUD/UI 面板提供波次状态数据源
- 可复用的波次事件监听模式
- 零侵入：不修改 WaveManager 任何代码

## 影响路径

- 新增：`Scripts/2D/Gameplay/WaveEventFeedback.cs` — 波次事件反馈管理器
- 新增：`Scripts/2D/Editor/WaveEventFeedbackMenu.cs` — Editor 调试菜单

## 不应触碰路径

- Scripts/2D/Character/Enemy/EnemyManager.cs（已有 WaveManager 控制逻辑，无需修改）
- Scripts/2D/Gameplay/WaveManager.cs（已稳定，不修改）
- Scenes/、Resources/SO/、ResourcesLocal/Prefabs/、StreamingAssets/
- 存档系统、Photon 同步、AssetBundle

## 风险等级：Low

- 不涉及 Scene、Prefab、ScriptableObject、StreamingAssets、存档、Photon
- 仅新增独立脚本，通过事件订阅与 WaveManager 松耦合
- Tip 反馈使用已有的 GlobalInit.ShowTip 接口，带降级保护

## 功能边界

- 订阅 WaveManager 全部5个事件并提供即时 Tip 提示
- 提供波次状态数据供 HUD 使用
- 提供 Editor 调试菜单
- 不修改 WaveManager 或任何已有代码
- 不接入 UI Prefab
- 不涉及存档或网络同步

## 业务规则说明

1. **波次开始提示**：订阅 OnWaveStart，显示"第 X 波来袭！"
2. **波次结束提示**：订阅 OnWaveEnd，显示"第 X 波已清除！"
3. **全部波次完成提示**：订阅 OnAllWavesCleared，显示"全部波次已清除！"
4. **波间休息提示**：订阅 OnRestStart，显示"休息中... X 秒后开始下一波"
5. **状态更新**：订阅 OnWaveStateChanged，更新内部状态数据供外部查询
6. **降级保护**：GlobalInit 或 Tip Prefab 不可用时，自动降级为 Debug.Log 输出

## 数据流说明

```
WaveManager 事件触发
    ↓
WaveEventFeedback 事件回调
    ↓
├── GlobalInit.ShowTip(text) → TipUI 实例化 → 玩家看到提示
│   └── 降级：Debug.Log(text)
├── OnWaveFeedbackChanged 事件 → 供 HUD/其他系统订阅
└── 更新内部 WaveStateData → 供外部查询
```

## 执行步骤

1. 创建 `Scripts/2D/Gameplay/WaveEventFeedback.cs` — 波次事件反馈管理器
2. 创建 `Scripts/2D/Editor/WaveEventFeedbackMenu.cs` — Editor 调试菜单
3. 静态验证：命名空间、Unity API、空引用保护、事件安全、降级路径
4. 编写验证记录

## 验证步骤

1. 编译验证：检查命名空间、类名、继承关系、Unity API 使用
2. 逻辑验证：检查事件订阅/取消订阅、回调逻辑、降级路径
3. 空引用验证：检查所有外部依赖的 null 检查（WaveManager、GlobalInit）
4. 破坏性验证：确认不修改任何已有文件
5. 代码风格验证：与 ComboBonusManager 风格一致

## 回滚方案

- 删除 `Scripts/2D/Gameplay/WaveEventFeedback.cs`
- 删除 `Scripts/2D/Editor/WaveEventFeedbackMenu.cs`
- 删除对应的 .meta 文件
- 无需恢复任何已有文件修改

## 结果区

- 最终状态：**[DONE]**
- 已完成内容：
  - 创建波次事件反馈管理器（WaveEventFeedback），订阅 WaveManager 全部5个事件
  - 实现5种波次事件的即时 Tip 文字提示反馈（波次开始/结束/全部清空/休息/状态变更）
  - 提供波次状态数据结构（WaveFeedbackState）供 HUD 和外部系统查询
  - 提供 OnWaveFeedbackChanged 事件供外部订阅（HUD 面板等）
  - 提供 OnWaveTipRequested 事件允许自定义提示展示
  - 所有外部依赖均有降级保护（GlobalInit 缺失→Debug.Log、WaveManager 缺失→静默跳过）
  - 提供 Editor 调试菜单（5项：查看状态/启用/禁用/事件订阅状态/模拟Tip测试）
- 修改的文件：无（仅新增文件，零侵入）
- 新增的游戏业务能力：
  - 波次来袭即时提示："第 X 波来袭! 准备迎战!"
  - 波次清除即时提示："第 X 波已清除! (共完成 Y 波)"
  - 全部波次通关提示："全部 X 波已清除! 你已征服所有波次!"
  - 波间休息倒计时提示："休息中... X 秒后下一波开始"
  - 波次状态数据层（WaveFeedbackState）：供 HUD 实时展示波次进度、存活敌人数、难度倍率
  - Editor 菜单：查看反馈状态、启用/禁用反馈、查看 WaveManager 事件订阅详情、模拟 Tip 测试
- 玩家侧效果：
  - 波次开始/结束/通关时有清晰的文字提示反馈
  - 波间休息时知道剩余准备时间
  - 全部波次完成时有成就感反馈
  - 不再"悄悄开始、悄悄结束"
- 开发侧接入方式：
  - 自动接入：首次访问 WaveEventFeedback.Instance 时自动订阅 WaveManager 事件
  - 手动接入：调用 WaveEventFeedback.Instance.Enable() / Disable()
  - HUD 接入：订阅 OnWaveFeedbackChanged 事件，读取 CurrentState 更新 UI
  - 自定义提示：订阅 OnWaveTipRequested 事件替代默认 Tip 显示
  - Editor 调试：Tools > Wave Event Feedback > 系列菜单
- 验证结果：静态验证全部通过（9个维度，40+检查项），Play Mode 待人工完成
- 验证记录路径：Agent/Reports/2026-04-30/feature_F010_WaveEventFeedback/validation_feature_F010.md
- 未完成项：无（核心逻辑已完整实现）
- 剩余风险：无（仅新增2个独立文件，零侵入资源/场景/存档，所有外部依赖均有降级保护）
- 后续建议：
  - 在 Unity Editor Play Mode 中验证波次提示实际显示效果
  - 为 HUD 创建波次状态显示组件（利用 WaveFeedbackState 数据和 OnWaveFeedbackChanged 事件）
  - 可扩展为波次开始的视觉特效（屏幕闪红、边框脉冲等）
  - 波间休息时可添加准备音效
  - 可接入成就系统：首次通关全部波次时触发成就
