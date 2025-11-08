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
            this.itemBox = PrefabConstant.BUILD_ITEM;
        }

        /// <inheritdoc/>
        protected override int GetQuantity(AItem item)
        {
            return ((ABuildItem)item).Quantity;
        }
    }
}
