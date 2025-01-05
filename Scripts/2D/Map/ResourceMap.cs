using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class ResourceMap : MonoBehaviour
    {
        public static ResourceMap Instance { get; set; }
        public int TreeCurCount { get; set; }

        private Tilemap resourceTileMap { get; set; }
        private int treeTotalCount = 100;

        private void Awake()
        {
            Instance = this;
            resourceTileMap = GetComponent<Tilemap>();
        }

        private void Update()
        {
            genTree();
        }

        private void genTree() {
            if(TreeCurCount < treeTotalCount)
            {
                Vector3Int pos = IsAvailableMap.Instance.genAvailablePosMap();
                resourceTileMap.SetTile(pos, (TileBase)ResourcesManager.Instance.getAsset("Tree"));
                TreeCurCount++;
                WorkerTaskManager.Instance.addTask(new CutTreeTask.CutTreeTaskBuilder().setTarget(pos).build());
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

        public void cutTree(Vector3Int posMap) {
            resourceTileMap.SetTile(posMap, null);
            TreeCurCount--;
        }
    }
}
