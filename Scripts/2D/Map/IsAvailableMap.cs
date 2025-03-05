using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.PointerEventData;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class IsAvailableMap : BaseTileMap
    {
        public static IsAvailableMap Instance { private set; get; }
        
        /// <summary>
        /// 已经显示绿色和红色Tile
        /// </summary>
        private List<Vector3Int> selectPos_s;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            selectPos_s = new List<Vector3Int>();
        }

        /// <summary>
        /// 展示周围grid的是否可建造
        /// </summary>
        /// <param name="index">grid在controller下的sibling index</param>
        public bool showRect(Vector3Int posMap,int width=10,int height=7,bool isBottomLeft=false) {
            bool isBuilding = true;
            clearShow();
            int hStart = 0, hEnd = height;
            int wStart = 0, wEnd = width;
            if (!isBottomLeft)
            {
                hStart = -height / 2;
                hEnd = height - height / 2;
                wStart = -width / 2;
                wEnd = width - width / 2;
            }
            for (int i = hStart; i< hEnd; i++)
            {
                for (int j = wStart; j < wEnd; j++)
                {
                    Vector3Int _posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    selectPos_s.Add(_posMap);
                    tilemap.SetTile(_posMap, (TileBase)ResourcesManager.Instance.getAsset("Snow"));
                    tilemap.RemoveTileFlags(_posMap, TileFlags.LockColor);
                    if (isAvailable(_posMap))
                    {
                        tilemap.SetColor(_posMap, new Color(0, 1, 0));
                    }
                    else
                    {
                        tilemap.SetColor(_posMap, new Color(1, 0, 0));
                        isBuilding = false;
                    }
                }
            }
            return isBuilding;
        }

        public void clearShow() {
            foreach (Vector3Int selectPos in selectPos_s)
            {
                tilemap.SetTile(selectPos, null);
            }
            selectPos_s.Clear();
        }

        /// <summary>
        /// 是否可以建造(地图与建筑)
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        private bool isAvailable(Vector3Int posMap) {
            return TileMap.Instance.isCanReach(posMap) &&
                BuildMap.Instance.isFreeTile(posMap) &&
                ResourceMap.Instance.isFreeTile(posMap);
        }

        /// <summary>
        /// 生成可用位置(地图与建筑)
        /// </summary>
        /// <param name="centerMap">default:全图找可用位置</param>
        /// <param name="radius"></param>
        /// <param name="isDrop"></param>
        /// <returns></returns>
        public Vector3Int genAvailablePosMap(Vector3Int centerMap=default, int radius=10, bool isDrop = false)
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
            bool flag = false;
            do
            {
                x = Random.Range(startX, endX);
                y = Random.Range(startY, endY);
                count++;
                if(count > 100)
                {
                    LogManager.Instance.log("genAvailablePosMap Error!!!", LogManager.LogLevel.Error);
                    return default;
                }
                // 如果是放置掉落物,则需要判断是否是可放置的位置
                bool A = !isAvailable(new Vector3Int(x, y, 0));
                flag = isDrop ? A || !ItemMap.Instance.isFreeTile(new Vector3Int(x, y, 0)) : A;
            } while (flag);
            return new Vector3Int(x, y, 0);
        }
    }
}
