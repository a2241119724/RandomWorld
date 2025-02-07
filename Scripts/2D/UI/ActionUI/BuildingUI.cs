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
            // 转为数组下标
            Vector3Int centerMap = TileMap.Instance.worldPosToMapPos(worldPos);
            BuildItem buildItem = ((BuildItem)ItemFactory.Instance.getBuildItemByName(ItemDataManager.Instance.getById(BuildMenuPanel.Instance.Select.item.id).imageName));
            // 建造
            if (IsAvailableMap.Instance.showRect(centerMap, buildItem.width, buildItem.height, buildItem.isBottomLeft) && Input.GetMouseButtonDown(0))
            {
                buildItem.addBuildTask(centerMap);
            }
        }
    }
}
