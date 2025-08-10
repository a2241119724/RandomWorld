namespace LAB2D
{
    using Photon.Pun;
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 地图
    /// </summary>
    public class TileMap : BaseTileMap
    {
        /// <summary>
        /// 瓦片类型
        /// </summary>
        [Serializable]
        public enum MapTileType
        {
            /// <summary>
            /// 默认,不进行渲染
            /// </summary>
            Default,

            /// <summary>
            /// 沙漠
            /// </summary>
            Desert,

            /// <summary>
            /// 沙漠
            /// </summary>
            Marsh,

            /// <summary>
            /// 草
            /// </summary>
            Grass,

            /// <summary>
            /// 雪
            /// </summary>
            Snow,

            /// <summary>
            /// 山
            /// </summary>
            Mountain,

            /// <summary>
            /// 水
            /// </summary>
            Water,
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static TileMap Instance { get; private set; }

        /// <summary>
        /// 地图数据
        /// </summary>
        public TileMapData TileMapDataLAB { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        /// <summary>
        /// 显示地图
        /// </summary>
        /// <param name="mapTiles">所有瓦片</param>
        /// <returns>迭代器</returns>
        public IEnumerator ShowTilemap(MapTileType[,] mapTiles)
        {
            AsyncProgressUI.Instance.SetTip("正在展示地图...");

            // 循环每一个点
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    AsyncProgressUI.Instance.AddOneProcess();
                    this.tilemap.SetTile(new Vector3Int(i, j, 0), (TileBase)ResourceManager.Instance.GetAsset(mapTiles[i, j].ToString()));
                    if (FrameControl.Instance.IsNeedStop(1))
                    {
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// 生成可用的位置，返回数组下标
        /// 可以选择以哪个点为中心，不选择则为所有
        /// 包括TileMap,ResourceMap,BuildMap可达位置
        /// </summary>
        /// <param name="centerMap">中心位置</param>
        /// <returns>位置</returns>
        public Vector3Int GenCanReachPos(Vector3 centerMap = default)
        {
            int x, y, startX = 0, endX = Height, startY = 0, endY = Width;
            if (centerMap != default)
            {
                startX = (int)Mathf.Max(centerMap.x - 20, 0);
                startY = (int)Mathf.Max(centerMap.y - 20, 0);
                endX = (int)Mathf.Min(centerMap.x + 20, Height);
                endY = (int)Mathf.Min(centerMap.y + 20, Width);
            }

            Vector3Int posMap;
            do
            {
                x = UnityEngine.Random.Range(startX, endX);
                y = UnityEngine.Random.Range(startY, endY);
                posMap = new Vector3Int(x, y, 0);
            }
            while (!(this.IsCanReach(posMap) && ResourceMap.Instance.IsCanReach(posMap) && BuildMap.Instance.IsCanReach(posMap)));
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 随机生成地图板块分布(未实例化)
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator Create()
        {
            AsyncProgressUI.Instance.SetTip("正在生成随机坐标...");
            for (int i = 0; i < this.TileMapDataLAB.RandomCount; i++)
            {
                this.TileMapDataLAB.MapTiles[UnityEngine.Random.Range(0, Height), UnityEngine.Random.Range(0, Width)] = (MapTileType)(UnityEngine.Random.Range(2, 14) / 2);
                AsyncProgressUI.Instance.AddOneProcess();
                if (FrameControl.Instance.IsNeedStop(1))
                {
                    yield return null;
                }
            }

            MapTileType[,] tiles = new MapTileType[Height, Width];
            AsyncProgressUI.Instance.SetTip("正在填补地图...");
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    if (FrameControl.Instance.IsNeedStop(1))
                    {
                        yield return null;
                    }

                    AsyncProgressUI.Instance.AddOneProcess();
                    if (this.TileMapDataLAB.MapTiles[i, j] != MapTileType.Default)
                    {
                        tiles[i, j] = this.TileMapDataLAB.MapTiles[i, j];
                        continue;
                    }

                    this.NeighborAndReplaceTiles(tiles, i, j);
                }
            }

            this.TileMapDataLAB.MapTiles = tiles;
            ResourceConstant.IsCompleteTileMap = true;
            this.CreateArroundTile();
            yield return this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
        }

        /// <summary>
        /// MapPos -> WorldPos
        /// </summary>
        /// <param name="posMap">地图位置</param>
        /// <returns>世界位置</returns>
        public Vector3 MapPosToWorldPos(Vector3Int posMap)
        {
            // return new Vector3(posMap.y + 0.5f, posMap.x + 0.5f, 0);
            return new Vector3(posMap.y, posMap.x, 0);
        }

        /// <summary>
        /// WorldPos -> MapPos
        /// </summary>
        /// <param name="worldPos">世界位置</param>
        /// <returns>地图位置</returns>
        public Vector3Int WorldPosToMapPos(Vector3 worldPos)
        {
            // return new Vector3Int(Mathf.RoundToInt(worldPos.y - 0.5f), Mathf.RoundToInt(worldPos.x - 0.5f), 0);
            return new Vector3Int(Mathf.RoundToInt(worldPos.y), Mathf.RoundToInt(worldPos.x), 0);
        }

        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        /// <returns>Map位置</returns>
        public Vector3Int GetMapPosByMouse()
        {
            return this.WorldPosToMapPos(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }

        /// <summary>
        /// 地图索引是否越界
        /// </summary>
        /// <param name="posMap">坐标</param>
        /// <returns>是否</returns>
        public bool IsOverBorder(Vector3Int posMap)
        {
            return !(posMap.x >= 0 && posMap.x < Height && posMap.y >= 0 && posMap.y < Width);
        }

        /// <summary>
        /// 设置进度
        /// </summary>
        /// <param name="height">高度</param>
        /// <param name="width">宽度</param>
        public void SetProgress(int height, int width)
        {
            this.TileMapDataLAB = new TileMapData(height, width, new MapTileType[height, width], width * height / 500);
            Height = height;
            Width = width;
            int total = width * height;
            total += this.TileMapDataLAB.RandomCount;
            total += ((width + height) * 2) + 4;
            total += width * height;
            AsyncProgressUI.Instance.AddTotal(total);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            AsyncProgressUI.Instance.SetTip("加载地图数据...");
            this.TileMapDataLAB = DataTool.LoadDataByBinary<TileMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            ResourceConstant.IsCompleteTileMap = true;
            Height = this.TileMapDataLAB.Height;
            Width = this.TileMapDataLAB.Width;
            this.CreateArroundTile();
            this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));

            // Worker.initMap(Height, Width);
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
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图数据");
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.TileMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图数据");
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
            Height = height;
            Width = width;
            int total = width * height;
            total += ((width + height) * 2) + 4;
            AsyncProgressUI.Instance.AddTotal(total);
        }

        /// <summary>
        /// 以(i,j)为中心,找最近的非默认板块,并赋给当前默认板块
        /// </summary>
        /// <param name="tiles">中心默认板块</param>
        /// <param name="i">中心横坐标</param>
        /// <param name="j">中心纵坐标</param>
        private void NeighborAndReplaceTiles(MapTileType[,] tiles, int i, int j)
        {
            // 寻找离自己最近的非默认板块
            for (int t = 1; t < Width; t++)
            {
                // 第一行
                int k = i - t;
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (this.TileMapDataLAB.MapTiles[k, l] != MapTileType.Default)
                        {
                            tiles[i, j] = this.TileMapDataLAB.MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                }

                // 中间左右两列
                for (++k; k < i + t; k++)
                {
                    int l = j - t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (this.TileMapDataLAB.MapTiles[k, l] != MapTileType.Default)
                        {
                            tiles[i, j] = this.TileMapDataLAB.MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }

                    l = j + t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (this.TileMapDataLAB.MapTiles[k, l] != MapTileType.Default)
                        {
                            tiles[i, j] = this.TileMapDataLAB.MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                }

                // 最后一行
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (this.TileMapDataLAB.MapTiles[k, l] != MapTileType.Default)
                        {
                            tiles[i, j] = this.TileMapDataLAB.MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 地图四周创建山阻止出去
        /// </summary>
        private void CreateArroundTile()
        {
            AsyncProgressUI.Instance.SetTip("创建地图四周...");

            // 上边
            for (int i = -1; i < Width; i++)
            {
                AsyncProgressUI.Instance.AddOneProcess();
                this.tilemap.SetTile(new Vector3Int(Height, i, 0), (TileBase)ResourceManager.Instance.GetAsset(MapTileType.Mountain.ToString()));
            }

            // 右边
            for (int i = 0; i <= Height; i++)
            {
                AsyncProgressUI.Instance.AddOneProcess();
                this.tilemap.SetTile(new Vector3Int(i, Width, 0), (TileBase)ResourceManager.Instance.GetAsset(MapTileType.Mountain.ToString()));
            }

            // 下边
            for (int i = 0; i <= Width; i++)
            {
                AsyncProgressUI.Instance.AddOneProcess();
                this.tilemap.SetTile(new Vector3Int(-1, i, 0), (TileBase)ResourceManager.Instance.GetAsset(MapTileType.Mountain.ToString()));
            }

            // 左边
            for (int i = -1; i < Height; i++)
            {
                AsyncProgressUI.Instance.AddOneProcess();
                this.tilemap.SetTile(new Vector3Int(i, -1, 0), (TileBase)ResourceManager.Instance.GetAsset(MapTileType.Mountain.ToString()));
            }
        }

        /// <summary>
        /// 瓦片数据
        /// </summary>
        [Serializable]
        public class TileMapData
        {
            /// <summary>
            /// 地图纵向长度
            /// </summary>
            public int Height;

            /// <summary>
            /// 地图横向长度
            /// </summary>
            public int Width;

            /// <summary>
            /// 地图瓦片
            /// </summary>
            public MapTileType[,] MapTiles;

            /// <summary>
            /// 随机点数量
            /// </summary>
            public int RandomCount;

            public TileMapData(int height, int width, MapTileType[,] mapTiles, int randomCount)
            {
                this.Height = height;
                this.Width = width;
                this.MapTiles = mapTiles;
                this.RandomCount = randomCount;
            }
        }
    }
}