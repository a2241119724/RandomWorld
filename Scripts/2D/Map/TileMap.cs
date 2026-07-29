namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 地图 — 同时实现 ITileMapQuery 以支持其他层通过接口查询地图。
    ///
    /// 地形类型完全由 TerrainConfigDatabase 数据驱动
    /// （Resources/SO/TerrainConfigs/ 下的 TerrainTileConfig .asset 文件）。
    /// </summary>
    public class TileMap : BaseTileMap, ITileMapQuery
    {
        /// <summary>
        /// 默认地图高度（存档不可用时的回退尺寸）
        /// </summary>
        public const int DefaultHeight = 548;

        /// <summary>
        /// 默认地图宽度（存档不可用时的回退尺寸）
        /// </summary>
        public const int DefaultWidth = 548;

        /// <summary>
        /// 单例
        /// </summary>
        public static TileMap Instance { get; private set; }

        /// <summary>
        /// 地图数据
        /// </summary>
        public TileMapData TileMapDataLAB { get; private set; }

        /// <summary>
        /// 地形生成策略（默认使用 RandomScatterFillGenerator）
        /// </summary>
        private ITerrainGenerator generator;

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;

            if (!ServiceLocator.TryGet(out this.generator))
            {
                this.generator = new RandomScatterFillGenerator();
            }
        }

        /// <summary>
        /// 显示地图 — 通过 TerrainConfigDatabase 查找每个瓦片的资源名。
        /// </summary>
        /// <param name="mapTiles">地形 ID 二维数组。</param>
        /// <returns>迭代器</returns>
        public IEnumerator ShowTilemap(int[,] mapTiles)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在展示地图...");

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int height = mapTiles.GetLength(0);
            int width = mapTiles.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Core.GameServices.AsyncProgressAddOneProvider();
                    int terrainId = mapTiles[i, j];
                    string resourceName = db.GetTileResourceName(terrainId);
                    if (!string.IsNullOrEmpty(resourceName) && terrainId != 0)
                    {
                        this.tilemap.SetTile(new Vector3Int(i, j, 0), (TileBase)AWorkerTask.ResourceLoadProvider(resourceName));
                    }

                    if (Core.ServiceLocator.Get<FrameControl>().IsNeedStop(1))
                    {
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// 最大重试次数，防止无限循环
        /// </summary>
        private const int GEN_POS_MAX_RETRIES = 10000;

        /// <summary>
        /// 生成可用的位置，返回数组下标
        /// 可以选择以哪个点为中心，不选择则为所有
        /// 包括TileMap,ResourceMap,BuildMap可达位置
        /// </summary>
        /// <param name="centerMap">中心位置</param>
        /// <returns>位置</returns>
        public Vector3Int GenCanReachPos(Vector3 centerMap = default)
        {
            if (this.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMapDataLAB is null, cannot generate reachable position", LogManager.LogLevelEnum.Error);
                return Vector3Int.zero;
            }

            int x, y, startX = 0, endX = this.TileMapDataLAB.Height, startY = 0, endY = this.TileMapDataLAB.Width;
            if (centerMap != default)
            {
                startX = (int)System.Math.Max(centerMap.x - 20, 0);
                startY = (int)System.Math.Max(centerMap.y - 20, 0);
                endX = (int)System.Math.Min(centerMap.x + 20, this.TileMapDataLAB.Height);
                endY = (int)System.Math.Min(centerMap.y + 20, this.TileMapDataLAB.Width);
            }

            int retries = 0;
            Vector3Int posMap;
            do
            {
                x = UnityEngine.Random.Range(startX, endX);
                y = UnityEngine.Random.Range(startY, endY);
                posMap = new Vector3Int(x, y, 0);
                retries++;
                if (retries > GEN_POS_MAX_RETRIES)
                {
                    AWorkerTask.LogProvider(
                        $"GenCanReachPos exceeded max retries ({GEN_POS_MAX_RETRIES}), returning fallback position",
                        LogManager.LogLevelEnum.Error);
                    return new Vector3Int(startX, startY, 0);
                }
            }
            while (!(this.IsCanReach(posMap) && Core.ServiceLocator.Get<ResourceMap>().IsCanReach(posMap) && Core.ServiceLocator.Get<BuildMap>().IsCanReach(posMap)));
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 随机生成地图板块分布(未实例化)。
        /// 委托给 ITerrainGenerator 策略执行具体算法。
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator Create()
        {
            int height = this.TileMapDataLAB.Height;
            int width = this.TileMapDataLAB.Width;
            int randomCount = this.TileMapDataLAB.RandomCount;

            // Step 1: 散布种子点
            int[,] tiles = new int[height, width];
            yield return this.StartCoroutine(this.generator.ScatterSeeds(tiles, randomCount, height, width));

            // Step 2: 填充空白区域
            yield return this.StartCoroutine(this.generator.Fill(tiles, height, width));

            this.TileMapDataLAB.MapTiles = tiles;
            this.CreateArroundTile();
            yield return this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
            Core.ServiceLocator.Get<MapInitCoordinator>().IsComplete = true;
        }

        /// <summary>
        /// 地图坐标转世界坐标
        /// </summary>
        public Vector3 MapPosToWorldPos(Vector3Int posMap)
        {
            return new Vector3(posMap.y, posMap.x, 0);
        }

        /// <summary>
        /// 地图坐标转世界坐标
        /// </summary>
        public Vector3 MapPosToWorldPos(Vector3IntLAB posMap)
        {
            return new Vector3(posMap.Y, posMap.X, 0);
        }

        /// <summary>
        /// 地图坐标转世界坐标
        /// </summary>
        public Vector3 MapPosToWorldPos(Vector2ShortLAB posMap)
        {
            return new Vector3(posMap.Y, posMap.X, 0);
        }

        /// <summary>
        /// 世界坐标转地图坐标
        /// </summary>
        public Vector3Int WorldPosToMapPos(Vector3 worldPos)
        {
            return new Vector3Int(MathHelper.RoundToInt(worldPos.y), MathHelper.RoundToInt(worldPos.x), 0);
        }

        // === ITileMapQuery 接口实现 ===

        /// <inheritdoc/>
        GameGridPosition ITileMapQuery.WorldPosToMapPos(GameVector2 worldPos)
        {
            Vector3 unityPos = new Vector3(worldPos.X, worldPos.Y, 0);
            Vector3Int mapPos = this.WorldPosToMapPos(unityPos);
            return new GameGridPosition(mapPos.x, mapPos.y);
        }

        /// <inheritdoc/>
        bool ITileMapQuery.IsCanReach(GameGridPosition posMap)
        {
            return this.IsCanReach(new Vector3Int(posMap.X, posMap.Y, 0));
        }

        /// <inheritdoc/>
        int ITileMapQuery.Width => this.TileMapDataLAB?.Width ?? 0;

        /// <inheritdoc/>
        int ITileMapQuery.Height => this.TileMapDataLAB?.Height ?? 0;

        /// <inheritdoc/>
        bool ITileMapQuery.IsInBounds(GameGridPosition posMap)
        {
            return posMap.X >= 0 && posMap.X < this.TileMapDataLAB?.Height
                && posMap.Y >= 0 && posMap.Y < this.TileMapDataLAB?.Width;
        }

        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        public Vector3Int GetMapPosByMouse()
        {
            return this.WorldPosToMapPos(UnityGlobalInputAdapter.GetMouseWorldPosition(Camera.main));
        }

        /// <summary>
        /// 地图索引是否越界
        /// </summary>
        public bool IsOverBorder(Vector3Int posMap)
        {
            return !(posMap.x >= 0 && posMap.x < this.TileMapDataLAB.Height && posMap.y >= 0 && posMap.y < this.TileMapDataLAB.Width);
        }

        /// <summary>
        /// 设置进度
        /// </summary>
        public void SetProgress(int height, int width)
        {
            this.TileMapDataLAB = new TileMapData(height, width, new int[height, width], width * height / 2000);
            int total = width * height;
            total += this.TileMapDataLAB.RandomCount;
            total += ((width + height) * 2) + 4;
            total += width * height;
            Core.GameServices.AsyncProgressAddTotalProvider(total);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            Core.GameServices.AsyncProgressSetTipProvider("加载地图数据...");
            this.TileMapDataLAB = DataTool.LoadDataByBinary<TileMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (this.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMap data not found in archive, generating new default map", LogManager.LogLevelEnum.Warning);
                Core.ServiceLocator.Get<MapInitCoordinator>().IsComplete = false;
                this.SetProgress(DefaultHeight, DefaultWidth);
                this.StartCoroutine(this.Create());
                return;
            }

            Core.ServiceLocator.Get<MapInitCoordinator>().IsComplete = true;
            this.CreateArroundTile();
            this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.TileMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            AWorkerTask.LogProvider("Request: 同步地图数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.TileMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            AWorkerTask.LogProvider("Response: 同步地图数据", LogManager.LogLevelEnum.Trace);
            this.TileMapDataLAB = DataTool.FromByteArray<TileMapData>(data);
            this.SetProgressAsync(this.TileMapDataLAB.MapTiles.GetLength(0), this.TileMapDataLAB.MapTiles.GetLength(1));
            this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
            this.CreateArroundTile();
        }

        /// <summary>
        /// 同步数据设置进度
        /// </summary>
        private void SetProgressAsync(int height, int width)
        {
            this.TileMapDataLAB.Height = height;
            this.TileMapDataLAB.Width = width;
            int total = width * height;
            total += ((width + height) * 2) + 4;
            Core.GameServices.AsyncProgressAddTotalProvider(total);
        }

        /// <summary>
        /// 地图四周创建边界地形阻止出去。
        /// 边界地形由 TerrainTileConfig.isBorder 标记决定。
        /// </summary>
        private void CreateArroundTile()
        {
            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int borderId = db.GetBorderTerrainId();
            string borderResourceName = db.GetTileResourceName(borderId);

            if (string.IsNullOrEmpty(borderResourceName))
            {
                AWorkerTask.LogProvider("CreateArroundTile: 没有配置边界地形（isBorder），跳过。", LogManager.LogLevelEnum.Warning);
                return;
            }

            Core.GameServices.AsyncProgressSetTipProvider("创建地图四周...");

            TileBase borderTile = (TileBase)AWorkerTask.ResourceLoadProvider(borderResourceName);

            // 上边
            for (int i = -1; i < this.TileMapDataLAB.Width; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(this.TileMapDataLAB.Height, i, 0), borderTile);
            }

            // 右边
            for (int i = 0; i <= this.TileMapDataLAB.Height; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(i, this.TileMapDataLAB.Width, 0), borderTile);
            }

            // 下边
            for (int i = 0; i <= this.TileMapDataLAB.Width; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(-1, i, 0), borderTile);
            }

            // 左边
            for (int i = -1; i < this.TileMapDataLAB.Height; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(i, -1, 0), borderTile);
            }
        }

        /// <summary>
        /// 瓦片数据 — 使用 int 存储地形 ID（map 到 TerrainTileConfig.terrainId）。
        /// </summary>
        [Serializable]
        public class TileMapData
        {
            public int Height;
            public int Width;

            /// <summary>
            /// 地图瓦片 — 每个值为地形 ID（0 = 未初始化/不渲染）。
            /// </summary>
            public int[,] MapTiles;

            public int RandomCount;

            public TileMapData(int height, int width, int[,] mapTiles, int randomCount)
            {
                this.Height = height;
                this.Width = width;
                this.MapTiles = mapTiles;
                this.RandomCount = randomCount;
            }
        }
    }
}
