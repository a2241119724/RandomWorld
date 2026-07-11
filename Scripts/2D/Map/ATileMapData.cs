namespace LAB2D.Map
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 瓦片地图数据,用于存储
    /// </summary>
    [Serializable]
    public class ATileMapData
    {
        /// <summary>
        /// 值: TileBase名称
        /// </summary>
        public Dictionary<Vector3IntLAB, string> PosMap;

        public ATileMapData()
        {
            this.PosMap = new Dictionary<Vector3IntLAB, string>();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="pos">位置</param>
        public void Remove(Vector3Int pos)
        {
            this.PosMap.Remove(Vector3IntLAB.ToVector3IntLAB(pos));
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="tileBase">瓦片</param>
        public void Add(Vector3Int pos, string tileBase)
        {
            this.PosMap.Add(Vector3IntLAB.ToVector3IntLAB(pos), tileBase);
        }

        /// <summary>
        /// 包含
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>是否</returns>
        public bool ContainKey(Vector3Int pos)
        {
            return this.PosMap.ContainsKey(Vector3IntLAB.ToVector3IntLAB(pos));
        }
    }
}
