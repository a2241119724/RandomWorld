namespace LAB2D.SO
{
    using LAB2D;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 创建物品数据
    /// </summary>
    [CreateAssetMenu(menuName = "SO/ItemDataSO", order = 0)]
    public class ItemDataSO : ScriptableObject
    {
        /// <summary>
        /// 物品类型
        /// </summary>
        public AItem.ItemTypeEnum ItemType;

        /// <summary>
        /// 物品数据
        /// </summary>
        public List<ItemData> ItemDatas;

        private static readonly int TypeInterval = 100000; // 类型间隔

        public void OnEnable()
        {
            if (this.ItemDatas == null)
            {
                this.ItemDatas = new List<ItemData>();
            }

            int index = ((int)this.ItemType) * TypeInterval;
            foreach (var itemData in this.ItemDatas)
            {
                itemData.Id = index++;
                itemData.Type = this.ItemType;
                itemData.EnsureTaskTime();
            }
        }
    }
}
