namespace LAB2D.MVC
{
    using LAB2D;
    using LAB2D.Item;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 用户：调用View和Controller
    /// 模型
    /// 视图：事件通知Controller
    /// 控制器：调用View和Model
    /// </summary>
    /// <typeparam name="IMV">ItemManagerView</typeparam>
    /// <typeparam name="M">Model</typeparam>
    /// <typeparam name="NV">NavigationView</typeparam>
    /// <typeparam name="IV">ItemView</typeparam>
    /// <typeparam name="IV_">InfoView</typeparam>
    public abstract class MVCController<IMV, M, NV, IV, IV_> : MonoBehaviourInit
        where M : MVCModel, new()
        where IV : MVCItemView
        where IMV : MVCItemManagerView<IV, M>
        where NV : MVCNavigationView
        where IV_ : MVCInfoView
    {
        /// <summary>
        /// 道具管理视图
        /// </summary>
        protected IMV itemManagerView;

        /// <summary>
        /// 模型
        /// </summary>
        protected M model;

        /// <summary>
        /// 导航视图
        /// </summary>
        protected NV navigationView;

        /// <summary>
        /// 信息视图
        /// </summary>
        protected IV_ infoView;

        private Color btnOriginColor; // 按钮原始颜色

        public virtual void Awake()
        {
            // 添加到ItemManagerView
            this.itemManagerView.ExchangeItem += this.ExchangeItem;
            this.itemManagerView.SetBorderColor += this.SetBorderColor;
            this.itemManagerView.GetItem += this.GetItem;
            this.itemManagerView.ShowInfo += this.ShowInfo;
            this.itemManagerView.SelectItem += this.OnSelectItem;
            this.model = new M();
            this.btnOriginColor = this.navigationView.GetComponentsInChildren<Button>()[0].GetComponent<RoundCorner>().color;
            this.SetBorderColor(0, "navigation");
            this.navigationView.OnClick += (int index) =>
            {
                this.SetBorderColor(index, "navigation");
                this.UpdateInventory();
            };

            // 设置初始颜色
        }

        /// <summary>
        /// 添加道具
        /// </summary>
        /// <param name="item">道具</param>
        public virtual void AddItem(AItem item)
        {
            this.model.Add(item);

            // 不能更新，由于更新需要打开背包
            // updateInventory();
        }

        /// <summary>
        /// 交换道具
        /// </summary>
        /// <param name="index1">道具1索引</param>
        /// <param name="index2">道具2索引</param>
        public void ExchangeItem(int index1, int index2)
        {
            this.model.Exchange(this.navigationView.CurItemType, index1, index2);
            this.UpdateInventory();
        }

        /// <summary>
        /// 删除道具
        /// </summary>
        /// <param name="index">索引</param>
        public void DeleteItem(int index)
        {
            this.model.Delete(this.navigationView.CurItemType, index);
            this.UpdateInventory();
        }

        /// <summary>
        /// 获取道具
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>道具</returns>
        public AItem GetItem(int index)
        {
            return this.model.Get(this.navigationView.CurItemType, index);
        }

        /// <summary>
        /// 减少道具数量
        /// </summary>
        /// <param name="item">道具</param>
        public void ReduceQuantity(AItem item)
        {
            this.model.ReduceQuantity(this.navigationView.CurItemType, item);
        }

        /// <summary>
        /// 获取道具索引
        /// </summary>
        /// <param name="item">道具</param>
        /// <returns>索引</returns>
        public int GetIndex(AItem item)
        {
            return this.model.GetIndex(this.navigationView.CurItemType, item);
        }

        /// <summary>
        /// 更新仓库界面
        /// </summary>
        public void UpdateInventory()
        {
            if (this.itemManagerView == null)
            {
                LogManager.Instance.Log("inventoryView is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.itemManagerView.UpdateView(this.navigationView.CurItemType, this.model);
        }

        /// <summary>
        /// 设置激活时的边框颜色
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="name">类型</param>
        public void SetBorderColor(int index, string name = "item")
        {
            switch (name)
            {
                case "item":
                    foreach (IV item in this.itemManagerView.ItemsView)
                    {
                        item.GetComponent<Image>().color = Color.white;
                        item.IsDrag = false;
                    }

                    this.itemManagerView.ItemsView[index].GetComponent<Image>().color = Color.red;
                    this.itemManagerView.ItemsView[index].IsDrag = true;
                    break;
                case "navigation":
                    Button[] btns = this.navigationView.GetComponentsInChildren<Button>();
                    foreach (Button btn in btns)
                    {
                        btn.GetComponent<RoundCorner>().color = this.btnOriginColor;
                    }

                    btns[index].GetComponent<RoundCorner>().color = new Color(100 / 255.0f, 120 / 255.0f, 150 / 255.0f, 255 / 255.0f);
                    break;
                default:
                    LogManager.Instance.Log("没有该类型边框可以修改!!!", LogManager.LogLevelEnum.Error);
                    break;
            }
        }

        /// <summary>
        /// 界面减减
        /// </summary>
        /// <param name="item">道具</param>
        public void ReduceQuantityUI(AItem item)
        {
            this.itemManagerView.ReduceQuantityUI(this.GetIndex(item));
        }

        /// <summary>
        /// 选择道具回调 — 子类可重写以更新 Panel 状态。
        /// 基类默认不做任何 Panel 操作，保持 MVC 分层干净。
        /// </summary>
        /// <param name="index">道具索引。</param>
        /// <param name="item">选中的道具。</param>
        protected virtual void OnSelectItem(int index, AItem item)
        {
        }

        /// <summary>
        /// 展示道具信息
        /// </summary>
        /// <param name="data">道具</param>
        public void ShowInfo(AItem data)
        {
            if (this.infoView == null)
            {
                LogManager.Instance.Log("infoView is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.infoView.ShowInfo(data);
        }

        private void OnEnable()
        {
            this.UpdateInventory();
        }
    }
}
