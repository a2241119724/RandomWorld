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

        /// <inheritdoc/>
        protected override void Init()
        {
            BackpackMenuPanel.Instance.Select.Init();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            this.CurItemType = ItemType.Weapon;
        }

        private void Start()
        {
            this.BindButton(ItemType.Weapon, ItemType.BackpackOther);
        }
    }
}
