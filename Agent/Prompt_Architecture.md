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
  Gameplay/                           # AchievementRuleService、ComboBonusRuleService、SkillRuleService 等
  Inventory/                          # InventoryService、InventoryGrid、InventoryCell、ResourceStack、InventoryStackingService、InventoryFoodReservationService、InventoryTakeReservationService、InventoryGridChangedEvent 等
  Player/                             # PlayerDamagePolicy、PlayerMovementPolicy、PlayerMovementService、IPlayerView、PlayerEvents 等
  Wave/                               # WaveBossRuleService、WaveConfigModel、WaveRuleService
  Worker/                             # WorkerAgentSnapshot、WorkerTaskAssignmentService、WorkerTaskProgressService、WorkerConditionRuleService 等
  Dialogue/                           # PromptAssemblyService、DialoguePromptProfileModel、IPromptTemplateProvider、ChatMessage 等

Scripts/2D/UnityAdapter/             # Unity 适配层
  UnityGameTime.cs / UnityLogger.cs / UnityVectorAdapter.cs / UnityPlayerInputAdapter.cs
  UnityMapAdapter.cs / UnityEnemySpawnAdapter.cs / UnityItemDefinitionAdapter.cs / TipHelper.cs
  UnityWaveSceneAdapter.cs / UnityWaveTimeScheduler.cs / UnityGlobalInputAdapter.cs / PlayerViewAdapter.cs

Scripts/2D/Core/                      # 基础设施层
  ServiceLocator.cs                   # 轻量级服务定位器（DI 过渡方案）
  GlobalInputProcessor.cs             # 全局输入处理器（ITickable 实现）
  KDTree / A* 寻路(Seek) / Singleton / Lock / GlobalPanelInitializer / MonoBehaviourInit

Scripts/2D/Gameplay/                  # 玩法管理器（依赖 Domain 和 UnityAdapter）
  WaveManager.cs / SkillManager.cs / WeatherGameplayEffect.cs / AchievementManager.cs
  SessionResultManager.cs / ComboBonusManager.cs / DeathPenaltyManager.cs
  FloatingTextManager.cs / WorkerConditionManager.cs / WorkerEfficiencyTracker.cs
  WorkerUpdateSystem.cs / ColonyCommandCenterManager.cs 等（共 29+ 个文件）

Scripts/2D/Character/
  Character.cs / CharacterHealthComponent.cs / CharacterDamageUIPresenter.cs
  CharacterManager.cs / ICharacterManager.cs / ICharacterCreator.cs 等接口
  Player/         # Player.cs / PlayerManager.cs / PlayerCreator.cs
  Enemy/          # AEnemy.cs / EnemyManager.cs / CommonEnemy/ / SeekEnemy/
  Worker/         # AWorker.cs / WorkerManager.cs / WorkerTaskManager.cs
    State/        # WorkerAttack/Dead/Escape/Move/Seek/WorkState
    Task/         # AWorkerTask.cs（Provider 委托模式）、WorkerBuild/Carry/Gather/Hungry/PlantTask + Individual/

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
  Panel/PanelUI/ForegroundUI/  # GameInfoUI / DebugUI / ToolMenu / Joystick 等

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
- 当前 ITickable 实现：`WorkerUpdateSystem`、`GlobalInputProcessor`、`WorkerTaskManager`、`EnvironmentManager`、`PlayerVitalAlertManager`
- 当前 IInitializable 实现：`AchievementManager`、`SkillManager`、`EquipmentBeamManager`、`EnemyLootManager`、`ComboBonusManager`

#### 3. GlobalInputProcessor — 全局输入处理解耦

位于 `Scripts/2D/Core/GlobalInputProcessor.cs`。从 GlobalInit 中提取的职责，实现 `ITickable`。

