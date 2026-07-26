# Unity Agent System

本目录用于沉淀 RandomWorld 项目的 Agent 架构方案、配置样例、任务模板和后续落地文档。它当前不是运行时插件，而是一套“用来辅助开发新功能、拆任务、查风险、生成代码/配置/验证步骤”的项目级工作流。

推荐用法：每次开发新功能时，先在 `Assets/Agent` 中完成任务理解、路由、任务卡和风险记录，再进入 `Scripts/2D`、`Resources`、`Scenes` 等业务目录修改代码或资源。

## 当前项目画像

RandomWorld 当前更接近一个 2D 随机世界/生存建造项目，已完成一轮架构分层改造，工程基础如下：

- `Scripts/2D/Domain`：纯规则与服务层（Character、Common、Dialogue、Gameplay、Inventory、Player、Wave、Worker），不含 UnityEngine 引用。
- `Scripts/2D/UnityAdapter`：Unity 特定适配层（Input、Time、Map、Vector、Logger、EnemySpawn 等适配器），桥接 Domain 和 Unity API。
- `Scripts/2D/Gameplay`：玩法管理器（波次、成就、技能、结算、连击、死亡惩罚、浮动文字、天气效果、工人状态等）。
- `Scripts/2D/Character`：玩家、工人、敌人、状态机、工人任务、寻路与基础角色管理（含接口抽象 ICharacterCreator 等）。
- `Scripts/2D/Item`：背包物品、建造物、房间、农田、家具、墙、门、掉落、实例工厂和各类管理器。
- `Scripts/2D/Map`：Tilemap、资源层、建造层、采集层、物品层、可用格检测，并参与存档和 Photon 同步。
- `Scripts/2D/MVC` 与 `Scripts/2D/UI`：背包/建造 MVC、面板栈、HUD、交互提示、AI 聊天面板。
- `Scripts/2D/Network`：Photon 网络适配层（INetworkView、NetworkViewAdapters、SyncSenderAdapters）。
- `Scripts/2D/Data` 与 `Scripts/2D/Manager`：全局数据、存档、资源加载、日志、天气、协程管理。
- `Scripts/2D/Enum` 与 `Scripts/2D/Constant`：公共枚举（20 个）和公共常量（18 个常量文件）。
- `Scripts/2D/Tool`：20 个工具脚本（AchievementTool、DataTool、HudFactory、SkillTool 等）。
- `Scripts/2D/Core`：核心工具（KDTree、A* 寻路、ServiceLocator、Singleton）。
- `Scripts/2D/AI/Dialogue`：AI 对话系统（LLM 客户端、Prompt 组装、RAG 知识检索、Memory 管理、UI 面板）。
- `Scripts/2D/Editor`：Editor 工具、Builder 自动生成器，以及 `Tests/Domain` 和 `Tests/Tool` 单元测试。
- `Resources/SO`、`Resources/Tilemap`、`Resources/Images`：ScriptableObject 配置、Tile 资源和美术资源。
- `StreamingAssets` 与 `AddressableAssetsData`：已有 AssetBundle/Addressables 相关基础。

## 文档索引

- [Agent 架构总方案](Docs/UnityAgentSystemArchitecture.md)：说明主 Agent、子 Agent、Skill、上下文和风险控制的完整设计。
- [Skill 能力清单](Docs/SkillCatalog.md)：说明代码生成、资源检查、场景分析、性能优化、构建修复等能力边界。
- [落地路线图](Docs/ImplementationRoadmap.md)：建议从只读扫描、任务卡、模板生成再逐步进入自动化工具。
- [Agent 注册配置示例](Config/agent_registry.json)：定义 Agent 职责、路径归属、触发词和默认 Skill。
- [任务路由配置示例](Config/task_router.json)：定义不同任务类型应该路由到哪些 Agent 和 Skill。
- [任务卡模板](Templates/agent_task_card.md)：每个功能开发前建议复制一份，记录需求、影响路径、风险、验证和回滚。
- [Prompt — 游戏业务功能](Prompt_Feature.md)：自动发现并开发游戏业务新功能（Feature 级别）。
- [Prompt — 效率工具](Prompt_Efficiency.md)：自动发现并开发效率/工具类功能（Efficiency 级别）。
- [Prompt — 体验升级](Prompt_Ambitious.md)：自动发现并开发大型游戏体验升级功能（Ambitious 级别）。
- [Prompt — UI 场景优化与视觉精炼](Prompt_UI.md)：对 `.unity` 场景和 `*.prefab` 预制体进行 YAML 级布局优化与视觉美学精炼，涵盖场景文件编辑、视觉审计、锚点/间距/颜色/字号体系化调整。

## 用 Agent 开发新功能

### 1. 写清需求

先把需求写成可执行输入，至少包含目标、触发方式、涉及对象、期望结果和限制条件。

推荐格式：

```text
功能名称：
目标：
触发方式：
涉及系统：
预期表现：
需要新增的资源或配置：
不能影响的旧逻辑：
验收方式：
```

示例：

```text
功能名称：新增 Worker 钓鱼任务
目标：工人可在水边执行钓鱼任务，并产出鱼类食物
触发方式：通过 Worker 任务面板下发
涉及系统：WorkerTask、寻路、地图水域判断、ItemDataSO、背包、UI
预期表现：工人走到水边，等待一段时间后获得 Fish 食物
需要新增的资源或配置：Fish 的 ItemDataSO、图标、可能的 Tile/掉落配置
不能影响的旧逻辑：已有采集、种植、建造任务
验收方式：离线新档、读档后、多人房间中分别验证任务执行
```

