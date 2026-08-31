namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.AI.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker寻找状态（薄壳）— 任务接取（玩家悬赏→全局任务→自主决策）已下沉至
    /// WorkerDecisionService（决策层）；本状态只负责：有任务时选任务邻居格寻路
    /// （含失败四件套）、无任务时调用决策层并按结果寻路、紧急检测、寻路时序闸门与 UI 文案。
    /// </summary>
    public class WorkerSeekState : AWorkerState
    {
        private readonly StringBuilder builder = new (128); // 减少GC
        private readonly WorkerDecisionService decision; // 决策服务（每 Worker 一个，持有决策簿记）
        private Vector3Int targetMap;

        public WorkerSeekState(AWorker character)
            : base(character)
        {
            this.decision = new WorkerDecisionService(character);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            // 位置越界兜底：worker 卡在地图外时寻路对越界起点直接失败，
            // 需要先传回地图内才能恢复正常行为。
            this.EnsureValidPosition(workerData);

            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            if (workerData.Task != null)
            {
                // 有任务时隐藏内心独白
                this.Character.HideDialogText();
                // 有任务 → 寻路到任务位置
                this.targetMap = Vector3IntLAB.ToVector3Int(workerData.Task.TargetMap);
                float minDistance = 99999.0f;
                Vector3Int closedPos = default;
                foreach (Vector3IntLAB pos in workerData.Task.AvailableNeighborPos)
                {
                    // 由于是斜对称
                    Vector3Int temp = new (this.targetMap.x + pos.Y, this.targetMap.y + pos.X, 0);
                    if (ASeek.IsCanReach(temp))
                    {
                        Vector3 worldPos = AWorkerTask.TileMapPositionProvider(temp);
                        float dx = worldPos.x - this.Character.transform.position.x;
                        float dy = worldPos.y - this.Character.transform.position.y;
                        float distance = (dx * dx) + (dy * dy);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closedPos = temp;
                        }
                    }
                }

                if (closedPos == default)
                {
                    AWorkerTask.LogProvider($"{workerData.Task.TaskType}, 没有邻居位置! workerPos=({posMap.x},{posMap.y}) targetMap=({this.targetMap.x},{this.targetMap.y})", LogManager.LogLevelEnum.Warning);

                    // 记入寻路失败缓存（FailCacheTtl=30s）：WorkerBrain 决策的
                    // ScanForResources 通过 IsRecentFail 跳过该目标。否则 GiveUpTask
                    // 释放 GatherMap 认领后，决策会立即重新选中同一目标，形成
                    // "失败→重建任务→又失败"的同帧死循环刷屏（观测：30ms 内重试 5+ 次）。
                    ASeek.RecordFail(this.targetMap);

                    // 标记任务失败时间，进入冷却期（10s），防止立即被重新分配形成死循环。
                    // 冷却结束后其他 Worker 可能可达（障碍物消失/位置变更），不永久删除。
                    workerData.Task.LastFailedTime = UnityEngine.Time.time;

                    // 同步记录睡眠失败时间，供 WorkerBrain 决策冷却使用：
                    // 防止 worker 卡死时"决策睡眠→无邻居→放弃"每帧死循环刷屏。
                    workerData.LastSleepFailTime = UnityEngine.Time.time;

                    this.Character.GiveUpTask();
                    return;
                }

                this.targetMap = closedPos;
                AWorkerTask.LogProvider(this.Character.name + " 寻路->" + this.targetMap, LogManager.LogLevelEnum.Trace);
                this.Character.Seek.Seek(this.targetMap);
                return;
            }

            // 没有任务 → 决策层接取（玩家悬赏 → 全局任务 → 自主决策 + 漫游簿记）。
            // 返回 true：已接取任务（Start 内部重入完成寻路）或漫游继续已自行寻路，不再 Seek；
            // 返回 false：moveTarget 为随机漫游路点。
            this.decision.SyncTargetMap(this.targetMap);
            if (this.decision.TryAcquireTask(out Vector3Int moveTarget))
            {
                return;
            }

            this.targetMap = moveTarget;
            AWorkerTask.LogProvider(this.Character.name + " 寻路->" + this.targetMap, LogManager.LogLevelEnum.Trace);
            this.Character.Seek.Seek(this.targetMap);
        }

        /// <summary>
        /// 位置合法性兜底：若 worker 的 map 坐标超出地图范围，强制传送到
        /// 地图内中心附近的可达位置。越界 worker 的寻路起点不被 A* 接受，
        /// 无法自行返回，必须先重置位置才能恢复正常行为。
        /// </summary>
        private void EnsureValidPosition(AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return;
            }

            TileMap tileMap = Core.ServiceLocator.Get<TileMap>();
            int height = tileMap?.TileMapDataLAB?.Height ?? 0;
            int width = tileMap?.TileMapDataLAB?.Width ?? 0;
            if (height <= 0 || width <= 0)
            {
                return; // 地图尚未就绪，等待下次进入时再检查
            }

            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            if (posMap.x >= 0 && posMap.y >= 0 && posMap.x < height && posMap.y < width)
            {
                return; // 在地图内，无需处理
            }

            // 越界 → 传送到地图中心附近的可达位置
            Vector3Int center = new Vector3Int(height / 2, width / 2, 0);
            Vector3Int target = tileMap.GenCanReachPos(center);
            this.Character.transform.position = AWorkerTask.TileMapPositionProvider(target);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 位置越界({posMap.x},{posMap.y}), 已重置到地图内({target.x},{target.y})",
                LogManager.LogLevelEnum.Warning);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 紧急生存检测已上移至 AWorker.CheckSurvivalEmergency（所有状态生效，
            // 原"仅 Seek 生效 + GiveUpTask 重入后再二次决策"的双重决策缺陷一并消除）。

            // 每60帧刷新一次
            if (Time.frameCount % 60 == 0)
            {
                this.builder.Clear();
                this.Character.WorkerStateText.text = this.builder.Append(this.preString)
                    .Append("<color=" + PixelUITheme.RichGold + ">Seeking: ")
                    .Append(MathHelper.RoundToInt(this.Character.Seek.SeekProgress * 100))
                    .Append("%</color>\nTarget: ")
                    .Append(this.targetMap.x)
                    .Append(",")
                    .Append(this.targetMap.y)
                    .ToString();
            }

            if (!this.Character.Seek.IsSeeking())
            {
                // 没有找到路
                if (!this.Character.Seek.IsHavePath())
                {
                    // 记录寻路失败位置，防止短时间内重复尝试同一不可达目标
                    ASeek.RecordFail(this.targetMap);

                    // 如果有任务
                    AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
                    if (workerData.Task != null)
                    {
                        this.Character.GiveUpTask();
                    }
                    else
                    {
                        this.Character.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                    }

                    AWorkerTask.LogProvider(this.Character.name + " 没有找到路!", LogManager.LogLevelEnum.Trace);
                    return;
                }

                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
