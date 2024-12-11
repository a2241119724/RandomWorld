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
        public static IsAvailableMap Instance { set; get; }
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
        public void showRect(Vector3Int posMap,int width=4,int height=3) {
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
                    }
                }
            }
        }

        public void clearShow() {
            foreach (Vector3Int selectPos in selectPos_s)
            {
                isAvailableMap.SetTile(selectPos, null);
            }
            selectPos_s.Clear();
        }

        /// <summary>
        /// �Ƿ���Խ���
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        private bool isAvailablePos(Vector3Int posMap) {
            return TileMap.Instance.isAvailableTile(posMap) && BuildMap.Instance.isAvailableTile(posMap);
        }
    }
}
