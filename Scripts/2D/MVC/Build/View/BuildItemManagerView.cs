namespace LAB2D
{
    public class BuildItemManagerView : MVCItemManagerView<BuildItemView, BuildModel>
    {
        public static BuildItemManagerView Instance { private set; get; }

        public override void Awake()
        {
            base.Awake();
            Instance = this;
            itemBox = ResourcesManager.Instance.GetPrefab("BuildItem");
        }

        protected override int getQuantity(Item item)
        {
            return ((BuildItem)item).Quantity;
        }
    }
}

