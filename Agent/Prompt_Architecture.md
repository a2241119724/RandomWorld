# RandomWorld Unity C# 架构改造 Prompt

你是一名资深 Unity 游戏架构师和 C# 工程师。请基于当前 `RandomWorld` 项目代码，帮助我做可落地、渐进式的架构改造，而不是给一份脱离项目现状的通用方案。

当前项目位于：

```text
Assets/
  Scripts/
    2D/
```

主要命名空间是：

```csharp
namespace LAB2D
```

项目已有大量可运行代码，请优先尊重现有结构、命名和玩法表现。改造目标是逐步降低核心游戏逻辑对 Unity 引擎、UI、场景对象和生命周期的强耦合，让核心规则更容易测试、复用和迁移。

## 1. 当前项目背景

这是一个 2D 生存/殖民地/战斗类 Unity 项目，已完成多轮架构分层改造，核心模块如下：

```text
Scripts/2D/Domain/                    # 纯规则层（不含 UnityEngine 引用）
  Common/                             # EventBus、GameVector2、GameGridPosition、IGameCommand/Event/Time/Logger/IInitializable/ITickable 等
  Character/                          # DamageCalculator、LevelProgressionService
  Gameplay/                           # AchievementRuleService、ComboBonusRuleService、SkillRuleService、SessionResultRuleService、DeathPenaltyRuleService、GameplaySessionStatsRuleService 等（10 个文件）
  Inventory/                          # InventoryService、InventoryGrid、InventoryCell、ResourceStack、InventoryStackingService、InventoryFoodReservationService、InventoryTakeReservationService、InventoryGridChangedEvent 等
  Player/                             # PlayerDamagePolicy、PlayerMovementPolicy、PlayerMovementService、IPlayerView、PlayerEvents 等
  Wave/                               # WaveBossRuleService、WaveRuleService、WaveFlowService、WaveSpawnPlanService、WaveConfigModel、WaveRuntimeState、WaveSpawnRequest、WaveFlowDecision、WaveEvents（4 个 IGameEvent）、IWaveStateProvider 等（12 个文件）
  Worker/                             # WorkerAgentSnapshot、WorkerTaskAssignmentService、WorkerTaskProgressService、WorkerConditionRuleService、WorkerEfficiencyRuleService、CurrencyManager/CurrencyAmount、WorkerPersonality、WorkerGoal、BountyData 等
  Dialogue/                           # PromptAssemblyService、DialoguePromptProfileModel、IPromptTemplateProvider、ChatMessage 等

Scripts/2D/UnityAdapter/             # Unity 适配层
  UnityGameTime.cs / UnityLogger.cs / UnityVectorAdapter.cs / UnityPlayerInputAdapter.cs
  UnityMapAdapter.cs / UnityEnemySpawnAdapter.cs / UnityItemDefinitionAdapter.cs / TipHelper.cs
  UnityWaveSceneAdapter.cs / UnityWaveTimeScheduler.cs / UnityGlobalInputAdapter.cs / PlayerViewAdapter.cs
  IWaveSceneAdapter.cs / IWaveTimeScheduler.cs / PlayerViewAdapter.cs

Scripts/2D/Core/                      # 基础设施层
  ServiceLocator.cs                   # 轻量级服务定位器（DI 过渡方案）
  GlobalInputProcessor.cs             # 全局输入处理器（ITickable 实现）
  KDTree / A* 寻路(Seek) / Singleton / Lock / GlobalPanelInitializer / MonoBehaviourInit

Scripts/2D/Gameplay/                  # 玩法管理器（依赖 Domain 和 UnityAdapter）
  WaveManager.cs / SkillManager.cs / WeatherGameplayEffect.cs / AchievementManager.cs
  SessionResultManager.cs / ComboBonusManager.cs / DeathPenaltyManager.cs
  FloatingTextManager.cs / WorkerConditionManager.cs / WorkerEfficiencyTracker.cs
  WorkerUpdateSystem.cs / ColonyCommandCenterManager.cs / TaskBoardManager.cs
  PlayerBountyService.cs / ShopNPCGenerator.cs / TerrainEffectManager.cs 等（共 30+ 个文件）

Scripts/2D/Character/
  Character.cs / CharacterHealthComponent.cs / CharacterDamageUIPresenter.cs
  CharacterManager.cs / ICharacterManager.cs / ICharacterCreator.cs 等接口
  Player/         # Player.cs / PlayerManager.cs / PlayerCreator.cs
  Enemy/          # AEnemy.cs / EnemyManager.cs / CommonEnemy/ / SeekEnemy/
  Worker/         # AWorker.cs / WorkerManager.cs / WorkerTaskManager.cs
    State/        # WorkerAttack/Dead/Escape/Move/Seek/WorkState
    Task/         # AWorkerTask.cs（Provider 委托模式）、WorkerBuild/Carry/Gather(采集+挖掘)/Hungry/Plant/Home/BountyTask + Individual/

Scripts/2D/Item/
  InventoryManager.cs / DropManager.cs / ItemInstanceFactory.cs
  Backpack/       # Consumable / Equipment(Weapon: Gun/Sword) / Food / Material / Seed
  Build/          # Door / Furniture(Bed) / Room / Wall

Scripts/2D/Map/
  TileMap.cs / BuildMap.cs / ItemMap.cs / ResourceMap.cs / GatherMap.cs / IsAvailableMap.cs 等

Scripts/2D/AI/Dialogue/
  Core/           # DialogueManager.cs / DialogueSession.cs / NPCDialogueTrigger.cs
  LLM/            # ILLMClient.cs / LlamaServerClient.cs / RemoteAPIClient.cs 等
  Prompt/         # PromptBuilder.cs / PromptTemplateLoader.cs / NPCPromptProfile.cs / INPCPromptProfileProvider.cs / ResourcesNpcPromptProfileProvider.cs
  RAG/            # GameKnowledgeRetriever.cs / GameKnowledgeEntry.cs
  Memory/         # DialogueMemoryManager.cs / ShortTermMemory.cs
  UI/             # DialoguePanel.cs / StreamingTextView.cs

Scripts/2D/Network/                   # 网络适配层
  INetworkView.cs / NetworkViewAdapters.cs / SyncSenderAdapters.cs

Scripts/2D/UI/
  Action/         # BuildingUI / GatherUI / ItemInfoUI / SelectUI 等
  Character/      # PlayerStatusUI / CharacterStatusUI / WorkerBedUI 等
  Effect/         # BloodUI / DamageUI / EquipmentBeam
  Panel/PanelUI/ForegroundUI/  # GameInfoUI / ToolMenu / Joystick 等

Scripts/2D/MVC/                       # Backpack/ 和 Build/ 各自的 Controller/Model/View

Scripts/2D/Enum/                      # 20 个公共枚举文件
Scripts/2D/Constant/                  # 18 个公共常量文件
Scripts/2D/Tool/                      # 20 个工具脚本
Scripts/2D/Manager/                   # ArchiveManager / LogManager / ResourceManager 等
Scripts/2D/Data/                      # GlobalData / ItemData / ISaveData / ISyncData 等
Scripts/2D/Serializable/              # Vector3LAB 等
Scripts/2D/Editor/                    # Editor 工具 + Tests/Domain + Tests/Tool 单元测试
```

项目已有自定义基础设施：

- `Singleton<T>` / `ASingletonSaveData<T>` / `ServiceLocator`（轻量级 DI 容器，已全局应用）
- `ITickable` / `IInitializable`（Domain 层生命周期接口，GlobalInit 统一驱动）
- `CharacterManager<CM, C, CC>` / 相关接口 `ICharacterManager` 等
- `Vector3LAB` / `Vector3IntLAB` / `GameVector2` / `GameGridPosition`（Domain 层已有纯 C# 值类型）
- `LogManager` / `IGameLogger`（已有 UnityAdapter 适配）
- `ResourceManager`
- `EventBus`（Domain 层纯 C# 事件总线，支持泛型 Publish + DynamicInvoke PublishInternal）
- `Tool` / `DataTool` / `VectorTool` 等 20 个工具类
- 20 个 `Enum` 公共枚举文件
- 18 个 `Constant` 公共常量文件
- `UnityAdapter/` 下 11 个适配器（Time、Input、Map、Vector、Logger、EnemySpawn、ItemDefinition、TipHelper、WaveScene、WaveTimeScheduler、GlobalInputAdapter）
- `Network/` 下 3 个网络适配脚本（INetworkView、NetworkViewAdapters、SyncSenderAdapters）
- `Editor/Tests/Domain` 和 `Editor/Tests/Tool` 下已有单元测试
- 多个 `Editor` 菜单用于安装、验证和调试功能

