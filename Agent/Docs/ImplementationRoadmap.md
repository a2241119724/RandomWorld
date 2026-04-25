# Agent 系统落地路线图

## 阶段 0：方案与配置沉淀

目标：建立项目级 Agent 方案、职责边界和配置样例。

已完成内容：

- `README.md`：Agent 目录说明和当前项目画像。
- `Docs/UnityAgentSystemArchitecture.md`：完整架构方案。
- `Docs/SkillCatalog.md`：Skill 能力清单。
- `Config/agent_registry.json`：Agent 与 Skill 注册样例。
- `Config/task_router.json`：任务路由样例。
- `Templates/agent_task_card.md`：任务卡模板。

验收标准：

- 新成员能通过 `Assets/Agent` 理解 Agent 系统的目标、边界和使用方式。
- 后续新增 Agent/Skill 时有可复用配置格式。

## 阶段 1：只读扫描与报告

目标：先做不改动业务代码的 Editor 工具，降低落地风险。

建议实现：

- `AgentDashboardWindow`：展示 Agent 列表、Skill 列表、任务卡入口。
- `AgentContextScanner`：扫描 `Scripts/2D`、`Resources`、`Scenes`，生成模块索引。
- `ResourceValidator`：检查 SO、Tile、Sprite、Prefab、AB 的命名和缺失。
- `SceneValidator`：检查 Missing Script、常见组件缺失和入口对象。
- `ReportExporter`：导出 Markdown/JSON 报告到 `Assets/Agent/Reports`。

优先扫描规则：

- `Resources/SO` 名称重复。
- `Resources/Tilemap/Item` 与 ItemData 命名关系。
- `StreamingAssets/Prefab` 是否存在。
- `Scripts/2D/Character/Worker/Task` 任务类型和 Builder 是否完整。
- `Scenes/Game.unity` 是否包含关键 Manager/Panel/Tilemap 对象。

验收标准：

- 不修改业务代码即可生成资源和模块报告。
- 报告能指出至少一类可执行风险，例如缺失 Prefab、重复 SO、缺 meta。

## 阶段 2：任务卡与路由面板

目标：让开发者输入需求后，系统能自动生成任务拆解和 Agent 路由建议。

建议实现：

- 读取 `Config/task_router.json`。
- 根据关键词、路径、类名、资源类型匹配子 Agent。
- 自动生成 `Templates/agent_task_card.md` 的实例。
- 支持风险等级标记：Scene、Prefab、SO、存档、Photon、AB。

示例：

```text
输入：新增 Worker 钓鱼任务
路由：ProjectDirectorAgent -> AI/NPC Agent + Map Agent + Item/Data Agent + UI Agent
Skill：ScriptGenerateSkill + ConfigGenerateSkill + ResourceCheckSkill + TestSkill
风险：存档兼容、任务优先级、Tile 判定、SO/Tile 名称绑定
```

验收标准：

- 常见需求能路由到正确 Agent。
- 任务卡中包含影响路径、验证步骤和回滚建议。

## 阶段 3：模板生成

目标：在人工确认后生成低风险、结构固定的代码和配置模板。

建议优先支持：

- `AWorkerTask` 派生任务模板。
- `AWorkerState` 或 Enemy State 模板。
- `ABasePanel<T>` 面板模板。
- Item/SO 配置模板。
- EditorWindow 工具模板。

必须包含：

- 命名规范。
- 所属目录。
- 依赖类。
- TODO 标记。
- 验证步骤。

验收标准：

- 生成的脚本能编译。
- 不覆盖现有文件。
- 不自动修改 Scene/Prefab。

## 阶段 4：资源与数据自动化

目标：减少 SO、Tile、Sprite、Prefab 命名和引用维护成本。

建议实现：

- SO 批量创建和字段校验。
- Tile/Item/SO 绑定关系检查。
- Prefab 和 AssetBundle 内容检查。
- 修改 Prefab 后提示重新打 AB。
- 存档字段变更报告。

验收标准：

- 新增物品时能生成数据清单和资源检查报告。
- AB 缺失能在进入 Play Mode 前发现。

## 阶段 5：调试与性能助手

目标：把 Debug Agent 和 Performance Agent 接入实际工作流。

建议实现：

- 粘贴控制台日志后自动归类：编译、运行时、资源、Photon、存档。
- Profiler 标记建议生成器。
- Worker 数量、Tilemap 大小、存档加载耗时的基线记录。
- 运行前检查 `GlobalInit`、Manager、Panel、Tilemap 的关键绑定。

验收标准：

- 常见 `NullReference`、`MissingReference`、资源缺失能给出定位路径。
- 性能报告能给出量化指标，而不是只给泛泛建议。

## 阶段 6：运行时辅助与 AIChatPanel 集成

目标：在稳定的开发辅助系统之上，谨慎接入运行时智能体验。

可选方向：

- AIChatPanel 显示调试建议或 NPC 对话。
- Runtime Agent 只读取白名单上下文，不直接写资源。
- NPC 对话、任务提示和教程系统可调用受限 Skill。
- 网络游戏中禁止把未验证的 Agent 结果直接用于同步状态。

验收标准：

- 运行时 Agent 不影响核心帧率。
- 联机状态下不会产生未同步或不可复现的关键游戏状态。
- 所有运行时输出都有本地兜底。

## 推荐首个可实施任务

建议从“只读资源检查工具”开始：

1. 扫描 `Resources/SO` 的 SO 名称和类型。
2. 扫描 `Resources/Tilemap/Item` 的 Tile 名称。
3. 扫描 `Resources/Images/Item` 的图标名称。
4. 输出 Item/SO/Tile/Image 绑定关系报告。
5. 标记缺失、重复、命名不一致和缺 meta。

这个任务收益高、风险低，能立刻服务当前 Item 数据优化和资源维护。

