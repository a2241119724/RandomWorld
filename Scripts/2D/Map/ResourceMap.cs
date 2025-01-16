using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class ResourceMap : AMonoSaveData
    {
        public static ResourceMap Instance { private set; get; }
        public ResourceMapData ResourceMapDataLAB { get; set; }

        private Tilemap resourceTileMap;

        private void Awake()
        {
            Instance = this;
            resourceTileMap = GetComponent<Tilemap>();
            ResourceMapDataLAB = new ResourceMapData(0,100);
        }

        public IEnumerator genTree() {
            while (true)
            {
                if (ResourceMapDataLAB.TreeCurCount < ResourceMapDataLAB.TreeTotalCount)
                {
                    Vector3Int pos = IsAvailableMap.Instance.genAvailablePosMap();
                    ResourceMapDataLAB.add(pos, "Tree");
                    resourceTileMap.SetTile(pos, (TileBase)ResourcesManager.Instance.getAsset("Tree"));
                    ResourceMapDataLAB.TreeCurCount++;
                    WorkerTaskManager.Instance.addTask(new CutTreeTask.CutTreeTaskBuilder().setTarget(pos).build());
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// 加载数据后,显示资源
        /// </summary>
        public void showResources(Dictionary<Vector3IntLAB, string> posMaps) {
            foreach(KeyValuePair<Vector3IntLAB, string> posMap in posMaps)
            {
                resourceTileMap.SetTile(Vector3IntLAB.toVector3Int(posMap.Key), 
                    (TileBase)ResourcesManager.Instance.getAsset(posMap.Value));
            }
        }

        /// <summary>
        /// 判断该坐标是否可用 
        /// </summary>
        /// <param name="x">横坐标</param>
        /// <param name="y">纵坐标</param>
        /// <returns></returns>
        public bool isAvailableTile(Vector3Int posMap)
        {
            return resourceTileMap.GetTile(posMap) == null;
        }

        public bool isCanReach(Vector3Int posMap) {
            return resourceTileMap.GetColliderType(posMap) == Tile.ColliderType.None;
        }

        public void cutTree(Vector3Int posMap) {
            ResourceMapDataLAB.remove(posMap);
            resourceTileMap.SetTile(posMap, null);
            ResourceMapDataLAB.TreeCurCount--;
        }

        /// <summary>
        /// 捡起资源
        /// </summary>
        /// <param name="posMap"></param>
        public void pickUp(Vector3Int posMap)
        {
            ResourceMapDataLAB.remove(posMap);
            resourceTileMap.SetTile(posMap, null);
        }

        /// <summary>
        /// 放置资源图标
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="tileBase"></param>
        public void putDown(Vector3Int posMap, TileBase tileBase)
        {
            if (ResourceMapDataLAB.containKey(posMap)) return;
            ResourceMapDataLAB.add(posMap, tileBase.name);
            resourceTileMap.SetTile(posMap, tileBase);
        }

        public TileBase getTile(Vector3Int pos)
        {
            return resourceTileMap.GetTile(pos);
        }

        public override void loadData()
        {
            base.loadData();
            ResourceMapDataLAB = Tool.loadDataByBinary<ResourceMapData>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            showResources(ResourceMapDataLAB.posMaps);
            StartCoroutine(genTree());
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
