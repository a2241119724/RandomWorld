# RandomWorld Unity Agent 系统扩展方案

## 1. 总体设计思路

本方案面向当前 RandomWorld 项目引入“主 Agent + 子 Agent + Skill 能力集”的智能化开发与运行辅助体系。它的首要目标不是让运行时游戏对象直接变成大模型代理，而是先建立一套可工程化、可审计、可扩展的开发辅助系统：主 Agent 负责任务理解、上下文建模、任务分发和结果汇总；子 Agent 负责各 Unity 领域模块；Skill 提供可复用的具体能力，如代码审查、脚本生成、场景分析、资源检查、性能优化和构建修复。

当前项目已有比较清晰的 2D 目录边界：`Map`、`Character`、`Item`、`MVC`、`UI`、`Data`、`Manager`、`Editor`、`Tool`。Agent 体系应尊重这些边界，把“智能化”放在工程协作层，而不是打散现有架构。

推荐分三层落地：

| 层级 | 主要职责 | 推荐放置位置 | 首期形态 |
| --- | --- | --- | --- |
| Orchestration | 主 Agent、任务路由、上下文管理、任务状态 | `Assets/Agent/Config`、未来 `Assets/Agent/Editor` | JSON 配置 + Editor 面板 |
| Domain Agents | Gameplay、Map、Item、UI、AI、Resource、Build 等子 Agent | `Assets/Agent/Config`、未来 `Assets/Agent/Runtime` | 配置驱动的职责声明 |
| Skills | 代码审查、脚本生成、错误分析、资源检查、测试建议等 | `Assets/Agent/Docs`、未来 `Assets/Agent/Skills` | 能力清单 + 执行规范 |

### 当前项目适配重点

RandomWorld 的 Agent 体系需要重点覆盖以下方向：

- 2D Tilemap 世界生成、地图层级、资源/采集/建造/物品层之间的同步关系。
- 工人任务系统、角色状态机、敌人 AI、AStar/Seek 寻路和 Worker 行为扩展。
- 背包、建造、房间、农田、装备、武器、掉落与 ScriptableObject 数据驱动。
- 面板栈、HUD、背包/建造 MVC、AIChatPanel 和运行时交互 UI。
- Photon 网络同步、地图数据请求、联网/离线差异和 RPC/缓存风险。
- 存档系统、多存档槽位、反射调用 `SaveData`/`LoadData` 的可维护性。
- Resources、AssetBundle、StreamingAssets、Addressables 的资源加载边界。
- Editor 自动化工具、数据生成器、Builder 生成器和后续批处理能力。

## 2. 主 Agent 设计

主 Agent 建议命名为 `ProjectDirectorAgent`。它不直接改业务对象，而是作为项目级任务控制器，负责把用户需求转换为可执行任务流，并把结果整理成开发者可理解、可验证、可回滚的产物。

### 2.1 核心职责

| 职责 | 说明 | 当前项目落点 |
| --- | --- | --- |
| 需求解析 | 判断任务属于开发、调试、优化、资源、玩法、重构、构建、文档或数据 | 通过关键词、目标路径、涉及类型和报错栈分类 |
| 上下文管理 | 维护模块地图、脚本索引、资源索引、场景索引、存档/网络风险信息 | 扫描 `Scripts/2D`、`Resources`、`Scenes`、`StreamingAssets` |
| 任务拆解 | 将一个需求拆成多个领域子任务，形成任务卡 | 如“新增工人种植任务”拆成 WorkerTask、ItemData、UI、Map、Test |
| Agent 路由 | 按目录、类名、资源类型和任务类型选择子 Agent | 参考 `Config/task_router.json` |
| Skill 选择 | 为每个子任务匹配可复用能力 | 例如 UI Agent 调用 ScriptGenerateSkill + TestSkill |
| 风险控制 | 标记影响存档、联网同步、Prefab 引用、Scene 修改、AB 打包的高风险操作 | 修改 `Resources/SO`、Prefab、地图层、Photon 同步时必须提示 |
| 结果汇总 | 汇总修改建议、验证步骤、剩余风险、后续扩展方向 | 生成任务报告或 PR/变更说明 |
| 记忆更新 | 把已确认的项目规则、命名习惯、常见错误写入项目记忆 | 未来放入 `Assets/Agent/Memory` |