### 已完成的关键架构改造

项目已完成多轮分层改造，以下是已落地的基础设施：

#### 1. ServiceLocator — 轻量级服务定位器

位于 `Scripts/2D/Core/ServiceLocator.cs`。从 Singleton 到依赖注入的过渡方案，不依赖反射/自动装配。

- 所有服务注册由 `GlobalInit` 在启动时显式完成
- 服务分两批注册：
  - `RegisterSafeServices()`：`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 提前注册约 40 个 `Singleton<T>`/`ASingletonSaveData<T>` 服务
  - `RegisterServices()`：Awake 中注册约 11 个 MonoBehaviour 服务和 UnityAdapter 实例
- 支持按接口类型和具体类型注册（如 `ServiceLocator.Register<ISkillManager>(SkillManager.Instance)`）
- 提供 `Get<T>()`、`TryGet<T>()`、`GetAll<T>()`、`IsRegistered<T>()`、`Reset()` 等方法
- 已全局替换直接 `.Instance` 调用为 `ServiceLocator.Get<T>()` 获取

#### 2. ITickable / IInitializable — 生命周期接口（Domain 层）

位于 `Scripts/2D/Domain/Common/IInitializable.cs`。纯 C# 接口，无 UnityEngine 依赖。

- `ITickable.Tick(float deltaTime)`：代替硬编码 Update 调用，由 GlobalInit 统一驱动
- `IInitializable.Initialize()`：代替分散的 Awake/Start 初始化，按顺序批量执行
- GlobalInit 维护 `orderedTickables` / `orderedInitializables` 列表，通过 `BuildTickableList()` / `BuildInitializableList()` 显式控制顺序
- 当前 ITickable 实现（6 个）：`WorkerUpdateSystem`、`AchievementManager`、`GlobalInputProcessor`、`WorkerTaskManager`、`EnvironmentManager`、`PlayerVitalAlertManager`
- 当前 IInitializable 实现（5 个）：`AchievementManager`、`SkillManager`、`EquipmentBeamManager`、`EnemyLootManager`、`ComboBonusManager`

#### 3. GameLoggerFactory — 统一日志工厂

位于 `Scripts/2D/Domain/Common/GameLoggerFactory.cs`。统一获取 `IGameLogger`，替换所有硬编码 `Debug.Log`。

- 31 个文件已完成迁移（Gameplay/ Manager/ UI/ Character 等各层）
- 通过 `ServiceLocator.Get<IGameLogger>()` 获取，日志实现由 `UnityLogger` 适配器提供
- 消除了跨模块的 `Debug.Log` 硬编码依赖

#### 3. GlobalInputProcessor — 全局输入处理解耦

位于 `Scripts/2D/Core/GlobalInputProcessor.cs`。从 GlobalInit 中提取的职责，实现 `ITickable`。

- 处理 ESC 键面板切换、鼠标点击关闭物品信息、成就面板切换
- 分离了 Update 逻辑与输入处理逻辑
- 已移除 ColonyCommandHud 输入处理（该逻辑已由 ColonyCommandCenterHUD 自行管理）

#### 4. AWorkerTask Provider 委托模式 — 依赖注入的轻量替代

位于 `Scripts/2D/Character/Worker/Task/AWorkerTask.cs`。使用静态 `Func`/`Action` 属性实现可替换的外部依赖。

- 约 35 个静态 Provider 属性，覆盖日志、地图、库存、物品、网络、UI 等外部依赖
- 每个 Provider 都有默认实现（访问现有 Singleton），可在测试中替换为桩
- 关键 Provider 示例：
  - `LogProvider`：日志输出（替代 `LogManager.Instance`）
  - `ProgressMultiplierProvider`：任务进度倍率（组合天气 + Worker 状态）
  - `WalkabilityProvider`：地图可通过性查询
  - `TaskLifecycleProvider`：任务生命周期追踪
  - `TaskCompletionProvider`：任务队列移除
  - `InventoryProvider`、`ItemDataProvider`、`ItemMapProvider`：库存/物品操作
  - `NetworkIsOnlineProvider`：网络状态查询
  - `ShowTipProvider`、`FloatingTextProvider`：UI 通知
- 各子类任务（`WorkerBuildTask`、`WorkerCarryTask` 等）通过 Provider 调用外部服务，不再直接依赖 Singleton

#### 5. EventBus 增强

位于 `Scripts/2D/Domain/Common/EventBus.cs`。纯 C# 实现。

- 支持泛型 `Publish<T>(T gameEvent) where T : IGameEvent`
- 新增 `PublishInternal(IGameEvent gameEvent)`：通过 DynamicInvoke 支持非泛型发布
- 已有单元测试覆盖：`EventBusTests.cs`、`DomainEventTests.cs`

#### 6. Dialogue 层接口抽象

- `INPCPromptProfileProvider`：NPC 配置提供者接口（`Scripts/2D/AI/Dialogue/Prompt/`）
- `ResourcesNpcPromptProfileProvider`：基于 `Resources.LoadAll` 的实现
- `IPromptTemplateProvider`：Domain 层 Prompt 模板提供者接口
- `PromptAssemblyService`：Domain 层纯 C# Prompt 组装服务
- `PromptBuilder`：支持构造函数注入 provider，可通过 `ServiceLocator` 获取

#### 7. UI 系统手动创建节点范式 — 移除动态自动生成

2026-07 完成。所有 UI 组件从"自动创建节点、挂载组件"改为"查找场景中已有节点"。

- **核心原则**：UI 节点需提前在 Scene 或 Prefab 中手动创建配置，代码只负责查找和绑定
- **查找失败处理**：节点不存在时输出警告提示开发者手动创建，不再自动生成
- **移除的自动创建逻辑**：
  - 移除自动创建 EventSystem 的逻辑
  - 移除冗余的 Canvas 自动创建代码
  - 移除 FloatingTextManager 中 Canvas 和对象池容器的自动创建
  - 移除 EquipmentBeamManager 中 beamContainer 的自动创建
- **简化的 Editor 工具**：AchievementMenu、ColonyCommandCenterMenu、SkillMenu、WeatherGameplayEffectMenu 等 Editor 菜单工具已简化
- **EnsureReferences 模式**：多个 UI 面板抽取统一的 `EnsureReferences()` 方法，在 Awake 中自动调用，消除重复的组件查找初始化代码
- **涉及的 UI 模块**：AchievementPanel、AchievementPopup、NearbyItemPickupHUD、SkillHUD、ColonyCommandCenterHUD、WeatherGameplayHUD、WorkerConditionHUD、WorkerSupplyHUD、WorkerTaskQueueHUD、FloatingTextManager、EquipmentBeamManager

## 2. 改造总目标

目标不是彻底移除 Unity，而是把职责分清：

```text
核心规则层：游戏规则、数据模型、数值计算、状态推进、任务分配、事件结果
Unity 适配层：Input、Time、Transform、TileMap、Resources、Photon、MonoBehaviour 生命周期的桥接
表现层：UI、动画、音效、特效、相机、场景对象、Prefab、Editor 菜单
```

理想状态：

- 核心规则可以在普通 C# 环境中单元测试
- Unity 脚本主要负责采集输入、调用规则、同步表现
- UI、动画、特效、音效由事件或回调驱动
- 地图、资源、存档、网络等外部能力通过接口隔离
- 每次改造后 Unity 项目仍然能运行

## 3. 重要约束

请严格遵守：

1. 不要一次性重构全部项目。
2. 不要破坏现有玩法、Prefab 绑定、Inspector 字段、AssetBundle、Photon 同步和 Editor 菜单。
3. 不要为了设计模式而设计模式。
4. 不要引入大型依赖注入框架；项目已使用 `ServiceLocator` + Provider 委托模式作为过渡方案。
5. 不要把所有 Singleton 一次性替换掉，可以先用 `ServiceLocator` 注册 + Provider 委托包装。
6. 不要把 Unity `ScriptableObject` 强行移出 Unity；它可以作为配置源，但核心规则不要直接依赖它。
7. 不要让 `Domain` 或纯规则类使用 `using UnityEngine;`。
8. 不要让纯规则类继承 `MonoBehaviour`、`MonoBehaviourPun` 或使用 Unity 生命周期方法。
9. 不要让核心规则直接调用 UI，例如 `PlayerStatusUI.Instance`、`ItemInfoUI.Instance`。
10. 不要让核心规则直接播放动画、音效、特效、生成 Prefab。
11. 输出代码时必须给完整文件内容，不能只给片段。
12. 如果信息不足，请基于已读代码给出最小可执行方案，不要空泛回答。
13. **每次根据本 Prompt 完成代码改造后，必须同步更新本 Prompt 文件**（`Agent/Prompt_Architecture.md`），反映最新的模块结构、Domain 服务清单、解耦进度、后续优先级变化。避免文档与代码脱节。

## 4. 分层建议

项目已完成多轮分层，当前实际结构与推荐目标：

```text
Assets/Scripts/2D/
  Domain/                     # ✅ 已存在：纯 C# 领域模型、规则、事件、命令、生命周期接口
    Common/                   # EventBus(IInitializable/ITickable)、GameVector2、GameGridPosition、IGameCommand/Event/Time/Logger、MathHelper
    Character/                # DamageCalculator、LevelProgressionService
    Gameplay/                 # AchievementRuleService、ComboBonusRuleService、SkillRuleService、SessionResultRuleService 等（10 个文件）
    Inventory/                # InventoryService、InventoryGrid、InventoryCell、ResourceStack、InventoryStackingService、InventoryFoodReservationService、InventoryTakeReservationService、InventoryGridChangedEvent
    Player/                   # PlayerDamagePolicy、PlayerMovementPolicy、PlayerVitalAlertRuleService
    Wave/                     # WaveBossRuleService、WaveConfigModel、WaveRuleService
    Worker/                   # WorkerAgentSnapshot、WorkerConditionRuleService、WorkerTaskAssignmentService、WorkerTaskProgressService、WorkerTaskCongestionRuleService、WorkerSupplyRuleService
    Dialogue/                 # PromptAssemblyService、DialoguePromptProfileModel、ChatMessage、IPromptTemplateProvider
  UnityAdapter/               # ✅ 已存在：Unity 类型、输入、时间、地图、资源的适配（14 个文件）
    UnityGameTime.cs / UnityLogger.cs / UnityVectorAdapter.cs / UnityPlayerInputAdapter.cs
    UnityMapAdapter.cs / UnityEnemySpawnAdapter.cs / UnityItemDefinitionAdapter.cs / TipHelper.cs
    UnityWaveSceneAdapter.cs / UnityWaveTimeScheduler.cs / UnityGlobalInputAdapter.cs / PlayerViewAdapter.cs
    IWaveSceneAdapter.cs / IWaveTimeScheduler.cs
  Core/                       # ✅ 已存在：ServiceLocator / GlobalInputProcessor / KDTree / A* 寻路 / Singleton / Lock
  Network/                    # ✅ 已存在：Photon 网络适配层
    INetworkView.cs / NetworkViewAdapters.cs / SyncSenderAdapters.cs
  Gameplay/                   # 玩法管理器（10 个非 MonoBehaviour Manager 已移除 using UnityEngine；通过 ServiceLocator + EventBus 解耦）
  Character/                  # 角色类（已有接口 + AWorkerTask Provider 委托模式）
  UI/ / MVC/ / Item/ / Map/   # 业务层（保持现状，逐步通过事件驱动与 Domain 交互）
  Enum/ / Constant/ / Tool/   # 公共代码层
  Editor/                     # Editor 工具 + Tests/Domain + Tests/Tool 单元测试
