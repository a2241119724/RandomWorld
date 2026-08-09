namespace LAB2D.UI.Panel
{
    using LAB2D;

    /// <summary>
    /// 面板中选择的道具数据
    /// </summary>
    public class SelectItemData
    {
        /// <summary>
        /// 选中的道具索引
        /// </summary>
        public int SelectItemIndex = -1;

        /// <summary>
        /// 选中的道具数据
        /// </summary>
        public AItem Item = null;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            this.SelectItemIndex = -1;
            this.Item = null;
        }
    }
}
