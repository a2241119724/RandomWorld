# Unity Agent System

本目录用于沉淀 RandomWorld 项目的 Agent 架构方案、配置样例、任务模板和后续落地文档。它当前不是运行时插件，而是一套“用来辅助开发新功能、拆任务、查风险、生成代码/配置/验证步骤”的项目级工作流。

推荐用法：每次开发新功能时，先在 `Assets/Agent` 中完成任务理解、路由、任务卡和风险记录，再进入 `Scripts/2D`、`Resources`、`Scenes` 等业务目录修改代码或资源。

## 当前项目画像

RandomWorld 当前更接近一个 2D 随机世界/生存建造项目，已有以下工程基础：

- `Scripts/2D/Map`：Tilemap、资源层、建造层、采集层、物品层、可用格检测，并参与存档和 Photon 同步。
- `Scripts/2D/Character`：玩家、工人、敌人、状态机、工人任务、寻路与基础角色管理。
- `Scripts/2D/Item`：背包物品、建造物、房间、农田、家具、墙、门、掉落、实例工厂和各类管理器。
- `Scripts/2D/MVC` 与 `Scripts/2D/UI`：背包/建造 MVC、面板栈、HUD、交互提示、AI 聊天面板。
- `Scripts/2D/Data` 与 `Scripts/2D/Manager`：全局数据、存档、资源加载、日志、天气、协程管理。
- `Resources/SO`、`Resources/Tilemap`、`Resources/Images`：ScriptableObject 配置、Tile 资源和美术资源。
- `StreamingAssets` 与 `AddressableAssetsData`：已有 AssetBundle/Addressables 相关基础。
- `Scripts/2D/Editor`：已有数据工具、UI 工具、Builder 自动生成器等 Editor 扩展雏形。
- `NetworkConnect`：基于 Photon PUN 的联网、房间和地图同步入口。

## 文档索引

- [Agent 架构总方案](Docs/UnityAgentSystemArchitecture.md)：说明主 Agent、子 Agent、Skill、上下文和风险控制的完整设计。
- [Skill 能力清单](Docs/SkillCatalog.md)：说明代码生成、资源检查、场景分析、性能优化、构建修复等能力边界。
- [落地路线图](Docs/ImplementationRoadmap.md)：建议从只读扫描、任务卡、模板生成再逐步进入自动化工具。
- [Agent 注册配置示例](Config/agent_registry.json)：定义 Agent 职责、路径归属、触发词和默认 Skill。
- [任务路由配置示例](Config/task_router.json)：定义不同任务类型应该路由到哪些 Agent 和 Skill。
- [任务卡模板](Templates/agent_task_card.md)：每个功能开发前建议复制一份，记录需求、影响路径、风险、验证和回滚。

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

## 让 Agent 自动发现功能缺口并开发

这一流程适合在你没有明确指定“下一个功能是什么”时使用。Agent 先只读扫描项目现状，发现可新增、可补齐、可优化的功能候选，再由你确认优先级，最后把其中一个候选转成任务卡并进入开发。

### 推荐工作模式

| 阶段 | 目标 | 允许动作 | 产物 |
| --- | --- | --- | --- |
| 功能发现 | 找出项目缺口和机会点 | 只读扫描代码、资源、文档、配置 | `feature_discovery.md` |
| 候选排序 | 评估价值、风险和实现成本 | 生成优先级建议，不改业务代码 | `feature_backlog.md` |
| 任务定稿 | 选中一个功能并拆解实现路径 | 生成或更新任务卡 | `task_<feature>.md` |
| 功能开发 | 按任务卡实施 | 只改相关路径 | 代码、资源、配置或文档变更 |
| 验证记录 | 验证结果并沉淀经验 | 编译、Play Mode、资源、存档、联网检查 | 任务卡结果区、风险记录 |

### 功能发现来源

Agent 可以从这些信号里发现“可能需要新增的功能”：

- `Agent/Docs/ImplementationRoadmap.md` 中未落地的阶段目标。
- `README.md`、模块文档、任务卡里出现的后续建议。
- `Scripts/2D` 中的 `TODO`、`FIXME`、临时实现、空方法、重复模式。
- 已有系统中只完成了一半的链路，例如有数据但没有 UI、有 UI 但没有行为、有行为但没有存档或验证。
- 资源绑定缺口，例如 SO、Tile、Sprite、Prefab、AssetBundle 命名不一致或缺失。
- 高风险模块的保护性功能，例如存档兼容检查、Photon 同步检查、资源完整性检查。
- 常见玩法扩展点，例如 Worker 新任务、Item/SO 新数据、地图层规则、UI 面板、Editor 自动化工具。

### 功能发现报告模板

建议把发现结果保存到 `Assets/Agent/Reports/<日期>/feature_discovery.md`：

