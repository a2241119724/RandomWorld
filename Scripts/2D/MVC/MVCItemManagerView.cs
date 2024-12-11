using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    /// <summary>
    /// ����MonoBehaviourû��ʵ�ֵ���
    /// </summary>
    /// <typeparam name="IV"></typeparam>
    public abstract class MVCItemManagerView<IV, M> : MonoBehaviour
        where M : MVCModel
        where IV : MVCItemView
    {
        public event Action<int, int> exchangeItem;
        public event Action<int, string> setBorderColor;
        public event Func<int, Item> get;
        public event Action<Item> showInfo;
        public List<IV> itemsView; // ���еĵ�����ͼ

        private Transform content; // ������դ���
        protected GameObject itemBox; // ÿ������

        public virtual void Awake()
        {
            content = transform.GetComponent<ScrollRect>().content;
            if (content == null)
            {
                Debug.LogError("content Not Found!!!");
                return;
            }
            itemsView = new List<IV>();
        }

        /// <summary>
        /// ���²ֿ����
        /// </summary>
        public void updateView(ItemType type, M model)
        {
            if (model == null)
            {
                Debug.LogError("data is null!!!");
                return;
            }
            //��������ItemBox
            //for (int i = 0; i < content.childCount; i++)
            //{
            //    Destroy(content.GetChild(i).gameObject);
            //}
            int len = itemsView.Count;
            for (int i = 0; i < len; i++)
            {
                //GameObject g = itemsView[i].transform.parent.gameObject;
                Destroy(itemsView[i].transform.parent.gameObject);
            }
            itemsView.Clear();
            // ���´�������Item
            for (int i = 0; i < model.count(type); i++)
            {
                if (model.get(type,i).id == -1) continue;
                GameObject g = Instantiate(itemBox);
                if (g == null)
                {
                    Debug.LogError("itemBox Instantiate Error!!!");
                    return;
                }
                g.name = itemBox.name;
                g.transform.SetParent(content, false);
                //t.transform.localScale = Vector3.one; // ���ƴ�С
                Tool.GetComponentInChildren<Text>(g, "ItemInfo").text = getQuantity(model.get(type,i)).ToString();
                Image image = Tool.GetComponentInChildren<Image>(g, "ItemImage");
                image.sprite = ResourcesManager.Instance.getImage(ItemDataManager.Instance.getById(model.get(type, i).id).imageName);
                image.preserveAspect = true;
                IV itemView = g.transform.Find("Item").GetComponent<IV>();
                // ��ӵ�ItemView
                itemView.exchangeItem += (int a, int b) => {
                    exchangeItem(a, b);
                };
                itemView.setBorderColor += (int a, string b) => {
                    setBorderColor(a,b);
                };
                itemView.get += (int a) => {
                    return get(a);
                };
                itemView.showInfo += (Item a) => {
                    showInfo(a);
                };
                itemsView.Add(itemView);
            }
        }
        protected abstract int getQuantity(Item item);

        public void reduceQuantityUI(int index)
        {
            Text t = Tool.GetComponentInChildren<Text>(content.GetChild(index).gameObject, "ItemInfo");
            // string -> int
            int count = int.Parse(t.text);
            --count;
            t.text = count.ToString();
        }
    }
}
