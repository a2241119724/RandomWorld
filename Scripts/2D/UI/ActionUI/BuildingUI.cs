using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LAB2D
{
    public class BuildingUI : MonoBehaviour
    {
        public static BuildingUI Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // תΪ�����±�
            Vector3Int centerMap = TileMap.Instance.worldPosToMapPos(worldPos);
            // ����
            if (IsAvailableMap.Instance.showRect(centerMap) && Input.GetMouseButtonDown(0))
            {
                ((BuildItem)ItemFactory.Instance.getItemByName(ItemDataManager.Instance.getById(BuildMenuPanel.Instance.Select.item.id).imageName)).addBuildTask(centerMap);
            }
        }
    }
}