```markdown
# Feature Discovery

## 扫描范围
- 文档：
- 代码：
- 资源：
- 场景：
- 配置：

## 候选功能

| ID | 功能候选 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent |
| --- | --- | --- | --- | --- | --- | --- | --- |
| F001 |  |  | High/Medium/Low | High/Medium/Low | High/Medium/Low | P0/P1/P2/P3 |  |

## 推荐优先开发
1.
2.
3.

## 不建议立即开发
-

## 需要人工确认
-
```

### 优先级判断规则

候选功能按以下顺序排序：

1. 先做低风险、高收益、能提升后续开发效率的功能，例如只读资源检查器、任务卡生成器、WorkerTask 模板生成器。
2. 再做闭环清晰的玩法小功能，例如一个新的 Worker 任务、一个新的 Item/SO 数据、一个 UI 操作入口。
3. 谨慎处理 Scene、Prefab、SO、存档、Photon、AssetBundle 相关功能，默认提高风险等级。
4. 避免一次性做跨多个核心系统的大功能。需要跨系统时，先拆成多个小任务卡。
5. 如果候选功能需要先补资源、补场景绑定或确认设计规则，就标记为 `Blocked`，不要直接开发。

推荐评分方式：

```text
P0 = 高价值 + 低/中风险 + 1 个任务卡可完成
P1 = 高价值 + 高风险，或中价值 + 低风险
P2 = 需要设计确认、资源确认或依赖其他任务
P3 = 暂时只有想法，缺少明确验收方式
```

### 操作步骤

1. 让 Agent 做一次只读功能发现。
2. 检查 `feature_discovery.md`，从 `P0` 或 `P1` 中选一个候选功能。
3. 让 Agent 把该候选转成任务卡，写清影响路径、验证步骤和回滚方案。
4. 确认任务卡后，再允许 Agent 修改代码或资源。
5. 完成后让 Agent 更新任务卡结果区，并把新的项目规则写回 README 或后续 Memory 文件。

### 可复制：自动发现功能的提示词

```text
请基于 ./Agent 中的 Agent 体系，帮我自动发现 RandomWorld 当前最值得新增或补齐的功能。

执行要求：
1. 先读取 Agent/README.md、Agent/Docs/ImplementationRoadmap.md、Agent/Docs/SkillCatalog.md、Agent/Config/agent_registry.json、Agent/Config/task_router.json。
2. 只读扫描相关项目上下文，不要修改业务代码、Scene、Prefab、SO 或资源。
3. 从文档缺口、TODO/FIXME、重复模式、资源绑定缺口、半完成链路、测试缺口中提取候选功能。
4. 为每个候选功能标记来源信号、价值、风险、成本、优先级、推荐 Agent 和推荐 Skill。
5. 把结果写入 Agent/Reports/<今天日期>/feature_discovery.md。
6. 最后给出最推荐开发的 1-3 个功能，但不要直接开发。
```

### 可复制：从候选功能进入开发的提示词

```text
请基于 Agent/Reports/<日期>/feature_discovery.md，选择候选功能 <功能 ID 或名称> 进入开发。

执行要求：
1. 读取 Agent/README.md、Agent/Config/agent_registry.json、Agent/Config/task_router.json 和候选功能来源上下文。
2. 生成任务卡 Agent/Reports/<日期>/task_<功能名>.md。
3. 任务卡必须包含：原始候选、任务分类、负责 Agent、需要的 Skill、影响路径、不应触碰路径、风险等级、执行步骤、验证步骤、回滚方案。
4. 如果涉及 Scene、Prefab、ScriptableObject、StreamingAssets、存档或 Photon，先停止在任务卡阶段，等待人工确认。
5. 如果风险为 Low 或 Medium，且任务卡已明确，再开始实现。
6. 实现时只修改和该任务直接相关的文件。
7. 完成后运行可行的验证，并把修改文件、验证结果、剩余风险写回任务卡。
```

### 推荐首批自动发现主题

| 主题 | 为什么适合先做 | 推荐产物 |
| --- | --- | --- |
| 资源完整性检查器 | 只读、风险低，能发现 SO/Tile/Sprite/Prefab/AB 缺口 | Editor 工具 + 报告 |
| WorkerTask 模板生成器 | 能加速后续玩法扩展，边界相对清晰 | Editor 工具 + 脚本模板 |
| Item/SO/Tile 绑定检查 | 当前资源和数据驱动链路较多，容易出现命名不一致 | 校验报告 |
| 存档字段兼容扫描 | 存档属于高风险区域，先做扫描比直接改结构更安全 | 兼容性报告 |
| Photon 同步风险检查 | 联机同步问题排查成本高，适合先做只读审计 | 同步风险清单 |

### 安全边界

