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

这是一个 2D 生存/殖民地/战斗类 Unity 项目，已完成一轮架构分层改造，核心模块如下：

```text
Scripts/2D/Domain/                    # 纯规则层（不含 UnityEngine 引用）
  Common/                             # EventBus、GameVector2、GameGridPosition、IGameCommand/Event/Time 等
  Character/                          # DamageCalculator、LevelProgressionService
  Gameplay/                           # AchievementRuleService、ComboBonusRuleService、SkillRuleService 等
  Inventory/                          # InventoryFoodReservationService、InventoryStackingService 等
  Player/                             # PlayerDamagePolicy、PlayerMovementPolicy 等
  Wave/                               # WaveBossRuleService、WaveConfigModel、WaveRuleService
  Worker/                             # WorkerAgentSnapshot、WorkerTaskAssignmentService 等
  Dialogue/                           # PromptAssemblyService、DialoguePromptProfileModel 等

Scripts/2D/UnityAdapter/             # Unity 适配层
  UnityGameTime.cs / UnityLogger.cs / UnityVectorAdapter.cs / UnityPlayerInputAdapter.cs
  UnityMapAdapter.cs / UnityEnemySpawnAdapter.cs / UnityItemDefinitionAdapter.cs / TipHelper.cs

Scripts/2D/Gameplay/                  # 玩法管理器（依赖 Domain 和 UnityAdapter）
  WaveManager.cs / SkillManager.cs / WeatherGameplayEffect.cs / AchievementManager.cs
  SessionResultManager.cs / ComboBonusManager.cs / DeathPenaltyManager.cs
  FloatingTextManager.cs / WorkerConditionManager.cs / WorkerEfficiencyTracker.cs 等（共 29 个文件）

Scripts/2D/Character/
  Character.cs / CharacterHealthComponent.cs / CharacterDamageUIPresenter.cs
  CharacterManager.cs / ICharacterManager.cs / ICharacterCreator.cs 等接口
  Player/         # Player.cs / PlayerManager.cs / PlayerCreator.cs
  Enemy/          # AEnemy.cs / EnemyManager.cs / CommonEnemy/ / SeekEnemy/
  Worker/         # AWorker.cs / WorkerManager.cs / WorkerTaskManager.cs
    State/        # WorkerAttack/Dead/Escape/Move/Seek/WorkState
    Task/         # WorkerBuild/Carry/Gather/Hungry/PlantTask + Individual/

Scripts/2D/Item/
  InventoryManager.cs / DropManager.cs / ItemInstanceFactory.cs
  Backpack/       # Consumable / Equipment(Weapon: Gun/Sword) / Food / Material / Seed
  Build/          # Door / Furniture(Bed) / Room / Wall

Scripts/2D/Map/
  TileMap.cs / BuildMap.cs / ItemMap.cs / ResourceMap.cs / GatherMap.cs / IsAvailableMap.cs 等

Scripts/2D/AI/Dialogue/
  Core/           # DialogueManager.cs / DialogueSession.cs / NPCDialogueTrigger.cs
  LLM/            # ILLMClient.cs / LlamaServerClient.cs / RemoteAPIClient.cs 等
  Prompt/         # PromptBuilder.cs / PromptTemplateLoader.cs / NPCPromptProfile.cs
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
Scripts/2D/Core/                      # KDTree / A* 寻路(Seek) / ServiceLocator / Singleton
Scripts/2D/Editor/                    # Editor 工具 + Tests/Domain + Tests/Tool 单元测试
Scripts/2D/Serializable/              # Vector3LAB 等
```

项目已有自定义基础设施：

