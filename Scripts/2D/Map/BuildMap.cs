namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 使用alpha判断Worker是否可以通行
    /// 使用Collider会出现,Worker寻路完成,但路径上的Tile被建造完成，导致不能通行
    /// 从半创建直接就不可通行
    /// </summary>
    public class BuildMap : BaseTileMap
    {
        /// <summary>
        /// Map地表数组下标
        /// </summary>
        private List<Vector3Int> targetMaps;

        /// <summary>
        /// 单例
        /// </summary>
        public static BuildMap Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.targetMaps = new List<Vector3Int>();
        }

        /// <summary>
        /// Color a 0.5f代表有碰撞体，0.49f代表没有碰撞体，
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        /// <param name="tile">瓦片</param>
        /// <param name="isCollider">是否是碰撞体</param>
        /// <returns>建造地图</returns>
        public BuildMap AddBuilding(Vector3Int targetMap, TileBase tile, bool isCollider = true)
        {
            this.tilemap.SetTile(targetMap, tile);
            this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            this.tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
            this.tilemap.SetColor(targetMap, new Color(1, 1, 1, isCollider ? 0.5f : 0.49f));
            if (!this.targetMaps.Contains(targetMap))
            {
                this.targetMaps.Add(targetMap);
            }

            return this;
        }

        /// <summary>
        /// 直接建造完成,Worker
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        /// <param name="tile">瓦片</param>
        /// <param name="isPass">是否可通行</param>
        /// <returns>地图瓦片</returns>
        public BuildMap DirectBuild(Vector3Int targetMap, TileBase tile, bool isPass = true)
        {
            this.tilemap.SetTile(targetMap, tile);
            if (isPass)
            {
                this.tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }

            return this;
        }

        /// <summary>
        /// 没有碰撞体的最后Color a为0.99f
        /// </summary>
        /// <param name="targetMap">目标位置</param>
        public void SetComplete(Vector3Int targetMap)
        {
            if (this.tilemap.GetColor(targetMap).a == 0.5f)
            {
                this.tilemap.SetColliderType(targetMap, Tile.ColliderType.Sprite);
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 1));
            }
            else
            {
                this.tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }

            RoomManager.Instance.Complete(targetMap);
        }

        /// <summary>
        /// 是否正在建造
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <returns>是否</returns>
        public bool IsBuilding(Vector3Int target)
        {
            return this.tilemap.GetColor(target).a < 1.0f;
        }

        /// <summary>
        /// 删除正在建造
        /// </summary>
        /// <param name="targetMap">建造目标</param>
        public void CancelBuilding(Vector3Int targetMap)
        {
            this.tilemap.SetTile(targetMap, null);
            this.targetMaps.Remove(targetMap);
        }

        /// <summary>
        /// 添加建造任务到地图
        /// </summary>
        public void AddTask()
        {
            Dictionary<int, ResourceInfo> resourceInfos = new ();
            resourceInfos.Add(
                ItemDataManager.Instance.GetByName("CustomWood").Id,
                new ResourceInfo(ItemDataManager.Instance.GetByName("CustomWood").Id, 5));
            foreach (Vector3Int targetMap in this.targetMaps)
            {
                // 不能再这里设置第一个坐标点，即Target，因为此时Inventory可能没有材料，返回default
                WorkerTaskManager.Instance.AddTask(new WorkerBuildTask.BuildTaskBuilder().SetBuildPos(targetMap)
                    .SetNeedResource(new Dictionary<int, ResourceInfo>(resourceInfos)).Build());
            }

            this.targetMaps.Clear();
        }

        /// <summary>
        /// 是否可以通行,Worker寻路时使用
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public override bool IsCanReach(Vector3Int posMap)
        {
            // 门可以通行
            return Mathf.Abs(this.tilemap.GetColor(posMap).a - 0.49f) < 1e-5
                || Mathf.Abs(this.tilemap.GetColor(posMap).a - 0.99f) < 1e-5
                || this.IsFreeTile(posMap);
        }
    }
}
