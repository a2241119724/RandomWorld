namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 建造时可用地图
    /// </summary>
    public class IsAvailableMap : BaseTileMap
    {
        /// <summary>
        /// 已经显示绿色和红色Tile
        /// </summary>
        private List<Vector3Int> selectPosS;

        /// <summary>
        /// 单例
        /// </summary>
        public static IsAvailableMap Instance { get; private set; }

        /// <summary>
        /// 展示周围grid的是否可建造
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="isBottomLeft">是否左下</param>
        /// <returns>是否</returns>
        public bool ShowRect(Vector3Int posMap, int width = 10, int height = 7, bool isBottomLeft = false)
        {
            bool isBuilding = true;
            this.ClearShow();
            int h_start = 0, h_end = height;
            int w_start = 0, w_end = width;
            if (!isBottomLeft)
            {
                h_start = -height / 2;
                h_end = height - (height / 2);
                w_start = -width / 2;
                w_end = width - (width / 2);
            }

            for (int i = h_start; i < h_end; i++)
            {
                for (int j = w_start; j < w_end; j++)
                {
                    Vector3Int posMap1 = new (posMap.x + i, posMap.y + j, 0);
                    this.selectPosS.Add(posMap1);
                    this.tilemap.SetTile(posMap1, (TileBase)ResourcesManager.Instance.GetAsset("Snow"));
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
            foreach (Vector3Int selectPos in this.selectPosS)
            {
                this.tilemap.SetTile(selectPos, null);
            }

            this.selectPosS.Clear();
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
            int x, y, startX = 0, endX = TileMap.Height, startY = 0, endY = TileMap.Width;
            if (centerMap != default)
            {
                startX = (int)Mathf.Max(centerMap.x - radius, 0);
                startY = (int)Mathf.Max(centerMap.y - radius, 0);
                endX = (int)Mathf.Min(centerMap.x + radius, TileMap.Height);
                endY = (int)Mathf.Min(centerMap.y + radius, TileMap.Width);
            }

            // 如果循环次数过多,则说明没有可用的位置
            int count = 0;
            bool flag;
            do
            {
                x = Random.Range(startX, endX);
                y = Random.Range(startY, endY);
                count++;
                if (count > 100)
                {
                    LogManager.Instance.Log("genAvailablePosMap Error!!!", LogManager.LogLevel.Error);
                    return default;
                }

                // 如果是放置掉落物,则需要判断是否是可放置的位置
                bool a = !this.IsAvailable(new Vector3Int(x, y, 0));
                flag = isDrop ? a || !ItemMap.Instance.IsFreeTile(new Vector3Int(x, y, 0)) : a;
            }
            while (flag);
            return new Vector3Int(x, y, 0);
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            this.selectPosS = new List<Vector3Int>();
        }

        /// <summary>
        /// 是否可以建造(地图与建筑)
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        private bool IsAvailable(Vector3Int posMap)
        {
            return TileMap.Instance.IsCanReach(posMap) &&
                BuildMap.Instance.IsFreeTile(posMap) &&
                ResourceMap.Instance.IsFreeTile(posMap);
        }
    }
}
