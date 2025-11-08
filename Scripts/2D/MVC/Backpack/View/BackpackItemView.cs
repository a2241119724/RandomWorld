namespace LAB2D
{
    /// <summary>
    /// 背包道具UI
    /// </summary>
    public class BackpackItemView : MVCItemView
    {
        /// <inheritdoc/>
        public override void SetSelect(int i, AItem item)
        {
            //// 发布事件,GetSiblingIndex索引(第几个孩子)
            // selectAndShow.select(item);
            BackpackMenuPanel.Instance.Select.SelectItemIndex = i;
            BackpackMenuPanel.Instance.Select.Item = (ABackpackItem)item;
        }
    }
}