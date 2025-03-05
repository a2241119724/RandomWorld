using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// 使用alpha判断Worker是否可以通行
    /// 使用Collider会出现,Worker寻路完成,但路径上的Tile被建造完成，导致不能通行
    /// 从半创建直接就不可通行
    /// </summary>
    public class BuildMap : BaseTileMap
    {
        public static BuildMap Instance { private set; get; }
        
        // Map地表数组下标
        private List<Vector3Int> targetMaps;


        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            targetMaps = new List<Vector3Int>();
        }

        /// <summary>
        /// Color a 0.5f代表有碰撞体，0.49f代表没有碰撞体，
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        public BuildMap addBuilding(Vector3Int targetMap, TileBase tile, bool isCollider=true) {
            tilemap.SetTile(targetMap, tile);
            tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
            tilemap.SetColor(targetMap, new Color(1,1,1, isCollider ? 0.5f : 0.49f));
            if (!targetMaps.Contains(targetMap))
            {
                targetMaps.Add(targetMap);
            }
            return this;
        }

        /// <summary>
        /// 直接建造完成,Worker
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        /// <param name="isPass">是否可通行</param>
        /// <returns></returns>
        public BuildMap directBuild(Vector3Int targetMap, TileBase tile, bool isPass=true)
        {
            tilemap.SetTile(targetMap, tile);
            if (isPass)
            {
                tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }
            return this;
        }

        /// <summary>
        /// 没有碰撞体的最后Color a为0.99f
        /// </summary>
        /// <param name="targetMap"></param>
        public void setComplete(Vector3Int targetMap)
        {
            if (tilemap.GetColor(targetMap).a == 0.5f)
            {
                tilemap.SetColliderType(targetMap, Tile.ColliderType.Sprite);
                tilemap.SetColor(targetMap, new Color(1, 1, 1, 1));
            }
            else
            {
                tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }
            RoomManager.Instance.complete(targetMap);
        }

        public bool isBuilding(Vector3Int target)
        {
            return tilemap.GetColor(target).a < 1.0f;
        }

        public void cancelBuilding(Vector3Int targetMap)
        {
            tilemap.SetTile(targetMap, null);
            targetMaps.Remove(targetMap);
        }

        public void addTask()
        {
            Dictionary<int, ResourceInfo> resourceInfos = new Dictionary<int, ResourceInfo>();
            resourceInfos.Add(ItemDataManager.Instance.getByName("CustomWood").id,
                new ResourceInfo(ItemDataManager.Instance.getByName("CustomWood").id, 5));
            foreach (Vector3Int targetMap in targetMaps) { 
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                WorkerTaskManager.Instance.addTask(new WorkerBuildTask.BuildTaskBuilder().setBuildPos(targetMap)
                    .setNeedResource(new Dictionary<int, ResourceInfo>(resourceInfos)).build());
            }
            targetMaps.Clear();
        }

        /// <summary>
        /// 是否可以通行,Worker寻路时使用
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public override bool isCanReach(Vector3Int posMap)
        {
            // 门可以通行
            return Mathf.Abs(tilemap.GetColor(posMap).a - 0.49f) < 1e-5
                || Mathf.Abs(tilemap.GetColor(posMap).a - 0.99f) < 1e-5 
                || base.isFreeTile(posMap);
        }
    }
}

