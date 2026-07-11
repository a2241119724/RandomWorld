namespace LAB2D.UI.Action
{
    using LAB2D;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 建造使用的UI
    /// </summary>
    public class BuildingUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerUpHandler, IPointerMoveHandler
    {
        private bool isDrag = false; // 如果拖拽则关闭默认建造范围，使用滑动建造范围
        private Vector3Int startPos;

        /// <summary>
        /// 单例
        /// </summary>
        public static BuildingUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;

            // 先打开设置Instance
            this.gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ABuildItem buildItem = (ABuildItem)ItemInstanceFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.Item.Id).EnName);
            if (!buildItem.IsCustomSize)
            {
                return;
            }

            this.isDrag = true;
            this.startPos = TileMap.Instance.WorldPosToMapPos(Camera.main.ScreenToWorldPoint(eventData.position));
        }

        public void OnDrag(PointerEventData eventData)
        {
            ABuildItem buildItem = (ABuildItem)ItemInstanceFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.Item.Id).EnName);
            if (!buildItem.IsCustomSize)
            {
                return;
            }

            Vector3Int currentPos = TileMap.Instance.WorldPosToMapPos(Camera.main.ScreenToWorldPoint(eventData.position));
            IsAvailableMap.Instance.ShowRect(this.startPos, currentPos.y - this.startPos.y + 1, this.startPos.x - currentPos.x + 1, AWorkerTask.RectType.TopLeft);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ABuildItem buildItem = (ABuildItem)ItemInstanceFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.Item.Id).EnName);
            if (!buildItem.IsCustomSize)
            {
                return;
            }

            this.isDrag = false;

            // 没有选择任何物品
            if (BuildMenuPanel.Instance.Select.Item == null)
            {
                return;
            }

            Vector3Int currentPos = TileMap.Instance.WorldPosToMapPos(Camera.main.ScreenToWorldPoint(eventData.position));

            // 建造
            if (IsAvailableMap.Instance.ShowRect(this.startPos, currentPos.y - this.startPos.y + 1, this.startPos.x - currentPos.x + 1, AWorkerTask.RectType.TopLeft))
            {
                buildItem.AddBuildTask(this.startPos, new ABuildItem.Extra(currentPos.y - this.startPos.y + 1, this.startPos.x - currentPos.x + 1, AWorkerTask.RectType.TopLeft));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // 没有选择任何物品
            if (BuildMenuPanel.Instance.Select.Item == null || this.isDrag)
            {
                return;
            }

            Vector3Int centerMap = TileMap.Instance.WorldPosToMapPos(Camera.main.ScreenToWorldPoint(eventData.position));
            ABuildItem buildItem = (ABuildItem)ItemInstanceFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.Item.Id).EnName);
            if (IsAvailableMap.Instance.ShowRect(centerMap, buildItem.Width, buildItem.Height, buildItem.RectType))
            {
                buildItem.AddBuildTask(centerMap, null);
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            // 没有选择任何物品
            if (BuildMenuPanel.Instance.Select.Item == null || this.isDrag)
            {
                return;
            }

            // 使用建造默认的大小
            Vector3Int centerMap = TileMap.Instance.WorldPosToMapPos(Camera.main.ScreenToWorldPoint(eventData.position));
            ABuildItem buildItem = (ABuildItem)ItemInstanceFactory.Instance.GetBuildItemByName(ItemDataManager.Instance.GetById(BuildMenuPanel.Instance.Select.Item.Id).EnName);
            IsAvailableMap.Instance.ShowRect(centerMap, buildItem.Width, buildItem.Height, buildItem.RectType);
        }
    }
}