```

当前不再需要的目录：
- `Application/`：未创建，用例服务逻辑可直接放在 Domain 或 Gameplay 中。
- `Presentation/`：未创建，表现层使用现有 UI/ + Character/ 中的 Presenter 类（如 CharacterDamageUIPresenter）。

后续改造优先在已有 `Domain/`、`UnityAdapter/`、`Core/` 目录中扩展，不创建新顶层目录。

## 5. 当前高耦合重点

分析项目时，请优先关注这些真实耦合点。

### Player / Character

代表文件：

```text
Scripts/2D/Character/Character.cs
Scripts/2D/Character/Player/Player.cs
```

常见问题：

- `Character` 继承 `MonoBehaviourPun`，同时包含生命值扣减、伤害计算、升级、装备属性计算、伤害 UI、浮动文字、受击变色、死亡处理。
- `Player` 中混合了输入读取、移动、动画、相机、Photon、本地玩家状态、技能热键、UI 刷新、无敌帧、死亡惩罚。
- 直接依赖 `Input`、`Time.time`、`Animator`、`Rigidbody2D`、`SpriteRenderer`、`Transform`、`Camera.main`、`GameObject.FindGameObjectWithTag`。
- 直接调用 `PlayerStatusUI.Instance`、`ForegroundPanel.Instance`、`DeathPenaltyManager.Instance`、`SkillManager.Instance` 等全局对象。

已有 Domain 抽离：

- `Domain/Character/DamageCalculator.cs` — DamageCalculator
- `Domain/Character/LevelProgressionService.cs` — LevelProgressionService
- `Domain/Player/PlayerDamagePolicy.cs` — PlayerDamagePolicy
- `Domain/Player/PlayerMovementPolicy.cs` — PlayerMovementPolicy
- `Domain/Player/PlayerVitalAlertRuleService.cs` — PlayerVitalAlertRuleService
- `CharacterDamageUIPresenter.cs` — CharacterDamageUIPresenter（表现层绑定）
- `CharacterHealthComponent.cs` — CharacterHealthComponent
- `UnityAdapter/UnityPlayerInputAdapter.cs` — UnityPlayerInputAdapter
- `UnityAdapter/UnityGlobalInputAdapter.cs` — UnityGlobalInputAdapter（全局快捷键输入）
- `UnityAdapter/PlayerViewAdapter.cs` — PlayerViewAdapter（Animator/Rigidbody2D/SpriteRenderer/Camera 表现封装）
- `Domain/Player/IPlayerView.cs` — IPlayerView（表现层抽象接口）
- `Core/GlobalInputProcessor.cs` — GlobalInputProcessor（ITickable，从 GlobalInit 提取）

已完成的解耦：

- 项目范围的 Singleton 直接调用已替换为 `ServiceLocator.Get<T>()`
- GlobalInit 的输入处理已提取到 `GlobalInputProcessor`
- 部分 UI 调用已通过 `AWorkerTask` Provider 委托模式解耦
- ✅ **Player 表现层提取** — `Animator`/`Rigidbody2D`/`SpriteRenderer`/`Camera` 操作从 Player.cs 移至 `PlayerViewAdapter`，通过 `IPlayerView` 接口隔离（2026-07）
- ✅ **Player Unity 初始化 Provider 提取** — 新增 5 个 Provider（`RigidbodySetupProvider`、`AnimatorProvider`、`MainCameraProvider`、`MiniCameraProvider`、`PlayerNameDisplayProvider`），覆盖 GetComponent<Rigidbody2D>/<Animator>、Camera.main、GameObject.Find、Text 显示等 Unity 初始化调用。删除死代码 `direction` 字段。`transform.position` 在 ReduceHp 事件边界处显式提取（2026-07）
- ✅ **Character.cs 初始化 Provider 提取 + 类型依赖消除** — 新增 `CharacterRootParentProvider`、`SpriteRendererSetupProvider` 2 个 Provider。新增虚属性 `IsPlayerCharacter` 消除 `CharacterData.ComputeAttribute()` 中 `this is PlayerData` 类型检查，移除 `using PlayerCharacter` 别名导入（2026-07）
- ✅ **CharacterHealthComponent 类型检查消除** — `ApplyDamage()` 中 `attacker is PlayerCharacter` 替换为 `attacker.IsPlayerCharacter` 虚属性调用，移除 `using PlayerCharacter = LAB2D.Character.Player.Player` 别名导入，消除 `CharacterHealthComponent` → `Player` 具体类型的反向依赖（2026-07）
- ✅ **Character.ReduceHp WorldPositionProvider 提取** — 新增 `WorldPositionProvider` 静态委托（`Func<Character, GameVector2>`），将 `ReduceHp()` 中 `this.transform.position.x/y` 的 Transform 直接访问提取为可替换 Provider。默认实现封装 Transform 访问，测试中可替换为固定坐标桩。Character.cs Provider 委托总数增至 21 个（2026-07）

优先抽离方向（本轮会话已完成 Character/Player 深入解耦全部标注项）：

- `PlayerMovementIntent`（配合已有 PlayerMovementPolicy + PlayerMovementService）
- `PlayerCommand` / `PlayerEvent`（✅ 部分已实现：PlayerAttackCommand、ActivateSkillCommand、PlayerAttackRequestedEvent、PlayerSkillActivatedEvent、PlayerStatusChangedEvent）
- `ICharacterCreator` 等接口（✅ 已完成：`CharacterCreator<CC>.Instance` 已替换为 `ServiceLocator.Get<CC>()`，非 UI .Instance 全部清零，2026-07）

### Worker Task

代表文件：

```text
Scripts/2D/Character/Worker/WorkerTaskManager.cs
Scripts/2D/Character/Worker/Task/AWorkerTask.cs
```

常见问题：

- ~~`WorkerTaskManager : MonoBehaviour` 负责任务队列、任务分配、距离计算、任务状态更新、Debug UI 刷新。~~ **✅ 已解决**：已实现 `ITickable`，Update 逻辑迁移至 `Tick(float)`，由 GlobalInit 统一驱动。
- `AWorkerTask` 虽然不是 `MonoBehaviour`，但依赖 `UnityEngine`、`UnityAction<AWorker>`、`Time.deltaTime`、`Mathf`、`BuildMap.Instance`、`WorkerConditionManager.Instance`。
- 任务规则依赖 `AWorker` 的 `transform.position` 和 Unity 地图对象。

已有 Domain 抽离：

- `Domain/Worker/WorkerTaskAssignmentService.cs` — WorkerTaskAssignmentService
- `Domain/Worker/WorkerTaskProgressService.cs` — WorkerTaskProgressService
- `Domain/Worker/WorkerConditionRuleService.cs` — WorkerConditionRuleService
- `Domain/Worker/WorkerTaskCongestionRuleService.cs` — WorkerTaskCongestionRuleService
- `Domain/Worker/WorkerSupplyRuleService.cs` — WorkerSupplyRuleService
- `Domain/Worker/WorkerAgentSnapshot.cs` — WorkerAgentSnapshot
- `Domain/Worker/WorkerTaskSnapshot.cs` — WorkerTaskSnapshot
- `Domain/Worker/WorkerTaskQueueSnapshot.cs` — WorkerTaskQueueSnapshot
- `Domain/Worker/WorkerTaskAssignmentReport.cs` — WorkerTaskAssignmentReport
- `Domain/Worker/WorkerSupplyReport.cs` — WorkerSupplyReport
- `Domain/Worker/WorkerTaskCongestionReport.cs` — WorkerTaskCongestionReport
- `Domain/Worker/WorkerTaskQueue.cs` — WorkerTaskQueue（泛型多优先级任务队列）
- `Domain/Worker/WorkerTaskQueueChangedEvent.cs` — WorkerTaskQueueChangedEvent（EventBus 事件）

已完成的解耦：

- **AWorkerTask Provider 委托模式**：约 35 个静态 `Func`/`Action` 属性替代了直接 Singleton 调用，**100% ServiceLocator 覆盖**（零 .Instance）
  - `LogProvider`、`WalkabilityProvider`、`ProgressMultiplierProvider`
  - `TaskLifecycleProvider`、`TaskCompletionProvider`
  - `InventoryProvider`、`ItemDataProvider`、`ItemMapProvider`
  - `NetworkIsOnlineProvider`、`FloatingTextProvider`、`ShowTipProvider`、`AttackEffectProvider`、`AsyncProgressProvider`×4、`LocateWorkerUIProvider`×2 等
  - 补注册 4 个服务：`AttackEffectManager`、`AsyncProgressUI`（已自注册）、`LocateWorkerUI`、`GlobalInit`（2026-07）
- `WorkerUpdateSystem` 实现 `ITickable`，由 GlobalInit 统一驱动
- `WorkerTaskTimeConfig` 从任务中分离为独立配置类
- ✅ **WorkerTaskManager 实现 ITickable** — `Update()` 迁移至 `Tick(float)`，GlobalInit 注册（2026-07）
- ✅ **WorkerTaskManager API 迁移到 GameGridPosition** — `GatherPositions`、`DeleteHungryTask(GameGridPosition)`、`CancelGatherTask(GameGridPosition)`，旧 Vector3Int API 标记 Obsolete（2026-07）
- ✅ **WorkerTaskManager 内部存储已迁移** — `gatherPositions: List<GameGridPosition>` 替代 `GatherPos: List<Vector3Int>`
- ✅ **AWorkerTask Provider 更新** — `DeleteHungryTaskProvider` 默认实现使用 GameGridPosition 转换

已完成解耦：
- ~~`WorkerTaskModel`~~ ✅ WorkerTaskQueue + WorkerTaskSnapshot（Domain/Worker）
- ~~`WorkerTaskQueue`~~ ✅ Domain/Worker/WorkerTaskQueue.cs
- ~~`WorkerTaskManager Update → ITickable`~~ ✅ 已完成
- ~~`WorkerTaskManager API → GameGridPosition`~~ ✅ 已完成
- ~~`CreateWorkerSnapshot transform.position`~~ ✅ WorkerPositionProvider（模式 B）
- `IWorkerTaskMapQuery`（地图查询接口）
- `WorkerTaskEvent`（通过 EventBus 通知任务状态变化）— 已有 WorkerTaskQueueChangedEvent

### Inventory / Item

代表文件：

```text
Scripts/2D/Item/InventoryManager.cs
Scripts/2D/Item/AItem.cs
Scripts/2D/Data/ItemDataManager.cs
```

常见问题：

- `InventoryManager` 是 `Singleton<InventoryManager>`，但内部大量使用 `Vector3Int`、`AWorker`、`ItemDataManager.Instance`、`ItemMap.Instance`、`WorkerTaskManager.Instance`、`ItemInfoUI.Instance`。
- 库存规则、地图格子、Worker 预占资源、UI 刷新混在一起。
- `ToString(pos)` 同时承担调试文本和业务状态读取。

已有 Domain 抽离：

- `Domain/Inventory/InventoryService.cs` — InventoryService（✅ 新增：纯 C# 库存服务，包装 InventoryGrid + 3 个 Service）
- `Domain/Inventory/InventoryGrid.cs` — InventoryGrid（纯 C# 网格模型，含位置/ID/类型三层索引 + 空格子 id=-1 索引）
- `Domain/Inventory/InventoryCell.cs` — InventoryCell（纯 C# 格子模型，包装 ResourceStack）
- `Domain/Inventory/ResourceStack.cs` — ResourceStack（不可变资源堆栈值对象）
- `Domain/Inventory/InventoryGridChangedEvent.cs` — InventoryGridChangedEvent（✅ 新增：纯数据事件）
- `Domain/Inventory/InventoryStackingService.cs` — InventoryStackingService
- `Domain/Inventory/InventoryFoodReservationService.cs` — InventoryFoodReservationService
- `Domain/Inventory/InventoryTakeReservationService.cs` — InventoryTakeReservationService
- `UnityAdapter/UnityItemDefinitionAdapter.cs` — UnityItemDefinitionAdapter
- `UnityAdapter/UnityVectorAdapter.cs` — ToGameGridPosition / ToVector3Int（✅ 新增 ToVector3Int 便捷方法）
- `AWorkerTask` 中的 `InventoryProvider`、`ItemDataProvider`、`ItemMapProvider` 等委托

已完成的解耦：

- ✅ `InventoryManager` 内部数据存储已从 3 个并行 Dictionary 迁移至 `InventoryService`（包装 `InventoryGrid`）
- ✅ `posToResource` / `id2Resource` / `TypeToResource` 不再作为独立字典维护
- ✅ `Vector3Int` ↔ `GameGridPosition` 转换在 API 边界通过 `UnityVectorAdapter` 完成
- ✅ 所有 public API 签名保持 100% 兼容
- ✅ `Editor/Tests/Domain/InventoryServiceTests.cs`（25 个单元测试）
- ✅ `ItemInfoUI` 已从 `InventoryCellChangedEvent` 迁移至 `InventoryGridChangedEvent`（纯结构化数据事件）
- ✅ `InventoryManager` 已停止发布 `InventoryCellChangedEvent`，`InventoryGridChangedEvent` 由 `InventoryService.PublishChange()` 统一发布（2026-07）

剩余工作：

- `preTakeResource` / `prePlaceResource` 仍以 `AWorker` 为 key（依赖 MonoBehaviour），未下沉到 Domain → ✅ 已完成：key 类型已从 `AWorker` 迁移为 `int` (worker.GetInstanceID())（2026-07）
- `TypeToResource` getter 每次动态计算（兼容层），建议后续调用方迁移到 `GetPositionsByType()`

### Wave / Gameplay

代表文件：

```text
Scripts/2D/Gameplay/WaveManager.cs
Scripts/2D/Gameplay/WaveBossRewardManager.cs
Scripts/2D/Gameplay/WeatherGameplayEffect.cs
```

常见问题：

- `WaveManager` 是普通 `Singleton<T>`，但依赖 `UnityEngine`、`Coroutine`、`WaitForSeconds`、`TileMap.Instance.StartCoroutine`、`EnemyManager.Instance.Create`、`PlayerManager.Instance.Mine.transform.position`。
- 波次规则、等待时间、生成位置、敌人创建、UI 事件混在一个类里。

已有 Domain 抽离：

- `Domain/Wave/WaveRuleService.cs` — WaveRuleService
- `Domain/Wave/WaveBossRuleService.cs` — WaveBossRuleService
- `Domain/Wave/WaveConfigModel.cs` — WaveConfigModel
- `UnityAdapter/UnityEnemySpawnAdapter.cs` — UnityEnemySpawnAdapter
- `UnityAdapter/UnityWaveSceneAdapter.cs` — UnityWaveSceneAdapter
- `UnityAdapter/UnityWaveTimeScheduler.cs` — UnityWaveTimeScheduler

已完成的解耦：

- ✅ **EnemyManager.IsWaveControlEnabled 适配器封装** — `IWaveSceneAdapter` 新增 `SetWaveControlEnabled(bool)`，WaveManager 零直接 EnemyManager 引用（2026-07）
- ✅ **WaveBossRewardManager BossVisualProvider 提取** — 新增 `BossVisualProvider` 静态委托（模式 B），`ApplyBossScale()` 中 Transform/SpriteRenderer/Color 操作已提取（2026-07）

已完成解耦：
- `WaveState` → ✅ `WaveRuntimeState`（Domain/Wave）
- `WaveSpawnRequest` → ✅ 已存在于 Domain/Wave
- `WaveEvent` → ✅ 已完成：4 个 IGameEvent 类型 + WaveManager EventBus 双通道发布（2026-07）
- `IEnemySpawnService` → ✅ UnityEnemySpawnAdapter
- `IWaveTimeScheduler` → ✅ UnityWaveTimeScheduler
- `IMapSpawnPointProvider` → ✅ UnityMapAdapter
- `IWaveSceneAdapter` → ✅ UnityWaveSceneAdapter（含 SetWaveControlEnabled、OnWaveStarted、TrySpawnEnemy、CountAliveEnemies 等）
- `WaveBossRewardManager` → ✅ 7 个 Provider + EventBus 订阅 + 零 WaveManager 引用
- `WaveEventFeedback` → ✅ EventBus 订阅 + IWaveStateProvider 状态读取
- `IWaveStateProvider` → ✅ Domain/Wave 接口（WaveManager 实现）

### AI Dialogue

代表文件：

```text
Scripts/2D/AI/Dialogue/Core/DialogueManager.cs
Scripts/2D/AI/Dialogue/LLM/ILLMClient.cs
Scripts/2D/AI/Dialogue/LLM/LlamaServerClient.cs
Scripts/2D/AI/Dialogue/LLM/RemoteAPIClient.cs
Scripts/2D/AI/Dialogue/Prompt/PromptBuilder.cs
Scripts/2D/AI/Dialogue/Prompt/PromptTemplateLoader.cs
Scripts/2D/AI/Dialogue/RAG/GameKnowledgeRetriever.cs
Scripts/2D/AI/Dialogue/Memory/DialogueMemoryManager.cs
Scripts/2D/AI/Dialogue/UI/
```

已有 Domain 抽离：

- `Domain/Dialogue/PromptAssemblyService.cs` — 纯 C# Prompt 组装服务
- `Domain/Dialogue/DialoguePromptProfileModel.cs` — 纯 C# Prompt 配置模型
- `Domain/Dialogue/ChatMessage.cs` — 纯 C# 对话消息模型
- `Domain/Dialogue/IPromptTemplateProvider.cs` — 纯 C# 模板提供者接口

已完成的解耦：

- `INPCPromptProfileProvider` 接口 + `ResourcesNpcPromptProfileProvider` 实现
- `PromptBuilder` 支持构造函数注入 provider（`INPCPromptProfileProvider`、`IPromptTemplateProvider`）
- `PromptBuilder` 通过 `ServiceLocator` 注册和获取

常见问题：

- `PromptTemplateLoader` 仍依赖 `Resources.Load` 加载模板（但已实现 `IPromptTemplateProvider` 接口）
- LLM 客户端、UI、NPC 触发器、游戏状态上下文需要继续分层

优先抽离方向：

- `DialogueContext`
- `DialogueTurn`
- `IDialogueProfileProvider`（已有 INPCPromptProfileProvider）
- `IPromptTemplateProvider`（已在 Domain 定义，PromptTemplateLoader 已实现）
- `IGameKnowledgeProvider`
- `ILLMClient` 保持接口化（已有 `Scripts/2D/AI/Dialogue/LLM/ILLMClient.cs`）

### 全局 Manager 耦合

已完成的解耦：

- 全面替换 `Singleton.Instance` 为 `ServiceLocator.Get<T>()`
- GlobalInit 分两批注册服务（BeforeSceneLoad + Awake）
- 输入处理提取到 `GlobalInputProcessor : ITickable`
- Worker 更新提取到 `WorkerUpdateSystem : ITickable`
- 多个 Manager 已注册接口类型（`ISkillManager`、`IPlayerVitalAlertManager`、`IWorkerConditionManager`、`IColonyCommandCenterService` 等）

剩余问题：

- 部分 UI 类仍直接调用 `ServiceLocator.Get<T>()`，未通过 EventBus 解耦
- 部分 Manager 的 MonoBehaviour 生命周期和业务逻辑仍有混合
- 部分 Manager 直接操作 `GameObject`、`Transform`、`Resources` 等 Unity 类型

## 6. 分析任务格式

开始分析时，请先输出当前代码耦合报告：

```md
## 当前耦合问题分析

