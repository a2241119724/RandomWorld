# Agent Context Scan

- 生成时间: 2026-05-09 12:23:24
- 工具菜单: `工具/智能体/导出上下文扫描报告`
- 扫描模式: 只读
- 本次任务目录: `Assets/Agent/Reports/2026-05-09/efficiency_F002_agent_context_scanner`
- 输出路径: `Assets/Agent/Reports/2026-05-09/efficiency_F002_agent_context_scanner/agent_context_scan.md`

## Agent 基础文件

| 路径 | 状态 | 大小 |
| --- | --- | ---: |
| `Assets/Agent/README.md` | 存在 | 8556 |
| `Assets/Agent/Docs/ImplementationRoadmap.md` | 存在 | 5134 |
| `Assets/Agent/Docs/SkillCatalog.md` | 存在 | 7405 |
| `Assets/Agent/Config/agent_registry.json` | 存在 | 9318 |
| `Assets/Agent/Config/task_router.json` | 存在 | 7883 |
| `Assets/Agent/Templates/agent_task_card.md` | 存在 | 1362 |

## 历史任务卡

- `Assets/Agent/Reports/2026-04-26/feature_F001_CombatStats/task_feature_F001_CombatStats.md`
- `Assets/Agent/Reports/2026-04-27/feature_F002_WaveSystem/task_feature_F002_WaveSystem.md`
- `Assets/Agent/Reports/2026-04-27/feature_F003_DeathPenalty/task_feature_F003_DeathPenalty.md`
- `Assets/Agent/Reports/2026-04-28/feature_F004_SessionResult/task_feature_F004_SessionResult.md`
- `Assets/Agent/Reports/2026-04-28/feature_F006_ItemCollectionMilestone/task_feature_F006_ItemCollectionMilestone.md`
- `Assets/Agent/Reports/2026-04-28/feature_F008_InvincibilityFrame/task_feature_F008_InvincibilityFrame.md`
- `Assets/Agent/Reports/2026-04-30/feature_F009_ComboBonus/task_feature_F009_ComboBonus.md`
- `Assets/Agent/Reports/2026-04-30/feature_F010_WaveEventFeedback/task_feature_F010_WaveEventFeedback.md`
- `Assets/Agent/Reports/2026-04-30/feature_F011_SessionResultAutoTrigger/task_feature_F011_SessionResultAutoTrigger.md`
- `Assets/Agent/Reports/2026-05-01/feature_F005_WorkerEfficiency/task_feature_F005_WorkerEfficiency.md`
- `Assets/Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/task_ambitious_A001_Experience_Hub.md`
- `Assets/Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/task_feature_F012_WeatherGameplayEffect.md`

## Scripts/2D 模块概况

| 模块 | C# 文件 | TODO/临时信号 | 空方法信号 |
| --- | ---: | ---: | ---: |
| `Assets/Scripts/2D` | 9 | 0 | 0 |
| `Assets/Scripts/2D/Attributes` | 1 | 0 | 0 |
| `Assets/Scripts/2D/Character` | 57 | 6 | 0 |
| `Assets/Scripts/2D/Constant` | 5 | 0 | 0 |
| `Assets/Scripts/2D/Core` | 8 | 3 | 0 |
| `Assets/Scripts/2D/Data` | 11 | 0 | 0 |
| `Assets/Scripts/2D/Editor` | 19 | 9 | 0 |
| `Assets/Scripts/2D/Enum` | 1 | 0 | 0 |
| `Assets/Scripts/2D/Flag` | 1 | 0 | 0 |
| `Assets/Scripts/2D/Gameplay` | 11 | 0 | 0 |
| `Assets/Scripts/2D/Item` | 43 | 5 | 0 |
| `Assets/Scripts/2D/MVC` | 18 | 0 | 0 |
| `Assets/Scripts/2D/Manager` | 5 | 0 | 0 |
| `Assets/Scripts/2D/Map` | 8 | 2 | 0 |
| `Assets/Scripts/2D/SO` | 5 | 0 | 0 |
| `Assets/Scripts/2D/Serializable` | 1 | 0 | 0 |
| `Assets/Scripts/2D/Tool` | 7 | 0 | 0 |
| `Assets/Scripts/2D/UI` | 50 | 1 | 0 |

