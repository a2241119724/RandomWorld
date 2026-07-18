# 架构审查报告 — RandomWorld 项目

**日期:** 2026-07-14
**审查范围:** 全项目（排除第三方库 Photon、TextMesh Pro）
**审查重点:** 非 Unity 引擎层面的架构设计问题

---

## 总体评估

项目在 Domain 层展现了良好的架构意图（纯 C# 领域模型、接口隔离、Adapter 模式、Command 模式），但**架构意图与运行时实现之间存在显著断裂**。大量精心设计的抽象在实际运行中未被接入，形成了"架构蓝图与实际代码双轨"的局面。

---

## 🔴 严重问题

### 1. UnityAdapter 层完全休眠 — 架构的核心断裂

**问题:** UnityAdapter 层定义了 6 个 Domain 接口的 Unity 实现（`UnityGameTime`、`UnityLogger`、`UnityEnemySpawnAdapter`、`UnityItemDefinitionAdapter`、`UnityMapAdapter`、`UnityPlayerInputAdapter`），但**在 `GlobalInit.RegisterServices()` 中没有一个被注册到 ServiceLocator**。取而代之的是直接注册具体单例：

```csharp
// GlobalInit.cs — 当前实际代码
ServiceLocator.Register(LogManager.Instance);      // 具体类型，非 IGameLogger
ServiceLocator.Register(TileMap.Instance);          // 具体类型，非 IMapSpawnPointProvider
ServiceLocator.Register(EnemyManager.Instance);     // 具体类型，非 IEnemySpawnService
```

所有 Gameplay 层的 Manager 直接通过 `Singleton<T>.Instance` 互相调用，完全绕过了 Domain 层定义的接口抽象。

**影响:**
- Domain 层的接口定义成了"摆设"，没有起到解耦作用
- 无法替换实现进行测试
- 代码审查中看到的"Clean Architecture"实际上不工作

**建议:** 在 `GlobalInit.RegisterServices()` 中将适配器实例按接口类型注册到 ServiceLocator。同时逐步将 Manager 中对 `XxxManager.Instance` 的直接调用改为通过接口注入。

---

### 2. Domain 层违反依赖规则（反向依赖）

**问题:** Domain 层（理论上应是最内层、不依赖任何外层）中存在对以下外层类型的 `using` 引用：

| 外部命名空间 | 被 Domain 使用的类型 | 所在层次 |
|---|---|---|
| `LAB2D.Enum` | `WorkerTaskType`, `EquipmentRarityType`, `WorkerConditionState` 等 16+ 枚举 | 外层/共享层 |
| `LAB2D.Character.Worker` | `AWorker`（常量引用） | Character 层 |
| `LAB2D.Character.Worker.Task` | `AWorkerTask`, `WorkerBuildTask` 等（`is` 类型判断） | Character 层 |
| `LAB2D.Item` | `ResourceInfo` | Item 层 |
| `LAB2D.Serializable` | `Vector3IntLAB` | 序列化层 |

`ColonyCommandCenterRuleService` 中的代码：
```csharp
// Domain/Gameplay/ColonyCommandCenterRuleService.cs
// Domain 层直接依赖外层 Character/Worker/Task 的具体类型
if (task is WorkerBuildTask buildTask) { ... }
else if (task is WorkerCarryTask carryTask) { ... }
```

**影响:** 这破坏了 Clean Architecture / 六边形架构的核心原则——Domain 层不应知道外层实现细节。如果要复用 Domain 层到另一个项目或进行单元测试，这些外部类型必须一并存在。

**建议:**
- 将 `LAB2D.Enum` 中的枚举定义移入 Domain 层（枚举本质上是纯数据，属于 Domain）
- `ColonyCommandCenterRuleService` 中对具体 Worker Task 类型的 `is` 判断应改为策略模式或访问者模式，或为 Tasks 引入 Domain 层的抽象接口
- `Vector3IntLAB` 应被 Domain 层的 `GameGridPosition` 替代

---

### 3. 重复实现 `IMapWalkabilityQuery`

**问题:** 同一接口有两个独立的、不一致的实现：

1. `UnityMapAdapter`（`UnityAdapter/UnityMapAdapter.cs`）— 通过 `UnityVectorAdapter` 做向量转换
2. `ColonyCommandCenterTool.BuildMapWalkabilityQuery`（`Tool/ColonyCommandCenterTool.cs:593`）— 私有嵌套类，手动构造 Vector3Int

这种重复说明适配器模式没有被强制执行，不同开发者选择了各自的实现方式。

**建议:** 统一使用 `UnityMapAdapter`，删除 `BuildMapWalkabilityQuery`，或至少将其重构为使用 `UnityVectorAdapter`。

---

## 🟡 中等问题

### 4. Singleton 泛滥

**问题:** 项目中超过 30 个类通过 `Singleton<T>.Instance` 暴露全局访问：

