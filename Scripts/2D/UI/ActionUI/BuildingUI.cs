namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 建造使用的UI
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static BuildingUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
        }

        public void Update()
        {
            // 没有选择任何物品
            if (BuildMenuPanel.Instance.Select.Item == null)
            {
                return;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // 转为数组下标
            Vector3Int centerMap = TileMap.Instance.WorldPosToMapPos(worldPos);
            BuildItem buildItem = (BuildItem)ItemFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.Item.Id).ImageName);

            // 建造
            if (IsAvailableMap.Instance.ShowRect(centerMap, buildItem.Width, buildItem.Height, buildItem.IsBottomLeft)
                && Input.GetMouseButtonDown(0))
            {
                buildItem.AddBuildTask(centerMap);
            }
        }
    }
}
