namespace LAB2D
{
    /// <summary>
    /// 建造导航按钮UI
    /// </summary>
    public class BuildNavigationView : MVCNavigationView
    {
        public void Start()
        {
            this.BindButton(Item.ItemType.Room, Item.ItemType.BuildOther);
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            BuildMenuPanel.Instance.Select.Init();
        }

        private void OnEnable()
        {
            this.CurItemType = Item.ItemType.Room;
        }
    }
}
