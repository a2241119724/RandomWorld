using UnityEngine;
using Photon.Pun;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

namespace LAB2D
{
    public class TileMap : MonoBehaviour
    {
        public static TileMap Instance { private set; get; }
        public int Height { set; get; } // 地图纵向长度
        public int Width { set; get; }  // 地图横向长度
        public Tiles[,] MapTiles { private set; get; } // 地图瓦片
        public int RandomCount { get; private set; } // 随机点的数量
        public Tilemap Tilemap { get; set; }

        //private readonly int SEND_QUANTITY = 10000; // 一次发送数量
        //private bool isOnce = false; // 是否创建完成
        //private int sendIndex = 0; // 发送数据的索引
        //private bool isSyncing; // 是否正在发送所有地图数据

        private void Awake()
        {
            Instance = this;
            Tilemap = GetComponent<Tilemap>();
        }

        private void Start()
        {
            //PhotonNetwork.ConnectUsingSettings();
            Tool.master(() =>
            {
                RandomCount = Width * Height / 1000;
                AsyncProgressUI.Instance.show();
                MapTiles = new Tiles[Height, Width];
                StartCoroutine(create());
            });
        }

        public IEnumerator showTilemap(Tiles[,] mapTiles)
        {
            AsyncProgressUI.Instance.setTip("正在展示地图...");
            int count = 0;
            for (int i = 0; i < Height; i++) // 循环每一个点
            {
                for (int j = 0; j < Width; j++)
                {
                    AsyncProgressUI.Instance.addOneProcess();
                    try
                    {
                        Tilemap.SetTile(new Vector3Int(i, j, 0), (TileBase)ResourcesManager.Instance.getAsset(mapTiles[i, j].ToString()));
                    }catch(KeyNotFoundException e)
                    {
                        Debug.Log(e.Data);
                        Debug.Log("没有key:" + mapTiles[i, j].ToString());
                    }
                    if (count++ > 10000)
                    {
                        count = 0;
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
        /// 判断该坐标是否可用 
        /// </summary>
        /// <param name="x">横坐标</param>
        /// <param name="y">纵坐标</param>
        /// <returns></returns>
        public bool isAvailableTile(Vector3Int posMap)
        {
            if (posMap.x < 0 || posMap.x >= Height || posMap.y < 0 || posMap.y >= Width) return false;
            if (MapTiles[posMap.x, posMap.y] == Tiles.Mountain)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 生成可用的位置，返回数组下标
        /// </summary>
        /// <returns></returns>
        public Vector3Int genAvailablePosMap(Vector3 centerMap=default(Vector3))
        {
            int x, y, startX=0, endX=Height, startY = 0, endY = Width;
            if (centerMap != default(Vector3))
            {
                startX = (int)Mathf.Max(centerMap.x - 100, 0);
                startY = (int)Mathf.Max(centerMap.y - 100, 0);
                endX = (int)Mathf.Min(centerMap.x + 100, Height);
                endY = (int)Mathf.Min(centerMap.y + 100, Width);
            }
            do
            {
                x = Random.Range(startX, endX);
                y = Random.Range(startY, endY);
            } while (!isAvailableTile(new Vector3Int(x,y,0)));
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 以(i,j)为中心,找最近的非默认板块,并赋给当前默认板块
        /// </summary>
        /// <param name="tiles">中心默认板块</param>
        /// <param name="i">中心横坐标</param>
        /// <param name="j">中心纵坐标</param>
        protected void NeighborAndReplaceTiles(Tiles[,] tiles, int i, int j)
        {
            for (int t = 1; t < Width; t++) // 寻找离自己最近的非默认板块
            {
                // 第一行
                int k = i - t;
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != Tiles.Default)
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
                        if (MapTiles[k, l] != Tiles.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // 赋给当前未初始化板块
                            return;
                        }
                    }
                    l = j + t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != Tiles.Default)
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
                        if (MapTiles[k, l] != Tiles.Default)
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
        protected IEnumerator create()
        {
            AsyncProgressUI.Instance.setTip("正在生成随机坐标...");
            for (int i = 0; i < RandomCount; i++) // 生成随机坐标
            {
                MapTiles[Random.Range(0, Height), Random.Range(0, Width)] = (Tiles)Random.Range(1, 6);
                AsyncProgressUI.Instance.addOneProcess();
                if (i % 1000 == 0)
                {
                    yield return null;
                }
            }
            Tiles[,] tiles = new Tiles[Height, Width];
            if (tiles == null)
            {
                Debug.LogError("tiles assign resource Error!!!");
                yield break;
            }
            AsyncProgressUI.Instance.setTip("正在填补地图...");
            for (int i = 0; i < Height; i++) // 循环每一个点
            {
                for (int j = 0; j < Width; j++)
                {
                    if ((i * Width + j) % 50000 == 0)
                    {
                        yield return null;
                    }
                    AsyncProgressUI.Instance.addOneProcess();
                    if (MapTiles[i, j] != Tiles.Default)
                    {
                        tiles[i, j] = MapTiles[i, j];
                        continue;
                    }
                    NeighborAndReplaceTiles(tiles, i, j);
                }
            }
            MapTiles = tiles;
            Worker.setMap();
            createArroundTile();
            yield return StartCoroutine(showTilemap(MapTiles));
            GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG_ROOT).GetComponent<EnemyGenerator>().enabled = true;
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
                Tilemap.SetTile(new Vector3Int(Height, i, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
            // 右边
            for (int i = 0; i <= Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(i, Width, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
            // 下边
            for (int i = 0; i <= Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(-1, i, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
            // 左边
            for (int i = -1; i < Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(i, -1, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
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
    }
    public enum Tiles
    {
        Default, // 默认,不进行渲染
        Desert, // 沙漠
        Marsh, // 沼泽
        Grass, // 草
        Snow, // 雪
        Mountain, // 山
        Build
    }
}