### 2.2 任务分类器

主 Agent 应使用多信号分类，而不是只靠关键词。

| 分类 | 判断信号 | 优先路由 |
| --- | --- | --- |
| 玩法开发 | 涉及角色、工人、敌人、背包、建造、采集、种植、战斗 | Gameplay Agent、AI/NPC Agent、Item Agent、Map Agent |
| UI 开发 | 涉及 Panel、HUD、按钮、背包界面、建造菜单、AIChat | UI Agent |
| 数据配置 | 涉及 `Resources/SO`、ItemData、BuildItemData、DropItemData、JSON | Data Agent、Resource Agent |
| 调试修复 | 出现异常栈、编译错误、NullReference、MissingReference、Photon 错误 | Debug Agent |
| 性能优化 | 涉及 FPS、GC、Draw Call、Tilemap 刷新、资源加载、寻路耗时 | Performance Agent |
| 资源管理 | 涉及 Prefab、Sprite、Tile、Material、Shader、AssetBundle、Addressables | Resource Agent |
| 存档/同步 | 涉及 `ISaveData`、`ISyncData`、Archive、Photon、RPC | Save Agent、Network Agent |
| 构建发布 | 涉及 Windows、Android、WebGL、StreamingAssets、AB、平台适配 | Build Agent |
| 编辑器工具 | 涉及 UnityEditor、Inspector、MenuItem、批处理、自动生成 | Tool Agent |
| 文档沉淀 | 涉及 README、模块说明、接口说明、流程图 | Document Agent |

### 2.3 上下文模型

主 Agent 维护的上下文建议拆为 6 类：

| 上下文 | 内容 | 更新时机 |
| --- | --- | --- |
| ModuleContext | 模块目录、核心类、依赖关系、职责边界 | 每次任务开始前轻量扫描 |
| AssetContext | SO、Prefab、Tile、Sprite、Material、Shader、AB 路径 | 资源任务或构建任务前刷新 |
| SceneContext | 场景列表、入口场景、关键 GameObject、组件绑定 | 场景/UI/运行时问题前刷新 |
| RuntimeContext | 启动顺序、单例、Update 高频逻辑、网络状态、存档状态 | 调试/性能任务前刷新 |
| RuleContext | 项目规则、命名规范、README 备注、已知坑 | 手动确认后更新 |
| TaskMemory | 历史任务、变更摘要、验证结果、失败回滚记录 | 每个任务结束后更新 |

### 2.4 主 Agent 决策流程

```text
用户需求
  -> ProjectDirectorAgent 解析意图
  -> 读取 AgentRegistry 与 TaskRouter
  -> 收集最小必要上下文
  -> 生成任务卡与风险等级
  -> 分发给一个或多个子 Agent
  -> 子 Agent 调用 Skill 执行
  -> 验证编译/资源/场景/运行路径
  -> 汇总结果、风险、回滚建议
  -> 更新项目记忆与报告
```

## 3. 子 Agent 分类与职责

### 3.1 Gameplay Agent

负责核心玩法、玩家输入、交互逻辑、采集、建造、战斗、时间/天气、游戏规则调整。

当前项目重点关注：

- `GlobalInit` 的全局输入和初始化逻辑。
- `BuildingUI`、`GatherUI`、`ItemInfoUI` 等交互入口。
- `ItemMap`、`BuildMap`、`GatherMap`、`ResourceMap` 的玩法层级关系。
- `AItem`、`ABackpackItem`、`ABuildItem`、`AWeapon` 的行为边界。

推荐调用 Skill：`CodeReviewSkill`、`ScriptGenerateSkill`、`RefactorSkill`、`TestSkill`、`DocumentSkill`。

### 3.2 Map Agent

负责 Tilemap、地图层级、随机资源、可建造区域、采集区域和地图存档/同步。

当前项目重点关注：

