using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class ResourceMap : BaseTileMap
    {
        public static ResourceMap Instance { private set; get; }
        public ResourceMapData ResourceMapDataLAB { get; set; }

        /// <summary>
        /// 仅占一格的资源，解决遮盖问题
        /// </summary>
        private Tilemap resourceTileMapOne;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            resourceTileMapOne = Tool.GetComponentInChildren<Tilemap>(transform.parent.gameObject, "ResourceMapOne");
            ResourceMapDataLAB = new ResourceMapData(0,100);
        }

        /// <summary>
        /// 生成资源，添加采摘任务
        /// </summary>
        /// <param name="coroutine">需要等待该协程执行完后再执行</param>
        /// <returns></returns>
        public IEnumerator genResource(Coroutine coroutine = null) {
            yield return coroutine;
            AsyncProgressUI.Instance.setTip("生成资源...");
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    AsyncProgressUI.Instance.addOneProcess();
                    if (FrameControl.Instance.isNeedStop(1))
                    {
                        yield return null;
                    }
                    Vector3Int posMap = new Vector3Int(i, j, 0);
                    if (TileMap.Instance.isCanReach(posMap) && UnityEngine.Random.Range(0.0f, 1.0f) > 0.9f)
                    {
                        TileType tileType = TileMap.Instance.MapTiles[i, j];
                        TileBase tileBase = ResourcesManager.Instance.getAssetByTileType(tileType);
                        if (tileBase == null) continue;
                        tilemap.SetTile(posMap, tileBase);
                        ResourceMapDataLAB.add(posMap, tileBase.name);
                        if (tileBase.name.Contains("Tree"))
                        {
                            ResourceMapDataLAB.TreeCurCount++;
                            //WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                            //    .setTarget(posMap).setGatherName("Tree").build());
                        }
                    }
                }
            }
            while (true)
            {
                if (ResourceMapDataLAB.TreeCurCount < ResourceMapDataLAB.TreeTotalCount)
                {
                    Vector3Int pos = IsAvailableMap.Instance.genAvailablePosMap();
                    TileType tileType = TileMap.Instance.MapTiles[pos.x, pos.y];
                    TileBase tileBase = ResourcesManager.Instance.getAssetByTileType(tileType,"Tree");
                    if(tileBase == null)
                    {
                        yield return null;
                        continue;
                    }
                    ResourceMapDataLAB.TreeCurCount++;
                    tilemap.SetTile(pos, tileBase);
                    ResourceMapDataLAB.add(pos, tileBase.name);
                    //WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                    //    .setTarget(pos).setGatherName("Tree").build());
                    refreshRound(pos);
                }
                yield return new WaitForSeconds(60.0f * 5);
            }
        }

        /// <summary>
        /// 生成新的资源时，刷新map,防止遮盖错误
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        private void refreshRound(Vector3Int center, int radius = 4) {
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    tilemap.RefreshTile(Tool.add(center,i,j));
                }
            }
        }

        public void cutTree(Vector3Int posMap) {
            ResourceMapDataLAB.remove(posMap);
            tilemap.SetTile(posMap, null);
            ResourceMapDataLAB.TreeCurCount--;
        }

        public override TileBase getTile(Vector3Int posMap)
        {
            TileBase tileBase = tilemap.GetTile(posMap);
            if(tileBase == null)
            {
                tileBase = resourceTileMapOne.GetTile(posMap);
            }
            return tileBase;
        }

        public void setProgress() 
        {
            AsyncProgressUI.Instance.addTotal(Height * Width);
        }

        public override void loadData()
        {
            base.loadData();
            ResourceMapDataLAB = Tool.loadDataByBinary<ResourceMapData>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            foreach (KeyValuePair<Vector3IntLAB, string> posMap in ResourceMapDataLAB.posMaps)
            {
                tilemap.SetTile(Vector3IntLAB.toVector3Int(posMap.Key),
                    (TileBase)ResourcesManager.Instance.getAsset(posMap.Value));
            }
            StartCoroutine(genResource());
        }

        public override void saveData()
        {
            base.saveData();
            Tool.saveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), ResourceMapDataLAB);
        }

        [Serializable]
        public class ResourceMapData {
            public int TreeCurCount;
            public int TreeTotalCount;
            /// <summary>
            /// string:TileBase
            /// </summary>
            public Dictionary<Vector3IntLAB, string> posMaps;

            public ResourceMapData(int treeCurCount, int treeTotalCount)
            {
                TreeCurCount = treeCurCount;
                TreeTotalCount = treeTotalCount;
                posMaps = new Dictionary<Vector3IntLAB, string>();
            }

            public void remove(Vector3Int pos)
            {
                posMaps.Remove(Vector3IntLAB.toVector3IntLAB(pos));
            }

            public void add(Vector3Int pos, string tileBase)
            {
                posMaps.Add(Vector3IntLAB.toVector3IntLAB(pos), tileBase);
            }

            public bool containKey(Vector3Int pos)
            {
                return posMaps.ContainsKey(Vector3IntLAB.toVector3IntLAB(pos));
            }
        }
    }

    [Serializable]
    public struct Vector3IntLAB
    {
        public int x;
        public int y;
        public int z;

        public Vector3IntLAB(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3Int toVector3Int(Vector3IntLAB vector3IntLAB)
        {
            return new Vector3Int(vector3IntLAB.x, vector3IntLAB.y, vector3IntLAB.z);
        }

        public static Vector3IntLAB toVector3IntLAB(Vector3Int vector3Int)
        {
            return new Vector3IntLAB(vector3Int.x, vector3Int.y, vector3Int.z);
        }
    }
}