- 处理 ESC 键面板切换、鼠标点击关闭物品信息、成就面板切换、殖民地命令中心 HUD
- 分离了 Update 逻辑与输入处理逻辑

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
9. 不要让核心规则直接调用 UI，例如 `PlayerStatusUI.Instance`、`ItemInfoUI.Instance`、`DebugUI.Instance`。
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
    Gameplay/                 # AchievementRuleService、ComboBonusRuleService、SkillRuleService、SessionResultRuleService 等
    Inventory/                # InventoryService、InventoryGrid、InventoryCell、ResourceStack、InventoryStackingService、InventoryFoodReservationService、InventoryTakeReservationService、InventoryGridChangedEvent
    Player/                   # PlayerDamagePolicy、PlayerMovementPolicy、PlayerVitalAlertRuleService
    Wave/                     # WaveBossRuleService、WaveConfigModel、WaveRuleService
    Worker/                   # WorkerAgentSnapshot、WorkerConditionRuleService、WorkerTaskAssignmentService、WorkerTaskProgressService、WorkerTaskCongestionRuleService、WorkerSupplyRuleService
    Dialogue/                 # PromptAssemblyService、DialoguePromptProfileModel、ChatMessage、IPromptTemplateProvider
  UnityAdapter/               # ✅ 已存在：Unity 类型、输入、时间、地图、资源的适配（11 个文件）
    UnityGameTime.cs / UnityLogger.cs / UnityVectorAdapter.cs / UnityPlayerInputAdapter.cs
    UnityMapAdapter.cs / UnityEnemySpawnAdapter.cs / UnityItemDefinitionAdapter.cs / TipHelper.cs
    UnityWaveSceneAdapter.cs / UnityWaveTimeScheduler.cs / UnityGlobalInputAdapter.cs
  Core/                       # ✅ 已存在：ServiceLocator / GlobalInputProcessor / KDTree / A* 寻路 / Singleton / Lock
  Network/                    # ✅ 已存在：Photon 网络适配层
    INetworkView.cs / NetworkViewAdapters.cs / SyncSenderAdapters.cs
  Gameplay/                   # 玩法管理器（已大量通过 ServiceLocator 获取依赖，继续推进通过 EventBus 解耦）
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

优先抽离方向：

- `CharacterRuntimeState`（✅ 已存在于 Domain/Common）
- `PlayerMovementIntent`（配合已有 PlayerMovementPolicy）
- `PlayerCommand` / `PlayerEvent`（✅ 部分已实现：PlayerAttackCommand、ActivateSkillCommand、PlayerAttackRequestedEvent、PlayerSkillActivatedEvent、PlayerStatusChangedEvent）
- `ICharacterCreator` 等接口（已有基础）

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

优先抽离方向：

- ~~`WorkerTaskModel`（纯 C# 任务数据模型）~~ ✅ 已完成：WorkerTaskQueue + WorkerTaskSnapshot
- ~~`WorkerTaskQueue`（纯 C# 任务队列）~~ ✅ 已完成：Domain/Worker/WorkerTaskQueue.cs
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

- `preTakeResource` / `prePlaceResource` 仍以 `AWorker` 为 key（依赖 MonoBehaviour），未下沉到 Domain
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

优先抽离方向：

- `WaveState`
- `WaveSpawnRequest`
- `WaveEvent`
- `IEnemySpawnService`（已有 UnityEnemySpawnAdapter 可配合）
- `IWaveTimeScheduler`（已有 UnityWaveTimeScheduler）
- `IMapSpawnPointProvider`（已有 UnityMapAdapter 实现）

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
| Wave | `WaveRuleService`、`WaveBossRuleService`、`WaveConfigModel` |
| Gameplay | `AchievementRuleService`、`SkillRuleService`、`ComboBonusRuleService`、`SessionResultRuleService` 等 |
| Dialogue | `PromptAssemblyService`、`DialoguePromptProfileModel`、`IPromptTemplateProvider` |
| Common | `EventBus`、`GameVector2`、`GameGridPosition`、`IGameTime`、`IGameLogger`、`ITickable`、`IInitializable` 等 |
| Unity Adapter | `UnityGameTime`、`UnityLogger`、`UnityVectorAdapter`、`UnityPlayerInputAdapter`、`UnityMapAdapter`、`UnityGlobalInputAdapter`、`UnityWaveSceneAdapter`、`UnityWaveTimeScheduler` 等（11 个文件） |

