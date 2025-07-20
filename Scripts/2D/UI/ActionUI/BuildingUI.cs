using UnityEngine;

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
            // 没有选择任何物品
            if (BuildMenuPanel.Instance.Select.item == null)
            {
                return;
            }
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // 转为数组下标
            Vector3Int centerMap = TileMap.Instance.WorldPosToMapPos(worldPos);
            BuildItem buildItem = ((BuildItem)ItemFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.item.Id).ImageName));
            // 建造
            if (IsAvailableMap.Instance.ShowRect(centerMap, buildItem.Width, buildItem.Height, buildItem.IsBottomLeft)
                && Input.GetMouseButtonDown(0))
            {
                buildItem.AddBuildTask(centerMap);
            }
        }
    }
}
