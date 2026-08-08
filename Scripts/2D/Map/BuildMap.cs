namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Item;
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
        /// <returns>链式</returns>
        public BuildMap AddBuild(Vector3Int targetMap, string tileName)
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
                    .SetNeedResource(buildCosts).Build(), new GameGridPosition(targetMap.x, targetMap.y, targetMap.z));

                // 设置可通过并且颜色变淡
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
                this.tilemap.SetColor(targetMap, this.initColor);
            }

            BuildTileData buildTileData = new BuildTileData(tileName, !isNeedBuild);
            if (this.BuildMapDataLAB.PosMap.ContainsKey(vector3IntLAB))
            {
                // 门
                this.BuildMapDataLAB.PosMap[vector3IntLAB] = buildTileData;
            }
            else
            {
                this.BuildMapDataLAB.PosMap.Add(vector3IntLAB, buildTileData);
            }

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
        /// <returns>true = 预约成功；false = 位置已被占用</returns>
        public bool ReserveBuildPosition(Vector3Int targetMap, string tileName)
        {
            Vector3IntLAB vector3IntLAB = Vector3IntLAB.ToVector3IntLAB(targetMap);

            // 位置已被其他 Worker 预约（IsComplete=false）或已完成建造 → 拒绝覆盖
            if (this.BuildMapDataLAB.PosMap.TryGetValue(vector3IntLAB, out var existing))
            {
                AWorkerTask.LogProvider(
                    $"ReserveBuildPosition: 位置已被占用 tile={existing.Name} complete={existing.IsComplete}",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            // 放置瓦片（视觉反馈）
            this.tilemap.SetTile(targetMap, (TileBase)AWorkerTask.ResourceLoadProvider(tileName));

            // 设置为建造中外观（半透明、无碰撞）
            this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
            this.tilemap.SetColor(targetMap, this.initColor);

            // 注册到 PosMap（供碰撞检查和 IsBuilding 查询）
            BuildTileData buildTileData = new BuildTileData(tileName, false);
            this.BuildMapDataLAB.PosMap.Add(vector3IntLAB, buildTileData);

            // 同步
            this.SyncSender.Broadcast(
                "SyncDataResp",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                DataTool.ToByteArray(buildTileData));

            return true;
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
        /// 是否可以通行,Worker寻路时使用
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public override bool IsCanReach(Vector3Int posMap)
        {
            if (this.IsFreeTile(posMap))
            {
                return true;
            }

            if (this.BuildMapDataLAB == null || this.BuildMapDataLAB.PosMap == null)
            {
                return true;
            }

            if (!this.BuildMapDataLAB.PosMap.ContainsKey(Vector3IntLAB.ToVector3IntLAB(posMap)))
            {
                return true;
            }

            BuildTileData buildTileData = this.BuildMapDataLAB.PosMap[Vector3IntLAB.ToVector3IntLAB(posMap)];
            if (buildTileData == null)
            {
                return true;
            }

            try
            {
                BuildItemData buildItemData = Core.ServiceLocator.Get<ItemDataManager>().GetBuildItemDataByName(buildTileData.Name);
                return buildItemData != null && buildItemData.IsPass;
            }
            catch (System.InvalidCastException)
            {
                // 非 BuildItemData 的 tile（如 Bounty 任务栏标记），默认可通行
                return true;
            }
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
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            BuildTileData buildTileData = DataTool.FromByteArray<BuildTileData>(buildTileDataLAB);
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
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            this.BuildMapDataLAB = DataTool.LoadDataByBinary<BuildMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name)) ?? new BuildMapData();
            foreach (var posMap in this.BuildMapDataLAB.PosMap)
            {
                this.DoDirectBuild(Vector3IntLAB.ToVector3Int(posMap.Key), (TileBase)AWorkerTask.ResourceLoadProvider(posMap.Value.Name));
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

            public BuildTileData(string name, bool isComplete)
            {
                this.Name = name;
                this.IsComplete = isComplete;
                this.BuilderName = string.Empty;
                this.OwnerName = string.Empty;
            }
        }
    }
}
