# 候选修复：WorkerBountyTask.Execute 每 tick 无条件 SetTask(BountyRestore) 刷屏

> 状态：**已修复**（2026-09-04，思路存档见 bug-fixes.md 当日条目，本文保留作排查过程记录）。

## 现象（2026-09-03 局 game.log 实锤）

- 孔峰瑜/范学各 `SetTask type=Bounty source=BountyRestore` **~2400 次/人**（悬赏运行期 2 次/秒，16ms 内 5 连发），00:13-00:27 风暴后随悬赏结束自愈。
- Debug 级只进 game.log 不刷 Console，功能因 `SetTask` 对 BountyRestore 的「不重启不打断」特判（AWorker.cs:1091）而侥幸正确——纯日志刷屏 + 每 tick 无效赋值浪费。

## 根因

`WorkerBountyTask.Execute`（WorkerBountyTask.cs:154）：注释意图是「innerTask.Finish 清除了 Task 后恢复悬赏本体」，实现却是 `if (workerData != null)` **每 tick 无条件** SetTask——innerTask 未完成时 Task 本来就是 this，恢复是冗余调用。

## 修复（一行条件）

```csharp
if (workerData != null && workerData.Task == null)
{
    worker.SetTask(this, WorkerTaskSource.BountyRestore);
}
```

语义保留：innerTask.Finish 清空 Task → 下一 Execute 发现 null → 恢复本体；其余帧零开销。修复后按惯例追加 bug-fixes.md 存档。