| 基础设施 | 状态 |
|---|---|
| ServiceLocator（轻量 DI） | ✅ 已全局落地，约 50 个服务注册 |
| ITickable / IInitializable（生命周期接口） | ✅ 已实现，5 个 ITickable + 5 个 IInitializable |
| GlobalInputProcessor（输入处理解耦） | ✅ 已从 GlobalInit 提取 |
| AWorkerTask Provider 委托模式 | ✅ 约 35 个静态 Provider 属性 |
| EventBus + PublishInternal | ✅ 已增强，有单元测试；已有 7 种事件类型（CharacterDamaged、PlayerStatusChanged、InventoryCellChanged、InventoryGridChanged、PlayerAttackRequested、PlayerSkillActivated、WorkerTaskQueueChanged） |
| Dialogue 接口抽象 | ✅ INPCPromptProfileProvider + IPromptTemplateProvider |
| 全局 Singleton → ServiceLocator 替换 | ✅ 已完成 |

后续改造优先方向（按低风险到高风险排列）：

1. ~~**Inventory EventBus 事件迁移**~~ ✅ 已完成：`ItemInfoUI` 已切换订阅 `InventoryGridChangedEvent`（纯结构化数据），`InventoryManager` 已停止发布 `InventoryCellChangedEvent`（2026-07）。
2. **Character/Player 深入解耦**：✅ 已完成 — 所有标注的耦合点已解决：`CharacterHealthComponent.ApplyDamage()` 中 `attacker is PlayerCharacter` → `attacker.IsPlayerCharacter`；`Character.ReduceHp()` 的 `transform.position` → `WorldPositionProvider` 委托。Player Provider 总数 20 个，Character Provider 总数 21 个。剩余可推进方向：`Character.ToString()` 中的 `transform.position` 访问（Debug 用途，低优先级）；非 UI 目录 `.Instance` 尾量迁移（2026-07）。
3. ~~**扩展 ITickable/IInitializable 覆盖范围**~~ 🔄 持续推进：WorkerTaskManager 已迁移至 ITickable（2026-07），当前 5 个 ITickable + 5 个 IInitializable。后续可将更多 Manager 的 Update/Start 逻辑迁移。
4. ~~**InventoryManager 深入解耦**~~ ✅ 已完成：内部数据存储已迁移至 `InventoryService`。
5. ~~**WorkerTaskManager 继续解耦**~~ ✅ 已完成：ITickable + GameGridPosition API 迁移（2026-07）。WorkerTaskQueue、WorkerTaskSnapshot 等纯 C# 类型已在 Domain 层。剩余 `KDTree` 和 `transform.position` 采集属于合理的算法/表现层依赖。
6. **WaveManager Coroutine 解耦**：通过 `IWaveTimeScheduler`（已有 UnityWaveTimeScheduler）替代直接 Coroutine 依赖。WaveManager 架构已很完善，此项为可选优化。
7. **存档/Photon 与 Domain 桥接**：确保 Domain 模型变更时存档兼容，Photon 同步走适配层而非直接引用。存档使用 BinaryFormatter + 反射驱动架构，大规模迁移风险高。Photon 层 `INetworkView`/`ISyncSender` 已覆盖主体路径，Weapon 层和 UI Lobby 管理仍直接依赖 Photon API。
8. **扩展单元测试**：`Editor/Tests/Domain/` 从 36 增至 37 个测试文件（新增 `PlayerMovementPolicyTests`）。**Gameplay 目录 .Instance 清零**（`ComboBonusManager`、`WaveEventFeedback` 已迁移至 ServiceLocator）。所有 Domain Service 均已覆盖测试。后续继续为 Provider 委托补充测试。

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
4. 打开 DebugUI / WorkerTaskQueueHUD，确认显示数量不重复、不丢失。
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
| WorkerTaskManager MonoBehaviour | Character/Worker | 需要重写 | 依赖 Unity 生命周期、Worker 实例、DebugUI |
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

