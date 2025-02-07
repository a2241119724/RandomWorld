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
        /// �Ѿ���ʾ��ɫ�ͺ�ɫTile
        /// </summary>
        private List<Vector3Int> selectPos_s;

        private void Awake()
        {
            Instance = this;
            isAvailableMap = GetComponent<Tilemap>();
            selectPos_s = new List<Vector3Int>();
        }

        /// <summary>
        /// չʾ��Χgrid���Ƿ�ɽ���
        /// </summary>
        /// <param name="index">grid��controller�µ�sibling index</param>
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
        /// �Ƿ���Խ���(��ͼ�뽨��)
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public bool isAvailablePos(Vector3Int posMap) {
            return TileMap.Instance.isAvailableTile(posMap) &&
                BuildMap.Instance.isAvailableTile(posMap) &&
                ResourceMap.Instance.isAvailableTile(posMap);
        }

        /// <summary>
        /// ���ɿ���λ��(��ͼ�뽨��)
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