- `TileMap`、`BaseTileMap`、`ATileMapData`。
- `ResourceMap`、`BuildMap`、`GatherMap`、`ItemMap`、`IsAvailableMap`。
- `Resources/Tilemap/Map`、`Resources/Tilemap/Item` 的命名和 Tile 引用。
- `ISyncData` 与 Photon 地图同步。

推荐调用 Skill：`SceneAnalyzeSkill`、`ResourceCheckSkill`、`PerformanceOptimizeSkill`、`ErrorAnalyzeSkill`、`TestSkill`。

### 3.3 UI Agent

负责 Panel、HUD、背包界面、建造界面、提示、拖拽、按钮事件、TextMesh Pro 字体。

当前项目重点关注：

- `PanelController` 的面板栈。
- `ABasePanel<T>`、`IBasePanel`、`IPanelCommonButton`。
- `BackpackController`、`BuildController` 和 MVC 视图。
- `ForegroundUI`、`GameInfoUI`、`GameTimeUI`、`AIChatPanel`。

推荐调用 Skill：`ScriptGenerateSkill`、`SceneAnalyzeSkill`、`CodeReviewSkill`、`TestSkill`。

### 3.4 AI/NPC Agent

负责 NPC、Worker、Enemy、状态机、任务系统、寻路、战斗 AI、对话逻辑。

当前项目重点关注：

- `Character`、`ACharacterState<T>`、`CharacterStateManager`。
- `WorkerManager`、`WorkerTaskManager`、`AWorkerTask` 与具体任务。
- `AWorkerState`、`WorkerSeekState`、`WorkerWorkState`、`WorkerMoveState`。
- `AEnemy`、`ACommonEnemy`、`ASeekEnemy` 与 Enemy 状态。
- `Core/Seek/AStar`、`ISeek`、`KDTree`。

推荐调用 Skill：`ScriptGenerateSkill`、`RefactorSkill`、`PerformanceOptimizeSkill`、`TestSkill`、`DocumentSkill`。

### 3.5 Item/Data Agent

负责背包、装备、武器、建造物、房间、农田、掉落、ScriptableObject 和数据驱动扩展。

当前项目重点关注：

- `AItem`、`ABackpackItem`、`ABuildItem`、`ItemInstanceFactory`。
- `ItemData`、`BuildItemData`、`ItemDataSO`、`BuildItemDataSO`、`DropItemDataSO`。
- `Resources/SO` 与 `Resources/Tilemap/Item` 的名称绑定。
- README 中“道具数据 ItemData 与地图瓦片 Tile 的名称关联绑定”的约束。

推荐调用 Skill：`ConfigGenerateSkill`、`ResourceCheckSkill`、`ScriptGenerateSkill`、`CodeReviewSkill`。

### 3.6 Resource Agent

负责 Prefab、材质、贴图、Tile、Sprite、Shader、AssetBundle、Addressables、资源路径和引用完整性。

当前项目重点关注：

- `ResourceManager` 通过 `Resources`、`StreamingAssets/Prefab` 和 AssetBundle 加载资源。
- `ResourceConstant`、`PrefabConstant`、`LayerConstant`、`TagConstant`。
- `ResourcesLocal/Prefabs`、`StreamingAssets`、`AddressableAssetsData`。
- README 中“修改 Prefab 后需要重新打 AB 包”的约束。

推荐调用 Skill：`ResourceCheckSkill`、`BuildFixSkill`、`PerformanceOptimizeSkill`、`DocumentSkill`。

### 3.7 Scene Agent

负责场景加载、场景切换、光照、环境、入口对象和组件绑定。

当前项目重点关注：

- `Scenes/RigisterOrLogin.unity`、`Scenes/Menu.unity`、`Scenes/Game.unity`。
- `Scenes/Game/All Profile.asset`。
- UI、Manager、Tilemap、Camera、Photon 相关对象绑定。

推荐调用 Skill：`SceneAnalyzeSkill`、`ErrorAnalyzeSkill`、`TestSkill`。

### 3.8 Debug Agent

