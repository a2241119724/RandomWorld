namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using LAB2D.Domain.Common;
    using LAB2D.Item;
    using LAB2D.Item.Build;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 使用Collider会出现, Worker寻路完成, 但路径上的Tile被建造完成，导致不能通行
    /// 正在建造的不可通行也是不可通行
    /// </summary>
    public class BuildMap : BaseTileMap
    {
        private Color initColor = new (1, 1, 1, 0.5f);

        /// <summary>
        /// 单例
        /// </summary>
        public static BuildMap Instance { get; private set; }

        /// <summary>
        /// 构建地图数据
        /// </summary>
        public BuildMapData BuildMapDataLAB { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public void Start()
        {
            this.BuildMapDataLAB = new BuildMapData();
        }

        /// <summary>
        /// 添加建造
        /// </summary>
        /// <param name="targetMap">建造位置</param>
        /// <param name="tileName">瓦片名称</param>
        /// <param name="priority">任务优先级，默认系统默认</param>
        /// <returns>链式</returns>
        public BuildMap AddBuild(Vector3Int targetMap, string tileName, int priority = WorkerTaskPriority.SystemDefault,
            int width = 1, int height = 1, AWorkerTask.RectType rectType = AWorkerTask.RectType.Center)
        {
            Vector3IntLAB vector3IntLAB = Vector3IntLAB.ToVector3IntLAB(targetMap);
            ItemData itemData = Core.ServiceLocator.Get<ItemDataManager>().GetByName(tileName);
            bool isNeedBuild = true;
            Dictionary<int, ResourceInfo> buildCosts = new Dictionary<int, ResourceInfo>();

            if (itemData is BuildItemData buildItemData)
            {
                isNeedBuild = buildItemData.IsNeedBuild;
                buildCosts = BuildResourceDict(buildItemData);
            }
            // 非 BuildItemData 的物品默认需要建造，使用空材料清单

            this.tilemap.SetTile(targetMap, (TileBase)AWorkerTask.ResourceLoadProvider(tileName));
            if (isNeedBuild)
            {
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                Core.ServiceLocator.Get<WorkerTaskManager>().AddTask(
                    new WorkerBuildTask.BuildTaskBuilder().SetBuildPos(targetMap)
                    .SetNeedResource(buildCosts).Build(), new GameGridPosition(targetMap.x, targetMap.y, targetMap.z),
                    priority);

                // 设置可通过并且颜色变淡
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
                this.tilemap.SetColor(targetMap, this.initColor);
            }

            BuildTileData buildTileData = new BuildTileData(tileName, !isNeedBuild, width, height);
            buildTileData.RectType = rectType;
            if (this.BuildMapDataLAB.PosMap.ContainsKey(vector3IntLAB))
            {
                // 门
                this.BuildMapDataLAB.PosMap[vector3IntLAB] = buildTileData;
            }
            else
            {
                this.BuildMapDataLAB.PosMap.Add(vector3IntLAB, buildTileData);
            }

            WalkabilityCache.UpdateCell(targetMap);

            this.SyncSender.Broadcast(
                "SyncDataResp",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                DataTool.ToByteArray(buildTileData));

            return this;
        }

        /// <summary>
        /// 仅注册建造位置（不自动创建任务）。
        /// 用于 Worker 自主建造场景：任务由 WorkerBrain 自行创建和分配，避免 AddBuild 内部自动创建重复任务。
        /// </summary>
        /// <param name="targetMap">建造位置</param>
        /// <param name="tileName">瓦片名称</param>
        /// <param name="builderName">建造者 Worker 名称（用于 VerifyBuildTasks 恢复指派关系）。</param>
        /// <param name="width">物品宽度</param>
        /// <param name="height">物品高度</param>
        /// <param name="rectType">Rect 类型（多格物品自动为副格注册碰撞体）</param>
        /// <returns>true = 预约成功；false = 位置已被占用</returns>
        public bool ReserveBuildPosition(Vector3Int targetMap, string tileName, string builderName = null,
            int width = 1, int height = 1, AWorkerTask.RectType rectType = AWorkerTask.RectType.Center)
        {
            Vector3IntLAB primaryLAB = Vector3IntLAB.ToVector3IntLAB(targetMap);

            // 1. 检查主格是否可用
            if (this.BuildMapDataLAB.PosMap.TryGetValue(primaryLAB, out var existing))
            {
                AWorkerTask.LogProvider(
                    $"ReserveBuildPosition: 位置已被占用 tile={existing.Name} complete={existing.IsComplete}",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            // 2. 多格物品：检查所有副格是否可用
            if (width > 1 || height > 1)
            {
                var allPositions = ABuildItem.GetOccupiedPositions(targetMap, width, height, rectType);
                foreach (var pos in allPositions)
                {
                    if (pos == targetMap) continue;
                    Vector3IntLAB secLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                    if (this.BuildMapDataLAB.PosMap.TryGetValue(secLAB, out var secExisting))
                    {
                        AWorkerTask.LogProvider(
                            $"ReserveBuildPosition: 副格已被占用 tile={secExisting.Name} pos=({pos.x},{pos.y})",
                            LogManager.LogLevelEnum.Warning);
                        return false;
                    }
                }
            }

            // 3. 注册主格（visual tile + IsComplete=false → 需要建造任务）
            TileBase tile = (TileBase)AWorkerTask.ResourceLoadProvider(tileName);
            if (tile != null)
            {
                this.tilemap.SetTile(targetMap, tile);
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
                this.tilemap.SetColor(targetMap, this.initColor);
            }

            BuildTileData primaryData = new BuildTileData(tileName, false, width, height);
            primaryData.RectType = rectType;
            if (!string.IsNullOrEmpty(builderName))
            {
                primaryData.BuilderName = builderName;
            }
            this.BuildMapDataLAB.PosMap.Add(primaryLAB, primaryData);
            WalkabilityCache.UpdateCell(targetMap);

            this.SyncSender.Broadcast(
                "SyncDataResp",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                DataTool.ToByteArray(primaryData));

            // 4. 多格物品：注册副格（纯本地碰撞体，不广播避免远程重复 visual tile）
            if (width > 1 || height > 1)
            {
                var allPositions = ABuildItem.GetOccupiedPositions(targetMap, width, height, rectType);
                foreach (var pos in allPositions)
                {
                    if (pos == targetMap) continue;
                    Vector3IntLAB secLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                    BuildTileData secData = new BuildTileData(tileName, true, width, height);
                    secData.RectType = rectType;
                    secData.BuilderName = builderName ?? string.Empty;
                    secData.OwnerName = builderName ?? string.Empty;
                    this.BuildMapDataLAB.PosMap.Add(secLAB, secData);
                    this.tilemap.SetColliderType(pos, Tile.ColliderType.Sprite);
                    WalkabilityCache.UpdateCell(pos);
                    // 不广播：主格同步 + SetComplete 多格逻辑会让远程也完成副格
                }
            }

            return true;
        }

        /// <summary>
        /// 为多格物品注册副格碰撞体（无 visual tile，IsComplete=true，纯阻挡寻路）。
        /// 调用前应确保位置可用。
        /// </summary>
        /// <param name="pos">副格地图坐标</param>
        /// <param name="tileName">瓦片名称（与主格相同）</param>
        /// <param name="builderName">建造者名称</param>
        /// <param name="width">物品宽度</param>
        /// <param name="height">物品高度</param>
        /// <param name="rectType">矩形类型</param>
        public void RegisterCollisionTile(Vector3Int pos, string tileName, string builderName,
            int width, int height, AWorkerTask.RectType rectType)
        {
            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
            if (this.BuildMapDataLAB.PosMap.ContainsKey(posLAB)) return;

            BuildTileData secData = new BuildTileData(tileName, true, width, height);
            secData.RectType = rectType;
            secData.BuilderName = builderName ?? string.Empty;
            secData.OwnerName = builderName ?? string.Empty;
            this.BuildMapDataLAB.PosMap.Add(posLAB, secData);
            this.tilemap.SetColliderType(pos, Tile.ColliderType.Sprite);
            WalkabilityCache.UpdateCell(pos);
            // 不广播：主格同步 + SetComplete 多格逻辑会让远程也完成副格
        }

        /// <summary>
        /// 直接建造完成,Worker
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        /// <param name="tile">瓦片</param>
        /// <param name="isPass">是否可通行</param>
        /// <returns>地图瓦片</returns>
        public BuildMap DirectBuild(Vector3Int targetMap, TileBase tile, bool isPass = true)
        {
            return this.DoDirectBuild(targetMap, tile, isPass);
        }

        /// <summary>
        /// 完成建造（带建造者和所属者信息）,设置颜色透明度为1,不可通过的添加碰撞体
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        /// <param name="builderName">建造者 Worker 名称</param>
        /// <param name="ownerName">所属者 Worker 名称</param>
        public void SetComplete(Vector3IntLAB targetMap, string builderName, string ownerName)
        {
            if (this.BuildMapDataLAB.PosMap.TryGetValue(targetMap, out BuildTileData buildTileData))
            {
                buildTileData.BuilderName = builderName ?? string.Empty;
                buildTileData.OwnerName = ownerName ?? string.Empty;

                // 多格物品：同步完成所有占用格
                if (buildTileData.Width > 1 || buildTileData.Height > 1)
                {
                    Vector3Int centerMap = Vector3IntLAB.ToVector3Int(targetMap);
                    var allPositions = ABuildItem.GetOccupiedPositions(
                        centerMap, buildTileData.Width, buildTileData.Height,
                        buildTileData.RectType);
                    foreach (var pos in allPositions)
                    {
                        Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                        if (this.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out BuildTileData otherTile)
                            && !otherTile.IsComplete
                            && otherTile.Name == buildTileData.Name)
                        {
                            otherTile.BuilderName = builderName ?? string.Empty;
                            otherTile.OwnerName = ownerName ?? string.Empty;
                            this.SetComplete(posLAB);
                        }
                    }
                    return; // SetComplete 已在循环中调用，无需再调用
                }
            }

            this.SetComplete(targetMap);
        }

        /// <summary>
        /// 完成建造,设置颜色透明度为1,不可通过的添加碰撞体
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        public void SetComplete(Vector3IntLAB targetMap)
        {
            if (!this.BuildMapDataLAB.PosMap.TryGetValue(targetMap, out BuildTileData buildTileData))
            {
                AWorkerTask.LogProvider(
                    $"SetComplete: 位置 {targetMap} 不在 PosMap 中，可能已被取消或同步移除",
                    LogManager.LogLevelEnum.Warning);
                return;
            }

            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(targetMap);
            buildTileData.IsComplete = true;
            Core.ServiceLocator.Get<RoomManager>().Complete(vector3Int);
            BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(buildTileData.Name);
            this.tilemap.SetColor(vector3Int, Color.white);
            if (buildItemData != null && !buildItemData.IsPass)
            {
                this.tilemap.SetColliderType(vector3Int, Tile.ColliderType.Sprite);
            }

            WalkabilityCache.UpdateCell(vector3Int);

            this.SyncSender.Broadcast(
                "SyncDataResp",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(vector3Int)),
                default);
        }

        /// <summary>
        /// 获取指定位置的建造瓦片数据
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>建造瓦片数据，若不存在则返回 null</returns>
        public BuildTileData GetBuildTileData(Vector3Int pos)
        {
            if (this.BuildMapDataLAB?.PosMap == null)
            {
                return null;
            }

            Vector3IntLAB key = Vector3IntLAB.ToVector3IntLAB(pos);
            this.BuildMapDataLAB.PosMap.TryGetValue(key, out BuildTileData data);
            return data;
        }

        /// <summary>
        /// 是否正在建造
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <returns>是否</returns>
        public bool IsBuilding(Vector3Int target)
        {
            if (!this.BuildMapDataLAB.PosMap.TryGetValue(Vector3IntLAB.ToVector3IntLAB(target), out BuildTileData data))
            {
                return false;
            }

            return !data.IsComplete;
        }

        /// <summary>
        /// 删除正在建造
        /// </summary>
        /// <param name="targetMap">建造目标</param>
        public void CancelBuilding(Vector3Int targetMap)
        {
            this.tilemap.SetTile(targetMap, null);
            this.BuildMapDataLAB.PosMap.Remove(Vector3IntLAB.ToVector3IntLAB(targetMap));
            WalkabilityCache.UpdateCell(targetMap);
            this.SyncSender.Broadcast(
                "SyncDataResp",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                default,
                true);
        }

        /// <summary>
        /// 添加建造任务到地图。
        /// 每个建筑按自身 BuildItemData.BuildCosts 查找材料需求，不再使用统一的硬编码材料。
        /// </summary>
        public void AddTask()
        {
            foreach (Vector3IntLAB targetMap in this.BuildMapDataLAB.PosMap.Keys)
            {
                string tileName = this.BuildMapDataLAB.PosMap[targetMap].Name;
                BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(tileName);
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                Core.ServiceLocator.Get<WorkerTaskManager>().AddTask(
                    new WorkerBuildTask.BuildTaskBuilder().SetBuildPos(Vector3IntLAB.ToVector3Int(targetMap))
                    .SetNeedResource(BuildResourceDict(buildItemData)).Build(), new GameGridPosition(targetMap.X, targetMap.Y, targetMap.Z));
            }

            this.BuildMapDataLAB.PosMap.Clear();
        }

        /// <summary>
        /// 是否可以通行,Worker寻路时使用。
        /// 先查 PosMap（捕获无 visual tile 的碰撞体/多格副格），再 fallback 到 IsFreeTile。
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public override bool IsCanReach(Vector3Int posMap)
        {
            if (this.BuildMapDataLAB?.PosMap != null)
            {
                Vector3IntLAB key = Vector3IntLAB.ToVector3IntLAB(posMap);
                if (this.BuildMapDataLAB.PosMap.TryGetValue(key, out BuildTileData data) && data != null)
                {
                    // 未建造完成（预注册/建造中）→ 可通行（碰撞体已被移除）
                    if (!data.IsComplete)
                    {
                        return true;
                    }

                    // 已完成 → 查 BuildItemData.IsPass
                    // 碰撞体（Name="BedCollision"等无对应 BuildItemData 的条目）→ GetBuildItemDataByName 返回 null → 不可通行
                    try
                    {
                        BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(data.Name);
                        return buildItemData != null && buildItemData.IsPass;
                    }
                    catch (System.InvalidCastException)
                    {
                        // 非 BuildItemData 的 tile（如 Bounty 任务栏标记），默认可通行
                        return true;
                    }
                }
            }

            // 不在 PosMap → fallback 到 visual tile 检查
            return this.IsFreeTile(posMap);
        }

        /// <summary>
        /// 获取指定地图位置的建造数据，用于外部查询（如视线检测）。
        /// </summary>
        /// <param name="posMap">地图坐标。</param>
        /// <returns>该位置的 BuildTileData，若不存在则返回 null。</returns>
        public BuildTileData GetBuildData(Vector3Int posMap)
        {
            if (this.BuildMapDataLAB?.PosMap == null) return null;
            Vector3IntLAB key = Vector3IntLAB.ToVector3IntLAB(posMap);
            this.BuildMapDataLAB.PosMap.TryGetValue(key, out BuildTileData data);
            return data;
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            AWorkerTask.LogProvider("Request: 同步地图建造数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.BuildMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            AWorkerTask.LogProvider("Response: 同步地图建造数据", LogManager.LogLevelEnum.Trace);
            BuildMapData buildMapData = DataTool.FromByteArray<BuildMapData>(data);
            this.BuildMapDataLAB = buildMapData;
            WalkabilityCache.Invalidate();
            Dictionary<Vector3IntLAB, BuildTileData>.Enumerator enumerator = buildMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(enumerator.Current.Key);
                this.tilemap.SetTile(vector3Int, (TileBase)AWorkerTask.ResourceLoadProvider(enumerator.Current.Value.Name));
                if (!enumerator.Current.Value.IsComplete)
                {
                    this.tilemap.SetColliderType(vector3Int, Tile.ColliderType.None);
                    this.tilemap.RemoveTileFlags(vector3Int, TileFlags.LockColor);
                    this.tilemap.SetColor(vector3Int, this.initColor);
                }

                WalkabilityCache.UpdateCell(vector3Int);
            }
        }

        /// <summary>
        /// 同步地图建造数据
        /// </summary>
        /// <param name="vector3IntLAB">位置</param>
        /// <param name="buildTileDataLAB">建造瓦片信息</param>
        /// <param name="isDelete">是否删除</param>
        [PunRPC]
        public void SyncDataResp(byte[] vector3IntLAB, byte[] buildTileDataLAB, bool isDelete = false)
        {
            AWorkerTask.LogProvider("Response: 同步地图建造数据", LogManager.LogLevelEnum.Trace);
            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            Vector3IntLAB key = Vector3IntLAB.ToVector3IntLAB(vector3Int);
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                this.BuildMapDataLAB.PosMap.Remove(key);
                WalkabilityCache.UpdateCell(vector3Int);
                return;
            }

            BuildTileData buildTileData = DataTool.FromByteArray<BuildTileData>(buildTileDataLAB);
            this.BuildMapDataLAB.PosMap[key] = buildTileData;
            if (!buildTileData.Name.Equals(string.Empty))
            {
                this.tilemap.SetTile(vector3Int, (TileBase)AWorkerTask.ResourceLoadProvider(buildTileData.Name));
            }

            BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(buildTileData.Name);
            if (buildTileData.IsComplete)
            {
                this.tilemap.SetColor(vector3Int, Color.white);
                if (buildItemData != null && !buildItemData.IsPass)
                {
                    this.tilemap.SetColliderType(vector3Int, Tile.ColliderType.Sprite);
                }
            }
            else
            {
                this.tilemap.RemoveTileFlags(vector3Int, TileFlags.LockColor);
                this.tilemap.SetColor(vector3Int, new Color(1, 1, 1, 0.5f));
                if (buildItemData.IsPass)
                {
                    this.tilemap.SetColliderType(vector3Int, Tile.ColliderType.None);
                }
            }

            WalkabilityCache.UpdateCell(vector3Int);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            this.BuildMapDataLAB = DataTool.LoadDataByBinary<BuildMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name)) ?? new BuildMapData();
            WalkabilityCache.Invalidate();
            foreach (var posMap in this.BuildMapDataLAB.PosMap)
            {
                Vector3Int pos = Vector3IntLAB.ToVector3Int(posMap.Key);
                BuildTileData tileData = posMap.Value;
                TileBase tile = (TileBase)AWorkerTask.ResourceLoadProvider(tileData.Name);
                this.tilemap.SetTile(pos, tile);

                if (tileData.IsComplete)
                {
                    // 已完成：全色 + 根据 IsPass 设置碰撞体
                    this.tilemap.SetColor(pos, Color.white);
                    BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(tileData.Name);
                    if (buildItemData != null && !buildItemData.IsPass)
                    {
                        this.tilemap.SetColliderType(pos, Tile.ColliderType.Sprite);
                    }
                }
                else
                {
                    // 未完成：半透明 + 无碰撞体
                    this.tilemap.RemoveTileFlags(pos, TileFlags.LockColor);
                    this.tilemap.SetColor(pos, this.initColor);
                    this.tilemap.SetColliderType(pos, Tile.ColliderType.None);
                }

                WalkabilityCache.UpdateCell(pos);
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.BuildMapDataLAB);
        }

        /// <summary>
        /// 直接建造完成,Worker
        /// </summary>
        private BuildMap DoDirectBuild(Vector3Int targetMap, TileBase tile, bool isPass = true)
        {
            this.tilemap.SetTile(targetMap, tile);
            if (isPass)
            {
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }

            WalkabilityCache.UpdateCell(targetMap);

            this.SyncSender.Broadcast(
                "SyncDataResp",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                tile.name,
                isPass,
                false);

            return this;
        }

        /// <summary>
        /// 从 BuildItemData.BuildCosts 构建资源需求字典。
        /// 若未配置 BuildCosts，fallback 到默认 CustomWood x5（向后兼容存量 SO）。
        /// </summary>
        /// <param name="buildItemData">建造物品数据</param>
        /// <returns>物品 ID → ResourceInfo 字典</returns>
        private static Dictionary<int, ResourceInfo> BuildResourceDict(BuildItemData buildItemData)
        {
            Dictionary<int, ResourceInfo> dict = new ();
            if (buildItemData != null && buildItemData.BuildCosts != null)
            {
                foreach (ResourceCost cost in buildItemData.BuildCosts)
                {
                    if (string.IsNullOrEmpty(cost.ItemName))
                    {
                        continue;
                    }

                    ItemData item = Core.ServiceLocator.Get<Data.ItemDataManager>().GetByName(cost.ItemName);
                    if (item != null && item.Id > 0)
                    {
                        dict[item.Id] = new ResourceInfo(item.Id, cost.Count);
                    }
                }
            }

            // Fallback: 存量 SO 未配置 BuildCosts 时，使用默认 CustomWood x5
            if (dict.Count == 0)
            {
                int woodId = Core.ServiceLocator.Get<Data.ItemDataManager>().GetByName("CustomWood").Id;
                dict[woodId] = new ResourceInfo(woodId, 5);
            }

            return dict;
        }

        /// <summary>
        /// 按瓦片名称获取建造资源需求（用于重新创建丢失的建造任务）。
        /// </summary>
        /// <param name="tileName">瓦片名称。</param>
        /// <returns>物品 ID → ResourceInfo 字典，若无法解析则返回空字典。</returns>
        public static Dictionary<int, ResourceInfo> GetBuildCost(string tileName)
        {
            BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(tileName);
            return BuildResourceDict(buildItemData);
        }

        /// <summary>
        /// 建造数据
        /// </summary>
        [Serializable]
        public class BuildMapData
        {
            /// <summary>
            /// 所有建造的地图数据
            /// 使用父类中的PosMap作为选中而未确定的地图数据
            /// </summary>
            public Dictionary<Vector3IntLAB, BuildTileData> PosMap;

            public BuildMapData()
            {
                this.PosMap = new Dictionary<Vector3IntLAB, BuildTileData>();
            }
        }

        /// <summary>
        /// 建造瓦片数据
        /// </summary>
        [Serializable]
        public class BuildTileData
        {
            /// <summary>
            /// 瓦片名称
            /// </summary>
            public string Name;

            /// <summary>
            /// 是否建造完成
            /// </summary>
            public bool IsComplete;

            /// <summary>
            /// 建造者 Worker 名称
            /// </summary>
            public string BuilderName;

            /// <summary>
            /// 所属者 Worker 名称（悬赏发布者，或建造者自己）
            /// </summary>
            public string OwnerName;

            /// <summary>
            /// 不可见碰撞体的标记名称（用于床等物品的第二格阻挡寻路）。
            /// </summary>
            public const string CollisionBlockName = "BedCollision";

            /// <summary>
            /// 物品宽度（用于多格建造物的占用格计算）
            /// </summary>
            public int Width;

            /// <summary>
            /// 物品高度（用于多格建造物的占用格计算）
            /// </summary>
            public int Height;

            /// <summary>
            /// Rect 类型（Center/TopLeft/BottomLeft），用于多格建造物完成时正确计算所有占用格。
            /// 默认 Center，向后兼容旧存档。
            /// </summary>
            public AWorkerTask.RectType RectType;

            public BuildTileData(string name, bool isComplete, int width = 1, int height = 1)
            {
                this.Name = name;
                this.IsComplete = isComplete;
                this.BuilderName = string.Empty;
                this.OwnerName = string.Empty;
                this.Width = width;
                this.Height = height;
                this.RectType = AWorkerTask.RectType.Center;
            }
        }
    }
}
