# F012 天气环境对玩法的影响系统任务卡

## 基本信息

- 候选ID：F012
- 原始候选：天气环境对玩法的影响系统
- 当前状态：已完成
- 本次任务目录：`Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/`
- 全局候选报告路径：`Agent/Reports/feature_discovery.md`
- 任务分类：游戏业务功能开发
- 游戏业务类型：玩家体验 / 环境玩法反馈
- 风险等级：低
- 负责 Agent：GameplayAgent + MapAgent + UIAgent + ToolAgent
- 需要的 Skill：ScriptGenerateSkill、CodeReviewSkill、SceneAnalyzeSkill、TestSkill

## 价值说明

- 玩家价值：天气不再只是视觉变化，雨天和雪天会影响移动、工作效率和灵气恢复，让每日天气有可感知的策略差异。
- 开发价值：激活已有 `WeatherManager` 和 `EnvironmentManager` 数据链路，为后续天气 HUD、天气事件、天气成就和环境玩法扩展提供低侵入基础。

## 影响路径

- `Scripts/2D/Manager/WeatherManager.cs`
- `Scripts/2D/Data/EnvironmentManager.cs`
- `Scripts/2D/Character/Player/Player.cs`
- `Scripts/2D/Core/Seek/ASeek.cs`
- `Scripts/2D/Character/Worker/Task/AWorkerTask.cs`
- `Scripts/2D/Tool/WeatherGameplayTool.cs`
- `Scripts/2D/Gameplay/WeatherGameplayEffect.cs`
- `Scripts/2D/UI/WeatherGameplayHUD.cs`
- `Scripts/2D/Editor/WeatherGameplayEffectMenu.cs`

## 不应触碰路径

- `StreamingAssets/`
- `AddressableAssetsData/`
- `Resources/SO/`
- `Resources/Tilemap/`
- `ResourcesLocal/Prefabs/`
- 存档结构相关字段
- Photon 同步逻辑和 RPC
- 已有 Scene / Prefab / ScriptableObject 资源文件

## 功能边界

- 本次只实现天气对运行时倍率的影响，不新增存档字段。
- 本次不改 AssetBundle、Addressables、StreamingAssets。
- 本次不手写 `Game.unity` YAML，不直接覆盖任何 Prefab。
- 敌人默认不受天气减速影响，避免改变战斗难度曲线。
- 吃饭和睡觉属于恢复行为，不受天气工作减速影响。

## 业务规则说明

- 晴天：玩家移动 1.00x，工人移动 1.00x，工人工作 1.00x，灵气恢复 1.05x。
- 雨天：玩家移动 0.92x，工人移动 0.90x，工人工作 0.94x，灵气恢复 1.12x。
- 雪天：玩家移动 0.84x，工人移动 0.78x，工人工作 0.82x，灵气恢复 0.86x。
- WeatherManager 每次切换天气时触发 `WeatherChanged`，WeatherGameplayEffect 同步状态并请求 Tip 提示。

## 数据流说明

1. `GameTimeUI` 每日调用 `WeatherManager.RandWeather()`。
2. `WeatherManager.SetWeather()` 设置当前天气并触发 `WeatherChanged`。
3. `WeatherGameplayEffect` 订阅天气事件，调用 `WeatherGameplayTool` 计算倍率。
4. `Player.Move()`、`ASeek.MoveByPath()`、`AWorkerTask.Execute()`、`EnvironmentManager.UpdateEnergy()` 读取天气倍率并应用到运行时逻辑。
5. `WeatherGameplayHUD` 和 Editor 菜单可读取 `WeatherGameplayState` 展示当前效果。

## UI 接入策略

- 已确认 `Game.unity` 真实路径：`Scenes/Game.unity`。
- 本次没有直接手写 `Game.unity`，原因是当前环境无法运行 Unity Editor，直接编辑大型场景 YAML 容易破坏已有 UI 引用。
- 本次没有创建 `ResourcesLocal` Prefab，原因是手写 Prefab YAML 需要可靠脚本 GUID 和组件引用校验，当前环境无法完成 Unity 导入验证。
- 已新增 Editor 菜单：`工具/天气玩法影响/创建天气 HUD 到 Game 场景`，可用 Unity 官方 Editor API 在 `Game.unity` 中创建独立 HUD。
- 运行时核心功能不依赖 HUD；未创建 HUD 时仍会通过已有 Tip 系统提示天气变化。

## Scene / Prefab / ResourcesLocal 生成策略

- `Game.unity`：未直接修改，改由 Editor 菜单生成独立 UI 根节点 `Feature_F012_WeatherGameplayHUD_Root`。
- `ResourcesLocal` Prefab：未创建。
- Editor 工具：已创建 `Scripts/2D/Editor/WeatherGameplayEffectMenu.cs`。
- 运行时降级：使用 `GlobalInit.ShowTip()`；缺失时降级为 `Debug.Log`。

## 工具类复用策略

- 已检查工具类：
  - `Scripts/2D/Tool/Tool.cs`
  - `Scripts/2D/Tool/VectorTool.cs`
  - `Scripts/2D/Tool/ResourceTool.cs`
  - `Scripts/2D/Tool/DateTool.cs`
  - `Scripts/2D/Tool/DataTool.cs`
  - `Scripts/2D/Tool/SyncDataTool.cs`