| 文件 | 类名 | 问题类型 | 当前问题 | 建议改造方式 | 风险 |
|---|---|---|---|---|---|
| Scripts/2D/Character/Player/Player.cs | Player | 输入/表现/规则混合 | 直接读取 Input、控制 Rigidbody2D、刷新 PlayerStatusUI | 抽出 PlayerCommand、PlayerMovementService、PlayerViewAdapter | 影响手感和 Photon 本地玩家判断 |
```

问题类型可以使用：

- 核心规则依赖 Unity
- MonoBehaviour 过重
- 业务逻辑直接操作 UI
- 业务逻辑直接操作动画/特效/音效
- 直接读取 Input
- 直接使用 Time
- 直接使用 Transform/Physics
- 直接调用全局 Singleton（应改为 ServiceLocator 或 Provider 委托）
- 直接调用 ServiceLocator（应改为接口注入或 EventBus）
- 地图/库存/任务互相强依赖
- Photon 网络逻辑与本地规则混合
- 难以单元测试

## 7. 改造输出格式

请按以下顺序输出：

```md
# RandomWorld 架构改造方案

## 1. 当前代码耦合问题分析

## 2. 推荐的渐进式目标结构

## 3. 本轮优先改造模块

## 4. 改造前问题说明

## 5. 改造后设计

