namespace LAB2D.MVC.Build.Controller
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.MVC.Build.Model;
    using LAB2D.MVC.Build.View;
    using System.Collections.Generic;

    /// <summary>
    /// 建造控制器
    /// </summary>
    public class BuildController : MVCController<BuildItemManagerView, BuildModel, BuildNavigationView, BuildItemView, BuildInfoView>
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static BuildController Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            this.itemManagerView = LAB2D.Tool.Tool.GetComponentInChildren<BuildItemManagerView>(this.gameObject, "Inventory");
            this.navigationView = LAB2D.Tool.Tool.GetComponentInChildren<BuildNavigationView>(this.gameObject, "Navigation");
            this.infoView = LAB2D.Tool.Tool.GetComponentInChildren<BuildInfoView>(this.gameObject, "Info");
            base.Awake();
            Instance = this;

            // 如果背包为空添加一个武器到背包
            this.navigationView.CurItemType = AItem.ItemTypeEnum.Room;
            if (this.model.IsNull(this.navigationView.CurItemType))
            {
                List<AItem> items = ServiceLocator.Get<ItemInstanceFactory>().GetBuildItems();
                foreach (AItem item in items)
                {
                    this.AddItem(item);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSelectItem(int index, AItem item)
        {
            BuildMenuPanel.Instance.Select.SelectItemIndex = index;
            BuildMenuPanel.Instance.Select.Item = item as ABuildItem;
        }
    }
}
