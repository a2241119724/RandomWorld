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

        public bool IsDrag { get; set; } // �Ƿ������ק
        /// <summary>
        /// �Ƿ�ʼ��ק(�����ק����ק��Item��ִֻ��OnEndDrag)
        /// </summary>
        public bool isBeginDrag;

        private int index; // ��ǰ��ק���ӵ�����
        private Transform parent; // ������
        //private SelectAndShowEventSO selectAndShow = null;
        private Vector3 offset; // �����λ�����������ĵ�ƫ����

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
            transform.SetParent(parent.parent.parent.parent.parent.parent, false); // ��קʱ��ֹ���������ڵ�
            GetComponent<CanvasGroup>().blocksRaycasts = false; // �Ƿ����߼��
        }

        public void OnDrag(PointerEventData e)
        {
            if (!IsDrag) return;
            Vector3 v = e.pressEventCamera.ScreenToWorldPoint(e.position); // ���ӿ�����ת��Ϊ��������
            v.z = -5; // ��֤�������(-20)�����(0)֮��
            transform.position = v + offset;
        }

        /// <summary>
        /// ͨ��������������ʵ�ֽ���
        /// </summary>
        /// <param name="e"></param>
        public void OnEndDrag(PointerEventData e)
        {
            if (!IsDrag || !isBeginDrag) return;
            isBeginDrag = false;
            GetComponent<CanvasGroup>().blocksRaycasts = true; // �Ƿ����߼��
            // ��ԭ��ԭ����λ��
            transform.SetParent(parent, false);
            transform.position = parent.position;
            GameObject g = e.pointerCurrentRaycast.gameObject; // Ҫ�����ĺ���
            if (g == null) return; // ������ק����Ļ����
            Transform imageBox; // ��ק���ĺ���
            if (g.name.Equals("Item")) // �ŵ��е��ߵ�λ��ʱ
            {
                imageBox = g.transform.parent;
            }
            else
            {
                return;
            }
            // ֪ͨController����λ�ñ任
            exchangeItem(index, imageBox.GetSiblingIndex());
            setBorderColor(imageBox.GetSiblingIndex(), "item");
            //// �ŵ��ϵ���λ��
            //parent.SetSiblingIndex(imageBox.GetSiblingIndex());
            //// ���ϵ��ĺ��ӷ�����ק������λ��
            //imageBox.SetSiblingIndex(index);
        }

        /// <summary>
        /// ����¼�
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData e)
        {
            // �޸�ѡ�б߿���ɫ
            setBorderColor(parent.GetSiblingIndex(), "item");
            // ��������
            int i = transform.parent.GetSiblingIndex();
            Item item = get(i);
            showInfo(item);
            setSelect(i, item);
        }

        public abstract void setSelect(int i, Item item);
    }

    /// <summary>
    /// ͨ���������������Itemʵ�ֽ���
    /// </summary>
    /// <param name="e"></param>
    //public void OnEndDrag(PointerEventData e)
    //{
    //    GameObject g = e.pointerCurrentRaycast.gameObject;
    //    if (g.name.Equals("itemImage")) // �ŵ��е���λ��ʱ
    //    {
    //        // ����λ�ñ任
    //        Item temp = myBag.itemList[index];
    //        myBag.itemList[index] = myBag.itemList[g.transform.parent.parent.GetSiblingIndex()];
    //        myBag.itemList[g.transform.parent.parent.GetSiblingIndex()] = temp;
    //        // �ŵ��ϵ���λ��
    //        transform.SetParent(g.transform.parent.parent);
    //        transform.position = transform.parent.position;
    //        // ��item��������ק��ԭ��������,ʵ�ֽ���
    //        g.transform.parent.position = originalParent.position;
    //        g.transform.parent.SetParent(originalParent);
    //    }
    //    else if (g.name.Equals("ImageBox")) // �ŵ���λ��ʱ
    //    {
    //        // ����λ�ñ任
    //        myBag.itemList[g.transform.GetSiblingIndex()] = myBag.itemList[index];
    //        if (g.transform.GetSiblingIndex() != index)
    //        {
    //            myBag.itemList[index] = null;
    //        }
    //        // �ŵ��ϵ���λ��
    //        transform.SetParent(g.transform);
    //        transform.position = transform.parent.position;
    //    }
    //    else
    //    {
    //        // �ϵ�����λ�ûص�ԭλ
    //        transform.SetParent(originalParent);
    //        transform.position = originalParent.position;
    //    }
    //    GetComponent<CanvasGroup>().blocksRaycasts = true; // �Ƿ���ֹ����Ͷ��
    //}
}