负责 Unity 控制台错误、编译失败、NullReference、MissingReference、资源缺失、Photon 连接错误、存档读写异常定位。

当前项目重点关注：

- `LogManager` 日志入口。
- 单例 `Instance` 初始化时序。
- `GlobalInit.Awake/Start/Update` 中依赖对象是否已经存在。
- `ResourceManager` 查找失败日志，如 `prefab not found`、`scriptable not found`。
- `ArchiveManager` 文件读写、反射调用和编码异常。

推荐调用 Skill：`ErrorAnalyzeSkill`、`CodeReviewSkill`、`SceneAnalyzeSkill`、`ResourceCheckSkill`。

### 3.9 Performance Agent

负责帧率、GC、内存、Tilemap 刷新、寻路、UI 重建、资源加载和网络同步性能。

当前项目重点关注：

- `GlobalData.MaxFrame = 300` 的帧率策略。
- `GlobalInit.Update` 中每帧遍历 Worker。
- AStar 寻路和 KDTree 查询。
- Tilemap 数据保存/加载和大地图同步。
- UI 拖拽、背包刷新、建造预览的 GC。
- AssetBundle/Resources 加载时机。

推荐调用 Skill：`PerformanceOptimizeSkill`、`CodeReviewSkill`、`TestSkill`。

### 3.10 Build Agent

负责打包设置、平台适配、AB 生成、StreamingAssets、Photon 配置、URP 设置和构建错误修复。

当前项目重点关注：

- `Build` 文件夹运行 exe 的约束。
- `StreamingAssets/Prefab` AssetBundle 是否存在。
- Windows、Android、WebGL 对文件 IO、Photon、Shader、URP 的差异。
- `Resources` 与 `Addressables` 混用策略。

推荐调用 Skill：`BuildFixSkill`、`ResourceCheckSkill`、`PerformanceOptimizeSkill`、`DocumentSkill`。

### 3.11 Tool Agent

负责 Unity Editor 工具、自动化脚本、批处理、Inspector 扩展和数据生成。

当前项目重点关注：

- `BuilderGenerator` 基于 `AssetPostprocessor` 自动生成 Builder。
- `Scripts/2D/Editor/DataToolMenu.cs`、`UITool.cs`、`OtherTool.cs`。
- 后续可增加 Agent 面板、资源扫描、SO 批量创建、任务报告导出。

推荐调用 Skill：`EditorToolSkill`、`ScriptGenerateSkill`、`CodeReviewSkill`、`DocumentSkill`。

### 3.12 Save/Network Agent

负责存档、多存档、数据同步、Photon RPC、离线/在线行为差异。

当前项目重点关注：

- `ArchiveManager` 的多存档槽、`Application.persistentDataPath`、`.lab` 数据。
- `ASaveData`、`AMonoSaveData`、`ISaveData`。
- `ISyncData`、`SyncDataTool`、`NetworkConnect.OnJoinedRoom`。
- `PhotonView`、`IPunObservable`、地图层同步和武器/特效同步。

推荐调用 Skill：`ErrorAnalyzeSkill`、`RefactorSkill`、`TestSkill`、`DocumentSkill`。

## 4. Skill 能力集设计

Skill 是低耦合的能力单元，应具有明确输入、输出和副作用边界。Skill 不决定“做什么”，只决定“怎么做某类动作”。

