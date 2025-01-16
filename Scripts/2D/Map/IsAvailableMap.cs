using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.PointerEventData;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class IsAvailableMap : MonoBehaviour
    {
        public static IsAvailableMap Instance { private set; get; }
        
        private Tilemap isAvailableMap;
        /// <summary>
        /// 已经显示绿色和红色Tile
        /// </summary>
        private List<Vector3Int> selectPos_s;

        private void Awake()
        {
            Instance = this;
            isAvailableMap = GetComponent<Tilemap>();
            selectPos_s = new List<Vector3Int>();
        }

        /// <summary>
        /// 展示周围grid的是否可建造
        /// </summary>
        /// <param name="index">grid在controller下的sibling index</param>
        public bool showRect(Vector3Int posMap,int width=10,int height=7) {
            bool isBuilding = true;
            clearShow();
            for (int i = -height/2;i< height - height / 2; i++)
            {
                for (int j = -width / 2; j < width - width / 2; j++)
                {
                    Vector3Int _posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    selectPos_s.Add(_posMap);
                    isAvailableMap.SetTile(_posMap, (TileBase)ResourcesManager.Instance.getAsset("Snow"));
                    isAvailableMap.RemoveTileFlags(_posMap, TileFlags.LockColor);
                    if (isAvailablePos(_posMap))
                    {
                        isAvailableMap.SetColor(_posMap, new Color(0, 1, 0));
                    }
                    else
                    {
                        isAvailableMap.SetColor(_posMap, new Color(1, 0, 0));
                        isBuilding = false;
                    }
                }
            }
            return isBuilding;
        }

        public void clearShow() {
            foreach (Vector3Int selectPos in selectPos_s)
            {
                isAvailableMap.SetTile(selectPos, null);
            }
            selectPos_s.Clear();
        }

        /// <summary>
        /// 是否可以建造(地图与建筑)
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public bool isAvailablePos(Vector3Int posMap) {
            return TileMap.Instance.isAvailableTile(posMap) &&
                BuildMap.Instance.isAvailableTile(posMap) &&
                ResourceMap.Instance.isAvailableTile(posMap);
        }

        /// <summary>
        /// 生成可用位置(地图与建筑)
        /// </summary>
        /// <param name="centerMap"></param>
        /// <returns></returns>
        public Vector3Int genAvailablePosMap(Vector3Int centerMap=default, int radius=10)
        {
            int x, y, startX = 0, endX = TileMap.Instance.Height, startY = 0, endY = TileMap.Instance.Width;
            if (centerMap != default)
            {
                startX = (int)Mathf.Max(centerMap.x - radius, 0);
                startY = (int)Mathf.Max(centerMap.y - radius, 0);
                endX = (int)Mathf.Min(centerMap.x + radius, TileMap.Instance.Height);
                endY = (int)Mathf.Min(centerMap.y + radius, TileMap.Instance.Width);
            }
            do
            {
                x = Random.Range(startX, endX);
                y = Random.Range(startY, endY);
            } while (!isAvailablePos(new Vector3Int(x, y, 0)));
            return new Vector3Int(x, y, 0);
        }
    }
}
