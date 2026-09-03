# M4 包 4 切片：每局修饰符（Session Modifiers）实施方案

> 状态：方案定稿待实施（2026-09-04 第 5 轮调查产出）。排在事件天气（m4-p4-event-weather-plan.md）之后。
> 大纲定位：`docs/游戏大纲.md:645` 包 4「每局不一样」——每局修饰符。

## 可行性结论（调查确认）

- **展示层零资产**：项目有 7+ 个 `EnsureRuntimePanel()` 运行时 HUD 先例（WeatherGameplayHUD 195 行为模板），纯代码创建；开局提示走 `GameServices.ShowTipProvider`。
- **效果通道现成**：灵气浓度合成（LingQiManager）、波次配置（WaveConfigModel）、Worker 任务进度（天气乘数同款叠乘点）均已有多乘数入口，修饰符=再叠一层「按局」乘数。
- **存档模式现成**：ASingletonSaveData（参照 FavorabilityManager）——开局 roll 后入档，读档恢复，幂等。

## 改动面（约 6 文件）

1. `Domain/Gameplay/SessionModifier/SessionModifierRuleService.cs`：修饰符池定义（id/中文名/描述/通道/数值）+ `Roll(int seed, int count)` 纯函数（不重复抽取）；`ModifierChannel` 枚举首版 4 通道：LingQiRecovery / EnemyStrength / WorkerWorkSpeed / EnemyLoot
2. `Gameplay/SessionModifierManager.cs`：IInitializable 宿主——无档 roll（seed=Random）/有档读；暴露 `GetChannelMultiplier(channel)`；入档 `ASingletonSaveData`
3. 接入三点：`LingQiManager` 浓度合成叠乘、`WaveManager.CreateWaveConfigModel`（EnemyStrength→难度/数量）、Worker 任务进度叠乘点（WorkerWorkSpeed）
4. `UI/SessionModifierHUD.cs`：`EnsureRuntimePanel()` 模式（热键实施时查空闲键，InputKeyConstant 统一分发）；列出本局修饰符名+数值+描述
5. 开局 Tip：roll 完成时 `ShowTipProvider("本局天机：...")`
6. 单测：`Editor/Tests/Domain/SessionModifierRuleServiceTests`——同 seed 同结果、不重复、池边界

## 设计约束

- 首版修饰符 6-8 个、每局 roll 2-3 个、数值幅度 ±15%~30%（可感知不破坏平衡）
- 与事件天气正交叠加（乘法叠乘，各自独立）
- 敌方强化类修饰符必须配对收益（如「妖兽凶猛+25% 攻击 / 战利品+40%」），避免纯负面体验
