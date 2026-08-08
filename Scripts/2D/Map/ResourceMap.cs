namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Item;
    using LAB2D.Serializable;
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
            this.resourceTileMapOne = LAB2D.Tool.Tool.GetComponentInChildren<Tilemap>(this.transform.parent.gameObject, "ResourceMapOne");
            this.ResourceMapDataLAB = new ResourceMapData(0, 100);
        }

        /// <summary>
        /// 生成资源
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator GenResource()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete);
            if (!Core.ServiceLocator.TryGet(out TileMap tm) || tm.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMap data not available, cannot generate resources", LogManager.LogLevelEnum.Error);
                yield break;
            }

            // 确保进度总数已注册（幂等，重复调用安全）
            this.SetProgress();

            Core.GameServices.AsyncProgressSetTipProvider("生成资源...");
            int resourcesPlaced = 0;
            int assetMissCount = 0;
            for (int i = 0; i < tm.TileMapDataLAB.Height; i++)
            {
                for (int j = 0; j < tm.TileMapDataLAB.Width; j++)
                {
                    Core.GameServices.AsyncProgressAddOneProvider();
                    if (Core.ServiceLocator.Get<FrameControl>().IsNeedStop(1))
                    {
                        yield return null;
                    }

                    Vector3Int posMap = new (i, j, 0);
                    int terrainId = tm.TileMapDataLAB.MapTiles[i, j];
                    if (tm.IsCanReach(posMap)
                        && Core.ServiceLocator.Get<TerrainConfigDatabase>().CanSpawnResources(terrainId)
                        && UnityEngine.Random.Range(0.0f, 1.0f) > 0.95f)
                    {
                        TileBase tileBase = Core.ServiceLocator.Get<ResourceManager>().GetAssetByTerrainId(terrainId);
                        if (tileBase == null)
                        {
                            assetMissCount++;
                            continue;
                        }

                        this.tilemap.SetTile(posMap, tileBase);
                        this.ResourceMapDataLAB.Add(posMap, tileBase.name);
                        resourcesPlaced++;
                    }
                }
            }

            if (resourcesPlaced == 0)
            {
                AWorkerTask.LogProvider($"GenResource: no resources placed (asset misses: {assetMissCount})", LogManager.LogLevelEnum.Warning);
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
            yield return new WaitUntil(() => Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete);
            while (true)
            {
                if (this.ResourceMapDataLAB.TreeCurCount < this.ResourceMapDataLAB.TreeTotalCount)
                {
                    if (!Core.ServiceLocator.TryGet(out TileMap tm) || tm.TileMapDataLAB == null)
                    {
                        AWorkerTask.LogProvider("TileMap data not available, tree generation paused", LogManager.LogLevelEnum.Error);
                        yield return new WaitForSeconds(60.0f * 5);
                        continue;
                    }

                    Vector3Int pos = Core.ServiceLocator.Get<IsAvailableMap>().GenAvailablePosMap();
                    int terrainId = tm.TileMapDataLAB.MapTiles[pos.x, pos.y];
                    if (!Core.ServiceLocator.Get<TerrainConfigDatabase>().CanGrowTrees(terrainId))
                    {
                        yield return null;
                        continue;
                    }

                    TileBase tileBase = Core.ServiceLocator.Get<ResourceManager>().GetAssetByTerrainId(terrainId);
                    if (tileBase == null)
                    {
                        yield return null;
                        continue;
                    }

                    this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(pos)), tileBase.name, false);

                    this.ResourceMapDataLAB.TreeCurCount++;
                    this.tilemap.SetTile(pos, tileBase);
                    this.ResourceMapDataLAB.Add(pos, tileBase.name);

                    if (this.TryGetGatherResourceInfo(pos, out ResourceInfo resourceInfo))
                    {
                        Core.ServiceLocator.Get<WorkerTaskManager>().AddTask(
                            new WorkerGatherTask.GatherTaskBuilder()
                            .SetTarget(pos).SetResourceInfo(resourceInfo).Build(), new GameGridPosition(pos.x, pos.y, pos.z));
                    }

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
            this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, false, true);

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
        /// 尝试获取可采集资源的信息。
        /// </summary>
        /// <param name="posMap">资源位置</param>
        /// <param name="resourceInfo">资源信息</param>
        /// <returns>该位置是否有可采集资源</returns>
        public bool TryGetGatherResourceInfo(Vector3Int posMap, out ResourceInfo resourceInfo)
        {
            resourceInfo = null;
            TileBase tileBase = this.GetTile(posMap);
            if (tileBase == null)
            {
                return false;
            }

            if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(tileBase.name, out ItemData itemData))
            {
                // 资源存在于 ResourceMap 但未在 ItemDataManager 注册（如 DesertGrass），
                // 仍允许采集，使用 Id=0 走默认掉落
                itemData = ItemData.Empty;
            }

            resourceInfo = new ResourceInfo(itemData.Id);
            return true;
        }

        private bool isProgressSet;

        /// <summary>
        /// 设置进度条（幂等，重复调用安全）
        /// </summary>
        public void SetProgress()
        {
            if (this.isProgressSet)
            {
                return;
            }

            if (!Core.ServiceLocator.TryGet(out TileMap tm) || tm.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMap data not initialized, cannot set resource generation progress", LogManager.LogLevelEnum.Error);
                return;
            }

            this.isProgressSet = true;
            Core.GameServices.AsyncProgressAddTotalProvider(tm.TileMapDataLAB.Height * tm.TileMapDataLAB.Width);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            Core.GameServices.AsyncProgressSetTipProvider("加载资源地图信息...");
            this.ResourceMapDataLAB = DataTool.LoadDataByBinary<ResourceMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (this.ResourceMapDataLAB == null)
            {
                // 降级方案：存档无资源数据时，等待地图生成完毕后自动生成资源
                AWorkerTask.LogProvider("ResourceMap data not found in archive, will generate new resources", LogManager.LogLevelEnum.Warning);
                this.ResourceMapDataLAB = new ResourceMapData(0, 100);
                this.SetProgress();
                this.StartCoroutine(this.GenResource());
                return;
            }

            foreach (KeyValuePair<Vector3IntLAB, string> posMap in this.ResourceMapDataLAB.PosMap)
            {
                this.tilemap.SetTile(Vector3IntLAB.ToVector3Int(posMap.Key), (TileBase)AWorkerTask.ResourceLoadProvider(posMap.Value));
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
            AWorkerTask.LogProvider("Request: 同步地图资源数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.ResourceMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            AWorkerTask.LogProvider("Response: 同步地图资源数据", LogManager.LogLevelEnum.Trace);
            this.SetProgress();
            ResourceMapData resourceMapData = DataTool.FromByteArray<ResourceMapData>(data);
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = resourceMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator.Current.Key),
                    (TileBase)AWorkerTask.ResourceLoadProvider(enumerator.Current.Value));
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
            AWorkerTask.LogProvider("Response: 同步地图资源数据", LogManager.LogLevelEnum.Trace);
            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            if (!tileBaseName.Equals(string.Empty))
            {
                this.tilemap.SetTile(vector3Int, (TileBase)AWorkerTask.ResourceLoadProvider(tileBaseName));
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
