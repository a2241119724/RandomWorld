namespace LAB2D
{
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