## 后续开发信号

- `Assets/Scripts/2D/Character/Enemy/AEnemy.cs:125` throw new System.NotImplementedException();
- `Assets/Scripts/2D/Character/Enemy/CommonEnemy/State/CommonEnemySeekState.cs:58` // TODO可以奔跑搜索，以后实现
- `Assets/Scripts/2D/Character/Worker/Task/AWorkerTask.cs:237` // TODO 仅执行一次
- `Assets/Scripts/2D/Character/Worker/Task/WorkerPlantTask.cs:53` // TODO 可以将种子放回
- `Assets/Scripts/2D/Character/Worker/WorkerTaskManager.cs:12` private readonly List<Dictionary<AWorkerTask, bool>> tasks; // 所有任务(list中越靠前优先级越大), TODO分离正在做的任务
- `Assets/Scripts/2D/Character/Worker/WorkerTaskManager.cs:13` private readonly List<WorkerHungryTask> hungryTasks; // 饥饿任务与pos挂钩，TODO与worker数量挂钩
- `Assets/Scripts/2D/Core/KDTree/KDTree.cs:261` return (new KDNode(best, -1), bestDistance); // 临时节点用于返回结果
- `Assets/Scripts/2D/Core/Seek/ASeek.cs:338` throw new System.NotImplementedException();
- `Assets/Scripts/2D/Core/Seek/AStar.cs:9` /// TODO 周围方块更新要重新寻路
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:29` "[TODO]",
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:34` "[TODO]",
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:180` string status = "[TODO]";
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:218` string status = "[TODO]";
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:274` return "[TODO]";
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:389` if (status != "[TODO]")
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:395` return "[TODO]";
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:402` return "[TODO]";
- `Assets/Scripts/2D/Editor/AgentCandidateHistoryIndexer.cs:434` return "[TODO]";
- `Assets/Scripts/2D/Item/Backpack/Equipment/Weapon/Gun/TraceBulletEffect.cs:52` // TODO Lerp
- `Assets/Scripts/2D/Item/Backpack/Food/Apple.cs:20` throw new NotImplementedException();
- `Assets/Scripts/2D/Item/FurnitureManager.cs:51` // TODO
- `Assets/Scripts/2D/Item/FurnitureManager.cs:59` // TODO
- `Assets/Scripts/2D/Item/InventoryManager.cs:211` /// TODO 没有预取
- `Assets/Scripts/2D/Map/BuildMap.cs:15` private Dictionary<int, ResourceInfo> resourceInfos; // TODO 需要的建筑材料
- `Assets/Scripts/2D/Map/TileMap.cs:255` Lock.IsCompleteTileMap = true; // TODO
- `Assets/Scripts/2D/UI/Panel/InventoryMenuPanel.cs:56` // TODO 删除Gameobject

## 资源概况

| 路径 | 状态 | 文件 | 子目录 | 缺失 .meta |
| --- | --- | ---: | ---: | ---: |
| `Assets/Resources/SO` | 存在 | 14 | 0 | 0 |
| `Assets/Resources/Tilemap` | 存在 | 76 | 11 | 0 |
| `Assets/Resources/Images` | 存在 | 67 | 29 | 0 |

## 高风险区域只读概况

| 路径 | 状态 | 文件 | 子目录 | 缺失 .meta |
| --- | --- | ---: | ---: | ---: |
| `Assets/Scenes` | 存在 | 7 | 1 | 0 |
| `Assets/StreamingAssets` | 存在 | 4 | 0 | 0 |
| `Assets/AddressableAssetsData` | 存在 | 17 | 5 | 0 |
| `Assets/ResourcesLocal` | 存在 | 26 | 11 | 0 |

## 后续建议

- 将本报告作为每次 Agent 自动发现前的上下文索引，优先查看 TODO 信号、资源缺口和历史任务卡。
- 高风险区域仅用于发现检查机会，不在本工具内做任何修复或写入。
- 如需修复资源、存档、Photon 或 AssetBundle 问题，单独生成任务卡后再执行。