```
Manager 层: LogManager, WeatherManager, ArchiveManager, ResourceManager, ItemDataManager, DropDataManager...
Gameplay 层: WaveManager, SkillManager, AchievementManager, ComboBonusManager, DeathPenaltyManager,
             FloatingTextManager, EnemyLootManager, ColonyCommandCenterManager 等 15+ 个
Character 层: PlayerManager, WorkerManager, EnemyManager, WorkerTaskManager
Map 层: TileMap, BuildMap, ResourceMap, ItemMap
UI 层: PanelController, 各 ABasePanel<T> 子类
```

**影响:**
- 紧耦合：任意类可以随意访问任意单例
- 测试困难：无法替换单例实现
- 初始化顺序敏感：单例的隐式依赖形成不可见的初始化依赖图
- 并行/多场景支持困难

**建议:**
- 将 Gameplay 层的 Manager 通过 ServiceLocator 按接口注册
- 使用构造函数注入或方法注入替代直接 Singleton 访问
- 至少为每个 Manager 定义接口，让消费者依赖接口而非实现

---

### 5. GlobalInit 上帝对象

**问题:** `GlobalInit` 集中了过多职责：
- 服务注册
- 面板初始化（`GlobalPanelInitializer`）
- 输入处理（`GlobalInputProcessor`）
- Tip 展示（实现 `ITipService`）
- Update 驱动循环（驱动 `WorkerUpdateSystem`）
- `IInitializable` 和 `ITickable` 生命周期的管理

虽然有 `GlobalPanelInitializer` 和 `GlobalInputProcessor` 的提取，但 `GlobalInit` 仍然是系统的"中心开关"，所有初始化逻辑最终都汇集于此。

**建议:** 将 `GlobalInit` 拆分为独立的 Bootstrap/Composition Root，使用更正式的应用生命周期管理（如 Unity 的 Scene 加载事件或自定义 Application 状态机）。

---

### 6. MVC 实现不规范

**问题:** `MVCController<T>` 的 `Awake()` 方法中存在对具体 UI Panel 的直接引用：

```csharp
// BackpackController 中
BackpackMenuPanel.Instance.Select(...);
// BuildController 中
BuildMenuPanel.Instance.Select(...);
```

这违反了 MVC 模式——Controller 不应直接知道 Panel 的存在。View 和 Controller 的边界被模糊。

此外，`MVCItemManagerView.UpdateView()` 通过销毁所有子节点并重新实例化来刷新 UI，在大型库存中可能产生性能问题。

**建议:** Controller 应通过事件/通知机制与 Panel 通信，或通过依赖注入获取 Panel 引用，而非直接访问 Singleton。

---

### 7. ServiceLocator 是反模式但未被正确使用

**问题:** `ServiceLocator` 同时承担两个角色：
1. 运行时服务注册/查找（本应使用 DI 容器）
2. 测试实例替换（`SetInstance()`）

但在实际代码中，它既没有完全替代 Singleton 模式，也没有提供任何 Singleton 模式不具备的优势——因为所有消费者仍然直接使用 `XxxManager.Instance`。

**建议:** 要么完全迁移到 ServiceLocator（所有依赖通过它解析），要么删除它，避免"中间状态"的混乱。如果采用 ServiceLocator，需要配套的编码规范禁止直接 Singleton 访问。

---

### 8. 测试覆盖仅限于 Domain 层

**问题:** 全部 34 个单元测试都在 `Editor/Tests/Domain/` 下，覆盖 `EventBus`、`DamageCalculator`、`SkillRuleService`、`WeatherGameplayRuleService`。

没有任何测试覆盖：
- Tool 层（转换逻辑、格式化逻辑）
- Manager 层（运行时状态管理）
- Character 系统（状态机、任务系统）
- AI/寻路系统（A* 算法正确性）
- MVC 控制器和视图
- 存档系统

**建议:** 至少为 Tool 层和 Character 状态机添加单元测试。Tool 层的纯逻辑方法（如 `ColonyCommandCenterTool` 的报告构建）同样不依赖 Unity，可像 Domain 层一样直接测试。

---

## 🟢 轻微问题

### 9. AgentFull — Python AI 框架嵌入 Unity 项目

**问题:** `AgentFull/` 目录是一个完整的 Python AI agent 框架，包含 LLM 客户端、多种模型支持（OpenAI/Anthropic/Ollama）、代码生成模板等。它被放置在 Unity Assets 目录中，但它：
- 不是 Unity 运行时的一部分
- 有自己的配置、缓存、日志系统
- 生成的代码（`StatusEffectController.cs`、`WorkerMoraleController.cs`）放在了 `Scripts/` 根目录和 `LAB2D.AgentGenerated` 命名空间

**影响:**
- 增加项目复杂度
- AI 生成的代码放在不规范的位置（根级 `Scripts/`）
- `LAB2D.AgentGenerated` 命名空间打破了 `LAB2D.{Layer}` 的组织约定

**建议:** 将 AgentFull 移到 Unity 项目外部（`D:\LAB\Unity\RandomWorld\Tools\AgentFull`）。将 AI 生成的代码放入 `Scripts/2D/AgentGenerated/` 子目录，与其他代码保持一致的层次组织。