- `Singleton<T>` / `ASingletonSaveData<T>` / `ServiceLocator`
- `CharacterManager<CM, C, CC>` / 相关接口 `ICharacterManager` 等
- `Vector3LAB` / `Vector3IntLAB` / `GameVector2` / `GameGridPosition`（Domain 层已有纯 C# 值类型）
- `LogManager` / `IGameLogger`（已有 UnityAdapter 适配）
- `ResourceManager`
- `EventBus`（Domain 层已有事件总线）
- `Tool` / `DataTool` / `VectorTool` 等 20 个工具类
- 20 个 `Enum` 公共枚举文件
- 18 个 `Constant` 公共常量文件
- `UnityAdapter/` 下 8 个适配器（Time、Input、Map、Vector、Logger、EnemySpawn、ItemDefinition、TipHelper）
- `Network/` 下 3 个网络适配脚本（INetworkView、NetworkViewAdapters、SyncSenderAdapters）
- `Editor/Tests/Domain` 和 `Editor/Tests/Tool` 下已有单元测试
- 多个 `Editor` 菜单用于安装、验证和调试功能

项目已完成第一轮分层改造，`Domain/` 和 `UnityAdapter/` 已有实际文件。后续改造应在现有分层基础上继续推进，优先在现有 `Scripts/2D` 结构下做小步抽离。

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
4. 不要引入大型依赖注入框架，除非项目已经在使用。
5. 不要把所有 Singleton 一次性替换掉，可以先包一层接口或 Facade。
6. 不要把 Unity `ScriptableObject` 强行移出 Unity；它可以作为配置源，但核心规则不要直接依赖它。
7. 不要让 `GameCore`、`Domain` 或纯规则类使用 `using UnityEngine;`。
8. 不要让纯规则类继承 `MonoBehaviour`、`MonoBehaviourPun` 或使用 Unity 生命周期方法。
9. 不要让核心规则直接调用 UI，例如 `PlayerStatusUI.Instance`、`ItemInfoUI.Instance`、`DebugUI.Instance`。
10. 不要让核心规则直接播放动画、音效、特效、生成 Prefab。
11. 输出代码时必须给完整文件内容，不能只给片段。
12. 如果信息不足，请基于已读代码给出最小可执行方案，不要空泛回答。

## 4. 分层建议

项目已完成第一轮分层，当前实际结构与推荐目标：

```text
Assets/Scripts/2D/
  Domain/                     # ✅ 已存在：纯 C# 领域模型、规则、事件、命令
    Common/                   # EventBus、GameVector2、GameGridPosition、IGameCommand/Event/Time/Logger、MathHelper
    Character/                # DamageCalculator、LevelProgressionService
    Gameplay/                 # AchievementRuleService、ComboBonusRuleService、SkillRuleService、SessionResultRuleService 等
    Inventory/                # InventoryFoodReservationService、InventoryStackingService、InventoryTakeReservationService
    Player/                   # PlayerDamagePolicy、PlayerMovementPolicy、PlayerVitalAlertRuleService
    Wave/                     # WaveBossRuleService、WaveConfigModel、WaveRuleService
    Worker/                   # WorkerAgentSnapshot、WorkerConditionRuleService、WorkerTaskAssignmentService 等
    Dialogue/                 # PromptAssemblyService、DialoguePromptProfileModel、ChatMessage
  UnityAdapter/               # ✅ 已存在：Unity 类型、输入、时间、地图、资源的适配
    UnityGameTime.cs / UnityLogger.cs / UnityVectorAdapter.cs / UnityPlayerInputAdapter.cs
    UnityMapAdapter.cs / UnityEnemySpawnAdapter.cs / UnityItemDefinitionAdapter.cs / TipHelper.cs
  Network/                    # ✅ 已存在：Photon 网络适配层
    INetworkView.cs / NetworkViewAdapters.cs / SyncSenderAdapters.cs
  Gameplay/                   # 玩法管理器（已部分引用 Domain 和 UnityAdapter，继续推进解耦）
  Character/                  # 角色类（已有接口抽象 ICharacterCreator、ICharacterManager 等）
  UI/ / MVC/ / Item/ / Map/   # 业务层（保持现状，逐步通过事件驱动与 Domain 交互）
  Enum/ / Constant/ / Tool/   # 公共代码层
  Core/                       # 底层算法（KDTree、A*、ServiceLocator、Singleton）
  Editor/                     # Editor 工具 + Tests/Domain + Tests/Tool 单元测试
```