- 本次复用：
  - `Tool.IsUIInputActive()`：HUD 热键输入保护。
  - `Tool.GetComponentInChildren<T>()`：HUD 文本绑定。
- 本次新增公共工具：
  - `Scripts/2D/Tool/WeatherGameplayTool.cs`
  - 用途：天气倍率计算、天气中文名、天气摘要文本、安全倍率计算。
- 业务脚本保留：
  - 天气事件订阅、状态缓存、Tip 提示、角色类型判断、HUD 绑定。
- 工具类保留：
  - 无状态通用计算和文本格式化。

## 执行步骤

1. 读取 Agent 文档、候选报告、历史任务，确认 F012 未完成且低风险。
2. 扫描 `Scripts/2D/Tool`，确认现有工具缺少天气倍率通用方法。
3. 新增 `WeatherGameplayTool` 作为通用计算工具。
4. 新增 `WeatherGameplayEffect` 作为运行时天气玩法数据源。
5. 扩展 `WeatherManager` 当前天气状态和事件。
6. 接入玩家移动、Worker 移动、Worker 任务进度、灵气恢复。
7. 新增 `WeatherGameplayHUD` 和 Editor 菜单生成入口。
8. 完成静态验证和记录回写。

## 验证步骤

1. 检查新增运行时代码不引用 `UnityEditor`。
2. 检查 `Scripts/2D/Tool` 新增工具类命名空间为 `LAB2D`。
3. 检查天气倍率调用点存在且只影响运行时数值。
4. 检查 Editor 菜单位于 `Scripts/2D/Editor`，不会进入运行时构建。
5. 检查 `.meta` 文件已创建。
6. 记录无法运行 Unity 编译和 Play Mode 的原因。

## 回滚方案

1. 删除新增文件：
   - `Scripts/2D/Tool/WeatherGameplayTool.cs`
   - `Scripts/2D/Gameplay/WeatherGameplayEffect.cs`
   - `Scripts/2D/UI/WeatherGameplayHUD.cs`
   - `Scripts/2D/Editor/WeatherGameplayEffectMenu.cs`
   - 对应 `.meta`
2. 从 `WeatherManager.cs` 移除 `CurrentWeather`、`WeatherChanged`、`SetWeather()`，恢复 `RandWeather()` 原逻辑。
3. 从 `EnvironmentManager.cs`、`Player.cs`、`ASeek.cs`、`AWorkerTask.cs` 移除天气倍率调用。
4. 若已通过 Editor 菜单创建 HUD，在 Unity 中执行 `工具/天气玩法影响/从当前场景移除天气 HUD`。

## 结果区

- 最终状态：`[DONE]`
- 已完成内容：天气当前状态、天气事件、天气倍率工具、运行时天气玩法效果、天气 Tip、可选 HUD、Editor 生成菜单。
- 修改的文件：
  - `Scripts/2D/Manager/WeatherManager.cs`
  - `Scripts/2D/Data/EnvironmentManager.cs`
  - `Scripts/2D/Character/Player/Player.cs`
  - `Scripts/2D/Core/Seek/ASeek.cs`
  - `Scripts/2D/Character/Worker/Task/AWorkerTask.cs`
- 新增文件：
  - `Scripts/2D/Tool/WeatherGameplayTool.cs`
  - `Scripts/2D/Gameplay/WeatherGameplayEffect.cs`
  - `Scripts/2D/UI/WeatherGameplayHUD.cs`
  - `Scripts/2D/Editor/WeatherGameplayEffectMenu.cs`
- 新增游戏业务能力：天气影响玩家移动、工人移动、工人任务进度和环境灵气恢复。
- 玩家侧效果：每日天气切换后会出现天气效果 Tip；雨雪天气下移动和工人效率发生差异。
- UI 生成位置：
  - 是否已写入 `Game.unity`：否。
  - 是否已创建 `ResourcesLocal` Prefab：否。
  - 是否改用 Editor 工具：是。
  - 是否改用运行时代码动态创建：否。
- 开发侧接入方式：核心能力自动接入；可在 Unity 中运行 `工具/天气玩法影响/创建天气 HUD 到 Game 场景` 安装 HUD。
- 验证结果：静态检查通过；Unity 编译和 Play Mode 未运行。
- 验证记录路径：`Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/validation_feature_F012.md`
- 未完成项：未在当前环境实际打开 Unity 执行菜单生成 HUD；未做 Play Mode 行为验证。
- 剩余风险：天气倍率需要在 Play Mode 中体验微调；HUD 菜单生成需 Unity Editor 验证。
- 是否复用了 `Scripts/2D/Tool`：是。
- 是否新增或修改了 `Scripts/2D/Tool` 下公共函数：是，新增 `WeatherGameplayTool`。
- 后续建议：在 Play Mode 中用 Editor 菜单模拟晴/雨/雪，微调倍率；可进一步扩展天气对采集掉落、敌人感知或特殊天气事件的影响。
