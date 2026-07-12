namespace LAB2D.Map
{
    using LAB2D;
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
        private Dictionary<int, ResourceInfo> resourceInfos; // TODO 需要的建筑材料
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
            this.resourceInfos = new ()
            {
                {
                    ItemDataManager.Instance.GetByName("CustomWood").Id,
                    new ResourceInfo(ItemDataManager.Instance.GetByName("CustomWood").Id, 5)
                },
            };
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
            BuildItemData buildItemData = ItemDataManager.Instance.GetBuildItemDataByName(tileName);
            this.tilemap.SetTile(targetMap, ResourceManager.Instance.GetAsset(tileName));
            if (buildItemData.IsNeedBuild)
            {
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                WorkerTaskManager.Instance.AddTask(
                    new WorkerBuildTask.BuildTaskBuilder().SetBuildPos(targetMap)
                    .SetNeedResource(new Dictionary<int, ResourceInfo>(this.resourceInfos)).Build(), Vector3IntLAB.ToVector3IntLAB(targetMap));

                // 设置可通过并且颜色变淡
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
                this.tilemap.SetColor(targetMap, this.initColor);
            }

            BuildTileData buildTileData = new BuildTileData(tileName, !buildItemData.IsNeedBuild);
            if (this.BuildMapDataLAB.PosMap.ContainsKey(vector3IntLAB))
            {
                // 门
                this.BuildMapDataLAB.PosMap[vector3IntLAB] = buildTileData;
            }
            else
            {
                this.BuildMapDataLAB.PosMap.Add(vector3IntLAB, buildTileData);
            }

            if (NetworkConnect.Instance.IsOnline)
            {
                this.PhotonView.RPC(
                    "SyncDataResp",
                    RpcTarget.Others,
                    DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                    DataTool.ToByteArray(buildTileData));
            }

            return this;
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
        /// 完成建造,设置颜色透明度为1,不可通过的添加碰撞体
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        public void SetComplete(Vector3IntLAB targetMap)
        {
            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(targetMap);
            BuildTileData buildTileData = this.BuildMapDataLAB.PosMap[targetMap];
            buildTileData.IsComplete = true;
            RoomManager.Instance.Complete(vector3Int);
            BuildItemData buildItemData = ItemDataManager.Instance.GetBuildItemDataByName(buildTileData.Name);
            this.tilemap.SetColor(vector3Int, Color.white);
            if (!buildItemData.IsPass)
            {
                this.tilemap.SetColliderType(vector3Int, Tile.ColliderType.Sprite);
            }

            if (NetworkConnect.Instance.IsOnline)
            {
                this.PhotonView.RPC(
                    "SyncDataResp",
                    RpcTarget.Others,
                    DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(vector3Int)),
                    default);
            }
        }

        /// <summary>
        /// 是否正在建造
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <returns>是否</returns>
        public bool IsBuilding(Vector3Int target)
        {
            return !this.BuildMapDataLAB.PosMap[Vector3IntLAB.ToVector3IntLAB(target)].IsComplete;
        }

        /// <summary>
        /// 删除正在建造
        /// </summary>
        /// <param name="targetMap">建造目标</param>
        public void CancelBuilding(Vector3Int targetMap)
        {
            this.tilemap.SetTile(targetMap, null);
            this.BuildMapDataLAB.PosMap.Remove(Vector3IntLAB.ToVector3IntLAB(targetMap));
            if (NetworkConnect.Instance.IsOnline)
            {
                this.PhotonView.RPC(
                "SyncDataResp",
                RpcTarget.Others,
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                default,
                true);
            }
        }

        /// <summary>
        /// 添加建造任务到地图
        /// </summary>
        public void AddTask()
        {
            Dictionary<int, ResourceInfo> resourceInfos = new ();
            resourceInfos.Add(
                ItemDataManager.Instance.GetByName("CustomWood").Id,
                new ResourceInfo(ItemDataManager.Instance.GetByName("CustomWood").Id, 5));
            foreach (Vector3IntLAB targetMap in this.BuildMapDataLAB.PosMap.Keys)
            {
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                WorkerTaskManager.Instance.AddTask(
                    new WorkerBuildTask.BuildTaskBuilder().SetBuildPos(Vector3IntLAB.ToVector3Int(targetMap))
                    .SetNeedResource(new Dictionary<int, ResourceInfo>(resourceInfos)).Build(), targetMap);
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

            if (!this.BuildMapDataLAB.PosMap.ContainsKey(Vector3IntLAB.ToVector3IntLAB(posMap)))
            {
                return true;
            }

            return ItemDataManager.Instance.GetBuildItemDataByName(
                this.BuildMapDataLAB.PosMap[Vector3IntLAB.ToVector3IntLAB(posMap)].Name).IsPass;
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图建造数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.BuildMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图建造数据", LogManager.LogLevelEnum.Trace);
            BuildMapData buildMapData = DataTool.FromByteArray<BuildMapData>(data);
            Dictionary<Vector3IntLAB, BuildTileData>.Enumerator enumerator = buildMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(enumerator.Current.Key);
                this.tilemap.SetTile(vector3Int, ResourceManager.Instance.GetAsset(enumerator.Current.Value.Name));
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
            LogManager.Instance.Log("Response: 同步地图建造数据", LogManager.LogLevelEnum.Trace);
            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            BuildTileData buildTileData = DataTool.FromByteArray<BuildTileData>(buildTileDataLAB);
            if (!buildTileData.Name.Equals(string.Empty))
            {
                this.tilemap.SetTile(vector3Int, ResourceManager.Instance.GetAsset(buildTileData.Name));
            }

            BuildItemData buildItemData = ItemDataManager.Instance.GetBuildItemDataByName(buildTileData.Name);
            if (buildTileData.IsComplete)
            {
                this.tilemap.SetColor(vector3Int, Color.white);
                if (!buildItemData.IsPass)
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
                this.DoDirectBuild(Vector3IntLAB.ToVector3Int(posMap.Key), ResourceManager.Instance.GetAsset(posMap.Value.Name));
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

            if (NetworkConnect.Instance.IsOnline)
            {
                this.PhotonView.RPC(
                "SyncDataResp",
                RpcTarget.Others,
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)),
                tile.name,
                isPass,
                false);
            }

            return this;
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

            public BuildTileData(string name, bool isComplete)
            {
                this.Name = name;
                this.IsComplete = isComplete;
            }
        }
    }
}
