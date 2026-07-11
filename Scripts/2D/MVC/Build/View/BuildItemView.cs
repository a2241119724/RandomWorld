namespace LAB2D.MVC.Build.View
{
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Item.Build;
    using LAB2D.UI.Panel;
    /// <summary>
    /// 建造道具UI
    /// </summary>
    public class BuildItemView : MVCItemView
    {
        /// <inheritdoc/>
        public override void SetSelect(int i, AItem item)
        {
            // // 发布事件,GetSiblingIndex索引(第几个孩子)
            // selectAndShow.select(item);
            BuildMenuPanel.Instance.Select.SelectItemIndex = i;
            BuildMenuPanel.Instance.Select.Item = (ABuildItem)item;
        }
    }
}