当前不再需要的目录：
- `Application/`：未创建，用例服务逻辑可直接放在 Domain 或 Gameplay 中。
- `Presentation/`：未创建，表现层使用现有 UI/ + Character/ 中的 Presenter 类（如 CharacterDamageUIPresenter）。

后续改造优先在已有 `Domain/`、`UnityAdapter/` 目录中扩展，不创建新顶层目录。

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

优先抽离方向：

- `CharacterRuntimeState`
- `PlayerMovementIntent`（配合已有 PlayerMovementPolicy）
- `PlayerCommand` / `PlayerEvent`
- `ICharacterCreator` 等接口（已有基础）

### Worker Task

代表文件：

```text
Scripts/2D/Character/Worker/WorkerTaskManager.cs
Scripts/2D/Character/Worker/Task/AWorkerTask.cs
```

常见问题：

- `WorkerTaskManager : MonoBehaviour` 负责任务队列、任务分配、距离计算、任务状态更新、Debug UI 刷新。
- `AWorkerTask` 虽然不是 `MonoBehaviour`，但依赖 `UnityEngine`、`UnityAction<AWorker>`、`Time.deltaTime`、`Mathf`、`BuildMap.Instance`、`WorkerConditionManager.Instance`。
- 任务规则依赖 `AWorker` 的 `transform.position` 和 Unity 地图对象。

已有 Domain 抽离：

- `Domain/Worker/WorkerTaskAssignmentService.cs` — WorkerTaskAssignmentService
- `Domain/Worker/WorkerTaskProgressService.cs` — WorkerTaskProgressService
- `Domain/Worker/WorkerConditionRuleService.cs` — WorkerConditionRuleService
- `Domain/Worker/WorkerTaskCongestionRuleService.cs` — WorkerTaskCongestionRuleService
- `Domain/Worker/WorkerSupplyRuleService.cs` — WorkerSupplyRuleService
- `Domain/Worker/WorkerAgentSnapshot.cs` — WorkerAgentSnapshot

优先抽离方向：

- `WorkerTaskModel`
- `WorkerTaskQueue`
- `IWorkerTaskMapQuery`
- `WorkerTaskEvent`

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

- `Domain/Inventory/InventoryFoodReservationService.cs` — InventoryFoodReservationService
- `Domain/Inventory/InventoryStackingService.cs` — InventoryStackingService
- `Domain/Inventory/InventoryTakeReservationService.cs` — InventoryTakeReservationService
- `UnityAdapter/UnityItemDefinitionAdapter.cs` — UnityItemDefinitionAdapter

优先抽离方向：

- `InventoryCell`
- `InventoryGrid`
- `ResourceStack`
- `InventoryService`
- `IItemDefinitionProvider`（已有 UnityItemDefinitionAdapter 可配合）
- `InventoryChangedEvent`

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

优先抽离方向：

- `WaveState`
- `WaveSpawnRequest`
- `WaveEvent`
- `IEnemySpawnService`（已有 UnityEnemySpawnAdapter 可配合）
- `IWaveTimeScheduler`
- `IMapSpawnPointProvider`

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

常见问题：

- `PromptBuilder` 仍依赖 `Resources.LoadAll<NPCPromptProfile>` 和 `ScriptableObject` 配置。
- LLM 客户端、UI、NPC 触发器、游戏状态上下文需要继续分层。

优先抽离方向：

- `DialogueContext`
- `DialogueTurn`
- `IDialogueProfileProvider`
- `IPromptTemplateProvider`
- `IGameKnowledgeProvider`
- `ILLMClient` 保持接口化（已有 `Scripts/2D/AI/Dialogue/LLM/ILLMClient.cs`）

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
- 直接调用全局 Singleton
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

