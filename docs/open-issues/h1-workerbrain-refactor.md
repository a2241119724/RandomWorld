# H1：WorkerBrain 巨石文件拆分方案

> 状态：**阶段 1-3 已完成**（2026-09-03，3446 → 2255 行）。阶段 4 暂缓——扫描组
> 依赖 `ScanRadius` 等实例配置，拆出需改方法签名，不再是纯机械搬家，留待下次
> 决策热路径需要动刀时顺带做。完成后本文档按 §5 约定归档（要点合并 spec.md）。

## 1. 问题定性

`Scripts/2D/AI/Worker/WorkerBrain.cs` 3446 行，承载 7 块互异职责，任何修改都要在巨石里定位，
review diff 噪声大，职责边界模糊（决策编排 / 房间编排 / 环境扫描互相缠绕）。

**定性修正**：早前把它记为「PreReserveAllRoomPositions 绕过 ABuildItem 管线」是**误判**——
该路径走 `BuildMap.ReserveBuildPosition`（BuildMap.cs:149，注释明确"用于 Worker 自主建造场景"）
+ `RegisterCollisionTile`，两者都是管线正规入口。房间墙/门/床/仓库的成套预留本就必须两阶段
（先查全再注册）+ 自管任务创建，与单建筑 AddBuildTask 语义不同，**不适用三同约定**。
因此 H1 是纯可维护性重构：**机械搬家、零行为变化**，不是管线收拢。

## 2. 现状职责块（行号为拆分前快照）

| 块 | 行区间 | 约行数 | 内容 |
|---|---|---|---|
| A 决策契约 | :20-277 | 260 | `WorkerDecisionType` 枚举、`Decision` 类 + Make/MakeGather/MakeBuild/MakePlant/MakeWithdraw 工厂、ResourceCandidate/BuildCandidate/PlantCandidate 私有结构 |
| B 决策主循环 | :278-797 | 520 | `Decide` / `ModelDecide` / `DecideBootstrap` |
| C 赏金·目标·扫描 | :798-1551 | 750 | RefreshGoal、TryMakeGoalDrivenBounty、TryMakeSelfCarry/PickUp、概率×3、ScanForResources/Specific/RoomArea/Food/DiggableTerrain |
| D 房间布局纯函数 | :1552-1897 | 350 | `RoomLayout`、GenerateRoomLayout、GetDoorPosition、GenerateRandomRoomParams、IsDoorEntryBlockedByFurniture、PrintRoomLayout、GetRoomLayout |
| E 建家决策编排 | :1898-2467 | 570 | TryMakeSelfBuildDecision（367 行）、CompleteStorageDirectly、GetBuildMaterialNeeds、FindFreeBuildPosition、RelocateHomeSite |
| F 选址·预留·清理 | :2468-3080 | 610 | PreReserveAllRoomPositions、ClearAbandoned×3、CanFitRoom、IsRoomOuterRingClear/IsObstructingNonGatherable/HasReachableNeighbor/IsRoomAreaBlockedInBuildMap/IsPositionInsideOtherWorkerRoom/IsHomeSiteClaimedByOther、TryPickHomeSite、TryInheritAbandonedHome |
| G 种植·存取·杂项 | :3049-3446 | 400 | TryMakeSelfPlant/Store/WithdrawForBuild/WithdrawForPlant、ScanForBuildPositions/PlantPositions、WorkerHasSeeds/Food、GetDecisionLabel |

拆分后 WorkerBrain 保留 B/E/G 主体（决策编排是其本职），预计 ~1900 行；
A/D/F/C 各自成文件后单文件均 <800 行。

## 3. 目标结构（均在 `LAB2D.AI.Worker` 命名空间、同目录）

| 新文件 | 内容 | 来源块 | 阶段 |
|---|---|---|---|
| `WorkerDecision.cs` | WorkerDecisionType、Decision（候选三结构暂留 Brain，随阶段 4 搬） | A | ✅ 1 |
| `WorkerHomeLayout.cs` | 全部布局纯函数 + RoomLayout 顶层类（方法名保留 `GetRoomLayout` 全名，最小 diff） | D | ✅ 2 |
| `WorkerHomeSiteService.cs` | 选址/预留/清理组（F 块 + E 块的 FindFreeBuildPosition/RelocateHomeSite 选址家族，共 713 行；static class，组内互调去 this） | E 尾 + F | ✅ 3 |
| `WorkerEnvironmentScanner.cs` | 5 个扫描 + ResourceProducesAnyMaterial + HasWalkableNeighborForDig + 候选三结构（升 internal）；依赖 ScanRadius 等实例配置，需改签名 | C 的扫描半区 | ⏸ 暂缓 |

## 4. 外部 API 影响（全库已 grep 确认，无其他消费方）

- `WorkerBrain.Decision` → 顶层 `Decision`：WorkerDecisionService（11 处）、WorkerModelDebugWindow（1 处）机械替换。
- `WorkerBrain.GetRoomLayout(wd)` → `WorkerHomeLayout.GetRoomLayout(wd)`：WorkerStorageTask（4）、WorkerBuildTask（2）、WorkerTaskManager（1）。
- `WorkerBrain.ClearDeadWorkerRoomTiles(wd)` → `WorkerHomeSiteService.ClearDeadWorkerRoomTiles(wd)`：WorkerDeadState（1）。
- `brain.TryPickHomeSite(...)`（WorkerDecisionService:116）→ `WorkerHomeSiteService.TryPickHomeSite(...)`。
- `WorkerBrain.ScanRadius` / `RefreshGoal`：仅注释提及，无代码引用。
- 存档零影响（只搬代码不动 WorkerData 字段）。

## 5. 风险与不变量

- **零行为变化是红线**：只做「剪切-粘贴-改访问前缀」，不顺手重命名变量/改逻辑/调日志。
- **热路径性能**：2026-09 决策链零分配优化刚落地（PosMap 预过滤、scratch 键复用、ref 就地取）。
  搬家时 scratch/缓存字段必须随其使用者整体迁移，不得复制成两份（会静默失效去重）。
- **两阶段预留语义**：PreReserveAllRoomPositions 的「先查全再注册 + 失败回滚 ClearAbandonedBuildTilesCore」
  是防部分注册残留的关键设计，搬家后保持原顺序原样。
- 每阶段：`python C:/Users/LAB/.claude/plans/build_bee.py`（MAIN_NEW 追加新文件）→ Unity 聚焦生成 .meta → 提交。
- 验证面：编译零错误 + 现有 Editor 单测通过 + 开一局观察 Worker 建家流程（选址→预注册→清资源→墙→门→床→仓库）不回归。