## 6. 完整代码

## 7. 接入方式

## 8. Command / Event / Adapter 流程

## 9. 迁移到其他引擎时的复用范围

## 10. 验证步骤

## 11. 风险与回滚方案

## 12. 后续迭代计划

## 13. 最终检查清单
```

## 8. 典型改造方式

项目已有多种解耦模式，选择时请优先考虑项目已有的模式：

### 8.1 已有的三种核心解耦模式

#### 模式 A：ServiceLocator 获取（最常用，已全局落地）

```csharp
// 注册（GlobalInit）
ServiceLocator.Register<ISkillManager>(SkillManager.Instance);

// 获取（业务代码中）
ISkillManager skillMgr = ServiceLocator.Get<ISkillManager>();
```

适用场景：需要在方法内部临时获取服务，或作为构造函数注入的 fallback。

#### 模式 B：Provider 委托模式（AWorkerTask 已大量使用）

```csharp
// 定义可替换的静态委托（在 AWorkerTask 等类中）
public static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
    = (message, level) => LogManager.Instance.Log(message, level);

public static System.Func<int, int, bool> WalkabilityProvider { get; set; }
    = (x, y) => BuildMap.Instance.IsCanReach(new UnityEngine.Vector3Int(x, y, 0));

// 使用（任务子类中）
LogProvider("任务开始", LogManager.LogLevelEnum.Info);
if (!WalkabilityProvider(x, y)) { return false; }

// 测试时替换
AWorkerTask.LogProvider = (msg, level) => { /* 静默 */ };
AWorkerTask.WalkabilityProvider = (x, y) => true;
```

适用场景：
- 需要解耦静态上下文中的外部依赖（如抽象类的非 MonoBehaviour 子类）
- 不想修改现有调用方签名
- 需要在测试中快速替换实现

#### 模式 C：EventBus 事件驱动

```csharp
// Domain 侧发布事件
EventBus.Instance.Publish(new CharacterDamagedEvent { ... });

// Unity 侧订阅事件（OnEnable/Start）
EventBus.Instance.Subscribe<CharacterDamagedEvent>(OnCharacterDamaged);

