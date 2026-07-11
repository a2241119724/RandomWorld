namespace LAB2D.MVC
{
    using LAB2D;
    using LAB2D.Item;
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// MVC道具UI
    /// </summary>
    public abstract class MVCItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        /// <summary>
        /// 是否开始拖拽(解决拖拽不能拽的Item会只执行OnEndDrag)
        /// </summary>
        public bool IsBeginDrag;

        // private SelectAndShowEventSO selectAndShow = null;
        private int index; // 当前拖拽格子的索引
        private Transform parent; // 父物体
        private Vector3 offset; // 鼠标点击位置与物体中心的偏移量

        /// <summary>
        /// 交换道具
        /// </summary>
        public event Action<int, int> ExchangeItem;

        /// <summary>
        /// 设置边框颜色
        /// </summary>
        public event Action<int, string> SetBorderColor;

        /// <summary>
        /// 获取道具
        /// </summary>
        public event Func<int, AItem> GetItem;

        /// <summary>
        /// 展示信息
        /// </summary>
        public event Action<AItem> ShowInfo;

        /// <summary>
        /// 是否可以拖拽
        /// </summary>
        public bool IsDrag { get; set; }

        /// <summary>
        /// 品质背景颜色
        /// </summary>
        public Color QualityColor { get; set; } = Color.white;

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData e)
        {
            if (!this.IsDrag)
            {
                return;
            }

            this.IsBeginDrag = true;

            // originalParent = transform.parent;
            this.index = this.parent.GetSiblingIndex();
            this.offset = this.transform.position - e.pressEventCamera.ScreenToWorldPoint(e.position);
            this.offset.z = 0;
            this.transform.SetParent(this.parent.parent.parent.parent.parent.parent, false); // 拖拽时防止被父物体遮挡
            this.GetComponent<CanvasGroup>().blocksRaycasts = false; // 是否射线检测
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData e)
        {
            if (!this.IsDrag)
            {
                return;
            }

            Vector3 v = e.pressEventCamera.ScreenToWorldPoint(e.position); // 将视口坐标转换为世界坐标
            v.z = -5; // 保证在射相机(-20)与面板(0)之间
            this.transform.position = v + this.offset;
        }

        /// <summary>
        /// 通过交换两个盒子实现交换
        /// </summary>
        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData e)
        {
            if (!this.IsDrag || !this.IsBeginDrag)
            {
                return;
            }

            this.IsBeginDrag = false;
            this.GetComponent<CanvasGroup>().blocksRaycasts = true; // 是否射线检测

            // 还原到原来的位置
            this.transform.SetParent(this.parent, false);
            this.transform.position = this.parent.position;
            GameObject g = e.pointerCurrentRaycast.gameObject; // 要交换的盒子
            if (g == null)
            {
                return; // 不能拖拽到屏幕外面
            }

            Transform imageBox; // 拖拽到的盒子

            // 放到有道具的位置时
            if (g.name.Equals("Item"))
            {
                imageBox = g.transform.parent;
            }
            else
            {
                return;
            }

            // 通知Controller数据位置变换
            this.ExchangeItem(this.index, imageBox.GetSiblingIndex());
            this.SetBorderColor(imageBox.GetSiblingIndex(), "item");

            // // 放到拖到的位置
            // parent.SetSiblingIndex(imageBox.GetSiblingIndex());
            // // 将拖到的盒子放在拖拽的物体位置
            // imageBox.SetSiblingIndex(index);
        }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData e)
        {
            // 修改选中边框颜色
            this.SetBorderColor(this.parent.GetSiblingIndex(), "item");

            // 道具索引
            int i = this.transform.parent.GetSiblingIndex();
            AItem item = this.GetItem(i);
            this.ShowInfo(item);
            this.SetSelect(i, item);
        }

        /// <summary>
        /// 设置选择的道具
        /// </summary>
        /// <param name="i">道具索引</param>
        /// <param name="item">道具</param>
        public abstract void SetSelect(int i, AItem item);

        public void Awake()
        {
            // selectAndShow = Resources.Load<SelectAndShowEventSO>("SO/SelectAndShowEvent");
        }

        public void Start()
        {
            this.parent = this.transform.parent;
            if (this.parent == null)
            {
                LogManager.Instance.Log("parent Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }
        }
    }

    /// <summary>
    /// 通过交换盒子下面的Item实现交换
    /// </summary>
    /// <param name="e"></param>
    // public void OnEndDrag(PointerEventData e)
    // {
    //     GameObject g = e.pointerCurrentRaycast.gameObject;
    //     if (g.name.Equals("itemImage")) // 放到有道具位置时
    //     {
    //         // 数据位置变换
    //         Item temp = myBag.itemList[index];
    //         myBag.itemList[index] = myBag.itemList[g.transform.parent.parent.GetSiblingIndex()];
    //         myBag.itemList[g.transform.parent.parent.GetSiblingIndex()] = temp;
    //         // 放到拖到的位置
    //         transform.SetParent(g.transform.parent.parent);
    //         transform.position = transform.parent.position;
    //         // 将item换到被拖拽的原父物体上,实现交换
    //         g.transform.parent.position = originalParent.position;
    //         g.transform.parent.SetParent(originalParent);
    //     }
    //     else if (g.name.Equals("ImageBox")) // 放到空位置时
    //     {
    //         // 数据位置变换
    //         myBag.itemList[g.transform.GetSiblingIndex()] = myBag.itemList[index];
    //         if (g.transform.GetSiblingIndex() != index)
    //         {
    //             myBag.itemList[index] = null;
    //         }
    //         // 放到拖到的位置
    //         transform.SetParent(g.transform);
    //         transform.position = transform.parent.position;
    //     }
    //     else
    //     {
    //         // 拖到其他位置回到原位
    //         transform.SetParent(originalParent);
    //         transform.position = originalParent.position;
    //     }
    //     GetComponent<CanvasGroup>().blocksRaycasts = true; // 是否阻止射线投射
    // }
}
