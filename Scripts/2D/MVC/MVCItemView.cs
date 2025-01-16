using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LAB2D
{
    public abstract class MVCItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public event Action<int,int> exchangeItem;
        public event Action<int,string> setBorderColor;
        public event Func<int,Item> get;
        public event Action<Item> showInfo;

        public bool IsDrag { get; set; } // 是否可以拖拽
        /// <summary>
        /// 是否开始拖拽(解决拖拽不能拽的Item会只执行OnEndDrag)
        /// </summary>
        public bool isBeginDrag;

        private int index; // 当前拖拽格子的索引
        private Transform parent; // 父物体
        //private SelectAndShowEventSO selectAndShow = null;
        private Vector3 offset; // 鼠标点击位置与物体中心的偏移量

        void Awake()
        {
            //selectAndShow = Resources.Load<SelectAndShowEventSO>("SO/SelectAndShowEvent");
        }

        void Start()
        {
            parent = transform.parent;
            if (parent == null)
            {
                Debug.LogError("parent Not Found!!!");
                return;
            }
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!IsDrag) return;
            isBeginDrag = true;
            // originalParent = transform.parent;
            index = parent.GetSiblingIndex();
            offset = transform.position - e.pressEventCamera.ScreenToWorldPoint(e.position);
            offset.z = 0;
            transform.SetParent(parent.parent.parent.parent.parent.parent, false); // 拖拽时防止被父物体遮挡
            GetComponent<CanvasGroup>().blocksRaycasts = false; // 是否射线检测
        }

        public void OnDrag(PointerEventData e)
        {
            if (!IsDrag) return;
            Vector3 v = e.pressEventCamera.ScreenToWorldPoint(e.position); // 将视口坐标转换为世界坐标
            v.z = -5; // 保证在射相机(-20)与面板(0)之间
            transform.position = v + offset;
        }

        /// <summary>
        /// 通过交换两个盒子实现交换
        /// </summary>
        /// <param name="e"></param>
        public void OnEndDrag(PointerEventData e)
        {
            if (!IsDrag || !isBeginDrag) return;
            isBeginDrag = false;
            GetComponent<CanvasGroup>().blocksRaycasts = true; // 是否射线检测
            // 还原到原来的位置
            transform.SetParent(parent, false);
            transform.position = parent.position;
            GameObject g = e.pointerCurrentRaycast.gameObject; // 要交换的盒子
            if (g == null) return; // 不能拖拽到屏幕外面
            Transform imageBox; // 拖拽到的盒子
            if (g.name.Equals("Item")) // 放到有道具的位置时
            {
                imageBox = g.transform.parent;
            }
            else
            {
                return;
            }
            // 通知Controller数据位置变换
            exchangeItem(index, imageBox.GetSiblingIndex());
            setBorderColor(imageBox.GetSiblingIndex(), "item");
            //// 放到拖到的位置
            //parent.SetSiblingIndex(imageBox.GetSiblingIndex());
            //// 将拖到的盒子放在拖拽的物体位置
            //imageBox.SetSiblingIndex(index);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData e)
        {
            // 修改选中边框颜色
            setBorderColor(parent.GetSiblingIndex(), "item");
            // 道具索引
            int i = transform.parent.GetSiblingIndex();
            Item item = get(i);
            showInfo(item);
            setSelect(i, item);
        }

        public abstract void setSelect(int i, Item item);
    }

    /// <summary>
    /// 通过交换盒子下面的Item实现交换
    /// </summary>
    /// <param name="e"></param>
    //public void OnEndDrag(PointerEventData e)
    //{
    //    GameObject g = e.pointerCurrentRaycast.gameObject;
    //    if (g.name.Equals("itemImage")) // 放到有道具位置时
    //    {
    //        // 数据位置变换
    //        Item temp = myBag.itemList[index];
    //        myBag.itemList[index] = myBag.itemList[g.transform.parent.parent.GetSiblingIndex()];
    //        myBag.itemList[g.transform.parent.parent.GetSiblingIndex()] = temp;
    //        // 放到拖到的位置
    //        transform.SetParent(g.transform.parent.parent);
    //        transform.position = transform.parent.position;
    //        // 将item换到被拖拽的原父物体上,实现交换
    //        g.transform.parent.position = originalParent.position;
    //        g.transform.parent.SetParent(originalParent);
    //    }
    //    else if (g.name.Equals("ImageBox")) // 放到空位置时
    //    {
    //        // 数据位置变换
    //        myBag.itemList[g.transform.GetSiblingIndex()] = myBag.itemList[index];
    //        if (g.transform.GetSiblingIndex() != index)
    //        {
    //            myBag.itemList[index] = null;
    //        }
    //        // 放到拖到的位置
    //        transform.SetParent(g.transform);
    //        transform.position = transform.parent.position;
    //    }
    //    else
    //    {
    //        // 拖到其他位置回到原位
    //        transform.SetParent(originalParent);
    //        transform.position = originalParent.position;
    //    }
    //    GetComponent<CanvasGroup>().blocksRaycasts = true; // 是否阻止射线投射
    //}
}
