namespace LAB2D.MVC.Backpack.View
{
    using LAB2D.Core;

    /// <summary>
    /// 背包导航按钮UI。
    /// </summary>
    public class BackpackNavigationView : MVCNavigationView
    {
        public static BackpackNavigationView Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        public void Start()
        {
            this.BindButton(AItem.ItemTypeEnum.Weapon, AItem.ItemTypeEnum.BackpackOther);
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            // 选择逻辑已迁移至 BackpackController.OnSelectItem
        }

        private void OnEnable()
        {
            this.CurItemType = AItem.ItemTypeEnum.Weapon;
        }
    }
}
