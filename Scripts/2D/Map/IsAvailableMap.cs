namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 建造时可用地图。
    ///
    /// 预览瓦片资源名可通过 Inspector 配置，默认为 "Snow"。
    /// 建造可用性检查委托给 TerrainConfigDatabase.CanBuild()。
    /// </summary>
    public class IsAvailableMap : BaseTileMap
    {
        /// <summary>
        /// 预览网格使用的瓦片资源名（可在 Inspector 中修改）。
        /// </summary>
        [SerializeField]
        private string previewTileResourceName = "Snow";

        /// <summary>
        /// 生成可用位置的最大重试次数。
        /// </summary>
        private const int GEN_POS_MAX_RETRIES = 100;

        /// <summary>
        /// 已经显示绿色和红色Tile
        /// </summary>
        private List<Vector3Int> selectPoses;

        /// <summary>
        /// 单例
        /// </summary>
        public static IsAvailableMap Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.selectPoses = new List<Vector3Int>();
        }

        /// <summary>
        /// 展示周围grid的是否可建造
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="rectType">rect的类型</param>
        /// <returns>是否</returns>
        public bool ShowRect(Vector3Int posMap, int width = 10, int height = 7, AWorkerTask.RectType rectType = AWorkerTask.RectType.Center)
        {
            bool isBuilding = true;
            this.ClearShow();

            // 左下角
            int h_start = 0, h_end = height;
            int w_start = 0, w_end = width;
            if (rectType == AWorkerTask.RectType.Center)
            {
                h_start = -height / 2;
                h_end = height - (height / 2);
                w_start = -width / 2;
                w_end = width - (width / 2);
            }
            else if (rectType == AWorkerTask.RectType.TopLeft)
            {
                h_start = 1 - height;
                h_end = 1;
            }

            for (int i = h_start; i < h_end; i++)
            {
                for (int j = w_start; j < w_end; j++)
                {
                    Vector3Int posMap1 = new (posMap.x + i, posMap.y + j, 0);
                    this.selectPoses.Add(posMap1);
                    this.tilemap.SetTile(posMap1, (TileBase)AWorkerTask.ResourceLoadProvider(this.previewTileResourceName));
                    this.tilemap.RemoveTileFlags(posMap1, TileFlags.LockColor);
                    if (this.IsAvailable(posMap1))
                    {
                        this.tilemap.SetColor(posMap1, new Color(0, 1, 0));
                    }
                    else
                    {
                        this.tilemap.SetColor(posMap1, new Color(1, 0, 0));
                        isBuilding = false;
                    }
                }
            }

            return isBuilding;
        }

        /// <summary>
        /// 清楚所有显示
        /// </summary>
        public void ClearShow()
        {
            foreach (Vector3Int selectPos in this.selectPoses)
            {
                this.tilemap.SetTile(selectPos, null);
            }

            this.selectPoses.Clear();
        }

        /// <summary>
        /// 生成可用位置(地图与建筑)
        /// </summary>
        /// <param name="centerMap">default:全图找可用位置</param>
        /// <param name="radius">半径</param>
        /// <param name="isDrop">是否是掉落物</param>
        /// <returns>位置</returns>
        public Vector3Int GenAvailablePosMap(Vector3Int centerMap = default, int radius = 10, bool isDrop = false)
        {
            int x, y, startX = 0, endX = Core.ServiceLocator.Get<TileMap>().TileMapDataLAB.Height, startY = 0, endY = Core.ServiceLocator.Get<TileMap>().TileMapDataLAB.Width;
            if (centerMap != default)
            {
                startX = (int)System.Math.Max(centerMap.x - radius, 0);
                startY = (int)System.Math.Max(centerMap.y - radius, 0);
                endX = (int)System.Math.Min(centerMap.x + radius, Core.ServiceLocator.Get<TileMap>().TileMapDataLAB.Height);
                endY = (int)System.Math.Min(centerMap.y + radius, Core.ServiceLocator.Get<TileMap>().TileMapDataLAB.Width);
            }

            // 如果循环次数过多,则说明没有可用的位置
            int count = 0;
            bool flag;
            do
            {
                x = Random.Range(startX, endX);
                y = Random.Range(startY, endY);
                count++;
                if (count > GEN_POS_MAX_RETRIES)
                {
                    AWorkerTask.LogProvider("genAvailablePosMap Error!!!", LogManager.LogLevelEnum.Error);
                    return default;
                }

                // 掉落物使用 IsTileFreeForDrop（不检查地形可建造性，草地/沙漠等都能放）
                // 建造/任务栏等使用 IsAvailable（要求地形可建造）
                flag = isDrop
                    ? !this.IsTileFreeForDrop(new Vector3Int(x, y, 0))
                    : !this.IsAvailable(new Vector3Int(x, y, 0));
            }
            while (flag);
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 空地是否可放置掉落物（地形可行走 + 无建筑 + 无资源 + 无已有掉落物）。
        /// 与建造不同，掉落物不需要地形允许建造，森林地面也能放。
        /// </summary>
        public bool IsTileFreeForDrop(Vector3Int posMap)
        {
            if (!Core.ServiceLocator.Get<TileMap>().IsCanReach(posMap) ||
                !Core.ServiceLocator.Get<BuildMap>().IsFreeTile(posMap) ||
                !Core.ServiceLocator.Get<ResourceMap>().IsFreeTile(posMap))
            {
                return false;
            }

            if (!Core.ServiceLocator.Get<ItemMap>().IsFreeTile(posMap))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否可以建造（地图地形 + 建筑 + 资源）。
        /// 地形可建造性委托给 TerrainConfigDatabase.CanBuild()。
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        private bool IsAvailable(Vector3Int posMap)
        {
            if (!Core.ServiceLocator.Get<TileMap>().IsCanReach(posMap) ||
                !Core.ServiceLocator.Get<BuildMap>().IsFreeTile(posMap) ||
                !Core.ServiceLocator.Get<ResourceMap>().IsFreeTile(posMap))
            {
                return false;
            }

            // 检查地形是否允许建造（数据驱动，由 TerrainTileConfig.canBuild 控制）
            TileMap tm = Core.ServiceLocator.Get<TileMap>();
            if (tm.TileMapDataLAB?.MapTiles != null &&
                posMap.x >= 0 && posMap.x < tm.TileMapDataLAB.Height &&
                posMap.y >= 0 && posMap.y < tm.TileMapDataLAB.Width)
            {
                int terrainId = tm.TileMapDataLAB.MapTiles[posMap.x, posMap.y];
                if (!Core.ServiceLocator.Get<TerrainConfigDatabase>().CanBuild(terrainId))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