// Unity 侧取消订阅（OnDisable/OnDestroy）
EventBus.Instance.Unsubscribe<CharacterDamagedEvent>(OnCharacterDamaged);

// 非泛型发布（适用于框架代码）
EventBus.Instance.PublishInternal(gameEvent);
```

适用场景：
- 核心规则通知表现层（UI、动画、音效）
- 跨模块解耦通信
- 一对多通知

### 8.2 输入使用 Command

不要让核心规则直接读 `UnityEngine.Input`。

```csharp
public interface IGameCommand
{
}

public sealed class PlayerMoveCommand : IGameCommand
{
    public long EntityId;
    public GameVector2 Direction;
    public bool IsRunning;
    public float DeltaTime;
}

public sealed class PlayerAttackCommand : IGameCommand
{
    public long EntityId;
}

public sealed class ActivateSkillCommand : IGameCommand
{
    public long EntityId;
    public int SlotIndex;
}
```

Unity 侧只负责把键盘、鼠标、摇杆转换为 Command：

```text
Input.GetKey / Joystick.Direction
  -> UnityPlayerInputAdapter / UnityGlobalInputAdapter
  -> PlayerMoveCommand
  -> PlayerMovementService
```

### 8.3 结果使用 Event

不要让核心规则直接刷新 UI 或播放动画。

```csharp
public interface IGameEvent
{
}

public sealed class PlayerMovedEvent : IGameEvent
{
    public long EntityId;
    public GameVector2 Position;
    public GameVector2 Direction;
    public bool IsRunning;
}

public sealed class CharacterDamagedEvent : IGameEvent
{
    public long TargetId;
    public long AttackerId;
    public float Damage;
    public bool IsCritical;
    public float RemainingHp;
}

public sealed class InventoryChangedEvent : IGameEvent
{
    public GameGridPosition Position;
    public int ItemId;
    public int Count;
}
```

Unity 侧监听事件并更新表现：

```text
PlayerMovedEvent
  -> PlayerViewAdapter
  -> Rigidbody2D / Transform / Animator / Camera

CharacterDamagedEvent
  -> DamageUI / FloatingTextManager / SpriteRenderer flash

InventoryChangedEvent
  -> ItemMap / ItemInfoUI / WorkerTaskManager bridge
```

### 8.4 Unity 类型使用 Adapter 转换

核心层不要使用 `Vector2`、`Vector3`、`Vector3Int`。

```csharp
public readonly struct GameVector2
{
    public readonly float X;
    public readonly float Y;

    public GameVector2(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }
}

public readonly struct GameGridPosition
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public GameGridPosition(int x, int y, int z = 0)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
}
```

Unity 适配层提供转换：

```csharp
public static class UnityVectorAdapter
{
    public static GameVector2 ToGameVector2(UnityEngine.Vector2 value)
    {
        return new GameVector2(value.x, value.y);
    }

    public static UnityEngine.Vector2 ToUnityVector2(GameVector2 value)
    {
        return new UnityEngine.Vector2(value.X, value.Y);
    }

    public static GameGridPosition ToGameGridPosition(UnityEngine.Vector3Int value)
    {
        return new GameGridPosition(value.x, value.y, value.z);
    }
}
```

### 8.5 外部能力使用接口

核心规则需要外部能力时，用接口隔离：

```csharp
public interface IGameTime
{
    float DeltaTime { get; }
    float Time { get; }
}

public interface IGameLogger
{
    void Log(string message);
    void Warning(string message);
    void Error(string message);
}

public interface IMapWalkabilityQuery
{
    bool IsCanReach(GameGridPosition position);
}

public interface IItemDefinitionProvider
{
    ItemDefinition GetById(int itemId);
}
```

Unity 实现放在 Adapter 中，通过 `ServiceLocator` 注册接口类型。

### 8.6 生命周期使用 ITickable / IInitializable

新增需要 Update 驱动或初始化顺序的组件时，实现 Domain 层接口：

```csharp
public sealed class MyNewSystem : ITickable
{
    public void Tick(float deltaTime)
    {
        // 每帧逻辑
    }
}

