namespace LAB2D
{
    /// <summary>
    /// 建造道具UI
    /// </summary>
    public class BuildItemManagerView : MVCItemManagerView<BuildItemView, BuildModel>
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static BuildItemManagerView Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.itemBox = ResourcesManager.Instance.GetPrefab("BuildItem");
        }

        /// <inheritdoc/>
        protected override int GetQuantity(Item item)
        {
            return ((BuildItem)item).Quantity;
        }
    }
}
