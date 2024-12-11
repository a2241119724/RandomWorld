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
        public int Height { set; get; } // ��ͼ���򳤶�
        public int Width { set; get; }  // ��ͼ���򳤶�
        public Tiles[,] MapTiles { private set; get; } // ��ͼ��Ƭ
        public int RandomCount { get; private set; } // ����������
        public Tilemap Tilemap { get; set; }

        //private readonly int SEND_QUANTITY = 10000; // һ�η�������
        //private bool isOnce = false; // �Ƿ񴴽����
        //private int sendIndex = 0; // �������ݵ�����
        //private bool isSyncing; // �Ƿ����ڷ������е�ͼ����

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
            AsyncProgressUI.Instance.setTip("����չʾ��ͼ...");
            int count = 0;
            for (int i = 0; i < Height; i++) // ѭ��ÿһ����
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
            if (posMap.x < 0 || posMap.x >= Height || posMap.y < 0 || posMap.y >= Width) return false;
            if (MapTiles[posMap.x, posMap.y] == Tiles.Mountain)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// ���ɿ��õ�λ�ã����������±�
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
        /// ��(i,j)Ϊ����,������ķ�Ĭ�ϰ��,��������ǰĬ�ϰ��
        /// </summary>
        /// <param name="tiles">����Ĭ�ϰ��</param>
        /// <param name="i">���ĺ�����</param>
        /// <param name="j">����������</param>
        protected void NeighborAndReplaceTiles(Tiles[,] tiles, int i, int j)
        {
            for (int t = 1; t < Width; t++) // Ѱ�����Լ�����ķ�Ĭ�ϰ��
            {
                // ��һ��
                int k = i - t;
                for (int l = j - t; l <= j + t; l++)
                {
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != Tiles.Default)
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
                        if (MapTiles[k, l] != Tiles.Default)
                        {
                            tiles[i, j] = MapTiles[k, l]; // ������ǰδ��ʼ�����
                            return;
                        }
                    }
                    l = j + t;
                    if (k >= 0 && k < Height && l >= 0 && l < Width)
                    {
                        if (MapTiles[k, l] != Tiles.Default)
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
                        if (MapTiles[k, l] != Tiles.Default)
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
        protected IEnumerator create()
        {
            AsyncProgressUI.Instance.setTip("���������������...");
            for (int i = 0; i < RandomCount; i++) // �����������
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
        /// ��ͼ���ܴ���ɽ��ֹ��ȥ
        /// </summary>
        private void createArroundTile()
        {
            AsyncProgressUI.Instance.setTip("����Χס����...");
            // �ϱ�
            for (int i = -1; i < Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(Height, i, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
            // �ұ�
            for (int i = 0; i <= Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(i, Width, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
            // �±�
            for (int i = 0; i <= Width; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(-1, i, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
            // ���
            for (int i = -1; i < Height; i++)
            {
                AsyncProgressUI.Instance.addOneProcess();
                Tilemap.SetTile(new Vector3Int(i, -1, 0), (TileBase)ResourcesManager.Instance.getAsset("Mountain"));
            }
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
    }
    public enum Tiles
    {
        Default, // Ĭ��,��������Ⱦ
        Desert, // ɳĮ
        Marsh, // ����
        Grass, // ��
        Snow, // ѩ
        Mountain, // ɽ
        Build
    }
}