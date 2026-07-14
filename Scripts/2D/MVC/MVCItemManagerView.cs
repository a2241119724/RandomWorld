namespace LAB2D.MVC
{
    using LAB2D;
    using LAB2D.Item;
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
        protected string itemBox;
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
        public event Func<int, AItem> GetItem;

        /// <summary>
        /// 展示信息
        /// </summary>
        public event Action<AItem> ShowInfo;

        /// <summary>
        /// 选择道具
        /// </summary>
        public event Action<int, AItem> SelectItem;

        public virtual void Awake()
        {
            this.content = this.transform.GetComponent<ScrollRect>().content;
            if (this.content == null)
            {
                LogManager.Instance.Log("content Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 确保 content 有 GridLayoutGroup（场景中可能已配置，代码兜底）
            GridLayoutGroup gridLayout = this.content.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = this.content.gameObject.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(100, 120);
                gridLayout.spacing = new Vector2(5, 5);
                gridLayout.padding = new RectOffset(5, 5, 5, 5);
                gridLayout.childAlignment = TextAnchor.UpperCenter;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                RectTransform contentRect = this.content.GetComponent<RectTransform>();
                float cellWidth = gridLayout.cellSize.x + gridLayout.spacing.x;
                gridLayout.constraintCount = Mathf.Max(1, Mathf.FloorToInt((contentRect.rect.width - gridLayout.padding.left - gridLayout.padding.right + gridLayout.spacing.x) / cellWidth));
            }

            // 确保 content 有 ContentSizeFitter 以自动调整高度
            ContentSizeFitter csf = this.content.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = this.content.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            this.ItemsView = new List<IV>();
        }

        /// <summary>
        /// 更新仓库界面
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="model">Model</param>
        public void UpdateView(AItem.ItemTypeEnum type, M model)
        {
            if (model == null)
            {
                LogManager.Instance.Log("data is null!!!", LogManager.LogLevelEnum.Error);
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

                GameObject g = ResourceManager.Instance.Instantiate(this.itemBox);
                if (g == null)
                {
                    return;
                }

                g.transform.SetParent(this.content, false);

                // 根据品质设置根节点背景颜色
                if (model.Get(type, i) is ABackpackItem backpackItem)
                {
                    Color qualityColor = EquipmentLootTool.GetQualityColor(backpackItem.Quality);
                    Image rootImage = g.GetComponent<Image>();
                    rootImage.fillCenter = true;
                    rootImage.color = qualityColor;
                }

                // t.transform.localScale = Vector3.one; // 控制大小
                Text itemInfoText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(g, "ItemInfo");
                if (itemInfoText != null)
                {
                    itemInfoText.text = this.GetQuantity(model.Get(type, i)).ToString();
                }

                Image image = LAB2D.Tool.Tool.GetComponentInChildren<Image>(g, "ItemImage");
                if (image != null)
                {
                    image.sprite = ResourceManager.Instance.GetImage(ItemDataManager.Instance.GetById(model.Get(type, i).Id).EnName);
                    image.preserveAspect = true;
                }

                Transform itemTransform = g.transform.Find("Item");
                if (itemTransform == null)
                {
                    LogManager.Instance.Log($"UpdateView: 在 itemBox prefab 中找不到 'Item' 子节点，请检查 prefab 结构", LogManager.LogLevelEnum.Error);
                    Destroy(g);
                    continue;
                }

                IV itemView = itemTransform.GetComponent<IV>();
                if (itemView == null)
                {
                    LogManager.Instance.Log($"UpdateView: 'Item' 子节点上缺少 {typeof(IV).Name} 组件，请检查 prefab 结构", LogManager.LogLevelEnum.Error);
                    Destroy(g);
                    continue;
                }

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
                itemView.ShowInfo += (AItem a) =>
                {
                    this.ShowInfo(a);
                };
                itemView.SelectItem += (int idx, AItem a) =>
                {
                    this.SelectItem?.Invoke(idx, a);
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
            Text t = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.content.GetChild(index).gameObject, "ItemInfo");

            // 字符串转整数
            int count = int.Parse(t.text);
            --count;
            t.text = count.ToString();
        }

        /// <summary>
        /// 获取道具数量
        /// </summary>
        /// <param name="item">道具</param>
        /// <returns>数量</returns>
        protected abstract int GetQuantity(AItem item);
    }
}
