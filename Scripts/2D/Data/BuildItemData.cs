namespace LAB2D.Data
{
    using LAB2D;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 建造所需的一种资源及其数量。
    /// 在 BuildItemDataSO 中配置，实现数据驱动的建造材料需求。
    /// </summary>
    [Serializable]
    public struct ResourceCost
    {
        /// <summary>
        /// 资源名称（对应 ItemData.EnName，通过 ItemDataManager 查找 ID）
        /// </summary>
        public string ItemName;

        /// <summary>
        /// 所需数量
        /// </summary>
        public int Count;
    }

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

        /// <summary>
        /// 建造所需资源列表（在 SO 中配置）。
        /// 为空时 fallback 到默认 CustomWood x5。
        /// </summary>
        public List<ResourceCost> BuildCosts;
    }
}
