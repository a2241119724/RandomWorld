namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 基地图
    /// </summary>
    public abstract class BaseTileMap : AMonoSaveData
    {
        /// <summary>
        /// 地图
        /// </summary>
        protected Tilemap tilemap;

        /// <summary>
        /// 地图纵向长度
        /// </summary>
        public static int Height { get; set; }

        /// <summary>
        /// 地图横向长度
        /// </summary>
        public static int Width { get; set; }

        /// <summary>
        /// 获取瓦片
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>瓦片</returns>
        public virtual TileBase GetTile(Vector3Int pos)
        {
            return this.tilemap.GetTile(pos);
        }

        /// <summary>
        /// 设置瓦片
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="tileBase">瓦片</param>
        public virtual void SetTile(Vector3Int pos, TileBase tileBase)
        {
            this.tilemap.SetTile(pos, tileBase);
        }

        /// <summary>
        /// 判断该坐标是否可用
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public virtual bool IsFreeTile(Vector3Int posMap)
        {
            return this.tilemap.GetTile(posMap) == null;
        }

        /// <summary>
        /// 判断该坐标是否没有碰撞体
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public virtual bool IsCanReach(Vector3Int posMap)
        {
            return this.tilemap.GetColliderType(posMap) == Tile.ColliderType.None;
        }

        protected virtual void Awake()
        {
            this.tilemap = this.GetComponent<Tilemap>();
        }
    }
}