| Skill | 主要能力 | 输出 |
| --- | --- | --- |
| CodeReviewSkill | 检查 C# 质量、命名、耦合、生命周期、单例时序、潜在 Bug | Review 报告、风险等级、修复建议 |
| ScriptGenerateSkill | 生成 MonoBehaviour、ScriptableObject、Editor、状态、任务、MVC 类 | 脚本草案、命名建议、依赖清单 |
| ErrorAnalyzeSkill | 分析编译错误、运行时报错、Unity 控制台日志、Photon 错误 | 根因定位、最小修复路径 |
| RefactorSkill | 重构职责、抽接口、拆方法、降低重复和耦合 | 重构计划、影响范围、迁移步骤 |
| SceneAnalyzeSkill | 分析 Scene 层级、组件绑定、Prefab 引用、Missing 脚本 | 场景检查报告 |
| ResourceCheckSkill | 检查 SO/Tile/Sprite/Prefab/AB 缺失、重复、命名、路径 | 资源完整性报告 |
| PerformanceOptimizeSkill | 分析渲染、GC、寻路、Tilemap、UI、内存、加载 | 优化清单、验证指标 |
| ConfigGenerateSkill | 生成 JSON、SO 数据结构、配置模板、数据校验规则 | 配置模板和字段说明 |
| EditorToolSkill | 生成 EditorWindow、MenuItem、批处理、Inspector 扩展 | Editor 工具草案 |
| TestSkill | 设计测试用例、调试步骤、运行时验证流程 | 测试清单、验收标准 |
| BuildFixSkill | 分析构建失败、平台差异、AB/StreamingAssets、URP | 构建修复方案 |
| DocumentSkill | 生成模块文档、接口说明、流程说明、维护手册 | Markdown 文档 |

详细清单见 [SkillCatalog.md](SkillCatalog.md)。

## 5. Agent 与 Skill 的调用关系

### 5.1 调用原则

- 主 Agent 可以调用任何子 Agent，但不直接调用底层资源修改能力。
- 子 Agent 根据任务需要调用 Skill，Skill 不能反向调度 Agent。
- 同一个 Skill 可以被多个 Agent 复用，但输出必须带上调用者、输入上下文、影响路径和验证建议。
- 高风险修改必须同时调用 `TestSkill`，资源/场景相关修改必须调用 `ResourceCheckSkill` 或 `SceneAnalyzeSkill`。

### 5.2 调用矩阵

| 子 Agent | 常用 Skill | 高风险时附加 Skill |
| --- | --- | --- |
| Gameplay Agent | ScriptGenerate、CodeReview、Refactor、Test | ResourceCheck、SceneAnalyze |
| Map Agent | SceneAnalyze、ResourceCheck、PerformanceOptimize、Test | BuildFix、ErrorAnalyze |
| UI Agent | ScriptGenerate、SceneAnalyze、CodeReview、Test | ResourceCheck |
| AI/NPC Agent | ScriptGenerate、Refactor、PerformanceOptimize、Test | ErrorAnalyze |
| Item/Data Agent | ConfigGenerate、ResourceCheck、ScriptGenerate、CodeReview | BuildFix、Test |
| Resource Agent | ResourceCheck、BuildFix、PerformanceOptimize、Document | SceneAnalyze |
| Scene Agent | SceneAnalyze、ErrorAnalyze、Test | ResourceCheck |
| Debug Agent | ErrorAnalyze、CodeReview、SceneAnalyze、ResourceCheck | Test |
| Performance Agent | PerformanceOptimize、CodeReview、Test | ResourceCheck |
| Build Agent | BuildFix、ResourceCheck、Document | PerformanceOptimize |
| Tool Agent | EditorTool、ScriptGenerate、CodeReview、Document | Test |
| Save/Network Agent | ErrorAnalyze、Refactor、Test、Document | BuildFix |

## 6. 推荐目录结构

当前已生成以下落地目录：

```text
Assets/Agent
  README.md
  Config/
    agent_registry.json
    task_router.json
  Docs/
    UnityAgentSystemArchitecture.md
    SkillCatalog.md
    ImplementationRoadmap.md
  Templates/
    agent_task_card.md
```

后续实施阶段建议扩展为：

```text
Assets/Agent
  Config/
    agent_registry.json
    task_router.json
    project_context_rules.json
  Docs/
    ...
  Editor/
    AgentDashboardWindow.cs
    AgentContextScanner.cs
    AgentResourceValidator.cs
    AgentReportExporter.cs
  Runtime/
    AgentRuntimeFacade.cs
    AgentTaskState.cs
    AgentEventBus.cs
  Skills/
    CodeReview/
    ResourceCheck/
    SceneAnalyze/
    BuildFix/
  Memory/
    project_rules.json
    module_index.json
    known_issues.json
  Reports/
    2026-04-26/
  Templates/
    agent_task_card.md
```

