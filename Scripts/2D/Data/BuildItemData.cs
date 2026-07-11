namespace LAB2D.Data
{
    using LAB2D;
    using System;

    /// <summary>
    /// 建造物品数据
    /// </summary>
    [Serializable]
    public class BuildItemData : ItemData
    {
        /// <summary>
        /// 是否可通
        /// </summary>
        public bool IsPass;

        /// <summary>
        /// 是否需要建造
        /// </summary>
        public bool IsNeedBuild;
    }
}
