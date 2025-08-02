namespace LAB2D
{
    /// <summary>
    /// 背包导航按钮UI
    /// </summary>
    public class BackpackNavigationView : MVCNavigationView
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static BackpackNavigationView Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            this.BindButton(Item.ItemType.Weapon, Item.ItemType.BackpackOther);
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            BackpackMenuPanel.Instance.Select.Init();
        }

        private void OnEnable()
        {
            this.CurItemType = Item.ItemType.Weapon;
        }
    }
}