注意：运行时代码和 Editor 代码必须分离。Editor 工具应放入 `Assets/Agent/Editor`，避免打进运行时包；如果未来加入运行时 Agent 面板或 AIChatPanel 集成，则放入 `Assets/Agent/Runtime`，并通过 asmdef 隔离依赖。

## 7. 示例配置方式

本方案优先推荐 JSON 配置，因为它便于外部自动化工具、EditorWindow 和版本控制审查。后续如需要在 Inspector 中可视化，可把 JSON 同步生成 ScriptableObject。

已有配置示例：

- `Config/agent_registry.json`：定义 Agent、Skill、项目上下文根路径和策略。
- `Config/task_router.json`：定义任务类型、关键词、路径匹配和路由优先级。

后续可新增 `AgentProfileSO`：

```csharp
[CreateAssetMenu(menuName = "LAB/Agent/Agent Profile")]
public class AgentProfileSO : ScriptableObject
{
    public string agentId;
    public string displayName;
    public string description;
    public List<string> ownedPaths;
    public List<string> defaultSkills;
    public int riskLevel;
}
```

建议只把稳定的 Agent 元数据转成 SO；任务记录、扫描索引、报告仍使用 JSON 或 Markdown，便于 diff 和自动化处理。

## 8. 示例任务执行流程

### 8.1 新增“工人钓鱼任务”

```text
用户输入：给 Worker 增加钓鱼任务，可在水边执行并产出鱼类食物。
ProjectDirectorAgent：
  1. 分类为玩法开发 + AI/NPC + Item/Data + Map。
  2. 识别影响路径：Character/Worker/Task、WorkerTaskManager、Resources/SO、Tilemap、UI/Action。
  3. 分发：
     - AI/NPC Agent：设计 WorkerFishingTask 与状态切换。
     - Map Agent：确认 Water Tile 邻接规则和可执行区域。
     - Item/Data Agent：生成鱼类 ItemDataSO/FoodItemData。
     - UI Agent：补充任务按钮或任务提示。
  4. 调用 Skill：
     - ScriptGenerateSkill：生成任务类草案。
     - ConfigGenerateSkill：生成 SO 字段建议。
     - ResourceCheckSkill：检查鱼图标、Tile 命名、SO 名称绑定。
     - TestSkill：生成验证步骤。
  5. 汇总风险：存档兼容、任务队列优先级、寻路终点、水边判定性能。
```

### 8.2 修复“prefab not found”

```text
用户输入：运行时报 prefab not found。
ProjectDirectorAgent：
  1. 分类为调试 + 资源管理 + 构建。
  2. 路由到 Debug Agent 和 Resource Agent。
  3. ErrorAnalyzeSkill 解析日志中的 prefabName。
  4. ResourceCheckSkill 检查 ResourcesLocal/Prefabs、StreamingAssets/Prefab、PrefabConstant。
  5. BuildFixSkill 判断是否需要重新打 AssetBundle。
  6. 输出修复路径：资源命名小写化、AB 是否包含目标 Prefab、是否更新 meta、是否重新构建。
```

### 8.3 优化大地图加载卡顿

```text
用户输入：Game 场景加载大地图时卡顿。
ProjectDirectorAgent：
  1. 分类为性能优化 + Map + Resource + Save。
  2. Performance Agent 分析 Tilemap 初始化、资源加载、存档反序列化和 Worker 初始化。
  3. Map Agent 分析 Tilemap 批量 SetTile、分帧加载、AsyncProgressUI 的进度更新。
  4. Resource Agent 分析 Resources/AB 的加载时机。
  5. TestSkill 给出 Profiler 标记、加载时间、GC Alloc、帧耗时验收指标。
```

## 9. 日志、权限、上下文记忆和回滚

### 9.1 日志

建议 Agent 系统使用独立日志，不和游戏运行时 `LogManager` 混淆。