### 输入使用 Command

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
  -> UnityPlayerInputAdapter
  -> PlayerMoveCommand
  -> PlayerMovementService
```

### 结果使用 Event

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

### Unity 类型使用 Adapter 转换

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

### 外部能力使用接口

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

Unity 实现放在 Adapter 或 Infrastructure 中。

## 9. 本项目优先推荐的后续改造

项目已完成第一轮分层（`Domain/`、`UnityAdapter/`、`Network/` 已创建并有实际代码）。已完成的抽离包括但不限于：

| 领域 | 已存在的 Domain 服务 |
|---|---|
| Worker | `WorkerTaskAssignmentService`、`WorkerTaskProgressService`、`WorkerConditionRuleService`、`WorkerTaskCongestionRuleService` |
| Player | `PlayerDamagePolicy`、`PlayerMovementPolicy`、`PlayerVitalAlertRuleService` |
| Inventory | `InventoryFoodReservationService`、`InventoryStackingService`、`InventoryTakeReservationService` |
| Wave | `WaveRuleService`、`WaveBossRuleService`、`WaveConfigModel` |
| Gameplay | `AchievementRuleService`、`SkillRuleService`、`ComboBonusRuleService`、`SessionResultRuleService` 等 |
| Dialogue | `PromptAssemblyService`、`DialoguePromptProfileModel` |
| Common | `EventBus`、`GameVector2`、`GameGridPosition`、`IGameTime`、`IGameLogger` 等 |
| Unity Adapter | `UnityGameTime`、`UnityLogger`、`UnityVectorAdapter`、`UnityPlayerInputAdapter`、`UnityMapAdapter` 等 |

后续改造优先方向（按低风险到高风险排列）：

1. **继续解耦 Gameplay 管理器**：`WaveManager`、`SkillManager`、`FloatingTextManager` 等仍有部分逻辑可直接调用 Domain 服务或走事件总线。
2. **Character/Player 深入解耦**：`Player.cs` 输入/表现/规则仍有混合，进一步推进 Command → Domain → Event 链路。
3. **InventoryManager 深入解耦**：库存预占、格子计算仍有部分在 MonoBehaviour 中。
4. **存档/Photon 与 Domain 桥接**：确保 Domain 模型变更时存档兼容，Photon 同步走适配层而非直接引用。
5. **扩展单元测试**：`Editor/Tests/Domain/` 已有测试基础，继续为新增 Domain 服务补充测试。

选择模块时，请说明原因：

```md
本轮选择：WaveManager 进一步解耦
原因：
- Domain/Wave 已有 WaveRuleService 和 WaveConfigModel
- 可将 MonoBehaviour 中剩余的波次计时、生成调度逻辑进一步迁移
- 风险较低，Domain 基础设施已就绪
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

如果无法运行 Unity，请明确说明“未运行 Unity 验证”，并给出静态检查结果和建议手测路径。

## 12. 迁移复用分析格式

请输出：

```md
## 引擎迁移复用分析

| 模块 | 当前或目标位置 | 迁移到 Godot/Unreal/服务端时是否可复用 | 原因 |
|---|---|---|---|
| Worker 任务分配规则 | Domain/Worker | 可复用 | 不依赖 UnityEngine，只处理快照和任务模型 |
| WorkerTaskManager MonoBehaviour | Character/Worker | 需要重写 | 依赖 Unity 生命周期、Worker 实例、DebugUI |
| Player 输入适配 | UnityAdapter/Input | 需要重写 | 依赖 Unity Input 和 Joystick |
| InventoryGrid | Domain/Inventory | 可复用 | 纯 C# 数据结构 |
| ItemMap 同步 | UnityAdapter/Map | 需要重写 | 依赖 TileMap 和 Unity Tilemap |
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