---

### 10. StatusEffectController 和 WorkerMoraleController 位置不当

**问题:** 这两个文件是 AI 生成的，位于 `Scripts/` 根目录下，而非 `Scripts/2D/` 子目录。这打破了目录组织约定。同时它们使用了 `LAB2D.AgentGenerated` 命名空间，与其他层的命名约定不一致。

**建议:** 移动到 `Scripts/2D/AgentGenerated/`，并考虑合并到现有的 Gameplay 层系统中（WorkerMoraleController 与 `WorkerConditionManager` 功能有重叠）。

---

### 11. 文档全为空模板

**问题:** `docs/ai-context/` 下的 spec.md、project-structure.md、progress.md 都是模板占位符，没有任何项目实际信息。这使得新开发者或 AI 助手无法快速理解项目。

**建议:** 至少填写 spec.md 的核心功能描述和 project-structure.md 的目录组织说明。

---

### 12. `ASingletonSaveData<T>` 继承 `Singleton<T>` — 单继承困境

**问题:** 由于 C# 不支持多重继承，`ASingletonSaveData<T>` 同时继承 `Singleton<T>` 和 `AMonoSaveData`（通过将 `AMonoSaveData` 作为 `Singleton<T>` 的基类）。这导致了复杂的继承链：

```
MonoBehaviour → AMonoSaveData → Singleton<T> → ASingletonSaveData<T>
```

当需要 `MonoBehaviourPun`（Photon 网络）时，`Character` 类就不能同时继承 `Singleton<T>`，必须自己处理单例逻辑。这是继承深度过大的典型症状。

**建议:** 使用组合代替继承。将 Singleton 行为和 SaveData 行为作为独立的 Component 附加，或使用接口 + 扩展方法。

---

### 13. 并发寻路与 Unity 主线程的潜在竞争

**问题:** `ASeek` 使用 `SemaphoreSlim` 和 `Task.Run` 进行后台寻路，`WalkabilityCache` 通过 `bool[,]` 数组在后台线程读取。虽然 `bool` 读取在 C# 中是原子的，但 `WalkabilityCache.Refresh()` 在主线程写入整个数组时，`IsWalkable()` 在后台线程读取——在 Refresh 期间读取的线程可能看到部分新旧数据的混合（数组引用替换不是原子的）。

**当前代码:**
```csharp
// WalkabilityCache.Refresh() 在主线程
for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
        _cache[x, y] = ASeek.IsCanReach(...);

// AStar 在后台线程
if (WalkabilityCache.IsWalkable(x, y)) { ... }
```

**建议:** 使用 `volatile` 或 `Thread.MemoryBarrier` 确保缓存写入对后台线程可见，或者使用不可变快照（交换数组引用）代替原地刷新。

---

## 架构优点（不应改动）

1. **Domain 层纯 C# 设计良好** — 12 个接口、20 个 RuleService、值对象、EventBus，均无 Unity 依赖，架构思路清晰
2. **RuleService + Tool + Manager 三层分离** — Domain 纯逻辑 → Tool 格式化/适配 → Manager 运行时状态，职责分明
3. **Command 模式** — `IGameCommand` 子类（`PlayerMoveCommand`、`PlayerAttackCommand`、`ActivateSkillCommand`）设计得当
4. **ColonyDiagnosticContext 委托注入** — 用 `Func<>` 委托替代 DI 框架的思路很实用
5. **SpendPool 对象池** — 对寻路数组的良好池化设计，减少 GC 压力
6. **EventBus 支持测试替换** — `SetInstance()` 方法允许测试中注入 mock
7. **像素 UI 主题系统** — `PixelUITheme` 集中管理 UI 颜色常量，保持视觉一致性
8. **Enum 提取到独立目录** — 将 `WorkerTaskType` 等枚举独立到 `LAB2D.Enum` 以解决 Domain 和 Character 层之间的循环依赖问题

---

## 优先修复顺序

| 优先级 | 问题 | 工作量 | 风险 |
|--------|------|--------|------|
| P0 | UnityAdapter 接入运行时（通过 ServiceLocator） | 中 | 低 — 只改注册，不改调用方 |
| P0 | Domain 层反向依赖清理（枚举移入 Domain） | 中 | 中 — 影响大量文件 |
| P1 | 删除重复的 `IMapWalkabilityQuery` 实现 | 低 | 低 |
| P1 | 填充项目文档（spec.md、project-structure.md） | 低 | 无 |
| P2 | GlobalInit 职责拆分 | 高 | 高 — 牵一发动全身 |
| P2 | Singleton 逐步替换为接口注入 | 高 | 高 — 需渐进式迁移 |
| P3 | AgentFull 移出 Assets 目录 | 低 | 低 |
| P3 | AI 生成代码移到正确位置 | 低 | 低 |
| Backlog | 添加 Tool/Manager 层测试 | 持续 | 低 |
| Backlog | MVC Panel 引用解耦 | 中 | 中 |
| Backlog | 寻路并发安全性加固 | 低 | 中 |
