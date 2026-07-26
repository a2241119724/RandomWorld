namespace LAB2D.SO
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Item;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 创建建造物品数据
    /// </summary>
    [CreateAssetMenu(menuName = "SO/BuildItemDataSO", order = 0)]
    public class BuildItemDataSO : ScriptableObject
    {
        /// <summary>
        /// 物品类型
        /// </summary>
        public AItem.ItemTypeEnum ItemType;

        /// <summary>
        /// 物品数据
        /// </summary>
        public List<BuildItemData> BuildItemDatas;

        private static readonly int TypeInterval = 100000; // 类型间隔

        public void OnEnable()
        {
            if (this.BuildItemDatas == null)
            {
                this.BuildItemDatas = new List<BuildItemData>();
            }

            int index = ((int)this.ItemType) * TypeInterval;
            foreach (var itemData in this.BuildItemDatas)
            {
                itemData.Id = index++;
                itemData.Type = this.ItemType;
                itemData.EnsureTaskTime();
            }
        }
    }
}
