namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// sizeDelta与size
    /// 当四个锚点重合时
    /// sizeDelta = size;
    /// 当不重合时,
    /// sizeDelta.x = rect.x - anchorRectangle.x;
    /// sizeDelta.y = rect.y - anchorRectangle.y;
    /// </summary>
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 direction = Vector2.zero; // 方向
        private RectTransform baseRect; // 摇杆可用范围
        private RectTransform background; // 摇杆背景
        private RectTransform handle; // 摇杆中心按钮
        private Vector2 originalPostion; // 原来的位置

        /// <summary>
        /// 单例
        /// </summary>
        public static Joystick Instance { get; private set; }

        /// <summary>
        /// 方向
        /// </summary>
        public Vector2 Direction
        {
            get
            {
                return this.direction;
            }
        }

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData)
        {
            // 将摇杆放到按下的位置
            // ScreenPointToLocalPointInRectangle(父节点,屏幕坐标,照相机{Sceen Space-Overlay则可以为空},返回值{屏幕坐标在父节点的局部坐标})
            RectTransformUtility.ScreenPointToLocalPointInRectangle(this.baseRect, eventData.position, Camera.main, out Vector2 localPosition);
            this.background.anchoredPosition = localPosition;
            this.OnDrag(eventData);
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            float radius = this.background.sizeDelta.x / 2;
            this.direction = eventData.position - RectTransformUtility.WorldToScreenPoint(Camera.main, this.background.position);
            if (this.direction.magnitude > radius)
            {
                this.direction = this.direction.normalized * radius;
            }

            this.handle.localPosition = this.direction; // 一样

            // handle.anchoredPosition = input;
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData)
        {
            this.direction = Vector2.zero;
            this.handle.anchoredPosition = Vector2.zero;
            this.background.anchoredPosition = this.originalPostion;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            this.baseRect = this.GetComponent<RectTransform>();
            if (this.baseRect == null)
            {
                LogManager.Instance.Log("baseRect Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            Vector2 center = new (0.5f, 0.5f);
            if (center == null)
            {
                LogManager.Instance.Log("center assign resource Error!!!", LogManager.LogLevel.Error);
                return;
            }

            // 初始化background
            this.background = this.transform.Find("Background").GetComponent<RectTransform>();
            this.originalPostion = this.background.GetComponent<RectTransform>().localPosition;
            if (this.background == null)
            {
                LogManager.Instance.Log("background Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.background.pivot = center;

            // 初始化handle
            this.handle = this.background.transform.Find("Handle").GetComponent<RectTransform>();
            if (this.handle == null)
            {
                LogManager.Instance.Log("handle Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.handle.anchorMin = center;
            this.handle.anchorMax = center;
            this.handle.pivot = center;
            this.handle.anchoredPosition = Vector2.zero;
        }
    }
}