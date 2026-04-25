# Agent Candidate History Index

- 生成时间: 2026-04-26 04:09:59
- 工具菜单: `Tools/Agent/导出候选历史状态索引`
- 扫描模式: 只读扫描历史 Markdown，仅写入本报告
- 本次任务目录: `Assets/Agent/Reports/2026-04-26/efficiency_F010_candidate_history_index`
- 输出路径: `Assets/Agent/Reports/2026-04-26/efficiency_F010_candidate_history_index/candidate_history_index.md`

## 扫描范围

| 类型 | 数量 |
| --- | ---: |
| `feature_discovery.md` | 1 |
| `task_*.md` | 1 |
| `validation_*.md` | 1 |
| 候选状态记录 | 10 |

## 状态汇总

| 状态 | 候选数 |
| --- | ---: |
| [TODO] | 3 |
| [DONE] | 4 |
| [SKIPPED] | 2 |
| [BLOCKED] | 0 |
| [PARTIAL] | 0 |

## 候选索引

| 候选ID | 归并状态 | 功能名称 | 来源文件 | 去重依据 |
| --- | --- | --- | --- | --- |
| F001 | [DONE] | Item 资源引用完整性只读扫描器 | `Assets/Agent/Reports/feature_discovery.md` | \| [DONE] \| F001 \| Item 资源引用完整性只读扫描器 \| `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs` 已存在，覆盖 `Resources/SO`、`Resources/Tilemap/Item`、`Resources/Images/Item` \| 高 \| 低 \| 中 \| P0 \| ResourceAgent \| ResourceCheckSkill \| 已有实现；去重依据：脚本常量 `efficiency_F001_item_resource_integrity_validator` 与菜单 `Tools/Data/导出Item资源绑定报告`；历史任务卡/验证记录未发现。 \| |
| F002 | [DONE] | Agent 上下文只读扫描器 | `Assets/Agent/Reports/feature_discovery.md` | \| [DONE] \| F002 \| Agent 上下文只读扫描器 \| `Scripts/2D/Editor/AgentContextScanner.cs` 已存在，可导出 Agent 文件、历史任务、脚本信号、资源根概况 \| 高 \| 低 \| 中 \| P0 \| ToolAgent \| EditorToolSkill \| 已有实现；去重依据：脚本常量 `efficiency_F002_agent_context_scanner` 与菜单 `Tools/Agent/导出上下文扫描报告`；历史任务卡/验证记录未发现。 \| |
| F010 | [DONE] | 候选历史状态索引器 | `Assets/Agent/Reports/feature_discovery.md` | \| [DONE] \| F010 \| 候选历史状态索引器 \| `Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs` 已存在，可递归读取候选总表、任务卡和验证记录 \| 高 \| 低 \| 中 \| P0 \| ToolAgent \| DocumentSkill \| 已有实现；去重依据：脚本常量 `efficiency_F010_candidate_history_index` 与菜单 `Tools/Agent/导出候选历史状态索引`；历史任务卡/验证记录未发现。 \| |
| F011 | [DONE] | Validation F011 | `Assets/Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/validation_efficiency_F011.md`<br>`Assets/Agent/Reports/feature_discovery.md` | 验证记录最终状态: [DONE]<br>\| [DONE] \| F011 \| AssetBundle 与 StreamingAssets 只读清单报告器 \| `StreamingAssets`、`ResourcesLocal/Prefabs`、`AddressableAssetsData` 已存在，`OtherTool` 有打 AB 包入口但缺少构建前后只读清单 \| 高 \| 低 \| 中 \| P0 \| BuildAgent \| BuildFixSkill \| 已完成；任务目录：`Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report`；任务卡：`Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/task_efficiency_F011_AssetBundle_Inventory_Report.md`；验证记录：`Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/validation_efficiency_F011.md`；修改文件：`Scripts/2D/Editor/AssetBundleInventoryReporter.cs`, `Scripts/2D/Editor/AssetBundleInventoryReporter.cs.meta`, `Agent/Reports/feature_discovery.md`；验证结果：静态检查通过，确认只写报告、不触发打包或资源修改；Unity CLI/csc 不可用，未做 Unity 编译；剩余风险：需在 Unity Editor 中首次执行菜单确认编译与报告生成。 \| |
| F012 | [TODO] | 存档结构只读字段扫描报告器 | `Assets/Agent/Reports/feature_discovery.md` | \| [TODO] \| F012 \| 存档结构只读字段扫描报告器 \| `ArchiveManager` 与 `Scripts/2D/Data` 属高风险区域，后续存档改动需要字段基线 \| 高 \| 中 \| 中 \| P1 \| SaveNetworkAgent \| ErrorAnalyzeSkill \| 只读扫描可做，暂不修改存档结构；适合后续任务。 \| |
| F013 | [TODO] | Scene 关键入口只读索引报告器 | `Assets/Agent/Reports/feature_discovery.md` | \| [TODO] \| F013 \| Scene 关键入口只读索引报告器 \| `Scenes/Game.unity` 等场景存在，运行入口和 Manager/Panel/Tilemap 绑定缺少结构化索引 \| 中 \| 中 \| 中 \| P1 \| SceneAgent \| SceneAnalyzeSkill \| 只读索引可做，直接修复 Scene/Prefab 缺失引用需跳过或单独确认。 \| |
| F014 | [TODO] | WorkerTask 模板生成器 | `Assets/Agent/Reports/feature_discovery.md` | \| [TODO] \| F014 \| WorkerTask 模板生成器 \| 路线图建议补齐 WorkerTask 模板，`Scripts/2D/Character/Worker/Task` 存在扩展信号 \| 中 \| 低 \| 中 \| P1 \| AINPCAgent \| ScriptGenerateSkill \| 可生成模板文件但不得自动接入任务队列、UI、存档或 Photon；适合后续低侵入任务。 \| |
| F015 | [SKIPPED] | 自动修复 Prefab 缺失绑定 | `Assets/Agent/Reports/feature_discovery.md` | \| [SKIPPED] \| F015 \| 自动修复 Prefab 缺失绑定 \| Prefab/ResourcesLocal 资源链路复杂，直接修复会改业务资产 \| 高 \| 高 \| 高 \| P1 \| ResourceAgent \| ResourceCheckSkill \| 自动跳过：涉及 Prefab 直接修改，需要人工确认和 Unity 场景/预制体验证。 \| |
| F016 | [SKIPPED] | 自动重打 AssetBundle 并同步 StreamingAssets | `Assets/Agent/Reports/feature_discovery.md` | \| [SKIPPED] \| F016 \| 自动重打 AssetBundle 并同步 StreamingAssets \| `OtherTool` 已有打包入口，自动执行会改 `StreamingAssets` 打包产物 \| 高 \| 高 \| 高 \| P1 \| BuildAgent \| BuildFixSkill \| 自动跳过：涉及 AssetBundle/StreamingAssets 直接写入，属于高风险产物修改。 \| |

## Feature Discovery 文件

- `Assets/Agent/Reports/feature_discovery.md` (6619 bytes)

## 任务卡文件

- `Assets/Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/task_efficiency_F011_AssetBundle_Inventory_Report.md` (3924 bytes)

## 验证记录文件

- `Assets/Agent/Reports/2026-04-26/efficiency_F011_AssetBundle_Inventory_Report/validation_efficiency_F011.md` (1937 bytes)

## 使用建议

- 后续自动发现候选前，先查看本索引中的 `[DONE]` 候选，避免重复实现。
- `[SKIPPED]` 和 `[BLOCKED]` 候选可以继续保留，但再次选择前应先确认风险是否已经降低。
- 本工具不修改业务资源；如需修复场景、预制体、SO、存档、联机同步或打包产物，请单独生成任务卡。
