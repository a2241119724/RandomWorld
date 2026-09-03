namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core.Seek;
    using LAB2D.Item;
    using LAB2D.Map;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 建家选址/预留/清理服务 — 选址螺旋搜索、房间位置两阶段预注册、
    /// 冲突搬迁与死亡清理。纯静态（参数 + ServiceLocator，无实例状态）。
    /// H1 拆分：从 WorkerBrain 巨石文件迁出，行为零变化。
    /// </summary>
    public static class WorkerHomeSiteService
    {
    /// <summary>
    /// 在 Worker 周围找一个空闲的可建造位置。
    /// 优先返回 PlannedHomePosition（如果仍然空闲），否则螺旋搜索新位置。
    /// 检查 BuildMap、地面可通行性、以及其他 Worker 的已规划建家位置。
    /// </summary>
    internal static Vector3Int? FindFreeBuildPosition(AWorker worker)
    {
        AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;

        // 优先：已有规划位置且仍然空闲（包括不被其他 Worker 占据）
        if (wd?.PlannedHomePosition != null)
        {
            Vector3Int planned = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
            if (ASeek.IsCanReach(planned)
                && CanFitRoom(planned, wd)
                && !IsHomeSiteClaimedByOther(planned, worker)
                && !IsRoomAreaBlockedInBuildMap(planned, worker, wd))
            {
                return planned;
            }

            // 规划位置已被占用或不可通行，清除并重新搜索
            wd.PlannedHomePosition = null;
        }

        Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);

        // 从 Worker 位置向外螺旋搜索空闲位置
        for (int r = 1; r <= 8; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (System.Math.Abs(dx) != r && System.Math.Abs(dy) != r) continue; // 只检查外围

                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);

                    // 检查是否可通行
                    if (!ASeek.IsCanReach(pos)) continue;

                    // 检查房间区域是否与 BuildMap 冲突
                    if (IsRoomAreaBlockedInBuildMap(pos, worker, wd)) continue;

                    // 检查是否被其他 Worker 规划为建家位置（防止房间重叠）
                    if (IsHomeSiteClaimedByOther(pos, worker)) continue;

                    // 检查房间能否完整放置（所有墙壁/门位置都可达）
                    if (!CanFitRoom(pos, wd)) continue;

                    return pos;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 原子搬迁：先选新位置并设置 PlannedHomePosition，再清除旧位置瓦片。
    /// 消除 PlannedHomePosition=null 的空窗期，防止其他 Worker 抢占附近位置。
    /// </summary>
    internal static void RelocateHomeSite(AWorker worker, AWorker.WorkerData wd)
    {
        var buildMap = Core.ServiceLocator.Get<BuildMap>();
        Vector3IntLAB oldPosition = wd.PlannedHomePosition;

        // 1. 先选新位置（保留旧 PlannedHomePosition，IsHomeSiteClaimedByOther 会排除旧位置自身）
        Vector3Int? newPos = FindFreeBuildPosition(worker);
        if (newPos.HasValue)
        {
            wd.PlannedHomePosition = Vector3IntLAB.ToVector3IntLAB(newPos.Value);
            AWorkerTask.LogProvider(
                $"{worker.name} 搬迁建家位置: ({newPos.Value.x},{newPos.Value.y})",
                LogManager.LogLevelEnum.Debug);
        }
        // 如果找不到新位置，PlannedHomePosition 保持旧值，Worker 下次会重试

        // 2. 清除旧位置的残留瓦片（包括 tilemap 视觉和 BuildMap 数据）
        if (oldPosition != null && buildMap?.BuildMapDataLAB?.PosMap != null)
        {
            var oldLayout = WorkerHomeLayout.GetRoomLayout(wd);
            Vector3Int oldCenter = Vector3IntLAB.ToVector3Int(oldPosition);
            for (int i = 0; i < oldLayout.WallCount; i++)
            {
                Vector3Int wallPos = oldCenter + oldLayout.WallOffsets[i];
                Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(wallPos);
                if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var tile)
                    && !tile.IsComplete)
                {
                    buildMap.BuildMapDataLAB.PosMap.Remove(posLAB);
                    buildMap.CancelBuilding(wallPos);
                }
            }
            Vector3Int oldDoor = oldCenter + oldLayout.DoorOffset;
            Vector3IntLAB doorLAB = Vector3IntLAB.ToVector3IntLAB(oldDoor);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(doorLAB, out var d) && !d.IsComplete)
            {
                buildMap.BuildMapDataLAB.PosMap.Remove(doorLAB);
                buildMap.CancelBuilding(oldDoor);
            }
            // 清理床两格：主格未完成时同时清理副格碰撞体
            Vector3IntLAB oldBed1LAB = Vector3IntLAB.ToVector3IntLAB(oldCenter + oldLayout.BedOffset);
            bool oldBed1Incomplete = buildMap.BuildMapDataLAB.PosMap.TryGetValue(oldBed1LAB, out var oldBed1) && !oldBed1.IsComplete;
            if (oldBed1Incomplete)
            {
                buildMap.BuildMapDataLAB.PosMap.Remove(oldBed1LAB);
                buildMap.CancelBuilding(oldCenter + oldLayout.BedOffset);
            }
            Vector3IntLAB oldBed2LAB = Vector3IntLAB.ToVector3IntLAB(oldCenter + oldLayout.BedSecondOffset);
            if (oldBed1Incomplete && buildMap.BuildMapDataLAB.PosMap.TryGetValue(oldBed2LAB, out var oldBed2) && oldBed2.IsComplete)
            {
                buildMap.BuildMapDataLAB.PosMap.Remove(oldBed2LAB);
                buildMap.CancelBuilding(oldCenter + oldLayout.BedSecondOffset);
            }
            // 清理仓库位置
            foreach (var so in oldLayout.StorageOffsets)
            {
                Vector3Int sPos = oldCenter + so;
                Vector3IntLAB sLAB = Vector3IntLAB.ToVector3IntLAB(sPos);
                if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(sLAB, out var st) && !st.IsComplete)
                {
                    buildMap.BuildMapDataLAB.PosMap.Remove(sLAB);
                    buildMap.CancelBuilding(sPos);
                }
            }
        }

        // 3. 重置建造阶段（新位置从墙壁 0 开始）
        wd.HomeBuildStage = 0;
    }

    /// <summary>
    /// 预注册房间所有建造位置到 BuildMap，阻止 GenTree 在墙/门/床位置生成树。
    /// 在 HomeBuildStage==0 时调用一次，后续 CreateSelfBuildTask 跳过已注册位置。
    /// </summary>
    /// <param name="center">房间中心</param>
    /// <param name="worker">当前 Worker（仅用于日志）</param>
    /// <returns>true = 全部注册成功；false = 存在冲突，需重新选址</returns>
    internal static bool PreReserveAllRoomPositions(Vector3Int center, AWorker worker, AWorker.WorkerData wd)
    {
        var buildMap = Core.ServiceLocator.Get<BuildMap>();
        if (buildMap == null) return false;

        var layout = WorkerHomeLayout.GetRoomLayout(wd);

        // 收集所有需要预注册的位置及其 tileName
        var positionsToReserve = new List<(Vector3Int pos, string tileName)>(layout.WallCount + 6);

        for (int i = 0; i < layout.WallCount; i++)
        {
            int dir = layout.WallDirections[i];
            string tileName = $"CustomRoomWall_{dir}";
            positionsToReserve.Add((center + layout.WallOffsets[i], tileName));
        }
        positionsToReserve.Add((center + layout.DoorOffset, "CustomDoor"));
        // 床预注册主格（建造任务位置 = BedOffset），副格通过 RegisterCollisionTile 添加
        positionsToReserve.Add((center + layout.BedOffset, "SingleBed"));
        for (int i = 0; i < layout.StorageOffsets.Count; i++)
        {
            string storageTileName = $"{WorkerHomeLayout.StorageTileName}_{layout.StorageDirections[i]}";
            positionsToReserve.Add((center + layout.StorageOffsets[i], storageTileName));
        }

        // 两阶段：先检查全部可用，再统一注册（避免部分注册后失败难以回滚）
        foreach (var (pos, tileName) in positionsToReserve)
        {
            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var existing))
            {
                if (existing.IsComplete)
                {
                    // 已完成 → 可接受（可能是其他 Worker 建的公共墙）
                    continue;
                }

                // 未完成且 BuilderName 为空 → 是之前预注册的残留（如 Gather 资源后重新进入此阶段），跳过
                if (string.IsNullOrEmpty(existing.BuilderName))
                {
                    continue;
                }

                // 未完成且 BuilderName 是自己 → 自己之前的预留，跳过
                if (existing.BuilderName == worker.name)
                {
                    continue;
                }

                // 被其他 Worker 占用且未完成 → 冲突
                AWorkerTask.LogProvider(
                    $"{worker.name} 预注册失败: 位置被占用 {tileName} pos=({pos.x},{pos.y}) 被 {existing.BuilderName}",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }
        }

        // 全部可用 → 执行注册
        foreach (var (pos, tileName) in positionsToReserve)
        {
            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
            if (buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
            {
                // 已在 PosMap 中（已完成，上面已跳过）→ 不重复注册
                continue;
            }

            int w = 1, h = 1;

            if (!buildMap.ReserveBuildPosition(pos, tileName, worker.name, w, h))
            {
                AWorkerTask.LogProvider(
                    $"{worker.name} 预注册执行失败: {tileName} pos=({pos.x},{pos.y})",
                    LogManager.LogLevelEnum.Error);
                // 清理已注册的位置（回滚）
                ClearAbandonedBuildTilesCore(wd, center);
                return false;
            }
        }

        // 为床第二格注册碰撞体（IsComplete=true，同一 Name + BuilderName，纯阻挡寻路）
        // 尺寸与 RectType 从 SingleBed 定义派生，而非硬编码
        Vector3Int bed2Pos = center + layout.BedSecondOffset;
        buildMap.RegisterCollisionTile(bed2Pos, "SingleBed", worker.name,
            RoomLayout.BedDef.Width, RoomLayout.BedDef.Height, RoomLayout.BedDef.RectType);

        AWorkerTask.LogProvider(
            $"{worker.name} 预注册完成: {layout.WallCount}面墙+门+床+4仓库 共{positionsToReserve.Count}个位置",
            LogManager.LogLevelEnum.Debug);
        return true;
    }

    /// <summary>
    /// 清除指定中心位置的所有建造瓦片（内部实现，不依赖 WorkerData）。
    /// </summary>
    private static void ClearAbandonedBuildTilesCore(AWorker.WorkerData wd, Vector3Int center)
    {
        // 如果 wd 为 null，使用给定 center；否则从 wd.PlannedHomePosition 推导
        if (wd != null)
        {
            if (wd.PlannedHomePosition == null) return;
            center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
        }

        var buildMap = Core.ServiceLocator.Get<BuildMap>();
        if (buildMap?.BuildMapDataLAB?.PosMap == null) return;

        // 获取布局（wd 为 null 时回退到默认尺寸）
        RoomLayout layout;
        if (wd != null && wd.HomeRoomWidth > 0)
        {
            layout = WorkerHomeLayout.GetRoomLayout(wd);
        }
        else
        {
            // 回退：使用最大尺寸清理，确保不残留
            layout = WorkerHomeLayout.GenerateRoomLayout(7, 7, 0, 1);
        }

        var positionsToClear = new List<Vector3Int>(layout.WallCount + 6);
        for (int i = 0; i < layout.WallCount; i++)
        {
            positionsToClear.Add(center + layout.WallOffsets[i]);
        }
        positionsToClear.Add(center + layout.DoorOffset);
        positionsToClear.Add(center + layout.BedOffset);
        positionsToClear.Add(center + layout.BedSecondOffset);
        foreach (var so in layout.StorageOffsets)
        {
            positionsToClear.Add(center + so);
        }

        foreach (var pos in positionsToClear)
        {
            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var tile)
                && !tile.IsComplete)
            {
                buildMap.BuildMapDataLAB.PosMap.Remove(posLAB);
                buildMap.CancelBuilding(pos);
            }
        }

        // 清理床第二格碰撞体：只在主格未完成时清理（主格完成=床已建完不应清理）
        Vector3Int bed1Pos = center + layout.BedOffset;
        Vector3IntLAB bed1LAB2 = Vector3IntLAB.ToVector3IntLAB(bed1Pos);
        bool bed1Complete = buildMap.BuildMapDataLAB.PosMap.TryGetValue(bed1LAB2, out var bed1Data) && bed1Data.IsComplete;

        if (!bed1Complete)
        {
            Vector3Int bed2Pos = center + layout.BedSecondOffset;
            Vector3IntLAB bed2LAB2 = Vector3IntLAB.ToVector3IntLAB(bed2Pos);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(bed2LAB2, out var bed2Data)
                && bed2Data.IsComplete)
            {
                buildMap.BuildMapDataLAB.PosMap.Remove(bed2LAB2);
                buildMap.CancelBuilding(bed2Pos);
            }
        }
    }

    /// <summary>
    /// 清除当前 Worker 已经废弃的建造瓦片（IsComplete=false 的 BuildMap 条目）。
    /// 当 Worker 因冲突重新选址时调用，防止残留墙壁影响后续选址。
    /// </summary>
    internal static void ClearAbandonedBuildTiles(AWorker.WorkerData wd)
    {
        if (wd?.PlannedHomePosition == null) return;
        ClearAbandonedBuildTilesCore(wd, default);
    }

    /// <summary>
    /// 死亡清理：清除 Worker 预注册但未完成的房间瓦片（墙/门/床/仓库）。
    /// Worker 死亡后这些瓦片会失去规划归属（IsPartOfWorkerPlannedRoom 只查存活 Worker），
    /// VerifyBuildTasks 每 5 秒扫描会把整间房逐格重建为无主建造任务——
    /// 观测：死亡后 0.8s 内整间房 20+ 条"为未完成建造重新创建任务"（见 bug-fixes.md）。
    /// 死亡时清理，避免"房间全部被发布"。已建成的瓦片（IsComplete=true）保留——那是真实建筑。
    /// </summary>
    /// <param name="wd">死亡 Worker 的数据</param>
    public static void ClearDeadWorkerRoomTiles(AWorker.WorkerData wd)
    {
        if (wd?.PlannedHomePosition == null) return;
        ClearAbandonedBuildTilesCore(wd, default);
    }

    /// <summary>
    /// 检查以该位置为中心的 7×7 房间能否完整放置。
    /// 检查所有墙壁和门位置的可达性（不仅仅是四角），防止选址在部分不可达的位置。
    /// </summary>
    internal static bool CanFitRoom(Vector3Int center, AWorker.WorkerData wd)
    {
        // 房间边界检查：根据房间参数动态计算
        var tileMap = Core.ServiceLocator.Get<TileMap>();
        int mapWidth = tileMap?.TileMapDataLAB?.Width ?? int.MaxValue;
        int mapHeight = tileMap?.TileMapDataLAB?.Height ?? int.MaxValue;

        var layout = WorkerHomeLayout.GetRoomLayout(wd);
        int hw = (wd.HomeRoomWidth - 1) / 2;
        int hh = (wd.HomeRoomHeight - 1) / 2;

        // 房间边界检查（含外圈：外圈整圈也必须在图内，Worker 要站外圈建墙）
        if (center.x - hw - 1 < 0 || center.y - hh - 1 < 0
            || center.x + hw + 1 >= mapWidth || center.y + hh + 1 >= mapHeight)
        {
            return false;
        }

        // 检查所有墙壁位置是否可达
        for (int i = 0; i < layout.WallCount; i++)
        {
            Vector3Int wallPos = center + layout.WallOffsets[i];
            if (!ASeek.IsCanReach(wallPos))
            {
                return false;
            }
        }

        // 检查门位置是否可达
        Vector3Int doorPos = center + layout.DoorOffset;
        if (!ASeek.IsCanReach(doorPos))
        {
            return false;
        }

        // 检查床格是否可达（仅第一格，第二格由碰撞体覆盖）
        if (!ASeek.IsCanReach(center + layout.BedOffset))
        {
            return false;
        }

        // 检查 4 个仓库位置是否可达
        foreach (var so in layout.StorageOffsets)
        {
            if (!ASeek.IsCanReach(center + so))
            {
                return false;
            }
        }

        // 房间外墙体外一圈不允许有"阻挡且不可采集/不可挖掘"的瓦片（水/建筑/他房墙等）。
        // 外圈允许可通行、可采集资源（树/矿）、可挖掘地形（山）——Worker 建墙本就要先清它们。
        if (!IsRoomOuterRingClear(center, hw, hh))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查房间外墙体外一圈（墙外 1 格，含角）是否不存在"阻挡且不可采集/不可挖掘"的瓦片。
    /// 外圈瓦片满足其一即通过：可通行、可采集资源（ResourceMap 树/矿）、可挖掘地形（山）。
    /// 仅当同时「阻挡」且「不可采集且不可挖掘」（水、其他建筑/墙等）时判定不合格——
    /// 因为 Worker 建墙必须站在外圈，此类瓦片永远无法清除会导致建造卡死。
    /// </summary>
    /// <param name="center">房间中心</param>
    /// <param name="hw">房间半宽（外墙宽 (width-1)/2）</param>
    /// <param name="hh">房间半高（外墙高 (height-1)/2）</param>
    /// <returns>true = 外圈无不可采集碰撞体</returns>
    private static bool IsRoomOuterRingClear(Vector3Int center, int hw, int hh)
    {
        var resourceMap = Core.ServiceLocator.Get<ResourceMap>();
        var terrainDb = Core.ServiceLocator.Get<TerrainConfigDatabase>();

        // 上边外圈 y = hh+1（含两角）
        for (int x = -hw - 1; x <= hw + 1; x++)
        {
            if (IsObstructingNonGatherable(center + new Vector3Int(x, hh + 1, 0), resourceMap, terrainDb))
            {
                return false;
            }
        }

        // 下边外圈 y = -hh-1（含两角）
        for (int x = -hw - 1; x <= hw + 1; x++)
        {
            if (IsObstructingNonGatherable(center + new Vector3Int(x, -hh - 1, 0), resourceMap, terrainDb))
            {
                return false;
            }
        }

        // 左边外圈 x = -hw-1（不含角，角已由上/下边覆盖）
        for (int y = -hh; y <= hh; y++)
        {
            if (IsObstructingNonGatherable(center + new Vector3Int(-hw - 1, y, 0), resourceMap, terrainDb))
            {
                return false;
            }
        }

        // 右边外圈 x = hw+1
        for (int y = -hh; y <= hh; y++)
        {
            if (IsObstructingNonGatherable(center + new Vector3Int(hw + 1, y, 0), resourceMap, terrainDb))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断位置是否为"阻挡且不可采集/不可挖掘"的瓦片（水、其他建筑/墙等）。
    /// 可通行（无碰撞体）→ false；可采集资源（ResourceMap 树/矿）→ false；可挖掘地形（山）→ false。
    /// </summary>
    /// <param name="pos">地图坐标</param>
    /// <param name="resourceMap">资源地图（可为 null）</param>
    /// <param name="terrainDb">地形配置（可为 null）</param>
    /// <returns>true = 阻挡且不可采集不可挖掘（外圈不允许）</returns>
    private static bool IsObstructingNonGatherable(Vector3Int pos, ResourceMap resourceMap, TerrainConfigDatabase terrainDb)
    {
        // 可通行（无碰撞体）→ 不阻挡
        if (ASeek.IsCanReach(pos))
        {
            return false;
        }

        // 可采集资源（树/矿等）→ Worker 采集后即可通行
        if (resourceMap != null && resourceMap.TryGetGatherResourceInfo(pos, out _))
        {
            return false;
        }

        // 可挖掘地形（山等）→ Worker 挖掘后即可通行
        var tileMap = Core.ServiceLocator.Get<TileMap>();
        if (terrainDb != null && tileMap?.TileMapDataLAB?.MapTiles != null
            && pos.x >= 0 && pos.y >= 0
            && pos.x < tileMap.TileMapDataLAB.MapTiles.GetLength(0)
            && pos.y < tileMap.TileMapDataLAB.MapTiles.GetLength(1))
        {
            int terrainId = tileMap.TileMapDataLAB.MapTiles[pos.x, pos.y];
            if (terrainDb.IsDiggable(terrainId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查建造位置是否至少有一个相邻格子可通行（Worker 需要站在旁边才能建造）。
    /// </summary>
    internal static bool HasReachableNeighbor(Vector3Int buildPos)
    {
        // 检查上下左右四个邻居
        Vector3Int[] neighbors = {
            new Vector3Int(0, 1, 0),   // 上
            new Vector3Int(1, 0, 0),   // 右
            new Vector3Int(0, -1, 0),  // 下
            new Vector3Int(-1, 0, 0),  // 左
        };

        foreach (var offset in neighbors)
        {
            Vector3Int neighbor = buildPos + offset;
            if (ASeek.IsCanReach(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查 7×7 房间区域的任何瓦片是否已被 BuildMap 占用（已完成或建造中）。
    /// 这是对 IsHomeSiteClaimedByOther 的补充：
    /// IsHomeSiteClaimedByOther 检查其他 Worker 的 PlannedHomePosition/HomePosition，
    /// 本方法检查 BuildMap 中的实际建筑瓦片（包括非 Worker 来源的建筑、已完成墙壁等）。
    /// </summary>
    /// <param name="center">房间中心位置</param>
    /// <returns>true 表示区域内有 BuildMap 瓦片占用</returns>
    /// <summary>
    /// 检查房间区域（墙壁/门/床位置）是否与 BuildMap 冲突。
    /// 如果指定了 worker，则跳过该 worker 自己建造的瓦片（用于读档后验证已有规划位置）。
    /// </summary>
    internal static bool IsRoomAreaBlockedInBuildMap(Vector3Int center, AWorker self, AWorker.WorkerData wd)
    {
        var buildMap = Core.ServiceLocator.Get<BuildMap>();
        if (buildMap?.BuildMapDataLAB?.PosMap == null) return false;

        string selfName = self != null ? self.name : null;
        var layout = WorkerHomeLayout.GetRoomLayout(wd);

        // 检查所有墙壁位置
        for (int i = 0; i < layout.WallCount; i++)
        {
            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(center + layout.WallOffsets[i]);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var tile))
            {
                // 跳过自己建造的瓦片（读档后已建好的墙壁不应视为冲突）
                if (selfName != null && tile.BuilderName == selfName) continue;
                return true;
            }
        }

        // 检查门位置
        Vector3IntLAB doorLAB = Vector3IntLAB.ToVector3IntLAB(center + layout.DoorOffset);
        if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(doorLAB, out var doorTile))
        {
            if (selfName != null && doorTile.BuilderName == selfName) { /* 跳过自己 */ }
            else return true;
        }

        // 检查床两格位置
        Vector3IntLAB bed1LAB = Vector3IntLAB.ToVector3IntLAB(center + layout.BedOffset);
        if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(bed1LAB, out var bedTile))
        {
            if (selfName != null && bedTile.BuilderName == selfName) { /* 跳过自己 */ }
            else return true;
        }
        Vector3IntLAB bed2LAB = Vector3IntLAB.ToVector3IntLAB(center + layout.BedSecondOffset);
        if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(bed2LAB, out var bed2Tile))
        {
            if (selfName != null && bed2Tile.BuilderName == selfName) { /* 跳过自己 */ }
            else return true;
        }

        // 检查仓库位置
        foreach (var so in layout.StorageOffsets)
        {
            Vector3IntLAB sLAB = Vector3IntLAB.ToVector3IntLAB(center + so);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(sLAB, out var st))
            {
                if (selfName != null && st.BuilderName == selfName) { /* 跳过自己 */ }
                else return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查某个建造位置（墙壁/门/床的具体坐标）是否落入其他 Worker 的房间范围内。
    /// 房间范围 = 中心 ±3（7×7），因此阈值是 ≤ 3。
    /// 与 IsHomeSiteClaimedByOther 不同：IsHomeSiteClaimedByOther 比较的是
    /// 两个房间中心之间的距离（阈值 ≤ 6），本方法比较的是单个建造位置
    /// 与另一个房间中心之间的距离（阈值 ≤ 3）。
    /// </summary>
    /// <param name="buildPos">墙壁/门/床的具体坐标</param>
    /// <param name="self">当前 Worker</param>
    /// <returns>true 表示该建造位置在另一个 Worker 的房间范围内</returns>
    internal static bool IsPositionInsideOtherWorkerRoom(Vector3Int buildPos, AWorker self)
    {
        var workerManager = Core.ServiceLocator.Get<WorkerManager>();
        if (workerManager?.Characters == null) return false;

        foreach (AWorker other in workerManager.Characters)
        {
            if (other == self) continue;
            AWorker.WorkerData otherWd = other.CharacterDataLAB as AWorker.WorkerData;
            if (otherWd == null) continue;

            Vector3IntLAB otherCenterLAB = otherWd.PlannedHomePosition ?? otherWd.HomePosition;
            if (otherCenterLAB == default) continue;

            Vector3Int otherCenter = Vector3IntLAB.ToVector3Int(otherCenterLAB);
            // 根据其他 Worker 的房间参数动态计算范围（默认回退到 ±3 即 7×7）
            int otherHw = otherWd.HomeRoomWidth > 0 ? (otherWd.HomeRoomWidth - 1) / 2 : 3;
            int otherHh = otherWd.HomeRoomHeight > 0 ? (otherWd.HomeRoomHeight - 1) / 2 : 3;
            if (System.Math.Abs(buildPos.x - otherCenter.x) <= otherHw
                && System.Math.Abs(buildPos.y - otherCenter.y) <= otherHh)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查以该位置为中心的 7×7 房间矩形，是否与其他 Worker 已规划或已完成的家重叠或太近。
    /// 中心距离 ≤ 7 时视为冲突，确保房间之间至少有一格行走间距。
    /// 依赖 RelocateHomeSite 保证 PlannedHomePosition 永远不为 null（消除空窗期）。
    /// </summary>
    internal static bool IsHomeSiteClaimedByOther(Vector3Int candidateCenter, AWorker self)
    {
        var workerManager = Core.ServiceLocator.Get<WorkerManager>();
        if (workerManager?.Characters == null) return false;

        foreach (AWorker other in workerManager.Characters)
        {
            if (other == self) continue;
            AWorker.WorkerData otherWd = other.CharacterDataLAB as AWorker.WorkerData;
            if (otherWd == null) continue;

            Vector3IntLAB otherCenterLAB = otherWd.PlannedHomePosition ?? otherWd.HomePosition;
            if (otherCenterLAB == default) continue;

            Vector3Int otherCenter = Vector3IntLAB.ToVector3Int(otherCenterLAB);
            if (System.Math.Abs(candidateCenter.x - otherCenter.x) <= 7
                && System.Math.Abs(candidateCenter.y - otherCenter.y) <= 7)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 为无家 Worker 选择建家位置。在附近扫描空地，选择最近的可建造位置。
    /// 结果写入 WorkerData.PlannedHomePosition，后续 FindFreeBuildPosition 会优先返回该位置。
    /// </summary>
    internal static void TryPickHomeSite(AWorker worker)
    {
        AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
        if (wd == null || wd.HomePosition != null) return; // 已有家
        if (wd.PlannedHomePosition != null) return;        // 已规划过

        // 先生成随机房间参数（后续所有方法都需要这些参数）
        if (wd.HomeRoomWidth == 0)
        {
            WorkerHomeLayout.GenerateRandomRoomParams(wd);
            // 建家参数选定诊断（事件点）：记录房间尺寸与门位参数，便于核对布局生成是否
            // 与 PrintRoomLayout 的字符画一致，以及门位是否避开家具（封门 bug 族）。
            AWorkerTask.LogProvider(
                $"[BrainDiag] {worker.name} 建家参数选定: {wd.HomeRoomWidth}x{wd.HomeRoomHeight} 门=边{wd.HomeDoorSide} idx{wd.HomeDoorIndex}",
                LogManager.LogLevelEnum.Debug);
        }

        // 优先检查是否有遗弃的空床（死亡 Worker 留下的完整房间）
        if (TryInheritAbandonedHome(worker, wd))
        {
            return;
        }

        Vector3Int? pos = FindFreeBuildPosition(worker);
        if (pos.HasValue)
        {
            wd.PlannedHomePosition = Vector3IntLAB.ToVector3IntLAB(pos.Value);
            AWorkerTask.LogProvider(
                $"{worker.name} 选定建家位置: ({pos.Value.x},{pos.Value.y})",
                LogManager.LogLevelEnum.Debug);
        }
    }

    /// <summary>
    /// 尝试继承遗弃的房间（死亡 Worker 留下）。如果遗弃房间结构完整，
    /// 直接绑定 Worker 到该床，跳过建造流程。
    /// </summary>
    private static bool TryInheritAbandonedHome(AWorker worker, AWorker.WorkerData wd)
    {
        var fm = Core.ServiceLocator.Get<FurnitureManager>();
        if (fm == null) return false;

        Vector3Int? abandonedBed = fm.GetAbandonedBedPosition();
        if (!abandonedBed.HasValue) return false;

        Vector3Int pos = abandonedBed.Value;

        // 验证房间结构：床位置所在瓦片必须可站立
        var buildMap = Core.ServiceLocator.Get<BuildMap>();
        if (buildMap != null && !buildMap.IsCanReach(pos))
        {
            // 房间结构已损坏，清理无效的床记录
            fm.BedToWorker.Remove(pos);
            return false;
        }

        // 直接绑定 Worker 到遗弃床
        fm.AddWorkerToBed(pos, worker);
        // PlannedHomePosition 是房间中心，床在中心 + BedOffset 处
        var layout = WorkerHomeLayout.GetRoomLayout(wd);
        wd.PlannedHomePosition = Vector3IntLAB.ToVector3IntLAB(pos - layout.BedOffset);
        wd.HomeBuildStage = layout.CompleteStage; // 建造完成（继承遗弃房间）
        AWorkerTask.LogProvider(
            $"{worker.name} 继承遗弃房间: ({pos.x},{pos.y})，跳过建造流程",
            LogManager.LogLevelEnum.Debug);
        return true;
    }
    }
}
