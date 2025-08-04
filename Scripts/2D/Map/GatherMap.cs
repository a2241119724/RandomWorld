namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 采集地图
    /// </summary>
    public class GatherMap : BaseTileMap
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static GatherMap Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        /// <summary>
        /// 添加采集物
        /// </summary>
        /// <param name="posMap">位置</param>
        public void AddGather(Vector3Int posMap)
        {
            this.tilemap.SetTile(posMap, (TileBase)ResourceManager.Instance.GetAsset("Gather"));
        }

        /// <summary>
        /// 删除采集物
        /// </summary>
        /// <param name="posMap">位置</param>
        public void CancelGather(Vector3Int posMap)
        {
            this.tilemap.SetTile(posMap, null);
        }
    }
}
