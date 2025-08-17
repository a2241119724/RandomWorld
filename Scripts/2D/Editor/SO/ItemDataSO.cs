namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 创建物品数据
    /// </summary>
    [CreateAssetMenu(menuName = "SO/ItemDataSO", order = 0)]
    public class ItemDataSO : ScriptableObject
    {
        public List<ItemData> ItemDatas;
    }
}