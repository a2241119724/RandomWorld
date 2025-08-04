namespace LAB2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
        public ResourceMapData ResourceMapDataLAB { get; set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.resourceTileMapOne = Tool.GetComponentInChildren<Tilemap>(this.transform.parent.gameObject, "ResourceMapOne");
            this.ResourceMapDataLAB = new ResourceMapData(0, 100);
        }

        /// <summary>
        /// 生成资源，添加采摘任务
        /// </summary>
        /// <param name="coroutine">需要等待该协程执行完后再执行</param>
        /// <returns>迭代器</returns>
        public IEnumerator GenResource(Coroutine coroutine = null)
        {
            yield return coroutine;
            AsyncProgressUI.Instance.SetTip("生成资源...");
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    AsyncProgressUI.Instance.AddOneProcess();
                    if (FrameControl.Instance.IsNeedStop(1))
                    {
                        yield return null;
                    }

                    Vector3Int posMap = new (i, j, 0);
                    if (TileMap.Instance.IsCanReach(posMap) && UnityEngine.Random.Range(0.0f, 1.0f) > 0.9f)
                    {
                        TileMap.MapTileType tileType = TileMap.Instance.MapTiles[i, j];
                        TileBase tileBase = ResourceManager.Instance.GetAssetByTileType(tileType);
                        if (tileBase == null)
                        {
                            continue;
                        }

                        this.tilemap.SetTile(posMap, tileBase);
                        this.ResourceMapDataLAB.Add(posMap, tileBase.name);
                        if (tileBase.name.Contains("Tree"))
                        {
                            this.ResourceMapDataLAB.TreeCurCount++;

                            // WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                            //     .setTarget(posMap).setGatherName("Tree").build());
                        }
                    }
                }
            }

            while (true)
            {
                if (this.ResourceMapDataLAB.TreeCurCount < this.ResourceMapDataLAB.TreeTotalCount)
                {
                    Vector3Int pos = IsAvailableMap.Instance.GenAvailablePosMap();
                    TileMap.MapTileType tileType = TileMap.Instance.MapTiles[pos.x, pos.y];
                    TileBase tileBase = ResourceManager.Instance.GetAssetByTileType(tileType, "Tree");
                    if (tileBase == null)
                    {
                        yield return null;
                        continue;
                    }

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
            AsyncProgressUI.Instance.AddTotal(Height * Width);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            this.ResourceMapDataLAB = Tool.LoadDataByBinary<ResourceMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            foreach (KeyValuePair<Vector3IntLAB, string> posMap in this.ResourceMapDataLAB.PosMaps)
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(posMap.Key),
                    (TileBase)ResourceManager.Instance.GetAsset(posMap.Value));
            }

            this.StartCoroutine(this.GenResource());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            Tool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.ResourceMapDataLAB);
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
                    this.tilemap.RefreshTile(Tool.Add(center, i, j));
                }
            }
        }

        /// <summary>
        /// 资源数据
        /// </summary>
        [Serializable]
        public class ResourceMapData
        {
            /// <summary>
            /// 当前树的数量
            /// </summary>
            public int TreeCurCount;

            /// <summary>
            /// 树的总数
            /// </summary>
            public int TreeTotalCount;

            /// <summary>
            /// string:TileBase
            /// </summary>
            public Dictionary<Vector3IntLAB, string> PosMaps;

            public ResourceMapData(int treeCurCount, int treeTotalCount)
            {
                this.TreeCurCount = treeCurCount;
                this.TreeTotalCount = treeTotalCount;
                this.PosMaps = new Dictionary<Vector3IntLAB, string>();
            }

            /// <summary>
            /// 删
            /// </summary>
            /// <param name="pos">位置</param>
            public void Remove(Vector3Int pos)
            {
                this.PosMaps.Remove(Vector3IntLAB.ToVector3IntLAB(pos));
            }

            /// <summary>
            /// 添加
            /// </summary>
            /// <param name="pos">位置</param>
            /// <param name="tileBase">瓦片</param>
            public void Add(Vector3Int pos, string tileBase)
            {
                this.PosMaps.Add(Vector3IntLAB.ToVector3IntLAB(pos), tileBase);
            }

            /// <summary>
            /// 包含
            /// </summary>
            /// <param name="pos">位置</param>
            /// <returns>是否</returns>
            public bool ContainKey(Vector3Int pos)
            {
                return this.PosMaps.ContainsKey(Vector3IntLAB.ToVector3IntLAB(pos));
            }
        }
    }

    /// <summary>
    /// 可序列化的Vector3Int
    /// </summary>
    [Serializable]
    public class Vector3IntLAB
    {
        /// <summary>
        /// X
        /// </summary>
        public int X;

        /// <summary>
        /// Y
        /// </summary>
        public int Y;

        /// <summary>
        /// Z
        /// </summary>
        public int Z;

        public Vector3IntLAB(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Vector3IntLAB to Vector3Int
        /// </summary>
        /// <param name="vector3IntLAB">Vector3IntLAB</param>
        /// <returns>Vector3Int</returns>
        public static Vector3Int ToVector3Int(Vector3IntLAB vector3IntLAB)
        {
            return new Vector3Int(vector3IntLAB.X, vector3IntLAB.Y, vector3IntLAB.Z);
        }

        /// <summary>
        /// Vector3Int to Vector3IntLAB
        /// </summary>
        /// <param name="vector3Int">Vector3Int</param>
        /// <returns>Vector3IntLAB</returns>
        public static Vector3IntLAB ToVector3IntLAB(Vector3Int vector3Int)
        {
            return new Vector3IntLAB(vector3Int.x, vector3Int.y, vector3Int.z);
        }
    }
}
