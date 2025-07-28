namespace LAB2D
{
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
            this.itemManagerView = Tool.GetComponentInChildren<BuildItemManagerView>(this.gameObject, "Inventory");
            this.navigationView = Tool.GetComponentInChildren<BuildNavigationView>(this.gameObject, "Navigation");
            this.infoView = Tool.GetComponentInChildren<BuildInfoView>(this.gameObject, "Info");
            base.Awake();
            Instance = this;

            // 如果背包为空添加一个武器到背包
            this.navigationView.CurItemType = Item.ItemType.Room;
            if (this.model.IsNull(this.navigationView.CurItemType))
            {
                List<Item> items = ItemFactory.Instance.GetBuildItems();
                foreach (Item item in items)
                {
                    this.AddItem(item);
                }
            }
        }
    }
}
