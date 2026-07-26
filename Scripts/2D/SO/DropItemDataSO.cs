namespace LAB2D.SO
{
    using LAB2D;
    using LAB2D.Data;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 掉落数据
    /// </summary>
    [CreateAssetMenu(menuName = "SO/DropItemDataSO", order = 0)]
    public class DropItemDataSO : ScriptableObject
    {
        /// <summary>
        /// 掉落信息
        /// </summary>
        public List<ResourceDropItem> ResourceDropItems;

        /// <summary>
        /// 掉落数据
        /// </summary>
        [Serializable]
        public class ResourceDropItem
        {
            /// <summary>
            /// 资源名称
            /// </summary>
            public string Name;

            /// <summary>
            /// key: 资源名称
            /// </summary>
            public List<DropItem> DropItems;
        }
    }
}