using UnityEngine;
using UnityEngine.EventSystems;

namespace LAB2D
{
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
        public static Joystick Instance { get; private set; }
        public Vector2 Direction { get { return direction; } }

        private Vector2 direction = Vector2.zero; // 方向
        private RectTransform baseRect; // 摇杆可用范围
        private RectTransform background; // 摇杆背景
        private RectTransform handle; // 摇杆中心按钮
        private Vector2 originalPostion; // 原来的位置

        void Awake() {
            Instance = this;
        }

        void Start()
        {
            baseRect = GetComponent<RectTransform>();
            if (baseRect == null)
            {
                Debug.LogError("baseRect Not Found!!!");
                return;
            }
            Vector2 center = new Vector2(0.5f, 0.5f);
            if (center == null)
            {
                Debug.LogError("center assign resource Error!!!");
                return;
            }
            // 初始化background
            background = transform.Find("Background").GetComponent<RectTransform>();
            originalPostion = background.GetComponent<RectTransform>().localPosition;
            if (background == null)
            {
                Debug.LogError("background Not Found!!!");
                return;
            }
            background.pivot = center;
            // 初始化handle
            handle = background.transform.Find("Handle").GetComponent<RectTransform>();
            if (handle == null)
            {
                Debug.LogError("handle Not Found!!!");
                return;
            }
            handle.anchorMin = center;
            handle.anchorMax = center;
            handle.pivot = center;
            handle.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 将摇杆放到按下的位置
            Vector2 localPosition;
            // ScreenPointToLocalPointInRectangle(父节点,屏幕坐标,照相机{Sceen Space-Overlay则可以为空},返回值{屏幕坐标在父节点的局部坐标})
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, Camera.main, out localPosition);
            background.anchoredPosition = localPosition;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            float radius = background.sizeDelta.x / 2;
            direction = eventData.position - RectTransformUtility.WorldToScreenPoint(Camera.main, background.position);
            if (direction.magnitude > radius)
            {
                direction = direction.normalized * radius;
            }
            handle.localPosition = direction; // 一样
            //handle.anchoredPosition = input;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            direction = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
            background.anchoredPosition = originalPostion;
        }
    }
}