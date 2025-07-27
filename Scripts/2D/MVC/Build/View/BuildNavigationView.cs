namespace LAB2D
{
    /// <summary>
    /// 建造导航按钮UI
    /// </summary>
    public class BuildNavigationView : MVCNavigationView
    {
        /// <inheritdoc/>
        protected override void Init()
        {
            BuildMenuPanel.Instance.Select.Init();
        }

        private void OnEnable()
        {
            this.CurItemType = ItemType.Room;
        }

        private void Start()
        {
            this.BindButton(ItemType.Room, ItemType.BuildOther);
        }
    }
}
