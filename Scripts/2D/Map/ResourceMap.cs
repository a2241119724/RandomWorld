namespace LAB2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 资源地图
    /// </summary>
    public class ResourceMap : BaseTileMap
    {
        private Tilemap resourceTileMapOne; // 仅占一格的资源，解决遮盖问题

        /// <summary>
        /// 单例
        /// </summary>
        public static ResourceMap Instance { get; private set; }

        /// <summary>
        /// 资源地图数据
        /// </summary>
        public ResourceMapData ResourceMapDataLAB { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.resourceTileMapOne = Tool.GetComponentInChildren<Tilemap>(this.transform.parent.gameObject, "ResourceMapOne");
            this.ResourceMapDataLAB = new ResourceMapData(0, 100);
        }

        /// <summary>
        /// 生成资源
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator GenResource()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Lock.IsCompleteTileMap);
            AsyncProgressUI.Instance.SetTip("生成资源...");
            for (int i = 0; i < TileMap.Instance.TileMapDataLAB.Height; i++)
            {
                for (int j = 0; j < TileMap.Instance.TileMapDataLAB.Width; j++)
                {
                    AsyncProgressUI.Instance.AddOneProcess();
                    if (FrameControl.Instance.IsNeedStop(1))
                    {
                        yield return null;
                    }

                    Vector3Int posMap = new (i, j, 0);
                    if (TileMap.Instance.IsCanReach(posMap) && UnityEngine.Random.Range(0.0f, 1.0f) > 0.9f)
                    {
                        TileMap.MapTileTypeEnum tileType = TileMap.Instance.TileMapDataLAB.MapTiles[i, j];
                        TileBase tileBase = ResourceManager.Instance.GetAssetByTileType(tileType);
                        if (tileBase == null)
                        {
                            continue;
                        }

                        this.tilemap.SetTile(posMap, tileBase);
                        this.ResourceMapDataLAB.Add(posMap, tileBase.name);
                        if (tileBase.name.Contains("Tree"))
                        {
                            // WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                            //     .setTarget(posMap).setGatherName("Tree").build());
                            this.ResourceMapDataLAB.TreeCurCount++;
                        }
                    }
                }
            }

            yield return this.StartCoroutine(this.GenTree());
        }

        /// <summary>
        /// 动态生成树
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator GenTree()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Lock.IsCompleteTileMap);
            while (true)
            {
                if (this.ResourceMapDataLAB.TreeCurCount < this.ResourceMapDataLAB.TreeTotalCount)
                {
                    Vector3Int pos = IsAvailableMap.Instance.GenAvailablePosMap();
                    TileMap.MapTileTypeEnum tileType = TileMap.Instance.TileMapDataLAB.MapTiles[pos.x, pos.y];
                    TileBase tileBase = ResourceManager.Instance.GetAssetByTileType(tileType, "Tree");
                    if (tileBase == null)
                    {
                        yield return null;
                        continue;
                    }

                    this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(pos)), tileBase.name, false);
                    this.ResourceMapDataLAB.TreeCurCount++;
                    this.tilemap.SetTile(pos, tileBase);
                    this.ResourceMapDataLAB.Add(pos, tileBase.name);

                    // WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                    //     .setTarget(pos).setGatherName("Tree").build());
                    this.RefreshRound(pos);
                }

                yield return new WaitForSeconds(60.0f * 5);
            }
        }

        /// <summary>
        /// 砍树
        /// </summary>
        /// <param name="posMap">位置</param>
        public void CutTree(Vector3Int posMap)
        {
            this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, false, true);
            this.ResourceMapDataLAB.Remove(posMap);
            this.tilemap.SetTile(posMap, null);
            this.ResourceMapDataLAB.TreeCurCount--;
        }

        /// <summary>
        /// 获取瓦片
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>瓦片</returns>
        public override TileBase GetTile(Vector3Int posMap)
        {
            TileBase tileBase = this.tilemap.GetTile(posMap);
            if (tileBase == null)
            {
                tileBase = this.resourceTileMapOne.GetTile(posMap);
            }

            return tileBase;
        }

        /// <summary>
        /// 设置进度条
        /// </summary>
        public void SetProgress()
        {
            AsyncProgressUI.Instance.AddTotal(TileMap.Instance.TileMapDataLAB.Height * TileMap.Instance.TileMapDataLAB.Width);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            AsyncProgressUI.Instance.SetTip("加载资源地图信息...");
            this.ResourceMapDataLAB = DataTool.LoadDataByBinary<ResourceMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            foreach (KeyValuePair<Vector3IntLAB, string> posMap in this.ResourceMapDataLAB.PosMap)
            {
                this.tilemap.SetTile(Vector3IntLAB.ToVector3Int(posMap.Key), (TileBase)ResourceManager.Instance.GetAsset(posMap.Value));
            }

            this.StartCoroutine(this.GenTree());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.ResourceMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图资源数据");
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.ResourceMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图资源数据");
            this.SetProgress();
            ResourceMapData resourceMapData = DataTool.FromByteArray<ResourceMapData>(data);
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = resourceMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator.Current.Key),
                    (TileBase)ResourceManager.Instance.GetAsset(enumerator.Current.Value));
            }
        }

        /// <summary>
        /// 同步地图资源数据
        /// </summary>
        /// <param name="vector3IntLAB">位置</param>
        /// <param name="tileBaseName">瓦片名称</param>
        /// <param name="isPass">是否可以通过</param>
        /// <param name="isDelete">是否删除</param>
        [PunRPC]
        public void SyncDataResp(byte[] vector3IntLAB, string tileBaseName, bool isPass = false, bool isDelete = false)
        {
            LogManager.Instance.Log("Response: 同步地图资源数据");
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
        }

        /// <summary>
        /// 生成新的资源时，刷新map,防止遮盖错误
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径</param>
        private void RefreshRound(Vector3Int center, int radius = 4)
        {
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    this.tilemap.RefreshTile(VectorTool.Add(center, i, j));
                }
            }
        }

        /// <summary>
        /// 资源数据
        /// </summary>
        [Serializable]
        public class ResourceMapData : ATileMapData
        {
            /// <summary>
            /// 当前树的数量
            /// </summary>
            public int TreeCurCount;

            /// <summary>
            /// 树的总数
            /// </summary>
            public int TreeTotalCount;

            public ResourceMapData(int treeCurCount, int treeTotalCount)
            {
                this.TreeCurCount = treeCurCount;
                this.TreeTotalCount = treeTotalCount;
            }
        }
    }
}
