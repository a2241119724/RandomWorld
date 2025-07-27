namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 由于MonoBehaviour没有实现单例
    /// </summary>
    /// <typeparam name="IV">MVCItemView</typeparam>
    /// <typeparam name="M">MVCModel</typeparam>
    public abstract class MVCItemManagerView<IV, M> : MonoBehaviour
        where M : MVCModel
        where IV : MVCItemView
    {
        /// <summary>
        /// 所有的道具视图
        /// </summary>
        public List<IV> ItemsView;

        /// <summary>
        /// 每个道具
        /// </summary>
        protected GameObject itemBox;
        private Transform content; // 背包的栅格框

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
        public event Func<int, Item> GetItem;

        /// <summary>
        /// 展示信息
        /// </summary>
        public event Action<Item> ShowInfo;

        public virtual void Awake()
        {
            this.content = this.transform.GetComponent<ScrollRect>().content;
            if (this.content == null)
            {
                LogManager.Instance.Log("content Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.ItemsView = new List<IV>();
        }

        /// <summary>
        /// 更新仓库界面
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="model">Model</param>
        public void UpdateView(ItemType type, M model)
        {
            if (model == null)
            {
                LogManager.Instance.Log("data is null!!!", LogManager.LogLevel.Error);
                return;
            }

            // 销毁所有ItemBox
            // for (int i = 0; i < content.childCount; i++)
            // {
            //     Destroy(content.GetChild(i).gameObject);
            // }
            int len = this.ItemsView.Count;
            for (int i = 0; i < len; i++)
            {
                // GameObject g = itemsView[i].transform.parent.gameObject;
                Destroy(this.ItemsView[i].transform.parent.gameObject);
            }

            this.ItemsView.Clear();

            // 重新创建所有Item
            for (int i = 0; i < model.Count(type); i++)
            {
                if (model.Get(type, i).Id == -1)
                {
                    continue;
                }

                GameObject g = Instantiate(this.itemBox);
                if (g == null)
                {
                    LogManager.Instance.Log("itemBox Instantiate Error!!!", LogManager.LogLevel.Error);
                    return;
                }

                g.name = this.itemBox.name;
                g.transform.SetParent(this.content, false);

                // t.transform.localScale = Vector3.one; // 控制大小
                Tool.GetComponentInChildren<Text>(g, "ItemInfo").text = this.GetQuantity(model.Get(type, i)).ToString();
                Image image = Tool.GetComponentInChildren<Image>(g, "ItemImage");
                image.sprite = ResourcesManager.Instance.GetImage(ItemDataManager.Instance.GetById(model.Get(type, i).Id).ImageName);
                image.preserveAspect = true;
                IV itemView = g.transform.Find("Item").GetComponent<IV>();

                // 添加到ItemView
                itemView.ExchangeItem += (int a, int b) =>
                {
                    this.ExchangeItem(a, b);
                };
                itemView.SetBorderColor += (int a, string b) =>
                {
                    this.SetBorderColor(a, b);
                };
                itemView.GetItem += (int a) =>
                {
                    return this.GetItem(a);
                };
                itemView.ShowInfo += (Item a) =>
                {
                    this.ShowInfo(a);
                };
                this.ItemsView.Add(itemView);
            }
        }

        /// <summary>
        /// 减少道具数量
        /// </summary>
        /// <param name="index">道具索引</param>
        public void ReduceQuantityUI(int index)
        {
            Text t = Tool.GetComponentInChildren<Text>(this.content.GetChild(index).gameObject, "ItemInfo");

            // string -> int
            int count = int.Parse(t.text);
            --count;
            t.text = count.ToString();
        }

        /// <summary>
        /// 获取道具数量
        /// </summary>
        /// <param name="item">道具</param>
        /// <returns>数量</returns>
        protected abstract int GetQuantity(Item item);
    }
}
