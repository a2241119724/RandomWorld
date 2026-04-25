# Feature Discovery

- 生成日期: 2026-04-26
- 维护位置: `Agent/Reports/feature_discovery.md`
- 维护规则: 全局候选功能只在本文件统一维护；历史任务目录中的旧版 `feature_discovery.md` 仅兼容读取，不再作为写入目标。

## 扫描范围

- Agent 基础文件: `Agent/README.md`, `Agent/Docs/ImplementationRoadmap.md`, `Agent/Docs/SkillCatalog.md`, `Agent/Config/agent_registry.json`, `Agent/Config/task_router.json`, `Agent/Templates/agent_task_card.md`
- 全局候选报告: `Agent/Reports/feature_discovery.md`
- 历史记录: 递归扫描 `Agent/Reports/` 下所有 `feature_discovery.md`, `task_*.md`, `validation_*.md`
- Editor 工具: `Scripts/2D/Editor`
- 代码信号: `Scripts/2D` 中的 TODO/FIXME/HACK/NotImplementedException/临时实现/空方法信号
- 资源信号: `Resources/SO`, `Resources/Tilemap`, `Resources/Images`
- 高风险只读区域: `Scenes`, `StreamingAssets`, `ResourcesLocal/Prefabs`, `AddressableAssetsData`

## 全局候选功能列表

| 状态 | 候选ID | 功能名称 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|
| [DONE] | F001 | Item 资源引用完整性只读扫描器 | `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs` 已存在，覆盖 `Resources/SO`、`Resources/Tilemap/Item`、`Resources/Images/Item` | 高 | 低 | 中 | P0 | ResourceAgent | ResourceCheckSkill | 已有实现；去重依据：脚本常量 `efficiency_F001_item_resource_integrity_validator` 与菜单 `Tools/Data/导出Item资源绑定报告`；历史任务卡/验证记录未发现。 |
| [DONE] | F002 | Agent 上下文只读扫描器 | `Scripts/2D/Editor/AgentContextScanner.cs` 已存在，可导出 Agent 文件、历史任务、脚本信号、资源根概况 | 高 | 低 | 中 | P0 | ToolAgent | EditorToolSkill | 已有实现；去重依据：脚本常量 `efficiency_F002_agent_context_scanner` 与菜单 `Tools/Agent/导出上下文扫描报告`；历史任务卡/验证记录未发现。 |
| [DONE] | F010 | 候选历史状态索引器 | `Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs` 已存在，可递归读取候选总表、任务卡和验证记录 | 高 | 低 | 中 | P0 | ToolAgent | DocumentSkill | 已有实现；去重依据：脚本常量 `efficiency_F010_candidate_history_index` 与菜单 `Tools/Agent/导出候选历史状态索引`；历史任务卡/验证记录未发现。 |
| [DONE] | F011 | AssetBundle 与 StreamingAssets 只读清单报告器 | `StreamingAssets`、`ResourcesLocal/Prefabs`、`AddressableAssetsData` 已存在，`OtherTool` 有打 AB 包入口但缺少构建前后只读清单 | 高 | 低 | 中 | P0 | BuildAgent | BuildFixSkill | 已完成；任务目录：`Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report`；任务卡：`Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/task_efficiency_F011_AssetBundle_Inventory_Report.md`；验证记录：`Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/validation_efficiency_F011.md`；修改文件：`Scripts/2D/Editor/AssetBundleInventoryReporter.cs`, `Scripts/2D/Editor/AssetBundleInventoryReporter.cs.meta`, `Agent/Reports/feature_discovery.md`；验证结果：静态检查通过，确认只写报告、不触发打包或资源修改；Unity CLI/csc 不可用，未做 Unity 编译；剩余风险：需在 Unity Editor 中首次执行菜单确认编译与报告生成。 |
| [TODO] | F012 | 存档结构只读字段扫描报告器 | `ArchiveManager` 与 `Scripts/2D/Data` 属高风险区域，后续存档改动需要字段基线 | 高 | 中 | 中 | P1 | SaveNetworkAgent | ErrorAnalyzeSkill | 只读扫描可做，暂不修改存档结构；适合后续任务。 |
| [TODO] | F013 | Scene 关键入口只读索引报告器 | `Scenes/Game.unity` 等场景存在，运行入口和 Manager/Panel/Tilemap 绑定缺少结构化索引 | 中 | 中 | 中 | P1 | SceneAgent | SceneAnalyzeSkill | 只读索引可做，直接修复 Scene/Prefab 缺失引用需跳过或单独确认。 |
| [TODO] | F014 | WorkerTask 模板生成器 | 路线图建议补齐 WorkerTask 模板，`Scripts/2D/Character/Worker/Task` 存在扩展信号 | 中 | 低 | 中 | P1 | AINPCAgent | ScriptGenerateSkill | 可生成模板文件但不得自动接入任务队列、UI、存档或 Photon；适合后续低侵入任务。 |
| [SKIPPED] | F015 | 自动修复 Prefab 缺失绑定 | Prefab/ResourcesLocal 资源链路复杂，直接修复会改业务资产 | 高 | 高 | 高 | P1 | ResourceAgent | ResourceCheckSkill | 自动跳过：涉及 Prefab 直接修改，需要人工确认和 Unity 场景/预制体验证。 |
| [SKIPPED] | F016 | 自动重打 AssetBundle 并同步 StreamingAssets | `OtherTool` 已有打包入口，自动执行会改 `StreamingAssets` 打包产物 | 高 | 高 | 高 | P1 | BuildAgent | BuildFixSkill | 自动跳过：涉及 AssetBundle/StreamingAssets 直接写入，属于高风险产物修改。 |

## 推荐优先开发

1. F012 存档结构只读字段扫描报告器
2. F013 Scene 关键入口只读索引报告器
3. F014 WorkerTask 模板生成器

## 被跳过的高风险候选

- F015 自动修复 Prefab 缺失绑定：涉及 Prefab 直接修改，跳过。
- F016 自动重打 AssetBundle 并同步 StreamingAssets：涉及 StreamingAssets/AssetBundle 直接写入，跳过。

## 已完成候选记录

- F001: 已发现实现文件 `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs`；修改文件和验证记录未在历史任务目录中发现。
- F002: 已发现实现文件 `Scripts/2D/Editor/AgentContextScanner.cs`；修改文件和验证记录未在历史任务目录中发现。
- F010: 已发现实现文件 `Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs`；修改文件和验证记录未在历史任务目录中发现。
- F011: 已实现 `Scripts/2D/Editor/AssetBundleInventoryReporter.cs`；任务卡 `Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/task_efficiency_F011_AssetBundle_Inventory_Report.md`；验证记录 `Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/validation_efficiency_F011.md`。

## 历史已完成候选去重依据

- 递归扫描 `Agent/Reports/` 下 `task_*.md` 与 `validation_*.md`，本次未发现历史任务卡或验证记录。
- 兼容读取 `Agent/Reports/` 下旧版 `feature_discovery.md`，本次未发现旧版任务目录候选表。
- 额外以现有 Editor 脚本中的候选目录常量、菜单路径和功能名作为去重依据，避免重复实现 F001、F002、F010。