| 日志类型 | 保存位置 | 内容 |
| --- | --- | --- |
| TaskLog | `Assets/Agent/Reports/<date>/task_<id>.md` | 任务输入、路由、执行、验证 |
| RiskLog | `Assets/Agent/Reports/<date>/risk_<id>.json` | 影响路径、风险等级、回滚建议 |
| ContextLog | `Assets/Agent/Memory/module_index.json` | 模块索引、资源索引、场景摘要 |

### 9.2 权限

建议按风险分级：

| 等级 | 允许操作 | 需要确认 |
| --- | --- | --- |
| L1 | 生成文档、扫描报告、只读分析 | 不需要 |
| L2 | 新增普通脚本、新增配置模板 | 修改前生成任务卡 |
| L3 | 修改现有 C#、SO、Prefab、Scene、Tilemap | 需要明确影响范围和回滚点 |
| L4 | 批量资源迁移、删除资源、重写存档结构、改网络同步协议 | 必须人工确认 |

### 9.3 上下文记忆

建议记录以下项目规则：

- 所有 Unity 资源修改必须保留 `.meta`。
- 道具数据 `ItemData` 与地图瓦片 `Tile` 存在名称绑定。
- 修改 Prefab 后需要重新打 AssetBundle。
- Photon RPC 大量数据不建议使用 buffer。
- `transform.Find("a/b/c")` 可获取 inactive 对象。
- 多存档、任务树、种植、房间判定是当前进行中方向。

### 9.4 失败回滚

每个任务卡必须记录：

- 变更前路径清单。
- 是否涉及 Scene、Prefab、SO、AssetBundle、存档格式。
- 验证失败时的回退步骤。
- 如果涉及存档结构，必须提供旧档迁移或兼容读取策略。

## 10. 后续可扩展方向

- Agent Dashboard：EditorWindow 展示任务、Agent、Skill、扫描结果和风险。
- Project Context Scanner：扫描 C#、Scene、Prefab、SO、Tile、Sprite，生成模块索引。
- Resource Validator：检查 Resources 与 StreamingAssets 的命名、重复、缺失和 AB 内容。
- Save Schema Guard：对 `ASaveData`/`AMonoSaveData` 的存档字段生成兼容性报告。
- Photon Sync Inspector：检查 `ISyncData`、`IPunObservable`、PhotonView 绑定和同步数据大小。
- Worker Task Generator：按模板生成 `AWorkerTask`、Builder、UI 按钮和测试清单。
- Item Data Generator：按配置批量生成 ItemDataSO、Tile、Sprite 引用检查报告。
- Performance Baseline：为地图加载、寻路、UI 刷新、存档加载建立 Profiler 基线。
- Documentation Bot：自动生成模块 README、类职责、调用链和维护建议。

## 11. 适合当前项目的落地建议

### 第一优先级：开发辅助而非运行时智能

当前项目已经有 AIChatPanel，但业务核心仍处于开发阶段。建议先把 Agent 体系落在 Editor/文档/配置/扫描报告层，帮助新增功能、查错、补文档、管资源，再考虑运行时智能 NPC 或 AI 辅助玩法。

### 第二优先级：围绕工人任务和数据驱动建立模板

README 显示工人任务树、种植任务、Item 数据优化仍在进行中。最适合优先 Agent 化的是：

- Worker 任务模板生成。
- ItemDataSO/BuildItemDataSO/DropItemDataSO 生成与校验。
- Tile/Item/SO 名称绑定检查。
- 任务新增后的测试步骤生成。

### 第三优先级：建立资源和构建检查

项目同时使用 `Resources`、`StreamingAssets`、AssetBundle 和 Addressables 数据。建议 Resource Agent 先做只读检查：

- Prefab 是否在 AB 中。
- `PrefabConstant` 名称是否能对应到实际资源。
- SO 名称是否重复。
- Tile 资源是否缺图或缺 meta。
- 修改 Prefab 后是否需要重新打包。

### 第四优先级：控制存档和网络修改风险

`ArchiveManager`、`ISaveData`、`ISyncData`、Photon 地图同步都属于高风险区域。任何涉及这些路径的任务，应自动提高风险等级，要求给出兼容性说明、迁移策略和多人同步验证步骤。

