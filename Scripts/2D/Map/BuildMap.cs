namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 使用alpha判断Worker是否可以通行
    /// 使用Collider会出现, Worker寻路完成, 但路径上的Tile被建造完成，导致不能通行
    /// 从半创建直接就不可通行
    /// </summary>
    public class BuildMap : BaseTileMap
    {
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
            this.BuildMapDataLAB = new BuildMapData();
        }

        /// <summary>
        /// Color a 0.5f代表有碰撞体，0.49f代表没有碰撞体，
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        /// <param name="tile">瓦片</param>
        /// <param name="isPass">是否可通过</param>
        /// <returns>建造地图</returns>
        public BuildMap AddBuilding(Vector3Int targetMap, TileBase tile, bool isPass = false)
        {
            this.tilemap.SetTile(targetMap, tile);
            this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
            this.tilemap.SetColor(targetMap, new Color(1, 1, 1, isPass ? 0.49f : 0.5f));
            Vector3IntLAB vector3IntLAB = Vector3IntLAB.ToVector3IntLAB(targetMap);
            if (!this.BuildMapDataLAB.TargetMaps.ContainsKey(vector3IntLAB))
            {
                this.BuildMapDataLAB.TargetMaps.Add(vector3IntLAB, tile.name);
            }

            this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)), true, true);
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
            this.tilemap.SetTile(targetMap, tile);
            if (isPass)
            {
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }

            this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)), tile.name, isPass, false);
            return this;
        }

        /// <summary>
        /// 没有碰撞体的最后Color a为0.99f
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        public void SetComplete(Vector3Int targetMap)
        {
            if (this.tilemap.GetColor(targetMap).a == 0.5f)
            {
                this.tilemap.SetColliderType(targetMap, Tile.ColliderType.Sprite);
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 1));
                this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)), string.Empty, false, false);
            }
            else
            {
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
                this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)), string.Empty, true, false);
            }

            RoomManager.Instance.Complete(targetMap);
        }

        /// <summary>
        /// 是否正在建造
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <returns>是否</returns>
        public bool IsBuilding(Vector3Int target)
        {
            return this.tilemap.GetColor(target).a < 1.0f;
        }

        /// <summary>
        /// 删除正在建造
        /// </summary>
        /// <param name="targetMap">建造目标</param>
        public void CancelBuilding(Vector3Int targetMap)
        {
            this.tilemap.SetTile(targetMap, null);
            this.BuildMapDataLAB.TargetMaps.Remove(Vector3IntLAB.ToVector3IntLAB(targetMap));
            this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(targetMap)), string.Empty, false, false, true);
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
            foreach (Vector3IntLAB targetMap in this.BuildMapDataLAB.TargetMaps.Keys)
            {
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                WorkerTaskManager.Instance.AddTask(new WorkerBuildTask.BuildTaskBuilder().SetBuildPos(Vector3IntLAB.ToVector3Int(targetMap))
                    .SetNeedResource(new Dictionary<int, ResourceInfo>(resourceInfos)).Build());
            }

            this.BuildMapDataLAB.TargetMaps.Clear();
        }

        /// <summary>
        /// 是否可以通行,Worker寻路时使用
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public override bool IsCanReach(Vector3Int posMap)
        {
            // 门可以通行
            return Mathf.Abs(this.tilemap.GetColor(posMap).a - 0.49f) < 1e-5
                || Mathf.Abs(this.tilemap.GetColor(posMap).a - 0.99f) < 1e-5
                || this.IsFreeTile(posMap);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图建造数据");
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.BuildMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图建造数据");
            BuildMapData buildMapData = DataTool.FromByteArray<BuildMapData>(data);
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = buildMapData.PosMaps.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator.Current.Key),
                    (TileBase)ResourceManager.Instance.GetAsset(enumerator.Current.Value));
            }

            Dictionary<Vector3IntLAB, string>.Enumerator enumerator1 = buildMapData.TargetMaps.GetEnumerator();
            while (enumerator1.MoveNext())
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator1.Current.Key),
                    (TileBase)ResourceManager.Instance.GetAsset(enumerator1.Current.Value));
            }
        }

        /// <summary>
        /// 同步地图建造数据
        /// </summary>
        /// <param name="vector3IntLAB">位置</param>
        /// <param name="tileBaseName">瓦片名称</param>
        /// <param name="isPass">是否可以通过</param>
        /// <param name="isBuilding">是否正在建造</param>
        /// <param name="isDelete">是否删除</param>
        [PunRPC]
        public void SyncDataResp(byte[] vector3IntLAB, string tileBaseName, bool isPass = false, bool isBuilding = false, bool isDelete = false)
        {
            LogManager.Instance.Log("Response: 同步地图建造数据");
            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            if (!tileBaseName.Equals(string.Empty))
            {
                this.tilemap.SetTile(vector3Int, ResourceManager.Instance.GetAsset(tileBaseName));
            }

            if (isPass)
            {
                this.tilemap.RemoveTileFlags(vector3Int, TileFlags.LockColor);
            }

            if (isBuilding)
            {
                this.tilemap.SetColor(vector3Int, new Color(1, 1, 1, 0.5f));
            }
        }

        /// <summary>
        /// 建造数据
        /// </summary>
        [Serializable]
        public class BuildMapData : ATileMapData
        {
            /// <summary>
            /// 正在建造的地图坐标
            /// </summary>
            public Dictionary<Vector3IntLAB, string> TargetMaps;

            public BuildMapData()
            {
                this.TargetMaps = new Dictionary<Vector3IntLAB, string>();
            }
        }
    }
}
