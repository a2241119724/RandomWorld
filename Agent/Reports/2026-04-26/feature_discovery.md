# Feature Discovery

## 扫描范围

- 文档: `README.md`, `Agent/README.md`, `Agent/Docs/ImplementationRoadmap.md`, `Agent/Docs/SkillCatalog.md`, `Agent/Templates/agent_task_card.md`
- 配置: `Agent/Config/agent_registry.json`, `Agent/Config/task_router.json`
- 历史任务卡: `Agent/Reports/2026-04-26/task_item_resource_integrity_validator.md`, `Agent/Reports/2026-04-26/task_agent_context_scanner.md`
- 代码: `Scripts/2D`, 重点查看 TODO、FIXME、临时实现、空方法、`NotImplementedException` 和 Editor 工具
- 资源: `Resources/SO`, `Resources/Tilemap`, `Resources/Images`
- 高风险只读区域: `Scenes`, `StreamingAssets`, `AddressableAssetsData`, `ResourcesLocal`, 存档代码和 Photon 同步代码

## 关键发现

- 路线图阶段 1 推荐优先落地只读扫描与报告工具，包括 `AgentContextScanner`、`ResourceValidator`、`SceneValidator` 和 `ReportExporter`。
- 今日报告目录已有 `task_item_resource_integrity_validator.md` 与 `resource_integrity_report.md`，说明 Item 资源检查器已成为基础设施，下一步更需要一个统一上下文索引。
- `Scripts/2D/Editor` 已有 Data/UI/Other/Builder 工具，继续新增 Editor 只读工具符合现有目录风格。
- `Scripts/2D` 中发现多处 TODO、临时实现和 `NotImplementedException`，但多数直接修复会触碰玩法、寻路、存档或 Photon，同步风险高于只读报告。
- `Resources/SO`、`Resources/Tilemap`、`Resources/Images` 有资源绑定检查价值，但本轮不直接修改资源。

## 候选功能

| ID | 功能候选 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| F001 | Item/SO/Tile/Image 资源绑定完整性检查器 | 路线图推荐首个任务；README 强调 ItemData 与 Tile 名称绑定；今日已有同名工具与报告 | High | Low | Low | 已落地/P0 | `tool` + `resource` + `item_data` | `editor_tool` + `resource_check` + `document` |
| F002 | Agent 上下文扫描器与模块索引报告 | 路线图阶段 1 推荐 `AgentContextScanner`；历史任务卡开始积累；后续自动发现需要统一入口 | High | Low | Low | P0 | `tool` + `debug` | `editor_tool` + `document` + `resource_check` |
| F003 | 任务卡自动生成器 | 路线图阶段 2 推荐读取 `task_router.json` 自动生成任务卡；当前仍依赖手写 | High | Low | Medium | P1 | `tool` + `project_director` | `editor_tool` + `script_generate` + `document` |
| F004 | 存档字段兼容只读扫描报告 | `task_router.json` 将存档结构列为高风险；`ArchiveManager` 已有多存档与旧档迁移逻辑 | High | Medium | Medium | P1 | `save_network` + `debug` | `error_analyze` + `document` + `test` |
| F005 | Photon 同步风险只读审计 | Photon/RPC 分布在网络、地图、角色和武器链路；联机问题排查成本高 | High | Medium | Medium | P1 | `save_network` + `debug` | `error_analyze` + `code_review` + `test` |
| F006 | Scene Missing Script/关键入口只读检查器 | 路线图阶段 1 推荐 `SceneValidator`；Game 场景依赖 Manager/Panel/Tilemap 绑定 | Medium | Medium | Medium | P2 | `scene` + `debug` | `scene_analyze` + `resource_check` |
| F007 | WorkerTask 模板生成器 | 路线图阶段 3 推荐；WorkerTaskManager 和任务类存在 TODO | Medium | Medium | Medium | P2 | `tool` + `ai_npc` | `editor_tool` + `script_generate` + `test` |
| F008 | 直接补齐缺失 Tile/Image/SO 资源 | 资源检查器已发现命名绑定缺口 | High | High | High | Blocked | `resource` + `item_data` | `resource_check` + `config_generate` |
| F009 | 直接修复 Worker/AI/Item TODO 或未实现异常 | `Scripts/2D` 中存在 TODO 与 `NotImplementedException` | Medium | High | Medium | Blocked | `ai_npc` + `gameplay` + `item_data` | `script_generate` + `test` |

## 推荐优先开发

1. F002: Agent 上下文扫描器与模块索引报告。低风险、高价值、边界清晰，可提升后续每次功能发现和任务卡生成效率。
2. F003: 任务卡自动生成器。价值高，但需要解析路由和模板填充，范围略大于 F002。
3. F004: 存档字段兼容只读扫描报告。适合保护高风险区域，但需要更细的存档结构理解。

## 本次自动选择

- 选择 F002: Agent 上下文扫描器与模块索引报告。
- 选择原因: F001 已有落地文件，F002 同为 P0，低风险、可在一个任务卡内完成，不直接修改业务资产或 Unity 资源引用，并能提升后续 Agent 流程效率。
- 落地路径: `Scripts/2D/Editor/AgentContextScanner.cs`，输出报告到 `Agent/Reports/<yyyy-MM-dd>/agent_context_scan.md`。

## 被跳过的高风险候选及原因

- 直接补齐缺失 Tile/Image/SO 资源: 需要修改 `Resources` 下 Unity 资产和 `.meta` 引用，可能影响运行时加载、Item 绑定和 AssetBundle 后续构建，本轮跳过。
- 直接修复 Worker/AI/Item TODO 或未实现异常: 涉及任务树、寻路、道具行为或玩法链路，可能牵连存档与 Photon，本轮跳过。
- 直接修改存档结构或 Photon 同步逻辑: 属于高风险区域，必须先做只读报告和兼容设计，本轮不直接修改。
- 重新打 AssetBundle 或修改 `StreamingAssets`: 会影响构建产物和运行时资源加载，本轮只记录为后续只读检查机会。
