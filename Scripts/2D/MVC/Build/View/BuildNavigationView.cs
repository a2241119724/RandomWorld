namespace LAB2D.MVC.Build.View
{
    /// <summary>
    /// 建造导航按钮UI。
    /// </summary>
    public class BuildNavigationView : MVCNavigationView
    {
        public void Start()
        {
            this.BindButton(AItem.ItemTypeEnum.Room, AItem.ItemTypeEnum.BuildOther);
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            // 选择逻辑已迁移至 BuildController.OnSelectItem
        }

        private void OnEnable()
        {
            this.CurItemType = AItem.ItemTypeEnum.Room;
        }
    }
}