### 2. 查任务路由

打开 `Config/task_router.json`，按关键词、路径和任务类型匹配路由。常见功能可参考下面的速查表。

| 功能类型                  | 优先 Agent                        | 常用 Skill                                             | 重点路径                                                      |
| --------------------- | ------------------------------- | ---------------------------------------------------- | --------------------------------------------------------- |
| 工人任务、NPC、敌人 AI        | `ai_npc`、`gameplay`             | `script_generate`、`test`、`performance_optimize`      | `Scripts/2D/Character`、`Scripts/2D/Core/Seek`             |
| 道具、背包、SO 数据           | `item_data`、`resource`          | `config_generate`、`resource_check`、`script_generate` | `Scripts/2D/Item`、`Scripts/2D/SO`、`Resources/SO`          |
| Tilemap、资源层、建造层       | `map`、`resource`、`save_network` | `scene_analyze`、`resource_check`、`test`              | `Scripts/2D/Map`、`Resources/Tilemap`                      |
| UI 面板、HUD、按钮          | `ui`、`gameplay`                 | `script_generate`、`scene_analyze`、`test`             | `Scripts/2D/UI`、`Scripts/2D/MVC`                          |
| UI 视觉优化与美学精炼       | `ui`、`scene`                   | `ui_refine`、`scene_analyze`、`resource_check`          | `Scenes/Game.unity`、`ResourcesLocal/Prefabs`              |
| 报错、空引用、资源缺失           | `debug`、`resource`、`scene`      | `error_analyze`、`code_review`、`resource_check`       | `Scripts/2D`、`Resources`、`Scenes`                         |
| 存档、Photon、同步          | `save_network`、`debug`          | `error_analyze`、`refactor`、`test`                    | `Scripts/2D/Data`、`ArchiveManager.cs`、`NetworkConnect.cs` |
| 打包、AB、StreamingAssets | `build`、`resource`              | `build_fix`、`resource_check`                         | `Build`、`StreamingAssets`、`AddressableAssetsData`         |
| Editor 自动化工具          | `tool`、`resource`               | `editor_tool`、`script_generate`、`document`           | `Scripts/2D/Editor`、`Agent`                               |

如果无法明确分类，先走 `project_director -> debug + tool`，只做只读分析和任务卡，不直接改业务代码。

### 3. 生成任务卡

复制任务卡模板，放到 `Assets/Agent/Reports/<日期>/` 下。目录不存在时先创建。

```powershell
New-Item -ItemType Directory -Force Agent/Reports/2026-04-26
Copy-Item Agent/Templates/agent_task_card.md Agent/Reports/2026-04-26/task_worker_fishing.md
```

任务卡中至少填这些内容：

- 用户原始需求。
- 任务分类和目标模块。
- 主要影响路径和不应触碰的路径。
- 子 Agent 分工。
- Skill 调用计划。
- 是否涉及 Scene、Prefab、ScriptableObject、AssetBundle、存档、Photon。
- 执行步骤、验证步骤和回滚方案。

### 4. 收集最小上下文

开发前只收集和本任务有关的代码、资源和场景信息，不做全项目大改。

建议检查顺序：

1. 查 `Config/agent_registry.json`，确认目标模块归哪个 Agent。
2. 查 `Docs/SkillCatalog.md`，确认需要哪些 Skill。
3. 查相关业务路径，例如 `Scripts/2D/Character/Worker`、`Scripts/2D/Item`、`Resources/SO`。
4. 查资源绑定规则，例如 ItemData 与 Tile 名称绑定、Prefab 修改后需要重新打 AB。
5. 如果涉及 Scene、Prefab、SO、存档或 Photon，把风险等级提高到 `High`。

### 5. 生成实现内容

按照任务类型选择产物，不要所有内容一次性生成。

脚本类功能：

- 先生成脚本草案和依赖清单。
- 新脚本放在对应业务目录，不放在 `Agent` 下。
- 保持命名、继承关系和现有 Builder/Manager 风格一致。
- 生成后补充验证步骤，而不是只提交代码。

资源和配置类功能：

- 先生成字段说明和命名规则。
- 新增或修改 `Resources/SO`、`Resources/Tilemap`、`Resources/Images` 前，必须记录 `.meta` 和引用关系。
- 修改 Prefab 后需要重新打 AssetBundle，并在任务卡中写明。

UI 类功能：

- 先确认面板基类、PanelController、按钮绑定方式和现有 UI 目录。
- 需要在 Unity Inspector 中绑定对象时，在任务卡中写明绑定路径。
- Button 添加点击函数时，先将脚本挂到物体上，再在按钮事件中引用该物体。

存档和网络类功能：

- 修改存档字段时写旧档兼容策略。
- 修改 Photon/RPC/同步数据时写离线和联机两套验证步骤。
- 大量数据不要走 Photon RPC buffer。

### 6. 验证与记录

每个任务完成后，至少做以下验证：

- Unity 编译无错误。
- Play Mode 能走通核心流程。
- 涉及资源时检查 `.meta`、SO、Tile、Sprite、Prefab 引用。
- 涉及存档时验证新档、旧档、保存后读档。
- 涉及 Photon 时验证离线、创建房间、加入房间、同步结果。
- 涉及 AB/StreamingAssets 时重新打包并验证构建后运行。

完成后把结果写回任务卡：

- 已完成内容。
- 修改的路径。
- 未完成项。
- 剩余风险。
- 后续建议。