public sealed class MyNewInitializer : IInitializable
{
    public void Initialize()
    {
        // 初始化逻辑
    }
}
```

在 `GlobalInit.BuildTickableList()` 或 `BuildInitializableList()` 中添加实例即可。

### 8.7 选择改造模式的原则

| 场景 | 推荐模式 |
|---|---|
| MonoBehaviour 中需要获取其他服务 | ServiceLocator.Get<T>() |
| 非 MonoBehaviour 类需要外部依赖 | Provider 委托模式 或 构造函数注入 |
| 核心规则通知表现层 | EventBus 事件驱动 |
| Unity 类型替换为纯 C# 类型 | Adapter 转换 |
| 新增定期执行的逻辑 | ITickable 接口 |
| 新增需要初始化顺序的逻辑 | IInitializable 接口 |
| 需要替换实现以支持测试 | 接口 + ServiceLocator 或 Provider 委托 |

## 9. 本项目优先推荐的后续改造

项目已完成多轮分层（`Domain/`、`UnityAdapter/`、`Core/`、`Network/` 已创建并有实际代码）。已完成的抽离包括但不限于：

| 领域 | 已存在的 Domain 服务 |
|---|---|
| Worker | `WorkerTaskAssignmentService`、`WorkerTaskProgressService`、`WorkerConditionRuleService`、`WorkerTaskCongestionRuleService`、`WorkerSupplyRuleService` |
| Player | `PlayerDamagePolicy`、`PlayerMovementPolicy`、`PlayerVitalAlertRuleService` |
| Inventory | `InventoryService`、`InventoryGrid`、`InventoryCell`、`ResourceStack`、`InventoryStackingService`、`InventoryFoodReservationService`、`InventoryTakeReservationService`、`InventoryGridChangedEvent` |
| Wave | `WaveRuleService`、`WaveBossRuleService`、`WaveFlowService`、`WaveSpawnPlanService`、`WaveConfigModel`、`WaveRuntimeState`、`WaveSpawnRequest`、`WaveFlowDecision`、`WaveStartedEvent`、`WaveEndedEvent`、`AllWavesClearedEvent`、`WaveRestStartedEvent`、`IWaveStateProvider`（12 个文件） |
| Gameplay | `AchievementRuleService`、`SkillRuleService`、`ComboBonusRuleService`、`SessionResultRuleService`、`DeathPenaltyRuleService`、`GameplaySessionStatsRuleService`、`WeatherGameplayRuleService`、`EquipmentLootRuleService`、`ColonyCommandCenterRuleService`、`PlayerVitalAlertRuleService`（10 个文件） |
| Dialogue | `PromptAssemblyService`、`DialoguePromptProfileModel`、`IPromptTemplateProvider` |
| Common | `EventBus`、`GameVector2`、`GameGridPosition`、`IGameTime`、`IGameLogger`、`ITickable`、`IInitializable` 等 |
| Unity Adapter | `UnityGameTime`、`UnityLogger`、`UnityVectorAdapter`、`UnityPlayerInputAdapter`、`UnityMapAdapter`、`UnityGlobalInputAdapter`、`UnityWaveSceneAdapter`、`UnityWaveTimeScheduler`、`PlayerViewAdapter` 等（14 个文件） |

| 基础设施 | 状态 |
|---|---|
| ServiceLocator（轻量 DI） | ✅ 已全局落地，约 50 个服务注册 |
| ITickable / IInitializable（生命周期接口） | ✅ 已实现，6 个 ITickable + 5 个 IInitializable |
| GlobalInputProcessor（输入处理解耦） | ✅ 已从 GlobalInit 提取 |
| AWorkerTask Provider 委托模式 | ✅ 约 35 个静态 Provider 属性 |
| EventBus + PublishInternal | ✅ 已增强，有单元测试；已有 11 种事件类型（CharacterDamaged、PlayerStatusChanged、InventoryCellChanged、InventoryGridChanged、PlayerAttackRequested、PlayerSkillActivated、WorkerTaskQueueChanged、WaveStarted、WaveEnded、AllWavesCleared、WaveRestStarted） |
| Dialogue 接口抽象 | ✅ INPCPromptProfileProvider + IPromptTemplateProvider |
| 全局 Singleton → ServiceLocator 替换 | ✅ 已完成 |
| GameLoggerFactory（统一日志工厂） | ✅ 31 文件迁移，替换硬编码 Debug.Log |
| Worker 生存与建造韧性 | ✅ 血瓶自动使用（HP<30%，3s 冷却）+ 建造任务恢复 + 卡死重试（3 次） |
| 地形挖掘 | ✅ GatherTask 扩展 + GatherMap 认领防多人冲突 |
| 装备对比弹窗 | ✅ 拾取装备时自动弹出属性差异对比，支持替换/丢弃 |
| 房间列表面板 | ✅ RoomListPanel，IsOverlay 模式不暂停游戏 |
| TaskPriority 常量 | ✅ 任务优先级统一常量管理，消除魔法数字 |

后续改造状态（2026-08 更新）：

1. ✅ **Inventory EventBus 事件迁移** — ItemInfoUI → InventoryGridChangedEvent
2. ✅ **Character/Player 深入解耦** — IsPlayerCharacter + WorldPositionProvider + CharacterCreator ServiceLocator
3. ✅ **ITickable/IInitializable 覆盖** — 6 ITickable + 5 IInitializable
4. ✅ **InventoryManager 深入解耦** — InventoryService + int key
5. ✅ **WorkerTaskManager 解耦** — ITickable + GameGridPosition + WorkerPositionProvider
6. ✅ **Wave 系统解耦** — 7 Provider + 4 EventBus 事件 + IWaveStateProvider + EventBus 订阅迁移
7. ✅ **Gameplay 层 UnityEngine 清理** — 12 个非 MonoBehaviour Manager/Singleton 零 `using UnityEngine`，所有 `Time.realtimeSinceStartup` 已迁移至 `IGameTime`（2026-07 终态收尾）
8. ✅ **UI 系统手动创建节点范式** — 所有 UI 组件从自动创建节点改为查找场景已有节点，移除自动创建 EventSystem/Canvas/对象池容器逻辑，Editor 菜单工具简化，抽取 EnsureReferences 通用模式（2026-07）
9. ✅ **GlobalInputProcessor 精简** — 移除 ColonyCommandHud 输入处理逻辑，该逻辑已由 ColonyCommandCenterHUD 自行管理（2026-07）
10. 🔒 **存档/Photon 桥接** — Photon 层已通过 INetworkView/ISyncSender 完善覆盖，存档 BinaryFormatter 迁移风险高，建议保持现状
11. 🔒 **WaveManager Coroutine** — 已通过 IWaveTimeScheduler 解耦，可选优化
12. 📋 **扩展单元测试** — 39 个 Domain 测试文件覆盖全部 RuleService，可继续为 Provider 委托补充测试
13. ✅ **GameLoggerFactory 统一日志** — 31 文件从 Debug.Log 迁移至 IGameLogger（2026-07）
14. ✅ **TaskPriority 常量管理** — 任务优先级统一使用常量类，消除魔法数字（2026-07）
15. ✅ **地形挖掘功能** — GatherTask 扩展支持地形挖掘，复用 GatherMap 认领机制（2026-08）
16. ✅ **血瓶自动使用** — Worker HP<30% 自动消耗背包血瓶，3 秒冷却（2026-08）
17. ✅ **建造任务恢复 + 卡死重试** — 位置预注册 → 任务恢复；最多 3 次卡死重试（2026-08）
18. ✅ **装备对比弹窗** — 拾取装备自动弹出属性差异对比，支持替换/丢弃（2026-07）
19. ✅ **房间列表面板** — RoomListPanel，IsOverlay 模式不暂停游戏（2026-08）
20. ✅ **PlantUI ServiceLocator 一致性** — BackpackController.Instance → ServiceLocator.TryGet/Get；FarmlandManager.Instance → ServiceLocator.Get。BackpackController 已在 Awake 中自注册 ServiceLocator，无需修改 GlobalInit。ABasePanel 子类保持 .Instance（架构决策：懒创建触发自注册）。全项目非 ABasePanel 的 .Instance 清零（2026-08）

选择模块时，请说明原因：

```md
本轮选择：WaveManager 进一步解耦
原因：
- Domain/Wave 已有 WaveRuleService 和 WaveConfigModel
- UnityAdapter 已有 UnityWaveTimeScheduler 和 UnityWaveSceneAdapter
- 可将 MonoBehaviour 中剩余的波次计时、生成调度逻辑进一步迁移
- 风险较低，Domain 和 Adapter 基础设施已就绪
```

## 10. 代码输出要求

输出代码时必须满足：

1. 给出完整文件路径。
2. 给出完整文件内容。
3. 新增纯规则类不能使用 `using UnityEngine;`。
4. 保持 `namespace LAB2D`，除非明确说明要新增子命名空间。
5. 保持现有 public API 兼容，避免直接破坏调用方。
6. 如果需要替换旧类，请先提供兼容层，而不是直接删掉旧行为。
7. 如果涉及 Inspector 字段、Prefab、场景对象，请说明如何迁移。
8. 如果涉及 Photon，本轮不要改变网络所有权逻辑，先把本地规则抽出来。
9. 如果涉及存档，必须说明新旧数据如何兼容。
10. 如果涉及 UI，必须说明事件订阅和取消订阅位置。
11. 优先使用项目已有的解耦模式（ServiceLocator、Provider 委托、EventBus、ITickable/IInitializable）。
12. 新增功能需要 Update 驱动时，优先实现 `ITickable` 而非直接写在 MonoBehaviour 中。

代码块格式：

```csharp
// 文件路径：Assets/Scripts/2D/Domain/Worker/WorkerTaskQueue.cs

namespace LAB2D
{
    public sealed class WorkerTaskQueue
    {
        ...
    }
}
```

## 11. 验证要求

每轮改造后，请给出可执行验证步骤：

```md
## 验证步骤

1. 打开原有测试场景或主场景。
2. 进入游戏，确认玩家移动、攻击、技能热键行为不变。
3. 创建 Worker 任务，确认任务仍会分配给最近可执行 Worker。
4. 打开 WorkerTaskQueueHUD，确认显示数量不重复、不丢失。
5. 检查 Console 是否出现 NullReferenceException。
6. 如果有 EditMode 测试，运行对应纯 C# 单元测试。
```

如果无法运行 Unity，请明确说明"未运行 Unity 验证"，并给出静态检查结果和建议手测路径。

## 12. 迁移复用分析格式

请输出：

```md
## 引擎迁移复用分析

| 模块 | 当前或目标位置 | 迁移到 Godot/Unreal/服务端时是否可复用 | 原因 |
|---|---|---|---|
| Worker 任务分配规则 | Domain/Worker | 可复用 | 不依赖 UnityEngine，只处理快照和任务模型 |
| WorkerTaskManager MonoBehaviour | Character/Worker | 需要重写 | 依赖 Unity 生命周期、Worker 实例 |
| AWorkerTask Provider 委托 | Character/Worker/Task | 部分可复用 | 默认实现依赖 Unity Singleton，但委托本身可替换 |
| Player 输入适配 | UnityAdapter/Input | 需要重写 | 依赖 Unity Input 和 Joystick |
| InventoryGrid | Domain/Inventory | 可复用 | 纯 C# 数据结构 |
| ItemMap 同步 | UnityAdapter/Map | 需要重写 | 依赖 TileMap 和 Unity Tilemap |
| EventBus | Domain/Common | 可复用 | 纯 C# 实现，零 UnityEngine 依赖 |
| ServiceLocator | Core | 可复用 | 纯 C# 实现，零 UnityEngine 依赖 |
| ITickable/IInitializable | Domain/Common | 可复用 | 纯 C# 接口，引擎层重新实现驱动即可 |
```

## 13. 渐进式改造计划模板

请使用以下模板制定计划：

```md
## 渐进式改造计划

### 阶段一：建立纯数据模型

目标：
- 从 `CharacterData`、Worker 任务、Inventory 资源格子、Wave 状态中抽出纯 C# Model
- 保留旧类继续运行，只让旧类持有或转换新 Model

交付物：
- `CharacterStats`
- `WorkerTaskModel`
- `InventoryCell`
- `WaveState`

风险：
- 旧字段和新 Model 可能出现双写不一致

验证：
- 对比改造前后角色属性、任务数量、库存数量、波次状态

### 阶段二：抽离规则 Service

目标：
- 把伤害、移动意图、任务分配、库存预占、波次计算放入 Service
- Service 不依赖 UnityEngine

交付物：
- `DamageCalculator`
- `WorkerTaskAssignmentService`
- `InventoryReservationService`
- `WaveRuleService`

风险：
- 行为顺序变化导致手感或任务优先级变化

验证：
- 为关键规则补充 EditMode 或普通 C# 单元测试

### 阶段三：建立 Command/Event

目标：
- 输入通过 Command 进入规则层
- 规则结果通过 Event 通知 Unity 表现层

交付物：
- `IGameCommand`
- `IGameEvent`
- `EventBus`
- 模块级 Command/Event

风险：
- 事件重复订阅、漏取消订阅、生命周期混乱

