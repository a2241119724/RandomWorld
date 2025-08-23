namespace LAB2D
{
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
        public List<ResurceData> DropItemDatas;

        /// <summary>
        /// 掉落数据
        /// </summary>
        [Serializable]
        public class ResurceData
        {
            /// <summary>
            /// 资源名称
            /// </summary>
            public string Name;

            /// <summary>
            /// key: 资源名称
            /// </summary>
            public List<DropData> DropData;
        }

        /// <summary>
        /// 掉落数据
        /// </summary>
        [Serializable]
        public class DropData
        {
            /// <summary>
            /// 掉落物资源名称
            /// </summary>
            public string Name;

            /// <summary>
            /// 掉落物数量
            /// </summary>
            public int Count;
        }
    }
}