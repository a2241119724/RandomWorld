# Bug Fix Log — Unity 日志修复存档

> 每次通过日志分析并解决 bug 后，把思路追加到此文件。开始新任务前**先通读本文件**，命中历史记录时直接引用验证，避免重复排查。

## 2026-08-13 Gather 无邻居位置死循环刷屏（25k 次）

- **现象**：日志中 `Gather, 没有邻居位置!` 出现 25,189 次，89% 集中在开局 20:27–20:28 两分钟。同一 worker（如"葛杉"）在 30ms 内对同一目标 `(311,411)` 重试 5+ 次，workerPos 不动：`失败 → 自主决策[SelfGather] → 创建自我采集任务 → 又失败`。
- **根因**：`WorkerSeekState.OnEnter` 的"没有邻居位置"失败块只设置 `Task.LastFailedTime`，**未调用 `ASeek.RecordFail`**。而决策 `ScanForResources` 用 `ASeek.IsRecentFail` 过滤失败目标 → 过滤失效 → 失败后 `GiveUpTask` 释放 `GatherMap` 认领 → 决策立刻重新选中同一目标 → 无限循环。（对比：`OnUpdate` 里寻路失败有 `ASeek.RecordFail`。）
- **修复**：
  - `Scripts/2D/Character/Worker/State/WorkerSeekState.cs`：失败块加 `ASeek.RecordFail(this.targetMap)`（FailCacheTtl=30s）。
  - `Scripts/2D/Character/Worker/Task/WorkerGatherTask.cs`：`Init()` 邻居从 2 方向（`Neighbors[1],[3]`）扩为 4 正交方向（`Neighbors[0..3]`，对齐 `WorkerBuildTask`/`WorkerDemolishTask`），提高密集资源区成功率。
- **验证**：重跑开局，观察 20:27 段"没有邻居位置"应从 25k 级降至低位且目标分散。
- **教训**：**决策循环 bug 先查"失败后是否进入冷却/失败缓存"**。`RecordFail` 是寻路失败的标准通道，"没有邻居位置"失败应复用同一通道。

## 2026-08-13 睡眠任务无邻居死循环（早期修复，前提有误但有效）

- **现象**：`Sleep, 没有邻居位置!` 9,346 次 + `创建地面睡觉任务(无床)` 9,969 次刷屏。
- **根因（修正）**：早期分析曾误判"worker 坐标越出 200×200 地图"，实际地图为 **1000×1000**（日志第 18 行 `地图=1000×1000`），坐标从未越界。真正的死循环机制同 Gather：决策→睡眠→无邻居→放弃→重新决策睡眠。越界兜底（`EnsureValidPosition`）从未触发（0 次）即为此证。
- **修复（有效部分）**：`WorkerBrain` 新增 `SleepFailCooldownSeconds=10f`，睡眠失败后 10s 内改发 Wander —— 效果明显：`创建地面睡觉任务` 从 9,969 → 38 次。
- **教训**：**地图尺寸必须以日志/存档为准，不要凭代码默认值推断**（`TileMap.DefaultHeight=548` 与运行时 1000×1000 也不同）。越界修复（`TileMap.IsCanReach` 地图外 false、`GenCanReachPos` clamp）保留，作为防御性加固但非本次主因。