验证：
- 统计事件触发次数，销毁对象后不再收到事件

### 阶段四：替换 Unity 强依赖

目标：
- 移除纯规则中的 `Vector3Int`、`Mathf`、`Time`、`Input`、`Transform`
- 用接口和自定义值对象替代

交付物：
- `GameVector2`
- `GameGridPosition`
- `IGameTime`
- `IMapWalkabilityQuery`
- `UnityVectorAdapter`

风险：
- 坐标轴 x/y 与地图 row/column 语义混淆

验证：
- 对比地图位置、寻路目标、任务距离、生成点

### 阶段五：增加测试和回归保护

目标：
- 核心规则可以脱离 Unity 测试
- 保留 Unity 场景手测路径

交付物：
- `DamageCalculatorTests`
- `WorkerTaskAssignmentServiceTests`
- `InventoryReservationServiceTests`
- `WaveRuleServiceTests`

风险：
- 旧代码缺少测试基线，初期只能覆盖新规则

验证：
- 运行测试并手动回归主场景
```

> **当前进度**：项目已完成阶段一~五的全部工作。**架构改造已达平台期**——剩余耦合点均在 MonoBehaviour/物理/渲染等本质 Unity 绑定层。
>
> **2026-07 架构改造终态报告（本轮会话完整成果）**：
>
> ### Gameplay 层 — 零 `using UnityEngine` 的 16 个非 MonoBehaviour Manager/Singleton
>
> PlayerVitalAlertManager, ColonyCommandCenterManager, WorkerTaskCongestionAdvisor, WaveEventFeedback,
> WorkerConditionManager, AchievementManager, ItemCollectionTracker, WeatherGameplayEffect,
> WorkerEfficiencyTracker, SessionResultManager, GameplaySessionStats, ComboBonusManager,
> WorkerSupplyIssueManager, **DeathPenaltyManager**, **SkillManager**, WorkerUpdateSystem
>
> ### Gameplay 层 — 仍 `using UnityEngine` 的 5 个文件（均为本质 Unity 绑定）
>
> | 文件 | 保留原因 |
> |------|----------|
> | EnemyLootManager | public API 使用 Vector3/Vector3Int；8 个 Provider 已隔离核心逻辑 |
> | WaveBossRewardManager | public API 使用 GameObject 参数；7 个 Provider 已隔离核心逻辑 |
> | EquipmentBeamManager | Mesh/Texture/Material 渲染 — 纯表现层 |
> | FloatingTextManager | Camera/Canvas/GameObject — 纯 UI 层 |
> | SessionResultAutoTrigger | MonoBehaviour 生命周期 — 设计如此 |
>
> ### 跨模块清理
>
> | 文件 | 改动 | 效果 |
> |------|------|------|
> | `Core/Lock.cs` | Random.Range → System.Random | 移除 using UnityEngine |
> | `Tool/NameGenerator.cs` | Random.Range → System.Random | 移除 using UnityEngine |
> | `Tool/EquipmentLootTool.cs` | Random.Range → RandomIntProvider/RandomFloatProvider | 隔离 Unity Random |
> | `Item/Backpack/Equipment/AEquipment.cs` | Random.Range → RandomFloatProvider | 纯数据类零 UnityEngine |
> | `Manager/ResourceManager.cs` | Random.Range → RandomIntProvider | 隔离 Unity Random |
> | `Gameplay/EnemyLootManager.cs` | 移除 using UnityEngine.Tilemaps | TileBase → 全限定名 |
>
> ### Worker 层
>
> | 改动 | 说明 |
> |------|------|
> | WorkerTaskManager 双重 Tick 修复 | 帧去重保护（`lastTickFrame`），防止 Update() + GlobalInit 双重驱动 |
> | AddTask API 迁移 | `GameGridPosition` 主重载，`Vector3IntLAB` 标记 Obsolete；10 个调用方全部迁移 |
> | TaskAddProvider 签名更新 | `Action<AWorkerTask, Vector3IntLAB, int>` → `Action<AWorkerTask, GameGridPosition, int>` |
>
> ### 基础设施
>
> | 改动 | 说明 |
> |------|------|
> | GlobalInit IGameTime 注册顺序修复 | `IGameTime`/`IGameLogger` 从 RegisterServices(Awake) 提前到 RegisterSafeServices(BeforeSceneLoad) |
> | 新增单元测试 | `Editor/Tests/Tool/ArchitectureRefactoringVerificationTests.cs` |
>
> ### Provider 委托完整清单
>
> 全项目累计新增 **17 个** Provider 委托（本轮会话），分布在：
> - DeathPenaltyManager: 4（GameplaySessionStats, DeathMenuPanel, RespawnPosition, RespawnPlacement）
> - EnemyLootManager: 8（+RandomIntProvider, +RandomFloatProvider）
> - SkillManager: 6（PlayerMine, FloatingTextDamage, FloatingTextHeal, PlayerWorldPosition, DashMovement, PlayerFacingDirection, EnemyWorldPosition）
> - WaveBossRewardManager: 7（BossVisual, RandomRange, ApplyHealReward, ApplyExperienceReward, LogWarning, Log, PlayerResolver）
> - WorkerSupplyIssueManager: 2（FoodInventory, BedBinding）
> - SessionResultManager: 1（IsPlayingProvider）
> - AEquipment: 1（RandomFloatProvider）
> - ResourceManager: 1（RandomIntProvider）
> - EquipmentLootTool: 2（RandomFloatProvider, RandomIntProvider）
>
> ### Bug 修复
>
> - WorkerTaskManager 双重 Tick（每帧执行两次 RunTaskAssignmentLoop）→ 帧去重保护
> - GameplaySessionStats 构造函数 KeyNotFoundException（IGameTime 未在 BeforeSceneLoad 注册）→ 注册顺序修复
>
> ### 架构决策
>
> **ABasePanel 使用 .Instance**：`ABasePanel<T>` 子类构造函数自动调用 `ServiceLocator.Register(this)`。`.Instance` 触发懒创建 → 自注册，是正确模式。**结论：ABasePanel 子类保持 `.Instance`，其他已注册服务使用 `ServiceLocator.Get<T>()`。**
>
> ### 当前架构状态
>
> - 🎉 非 UI `.Instance` 全部清零；UI 层非 ABasePanel `.Instance` 全部清零（PlantUI 收尾，2026-08）
> - 🎉 Gameplay 层 16 个非 MonoBehaviour 零 `using UnityEngine`（+6 相比改造前）
> - 🎉 所有 `Time.realtimeSinceStartup` 已迁移至 `IGameTime`
> - 🎉 全限定名 `UnityEngine.` 引用仅存在于 Provider 默认实现中
> - 🎉 39 个 Domain 单元测试 + 1 个 Provider 验证测试
> - ⚠️ 平台期已到：剩余 5 个 Gameplay 文件 + 跨模块文件均属本质 Unity 绑定
> - 📋 后续：功能开发中持续使用 Provider/EventBus/IGameTime 模式

## 14. 最终检查清单

每轮方案最后输出：

```md
## 最终检查清单

- [ ] 新增纯规则类没有 `using UnityEngine;`
- [ ] 新增纯规则类没有继承 `MonoBehaviour` / `MonoBehaviourPun`
- [ ] 核心规则没有直接读取 `Input`
- [ ] 核心规则没有直接使用 `Time.deltaTime` / `Time.time`
- [ ] 核心规则没有直接操作 `Transform` / `GameObject`
- [ ] 核心规则没有直接刷新 UI
- [ ] 核心规则没有直接播放动画、音效、特效
- [ ] Unity 类型通过 Adapter 转换
- [ ] 输入通过 Command 或明确的参数进入规则层
- [ ] 规则结果通过 Event、返回值或回调通知表现层
- [ ] 新增外部依赖使用 ServiceLocator、Provider 委托或接口注入
- [ ] 新增 Update 逻辑优先使用 ITickable 接口
- [ ] 新增初始化逻辑优先使用 IInitializable 接口
- [ ] 旧 MonoBehaviour 仍可作为兼容桥接
- [ ] 保留现有 Prefab / Inspector / Photon 行为
- [ ] 给出可执行验证步骤
- [ ] 标明未验证项和剩余风险
```

## 15. 请开始执行

请基于当前项目真实代码进行分析和改造。

如果我没有指定模块，请先整体扫描耦合点，然后选择一个最适合第一轮改造的模块。

优先原则：

- 先小步抽离，再逐步迁移
- 先保证 Unity 可运行，再追求架构纯净
- 先保留兼容层，再替换调用方
- 先抽规则和数据，再抽事件和适配
- 每轮只改一个清晰模块
- 优先使用项目已有的解耦模式，不引入新的基础设施
