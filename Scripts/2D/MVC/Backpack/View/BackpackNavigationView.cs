namespace LAB2D
{
    public class BackpackNavigationView : MVCNavigationView
    {
        public static BackpackNavigationView Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            CurItemType = ItemType.Weapon;
        }

        void Start()
        {
            bindButton(ItemType.Weapon, ItemType.BackpackOther);
        }

        protected override void init()
        {
            BackpackMenuPanel.Instance.Select.Init();
        }
    }
}
