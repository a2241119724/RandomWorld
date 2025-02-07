using UnityEngine;
using Photon.Pun;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using System;

namespace LAB2D
{
    public class TileMap : AMonoSaveData
    {
        public static TileMap Instance { private set; get; }
        public int Height { set; get; } // ��ͼ���򳤶�
        public int Width { set; get; }  // ��ͼ���򳤶�
        public TileType[,] MapTiles { set; get; } // ��ͼ��Ƭ
        public int RandomCount { get; set; } // ����������
        
        private Tilemap tilemap { get; set; }

        //private readonly int SEND_QUANTITY = 10000; // һ�η�������
        //private bool isOnce = false; // �Ƿ񴴽����
        //private int sendIndex = 0; // �������ݵ�����
        //private bool isSyncing; // �Ƿ����ڷ������е�ͼ����

        private void Awake()
        {
            Instance = this;
            tilemap = GetComponent<Tilemap>();
        }

        public IEnumerator showTilemap(TileType[,] mapTiles)
        {
            AsyncProgressUI.Instance.setTip("����չʾ��ͼ...");
            int count = 0;
            for (int i = 0; i < Height; i++) // ѭ��ÿһ����
            {
                for (int j = 0; j < Width; j++)
                {
                    AsyncProgressUI.Instance.addOneProcess();
                    try
                    {
                        tilemap.SetTile(new Vector3Int(i, j, 0), (TileBase)ResourcesManager.Instance.getAsset(mapTiles[i, j].ToString()));
                    }catch(KeyNotFoundException e)
                    {
                        Debug.Log(e.Data);
                        Debug.Log("û��key:" + mapTiles[i, j].ToString());
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
        /// ʹ���¼�����û�����������Master��������
        /// </summary>
        public void initData()
        {
            //isSyncing = true;
            //sendIndex = 0;
        }

        /// <summary>
        /// �жϸ������Ƿ���� 
        /// </summary>
        /// <param name="x">������</param>
        /// <param name="y">������</param>
        /// <returns></returns>
        public bool isAvailableTile(Vector3Int posMap)
        {
            //if (posMap.x < 0 || posMap.x >= Height || posMap.y < 0 || posMap.y >= Width) return false;
            //if (MapTiles[posMap.x, posMap.y] == Tiles.Mountain)
            //{
            //    return false;
            //}
            //return true;
            return tilemap.GetColliderType(posMap) == Tile.ColliderType.None;
            //return tilemap.GetTile(posMap) != ResourcesManager.Instance.getAsset(TileType.Mountain.ToString());
        }

        /// <summary>
        /// ���ɿ��õ�λ�ã����������±�
        /// ����ѡ�����ĸ���Ϊ���ģ���ѡ����Ϊ����
        /// </summary>
        /// <returns></returns>
        public Vector3Int genAvailablePosMap(Vector3 centerMap=default(Vector3))
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
            } while (!isAvailableTile(new Vector3Int(x,y,0)));
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// ��(i,j)Ϊ����,������ķ�Ĭ�ϰ��,��������ǰĬ�ϰ��
        /// </summary>
        /// <param name="tiles">����Ĭ�ϰ��</param>
        /// <param name="i">���ĺ�����</param>
        /// <param name="j">����������</param>
        protected void NeighborAndReplaceTiles(TileType[,] tiles, int i, int j)
        {
            for (int t = 1; t < Width; t++) // Ѱ�����Լ�����ķ�Ĭ�ϰ��
            {
                // ��һ��
                int k = i - t;
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // ������ǰδ��ʼ�����
                            return;
                        }
                    }
                }
                // �м���������
                for (++k; k < i + t; k++)
                {
                    int l = j - t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // ������ǰδ��ʼ�����
                            return;
                        }
                    }
                    l = j + t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // ������ǰδ��ʼ�����
                            return;
                        }
                    }
                }
                // ���һ��
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != TileType.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // ������ǰδ��ʼ�����
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ������ɵ�ͼ���ֲ�(δʵ����)
        /// </summary>
        /// <returns></returns>
        public IEnumerator create()
        {
            AsyncProgressUI.Instance.setTip("���������������...");
            for (int i = 0; i < RandomCount; i++) // �����������
            {
                MapTiles[UnityEngine.Random.Range(0, Height), UnityEngine.Random.Range(0, Width)] = (TileType)(UnityEngine.Random.Range(2, 12) / 2);
                AsyncProgressUI.Instance.addOneProcess();
                if (i % 1000 == 0)
                {
                    yield return null;
                }
            }
            TileType[,] tiles = new TileType[Height, Width];
            if (tiles == null)
            {
                Debug.LogError("tiles assign resource Error!!!");
                yield break;
            }
            AsyncProgressUI.Instance.setTip("�������ͼ...");
            for (int i = 0; i < Height; i++) // ѭ��ÿһ����
            {
                for (int j = 0; j < Width; j++)
                {
                    if ((i * Width + j) % 50000 == 0)
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
        /// ��ͼ���ܴ���ɽ��ֹ��ȥ
        /// </summary>
        private void createArroundTile()
        {
            AsyncProgressUI.Instance.setTip("����Χס����...");
            // �ϱ�
            for (int i = -1; i < Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(Height, i, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
            // �ұ�
            for (int i = 0; i <= Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(i, Width, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
            // �±�
            for (int i = 0; i <= Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                tilemap.SetTile(new Vector3Int(-1, i, 0), (TileBase)ResourcesManager.Instance.getAsset(TileType.Mountain.ToString()));
            }
            // ���
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
            return new Vector3(posMap.y + 0.5f, posMap.x + 0.5f, 0);
        }

        /// <summary>
        /// WorldPos -> MapPos
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Vector3Int worldPosToMapPos(Vector3 worldPos)
        {
            return new Vector3Int(Mathf.RoundToInt(worldPos.y - 0.5f), Mathf.RoundToInt(worldPos.x - 0.5f), 0);
        }

        /// <summary>
        /// ��ͼ�����Ƿ�Խ��
        /// </summary>
        /// <param name="x">��ʵ����</param>
        /// <param name="y">��ʵ����</param>
        /// <returns></returns>
        public bool isOverBorder(int x, int y)
        {
            return !(x >= 0 && x < Height && y >= 0 && y < Width);
        }

        public TileBase getTile(Vector3Int pos) {
            return tilemap.GetTile(pos);
        }

        public override void loadData()
        {
            base.loadData();
            TileMapData data = Tool.loadDataByBinary<TileMapData>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            Height = data.Height;
            Width = data.Width;
            MapTiles = data.MapTiles;
            RandomCount = data.RandomCount;
            createArroundTile();
            StartCoroutine(showTilemap(MapTiles));
            StartCoroutine(EnemyCreator.Instance.genEnemy());
            //Worker.initMap(Height, Width);
        }

        public override void saveData()
        {
            base.saveData();
            TileMapData tileMapData = new TileMapData(Height,Width,MapTiles,RandomCount);
            Tool.saveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), tileMapData);
        }

        ///// <summary>
        ///// ��������
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
        //        // ÿ����ɫ����ֻ����һ��ȫ����Ϣ
        //        if (!isOnce)
        //        {
        //            isOnce = true;
        //            MapTiles = new Tiles[Length, Width];
        //            createArroundTile();
        //        }
        //        // ���ƽ��������ݽ���һ��,������ظ����ݾͲ�������
        //        if (MapTiles[Length - 1, Width - 1] == Tiles.Default && stream.PeekNext() is byte[])
        //        {
        //            DeserializeTiles(stream);
        //        }
        //    }
        //}

        ///// <summary>
        ///// ���л���ͼ
        ///// Э��LAB_1
        ///// ÿ�η���2 + sendQuantity
        ///// ǰ2���ֽڱ�ʶ�����ͼ����ʼ����ϵ��[start,(start + 1))
        ///// [start * sendQuantity,(start + 1) * sendQuantity)
        ///// </summary>
        //private void SerializeTiles(PhotonStream stream)
        //{
        //    // ÿ�η���1000����ͼ����
        //    byte[] tiles = new byte[SEND_QUANTITY + 2];
        //    // ǰ���ֽڷ����ݷ�Χ(С�˴洢)
        //    tiles[0] = (byte)(sendIndex % (1 << 8));
        //    tiles[1] = (byte)(sendIndex / (1 << 8));
        //    int temp = 2; // �Ӷ���ʼ������
        //    int len = (sendIndex + 1) * SEND_QUANTITY;
        //    int total = Width * Length;
        //    for (int i = sendIndex * SEND_QUANTITY; i < len; i++)
        //    {
        //        // ���û�г������䴰�ڣ���˴�Ϊ�������һ��
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
        ///// �����л���ͼ
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
        //            // �����¼�������
        //            PlayerManager.Instance.create();
        //            return;
        //        }
        //    }
        //}

        [Serializable]
        public class TileMapData {
            public int Height { set; get; } // ��ͼ���򳤶�
            public int Width { set; get; }  // ��ͼ���򳤶�
            public TileType[,] MapTiles { set; get; } // ��ͼ��Ƭ
            public int RandomCount { get; set; } // ����������

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
        Default, // Ĭ��,��������Ⱦ
        Desert, // ɳĮ
        Marsh, // ����
        Grass, // ��
        Snow, // ѩ
        Mountain, // ɽ
        Water, // ˮ
    }
}