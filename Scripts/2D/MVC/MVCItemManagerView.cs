using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    /// <summary>
    /// 由于MonoBehaviour没有实现单例
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
        public List<IV> itemsView; // 所有的道具视图

        private Transform content; // 背包的栅格框
        protected GameObject itemBox; // 每个道具

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
        /// 更新仓库界面
        /// </summary>
        public void updateView(ItemType type, M model)
        {
            if (model == null)
            {
                Debug.LogError("data is null!!!");
                return;
            }
            //销毁所有ItemBox
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
            // 重新创建所有Item
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
                //t.transform.localScale = Vector3.one; // 控制大小
                Tool.GetComponentInChildren<Text>(g, "ItemInfo").text = getQuantity(model.get(type,i)).ToString();
                Image image = Tool.GetComponentInChildren<Image>(g, "ItemImage");
                image.sprite = ResourcesManager.Instance.getImage(ItemDataManager.Instance.getById(model.get(type, i).id).imageName);
                image.preserveAspect = true;
                IV itemView = g.transform.Find("Item").GetComponent<IV>();
                // 添加到ItemView
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
