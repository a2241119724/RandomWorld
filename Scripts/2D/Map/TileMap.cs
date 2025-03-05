using UnityEngine;
using Photon.Pun;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using System;

namespace LAB2D
{
    public class TileMap : BaseTileMap
    {
        public static TileMap Instance { private set; get; }
        public TileType[,] MapTiles { set; get; } // 地图瓦片
        
        private int randomCount { get; set; } // 随机点的数量
        //private readonly int SEND_QUANTITY = 10000; // 一次发送数量
        //private bool isOnce = false; // 是否创建完成
        //private int sendIndex = 0; // 发送数据的索引
        //private bool isSyncing; // 是否正在发送所有地图数据

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public IEnumerator showTilemap(TileType[,] mapTiles)
        {
            AsyncProgressUI.Instance.setTip("正在展示地图...");
            for (int i = 0; i < Height; i++) // 循环每一个点
            {
                for (int j = 0; j < Width; j++)
                {
                    AsyncProgressUI.Instance.addOneProcess();
                    tilemap.SetTile(new Vector3Int(i, j, 0), (TileBase)ResourcesManager.Instance.getAsset(mapTiles[i, j].ToString()));
                    if (FrameControl.Instance.isNeedStop(1))
                    {
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// 使得新加入的用户调用来控制Master发送数据
        /// </summary>
        public void initData()
        {
            //isSyncing = true;
            //sendIndex = 0;
        }

        /// <summary>
        /// 生成可用的位置，返回数组下标
        /// 可以选择以哪个点为中心，不选择则为所有
        /// </summary>
        /// <returns></returns>
        public Vector3Int genCanReachPos(Vector3 centerMap=default(Vector3))
        {
            int x, y, startX=0, endX=Height, startY = 0, endY = Width;
            if (centerMap != default(Vector3))
            {
                startX = (int)Mathf.Max(centerMap.x - 50, 0);
                startY = (int)Mathf.Max(centerMap.y - 50, 0);
                endX = (int)Mathf.Min(centerMap.x + 50, Height);
                endY = (int)Mathf.Min(centerMap.y + 50, Width);
            }
            do
            {
                x = UnityEngine.Random.Range(startX, endX);
                y = UnityEngine.Random.Range(startY, endY);
            } while (!isCanReach(new Vector3Int(x,y,0)));
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 以(i,j)为中心,找最近的非默认板块,并赋给当前默认板块
        /// </summary>
        /// <param name="tiles">中心默认板块</param>
        /// <param name="i">中心横坐标</param>
        /// <param name="j">中心纵坐标</param>
        protected void NeighborAndReplaceTiles(TileType[,] tiles, int i, int j)
        {
            for (int t = 1; t < Width; t++) // 寻找离自己最近的非默认板块
            {
                // 第一行
                int k = i - t;
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // 赋给当前未初始化板块
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
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                    l = j + t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                }
                // 最后一行
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 随机生成地图板块分布(未实例化)
        /// </summary>
        /// <returns></returns>
        public IEnumerator create()
        {
            AsyncProgressUI.Instance.setTip("正在生成随机坐标...");
            for (int i = 0; i < randomCount; i++) // 生成随机坐标
            {
                MapTiles[UnityEngine.Random.Range(0, Height), UnityEngine.Random.Range(0, Width)] = (TileType)(UnityEngine.Random.Range(2, 14) / 2);
                AsyncProgressUI.Instance.addOneProcess();
                if (FrameControl.Instance.isNeedStop(1))
                {
                    yield return null;
                }
            }
            TileType[,] tiles = new TileType[Height, Width];
            if (tiles == null)
            {
                LogManager.Instance.log("tiles assign resource Error!!!", LogManager.LogLevel.Error);
                yield break;
            }
            AsyncProgressUI.Instance.setTip("正在填补地图...");
            for (int i = 0; i < Height; i++) // 循环每一个点
            {
                for (int j = 0; j < Width; j++)
                {
                    if (FrameControl.Instance.isNeedStop(1))
                    {
                        yield return null;
                    }
                    AsyncProgressUI.Instance.addOneProcess();
                    if (MapTiles[i, j] != TileType.Default)
                    {
                        tiles[i, j] = MapTiles[i, j];
                        continue;
                    }
                    NeighborAndReplaceTiles(tiles, i, j);
                }
            }
            MapTiles = tiles;
            createArroundTile();
            StartCoroutine(showTilemap(MapTiles));
            StartCoroutine(EnemyCreator.Instance.genEnemy());
        }

        /// <summary>
        /// 地图四周创建山阻止出去
        /// </summary>
        private void createArroundTile()
        {
            AsyncProgressUI.Instance.setTip("正在围住四周...");
            // 上边
            for (int i = -1; i < Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(Height, i, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
            // 右边
            for (int i = 0; i <= Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(i, Width, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
            // 下边
            for (int i = 0; i <= Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(-1, i, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
            // 左边
            for (int i = -1; i < Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(i, -1, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
        }

        /// <summary>
        /// MapPos -> WorldPos 
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public Vector3 mapPosToWorldPos(Vector3Int posMap) {
            return new Vector3(posMap.y, posMap.x, 0);
            //return new Vector3(posMap.y + 0.5f, posMap.x + 0.5f, 0);
        }

        /// <summary>
        /// WorldPos -> MapPos
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Vector3Int worldPosToMapPos(Vector3 worldPos)
        {
            return new Vector3Int(Mathf.RoundToInt(worldPos.y), Mathf.RoundToInt(worldPos.x), 0);
            //return new Vector3Int(Mathf.RoundToInt(worldPos.y - 0.5f), Mathf.RoundToInt(worldPos.x - 0.5f), 0);
        }

        /// <summary>
        /// 地图索引是否越界
        /// </summary>
        /// <param name="x">真实坐标</param>
        /// <param name="y">真实坐标</param>
        /// <returns></returns>
        public bool isOverBorder(int x, int y)
        {
            return !(x >= 0 && x < Height && y >= 0 && y < Width);
        }

        public void setProgress(int height, int width)
        {
            Height = height;
            Width = width;
            randomCount = width * height / 500;
            MapTiles = new TileType[height, width];
            int total = width * height;
            total += randomCount;
            total += (width + height) * 2 + 4;
            total += width * height;
            AsyncProgressUI.Instance.addTotal(total);
        }

        public int getCanReachCount()
        {
            int count = 0;
            for (int i = 0; i < Height; i++) // 循环每一个点
            {
                for (int j = 0; j < Width; j++)
                {
                    if (MapTiles[i, j] == TileType.Mountain || MapTiles[i, j] == TileType.Water) continue;
                    count++;
                }
            }
            return count;
        }

        public override void loadData()
        {
            base.loadData();
            TileMapData data = Tool.loadDataByBinary<TileMapData>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            Height = data.Height;
            Width = data.Width;
            MapTiles = data.MapTiles;
            randomCount = data.RandomCount;
            createArroundTile();
            StartCoroutine(showTilemap(MapTiles));
            StartCoroutine(EnemyCreator.Instance.genEnemy());
            //Worker.initMap(Height, Width);
        }

        public override void saveData()
        {
            base.saveData();
            TileMapData tileMapData = new TileMapData(Height,Width,MapTiles,randomCount);
            Tool.saveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), tileMapData);
        }

        ///// <summary>
        ///// 传输数据
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="info"></param>
        //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        //{
        //    if (stream.IsWriting)
        //    {
        //        Tool.master(() =>
        //        {
        //            if (isSyncing)
        //            {
        //                stream.SendNext(Length);
        //                stream.SendNext(Width);
        //                SerializeTiles(stream);
        //            }
        //        });
        //    }
        //    else
        //    {
        //        Length = (int)stream.ReceiveNext();
        //        Width = (int)stream.ReceiveNext();
        //        // 每个角色加入只接收一次全局信息
        //        if (!isOnce)
        //        {
        //            isOnce = true;
        //            MapTiles = new Tiles[Length, Width];
        //            createArroundTile();
        //        }
        //        // 控制将所有数据接收一边,后面的重复数据就不接收了
        //        if (MapTiles[Length - 1, Width - 1] == Tiles.Default && stream.PeekNext() is byte[])
        //        {
        //            DeserializeTiles(stream);
        //        }
        //    }
        //}

        ///// <summary>
        ///// 序列化地图
        ///// 协议LAB_1
        ///// 每次发送2 + sendQuantity
        ///// 前2个字节标识传输地图的起始索引系数[start,(start + 1))
        ///// [start * sendQuantity,(start + 1) * sendQuantity)
        ///// </summary>
        //private void SerializeTiles(PhotonStream stream)
        //{
        //    // 每次发送1000个地图数据
        //    byte[] tiles = new byte[SEND_QUANTITY + 2];
        //    // 前两字节放数据范围(小端存储)
        //    tiles[0] = (byte)(sendIndex % (1 << 8));
        //    tiles[1] = (byte)(sendIndex / (1 << 8));
        //    int temp = 2; // 从而开始存数据
        //    int len = (sendIndex + 1) * SEND_QUANTITY;
        //    int total = Width * Length;
        //    for (int i = sendIndex * SEND_QUANTITY; i < len; i++)
        //    {
        //        // 如果没有充满传输窗口，则此次为传输最后一次
        //        if (i >= total)
        //        {
        //            stream.SendNext(tiles);
        //            isSyncing = false;
        //            return;
        //        }
        //        tiles[temp++] = (byte)MapTiles[i / Width, i % Width];
        //    }
        //    stream.SendNext(tiles);
        //    ++sendIndex;
        //}

        ///// <summary>
        ///// 反序列化地图
        ///// </summary>
        //private void DeserializeTiles(PhotonStream stream)
        //{
        //    byte[] tiles = (byte[])stream.ReceiveNext();
        //    int temp = 2;
        //    int start = tiles[1] * (1 << 8) + tiles[0];
        //    int len = (start + 1) * SEND_QUANTITY;
        //    int total = Width * Length;
        //    for (int i = start * SEND_QUANTITY; i < len; i++)
        //    {
        //        MapTiles[i / Width, i % Width] = (Tiles)tiles[temp++];
        //        if (i == (total - 1))
        //        {
        //            // 创建新加入的玩家
        //            PlayerManager.Instance.create();
        //            return;
        //        }
        //    }
        //}

        [Serializable]
        public class TileMapData {
            public int Height { set; get; } // 地图纵向长度
            public int Width { set; get; }  // 地图横向长度
            public TileType[,] MapTiles { set; get; } // 地图瓦片
            public int RandomCount { get; set; } // 随机点的数量

            public TileMapData(int height, int width, TileType[,] mapTiles, int randomCount)
            {
                Height = height;
                Width = width;
                MapTiles = mapTiles;
                RandomCount = randomCount;
            }
        }
    }
    [Serializable]
    public enum TileType
    {
        Default, // 默认,不进行渲染
        Desert, // 沙漠
        Marsh, // 沼泽
        Grass, // 草
        Snow, // 雪
        Mountain, // 山
        Water, // 水
    }
}