- 自动发现阶段只能读文件和生成报告，不能改业务代码。
- 没有任务卡时，不允许修改 `Scripts/2D`、`Resources`、`Scenes`、`StreamingAssets`。
- 涉及 Scene、Prefab、SO、存档、Photon、AssetBundle 的候选功能必须人工确认。
- 一次只实现一个候选功能，不把多个 P0/P1 合并成一个大改。
- 开发完成后必须记录验证结果；如果无法验证，要明确写出未验证原因。

## 可复制给开发 Agent 的输入模板

开发新功能时，可以直接把下面内容复制给 Codex 或其他 Agent：

```text
请基于 ./Agent 中的 Agent 体系开发新功能。

需求：
<粘贴功能需求>

请按以下步骤执行：
1. 读取 Agent/README.md、Config/agent_registry.json、Config/task_router.json。
2. 判断任务类型、负责 Agent、需要的 Skill 和风险等级。
3. 先给出或更新任务卡内容，列出影响路径、验证步骤和回滚方案。
4. 只修改和本任务有关的代码、资源或文档。
5. 若涉及 Scene、Prefab、ScriptableObject、StreamingAssets、存档或 Photon，必须说明风险和验证方式。
6. 完成后汇总修改文件、验证结果和剩余风险。
```

## 示例：新增 Worker 任务

以“新增 Worker 钓鱼任务”为例，建议任务拆分如下：

| 子任务     | Agent              | Skill                              | 产物                              |
| ------- | ------------------ | ---------------------------------- | ------------------------------- |
| 任务行为设计  | `ai_npc`           | `script_generate`                  | `WorkerFishingTask` 草案、任务状态切换说明 |
| 水域和站位判断 | `map`              | `scene_analyze`、`test`             | 水边可执行规则、Tile 判断验证               |
| 鱼类道具数据  | `item_data`        | `config_generate`、`resource_check` | Fish ItemDataSO 字段、图标和资源命名清单    |
| 任务按钮或提示 | `ui`               | `script_generate`、`test`           | UI 按钮绑定说明、Panel 验证步骤            |
| 验收与回滚   | `project_director` | `test`、`document`                  | 任务卡结果、风险和回滚路径                   |

最低验证清单：

1. 新游戏中 Worker 能接收钓鱼任务。
2. 工人能寻路到水边，不会站到不可用格。
3. 任务完成后背包或掉落中出现 Fish。
4. 保存后读档，Fish 和 Worker 状态不丢失。
5. 联机房间中任务结果不产生不同步。
6. 不影响已有采集、建造、种植任务。

## 示例：新增 Item/SO 数据

推荐流程：

1. 用 `item_data` 路由任务。
2. 调用 `config_generate` 生成字段、默认值和校验规则。
3. 调用 `resource_check` 检查 `Resources/SO`、`Resources/Images/Item`、`Resources/Tilemap/Item`。
4. 确认 ItemData 与 Tile 的名称绑定关系。
5. 如果会进入背包、地图掉落或建造系统，再补 UI 和 Map 验证。

必须记录：

- SO 路径和资源名。
- 图标路径。
- Tile 路径。
- 是否需要 Prefab。
- 是否需要 AB 更新。
- 是否影响旧存档。

## 示例：新增 Editor 工具

推荐从只读工具开始，不直接修改资源。

首个适合实现的工具是资源检查器：

1. 扫描 `Resources/SO` 的 SO 名称和类型。
2. 扫描 `Resources/Tilemap/Item` 的 Tile 名称。
3. 扫描 `Resources/Images/Item` 的图标名称。
4. 输出 Item/SO/Tile/Image 绑定关系报告到 `Assets/Agent/Reports`。
5. 标记缺失、重复、命名不一致和缺 `.meta` 的资源。

这个任务风险低，能立刻服务道具数据、Tile 绑定和资源维护。

## 落地原则

- Agent 只负责任务决策和编排，Skill 只负责可复用能力，避免一个模块同时承担调度和执行。
- 初期优先用于开发辅助、Editor 工具、文档生成、代码审查和资源检查，不直接接管核心运行时逻辑。
- 对现有业务代码保持低侵入：优先通过 `Assets/Agent/Editor`、配置文件、扫描器和报告目录接入。
- 修改 `Resources`、`StreamingAssets`、Prefab、Scene 或 ScriptableObject 前必须生成任务卡，记录风险和回滚路径。
- 所有自动生成结果都要能追溯到任务、Agent、Skill、输入上下文和验证步骤。
- 所有 Unity 资源修改必须保留 `.meta`。
- 修改 Prefab 后需要重新打 AssetBundle。
- 道具数据 `ItemData` 与地图瓦片 `Tile` 存在名称关联绑定。
- 存档和 Photon 同步属于高风险区域，必须保留兼容性说明和验证步骤。