> **当前进度**：项目已完成阶段一~四的大部分工作。`Domain/` 已有丰富的纯 C# Service、Model、EventBus、值类型、生命周期接口；`UnityAdapter/` 已有 12 个文件（新增 `ToVector3Int` 便捷方法）；`ServiceLocator` 已全局落地；`AWorkerTask` Provider 委托模式已成熟。
>
> **最近完成（2026-07）**：
> - **扩展单元测试 + Gameplay .Instance 清零** — 新增 `PlayerMovementPolicyTests`（11 个测试用例），覆盖 `ClampRunSpeedMultiplier`（5 个）和 `ApplyRunMultiplier`（6 个）—— 唯一缺少测试的纯 Domain 服务。Gameplay 目录 `.Instance` 零化：`ComboBonusManager` 中 `GameplaySessionStats.Instance` 回退 → `ServiceLocator.Get<GameplaySessionStats>()`；`WaveEventFeedback` 中 3 处 `WaveManager.Instance` 回退 → `ServiceLocator.Get<WaveManager>()`。`WorkerUpdateSystem.NearbyItemPickupHUD.Instance` 保留（动态创建 GameObject，null-conditional 访问是最安全模式）。Domain 测试文件总数从 36 增至 37。**Core 目录调研**：大部分 .Instance 为 ABasePanel 子类（正确模式）或注释/外部工具类引用，无需迁移。**ForegroundPanel Photon 泄漏修复**：新增 `WeaponAttackRPCProvider` 静态委托（模式 B），将 `ExecuteAttack()` 中 `GetComponent<PhotonView>().RPC("Attack", RpcTarget.All)` 直接 Photon 调用提取为可替换 Provider。ForegroundPanel 可执行代码现已零 Photon 直接引用（2026-07）。
> - **Character/Player Unity 初始化 Provider 提取** — Player.cs 新增 5 个静态 Provider（`RigidbodySetupProvider`、`AnimatorProvider`、`MainCameraProvider`、`MiniCameraProvider`、`PlayerNameDisplayProvider`），将 `Awake()`/`Start()` 中 `GetComponent<Rigidbody2D>()`、`GetComponent<Animator>()`、`GameObject.FindGameObjectWithTag`、`Camera.main`、`Tool.GetComponentInChildren<Text>` 等 Unity 组件初始化代码提取为可替换委托。删除死代码 `private Vector3 direction` 字段（赋值后从未读取）及其初始化。`ReduceHp()` 事件中的 `transform.position.x/y` 提取为局部变量显式边界。Player.cs 从 555 行变为 625 行（+70 行，主要是 5 个 Provider + XML 注释）。Character.cs 新增 `CharacterRootParentProvider` 和 `SpriteRendererSetupProvider` 2 个 Provider，提取 `Awake()` 中 `GameObject.FindGameObjectWithTag("CharacterRoot")` + `SetParent` 和 `Start()` 中 `GetComponent<SpriteRenderer>()` 初始化。新增虚属性 `Character.IsPlayerCharacter`（默认 false，Player 重写为 true），`CharacterData.ComputeAttribute()` 接受 `bool isPlayer` 参数替代 `this is PlayerData` 类型检查。移除 `using PlayerCharacter` 别名导入，消除 `CharacterData` → `Player.PlayerData` 反向类型依赖。**Player.cs Provider 委托总数增至 20 个**（原有 15 个 + 新增 5 个）。Character.cs 新增 2 个 Provider（2026-07）。
> - **CharacterHealthComponent 类型检查消除 + Character WorldPositionProvider** — `CharacterHealthComponent.ApplyDamage()` 中 `attacker is PlayerCharacter` 替换为 `attacker.IsPlayerCharacter` 虚属性调用（`IsPlayerCharacter` 已于上一轮添加），移除 `using PlayerCharacter = LAB2D.Character.Player.Player` 别名。`Character.ReduceHp()` 新增 `WorldPositionProvider` 静态委托（模式 B），默认实现 `new GameVector2(c.transform.position.x, c.transform.position.y)`，`ReduceHp()` 核心伤害流程不再直接访问 `Transform.position`。Character.cs Provider 委托总数增至 21 个（原有 20 个 + 新增 1 个）。**Character/Player 深入解耦全部标注项已完成**（2026-07）。
> - **Data/ + Item/ .Instance 尾量清零** — `ItemDataManager.cs` 3 处 `ResourceManager.Instance` → `ServiceLocator.Get<ResourceManager>()`；`ItemInstanceFactory.cs` 1 处 `ItemDataManager.Instance` → `ServiceLocator.Get<ItemDataManager>()`（该文件其他 4 处已在之前迁移）。`ItemDataManager.Awake()` 新增 `ServiceLocator.Register<ItemDataManager>(this)` 自注册（解决初始化时序：`Awake()` 内部调用 `InitItemInstances()` 时 GlobalInit 尚未注册 ItemDataManager）。两个文件现已零 `.Instance` 调用。`CharacterManager.cs` 的 `CharacterCreator<CC>.Instance` 涉及泛型类型注册，留待后续处理。**Data/ 和 Item/ 目录 .Instance 清零**。非 UI 目录剩余 .Instance 引用：12 → 8（Core/Seek/ 7 处 + Character/ 1 处）（2026-07）。
> - **Core/Seek/ UnityMainThreadDispatcher.Instance 清零** — `AStar.cs` 5 处 + `ASeek.cs` 2 处 `UnityMainThreadDispatcher.Instance` → `Core.ServiceLocator.Get<UnityMainThreadDispatcher>()`。`GlobalInit.RegisterServices()` 新增 `ServiceLocator.Register(UnityMainThreadDispatcher.Instance)`。GlobalInit 的 `[DefaultExecutionOrder(100)]` 确保登记时 Instance 已设置。Core/Seek/ 目录 .Instance 全部清零。**非 UI 目录 .Instance 残余：12 → 1**（仅剩 CharacterManager.cs 1 处 `CharacterCreator<CC>.Instance`）（2026-07）。
> - **Player 表现层提取** — 新增 `IPlayerView` 接口（`Domain/Player/`）和 `PlayerViewAdapter`（`UnityAdapter/`）。Player.cs 中 `Animator`/`Rigidbody2D`/`SpriteRenderer`/`Camera` 的直接操作（~60 行）已移至 Adapter：受击闪烁（`PlayHitFlash` 替代 `spriteRenderer.color` + `Invoke`）、移动动画（`ApplyMoveAnimation`/`ApplyIdleAnimation`）、摄像机跟随（`EnsureCameraFollow`）、边缘特效 + 闪烁计时器（`Tick`）、视角切换（`TogglePerspective`）。移除 `BindCameras`/`ApplyMovePresentation`/`ApplyIdlePresentation` 三个私有方法。Player.cs 从 555 行缩减至 497 行。`IPlayerView` 零 `using UnityEngine`，可被任意引擎实现。
> - InventoryManager 内部重构 — 数据存储从 3 个并行 `Dictionary<Vector3Int, ...>` 迁移至 `Domain/Inventory/InventoryService`（包装 `InventoryGrid`），`InventoryGrid` 修复了空格子 id=-1 索引维护。新增 `InventoryGridChangedEvent`（纯数据事件）和 `InventoryServiceTests`（25 个单元测试）。public API 100% 兼容。
> - **Inventory 事件迁移** — `ItemInfoUI` 从 `InventoryCellChangedEvent`（携带 UI 格式化字符串）迁移至 `InventoryGridChangedEvent`（纯结构化数据）。`InventoryManager` 已停止发布旧事件（移除 11 处发布点），`InventoryService.PublishChange()` 统一发布。`InventoryCellChangedEvent` 类标记为废弃。
> - **WorkerTaskManager ITickable + GameGridPosition 迁移** — `WorkerTaskManager` 实现 `ITickable` 接口，`Update()` 中的任务分配循环迁移至 `Tick(float deltaTime)`，由 `GlobalInit.BuildTickableList()` 统一驱动（排在 WorkerUpdateSystem 之后）。`Update()` 保留作为兼容桥（委托给 Tick）。公开 API 迁移：`GatherPositions: List<GameGridPosition>`（替代旧 `GatherPos: List<Vector3Int>`）、`DeleteHungryTask(GameGridPosition)`、`CancelGatherTask(GameGridPosition)`，旧 Vector3Int 方法标记 `[Obsolete]` 保持向后兼容。`AWorkerTask.DeleteHungryTaskProvider` 默认实现已更新。调用方 `GatherUI`、`RectBoxUI` 已切换至新 API。ITickable 实现总数增至 5 个。
>
> - **Character 基类 ReduceHp 表现层回调提取** — 新增 `DamageFlashProvider` 静态委托（`System.Action<Character>`），将 `Character.ReduceHp()` 中内联的 `spriteRenderer.color = Color.red` + `Invoke("ResetColor")` 提取为可替换的 Provider。遵循项目已有的 AWorkerTask Provider 委托模式。`ResetColor()` 增加 null 安全检查。`ReduceHp()` 核心伤害流程不再硬编码 Unity 表现层操作，测试中可通过 `Character.DamageFlashProvider = (t) => {}` 静默。调用方无需变更。
> - **Player.Death() 层切换提取** — 新增 `DeathLayerSwitchProvider` 静态委托（`Action<Player>`），将 `Player.Death()` 中的 `gameObject.layer = LayerMask.NameToLayer("Default")` 提取为可替换的 Provider。遵循项目已有的 Provider 委托模式。Player.cs 中 `Death()` 不再直接操作 `gameObject.layer`。
>
> - **Player.IsArround GameGridPosition API 迁移** — 新增 `IsArround(GameGridPosition pos, int range = 50)` 重载，使用 Domain 层 `GameGridPosition` 替代 `UnityEngine.Vector3`。内部通过 `TileMapWorldToMapProvider` 转换玩家世界坐标到网格坐标。旧 `IsArround(Vector3)` 标记 `[Obsolete]`。零调用方，无迁移成本。
> - **Character 基类综合解耦** — 新增 `MoveSpeedProvider`（模式 B 委托）；`CheckBug` 嵌套类提取为独立 `CollisionBugDetector` 工具类（`Tool/CollisionBugDetector.cs`）；`CharacterData.ComputeAttribute()` 参数化，接受 `Attribute basicAttribute` 替代通过 `this.Character.basicAttribute` 反向引用，减弱 `CharacterData` → `Character`（MonoBehaviour）耦合。
>
> - **Photon 网络调用桥接** — `INetworkView` 新增 `IsMasterClient` 属性，`PunNetworkViewAdapter` / `OfflineNetworkView` 实现。`AWorkerTask` 新增 `NetworkIsMasterClientProvider` + `NetworkDestroyProvider` 两个静态委托。替换 9 处业务代码中的直接 Photon 调用：`ASeekEnemy.Death()`、`ACommonEnemy.Death()` → `NetworkView.IsMasterClient`；`AWorker.DeathProvider`、`ForegroundPanel`、`SyncDataTool`(3处) → `NetworkIsMasterClientProvider()`；`SeekEnemyDeadState`、`CommonEnemyDeadState`、`WorkerDeadState`、`BackpackMenuPanel` → `NetworkDestroyProvider`。`Player.cs` 清理 2 处冗余 `PhotonNetwork.IsConnected`（`NetworkView.IsOnline` 已封装）。**业务代码已零 Photon 直接调用**，剩余引用仅在 Adapter/Provider 层或注释中。
> - **Player PhotonNetwork 残余引用提取** — 新增 `LocalPlayerTagObjectProvider`（`Action<Player>`）和 `LocalPlayerNameProvider`（`Func<string>`）两个 Provider 委托。将 `Player.Start()` 中最后 2 处 PhotonNetwork 直接调用（`PhotonNetwork.LocalPlayer.TagObject`、`PhotonNetwork.NickName`）替换为 Provider 调用。遵循项目已有的模式 B Provider 委托模式。Player.cs 的 PhotonNetwork 引用现已全部封装在 Provider 默认实现内部（2026-07）。
> - **ComboBonusManager → IInitializable 迁移** — 实现 `IInitializable` 接口，`EnsureInitialized()` 私有方法提升为 `public void Initialize()`，新增 `IsInitialized` 公开属性。移除 5 个属性 getter（DamageMultiplier、ExperienceMultiplier、CurrentCombo、CurrentTierIndex、GetCurrentTierLabel）中的懒初始化守卫。由 `GlobalInit.BuildInitializableList()` 统一驱动初始化，IInitializable 实现总数增至 5 个（2026-07）。
> - **AWorkerTask Provider 默认值 .Instance → ServiceLocator（完成）** — 将 ~35 个静态 Provider 默认实现中的 `.Instance` 调用全部替换为 `ServiceLocator.Get<T>()`。分两阶段完成：(1) 约 27 个已注册服务 → ServiceLocator（2026-07 前）；(2) 剩余 8 个未注册服务 → 补注册 AttackEffectManager、AsyncProgressUI、LocateWorkerUI、GlobalInit 后替换（2026-07）。AWorkerTask.cs 现已 **零 .Instance 调用**（代码和注释均清零）。
>
> - **AWorkerTask .Instance 清零 + 未注册服务补注册** — 补注册 4 个未注册服务（`AttackEffectManager` 在 RegisterSafeServices、`AsyncProgressUI` 已自注册、`LocateWorkerUI` 和 `GlobalInit` 在 Awake 中新增 `ServiceLocator.Register(this)`）。AWorkerTask 最后 8 处 Provider 默认实现从 `.Instance` 迁移至 `ServiceLocator.Get<T>()`。同步更新 9 处 XML 文档注释。AWorkerTask.cs 实现 **100% ServiceLocator 覆盖**（零 `.Instance`，含注释）（2026-07）。
>
> - **ItemInfoUI + RectBoxUI ServiceLocator 迁移** — ItemInfoUI.cs（32→0）、RectBoxUI.cs（23→0）的 `.Instance` 调用全部迁移至 `ServiceLocator.Get<T>()`。补注册 6 个服务：`EventBus`、`SelectManagerPool`（RegisterSafeServices）；`GatherUI`、`WorkerBedUI`、`ItemInfoUI`、`RectBoxUI`（Awake 自注册）。`ItemInfoPanel`、`ForegroundPanel` 由 `ABasePanel<T>` 构造函数自动注册（已有机制）。（2026-07）
>
> - **ABasePanel 注册时机修复** — `ABasePanel<T>` 子类构造函数调用 `Init()` → `GameObject.FindGameObjectWithTag`，不能在 `RegisterSafeServices`（BeforeSceneLoad，无场景）中注册。修正为在 `RegisterServices`（Awake，场景已加载）中注册 `ItemInfoPanel`、`ForegroundPanel`、`BuildMenuPanel`、`PauseMenuPanel`、`SettingMenuPanel`。（2026-07）
>
> - **BuildingUI + AmbitiousExperienceHub + ForegroundPanel + BackpackMenuPanel 迁移** — 4 个文件（17+14+12+10=53 处 .Instance）全部迁移至 `ServiceLocator.Get<T>()`。补注册 6 个服务：`WaveEventFeedback`（RegisterSafeServices）；`BuildMenuPanel`、`PauseMenuPanel`、`SettingMenuPanel`（RegisterServices）；`BackpackController`、`BackpackNavigationView`（Awake 自注册）。UI/ 目录 .Instance 从 236 降至 128（-46%）（2026-07）。
>
> - **NewOrContinuePanel + CreateDataPanel + CreateMenuPanel + JoinMenuPanel + GatherUI + AddWearTaskUI + WorkerBedUI + ItemInfoPanel 迁移** — 8 个文件（21+12+8+7+7+6+6+5=72 处 .Instance）全部迁移至 `ServiceLocator.Get<T>()`。补注册 7 个服务：`NewOrContinuePanel`、`CreateDataPanel`、`CreateMenuPanel`、`JoinMenuPanel`、`AsyncProgressPanel`（RegisterServices）；`AddWearTaskUI`、`JoinMenuUI`（Awake 自注册）。UI/ 目录 .Instance 从 128 降至 56（累计 -76%）（2026-07）。
>
> - **UI 目录尾量清零** — 剩余 29 个文件、56 处 .Instance 引用全部批量迁移至 `ServiceLocator.Get<T>()`。补注册 4 个服务：`CreateOrJoinPanel`、`WorkerTaskTogglePanel`、`InventoryMenuPanel`、`AIChatPanel`（RegisterServices）；`AIChatUI`（Awake 自注册）。覆盖服务：DialogueManager、ColonyCommandCenterManager、WeatherGameplayEffect、WaveBossRewardManager、WorkerSupplyIssueManager、WorkerConditionManager、WeatherManager、GameplaySessionStats、PrefabManager（注释中）、EventBus、ResourceManager、PlayerManager、ForegroundPanel、TileMap、InventoryManager、PanelController、GlobalInit、WorkerManager、BuildMenuPanel、BackpackMenuPanel、PauseMenuPanel、CreateMenuPanel、JoinMenuPanel、CreateOrJoinPanel、BuildingUI、AIChatUI、AIChatPanel、WorkerTaskTogglePanel、InventoryMenuPanel、SettingMenuPanel、SelectManagerPool。**UI/ 目录 236→0（可执行代码），仅剩 1 处注释引用**（2026-07）。
>
> **🎉 UI 层 ServiceLocator 迁移已全部完成。**
>
> **架构决策 — ABasePanel 使用 .Instance 的原因**：
> `ABasePanel<T>` 子类构造函数自动调用 `ServiceLocator.Register(this)` 并执行 `Init()`（依赖 `GameObject.FindGameObjectWithTag`）。`.Instance` 触发懒创建 → 构造函数 → 自注册，是正确模式。`ServiceLocator.Get<T>()` 跳过懒创建，会导致 `KeyNotFoundException`（对早于 GlobalInit.Awake 执行的脚本）或 `NullReferenceException`（BeforeSceneLoad 无场景）。**结论：ABasePanel 子类保持 `.Instance`，其他已注册服务使用 `ServiceLocator.Get<T>()`。**（2026-07）
>
> 当前应重点推进：**Character/ 目录 .Instance 尾量**（最后 1 处 CharacterManager.cs `CharacterCreator<CC>.Instance`，涉及泛型类型注册）、**存档/Photon 桥接架构调研**。Gameplay/、Data/、Item/、Core/Seek/ 目录 .Instance 已全部清零。非 UI 目录总计 33→1。

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
