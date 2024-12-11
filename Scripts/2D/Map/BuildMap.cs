using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class BuildMap : MonoBehaviour
    {
        public static BuildMap Instance { get; set; }
        public Tilemap BuildTileMap { get; set; }
        
        // Map地表数组下标
        private List<Vector3Int> targetMaps;

        private void Awake()
        {
            Instance = this;
            BuildTileMap = GetComponent<Tilemap>();
            targetMaps = new List<Vector3Int>();
        }

        /// <summary>
        /// Color a 0.5f代表有碰撞体，0.49f代表没有碰撞体，
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        public BuildMap addBuilding(Vector3Int targetMap, TileBase tile, bool isCollider=true) {
            BuildTileMap.SetTile(targetMap, tile);
            BuildTileMap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            BuildTileMap.SetColliderType(targetMap, Tile.ColliderType.None);
            BuildTileMap.SetColor(targetMap, new Color(1,1,1, isCollider ? 0.5f : 0.49f));
            if (!targetMaps.Contains(targetMap))
            {
                targetMaps.Add(targetMap);
            }
            return this;
        }

        /// <summary>
        /// 没有碰撞体的最后Color a为0.99f
        /// </summary>
        /// <param name="targetMap"></param>
        public void setComplete(Vector3Int targetMap)
        {
            if (BuildTileMap.GetColor(targetMap).a == 0.5f)
            {
                BuildTileMap.SetColliderType(targetMap, Tile.ColliderType.Grid);
                BuildTileMap.SetColor(targetMap, new Color(1, 1, 1, 1));
            }
            else
            {
                BuildTileMap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }
        }

        public bool isBuilding(Vector3Int target)
        {
            return BuildTileMap.GetColor(target).a < 1.0f;
        }

        public void cancelBuilding(Vector3Int targetMap)
        {
            BuildTileMap.SetTile(targetMap, null);
            targetMaps.Remove(targetMap);
        }

        public void addTask()
        {
            foreach (Vector3Int targetMap in targetMaps) { 
                WorkerTaskManager.Instance.addTask(new BuildTask.BuildTaskBuilder().setTarget(targetMap).build());
            }
            targetMaps.Clear();
        }

        public bool isAvailableTile(Vector3Int posMap) {
            return BuildTileMap.GetTile(posMap) == null;
        }
    }
}

