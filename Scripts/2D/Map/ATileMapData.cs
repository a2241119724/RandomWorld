namespace LAB2D
{
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
        /// value: TileBase name
        /// </summary>
        public Dictionary<Vector3IntLAB, string> PosMaps;

        public ATileMapData()
        {
            this.PosMaps = new Dictionary<Vector3IntLAB, string>();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="pos">位置</param>
        public void Remove(Vector3Int pos)
        {
            this.PosMaps.Remove(Vector3IntLAB.ToVector3IntLAB(pos));
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="tileBase">瓦片</param>
        public void Add(Vector3Int pos, string tileBase)
        {
            this.PosMaps.Add(Vector3IntLAB.ToVector3IntLAB(pos), tileBase);
        }

        /// <summary>
        /// 包含
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>是否</returns>
        public bool ContainKey(Vector3Int pos)
        {
            return this.PosMaps.ContainsKey(Vector3IntLAB.ToVector3IntLAB(pos));
        }
    }

    /// <summary>
    /// 可序列化的Vector3Int
    /// </summary>
    [Serializable]
    public class Vector3IntLAB
    {
        /// <summary>
        /// X
        /// </summary>
        public int X;

        /// <summary>
        /// Y
        /// </summary>
        public int Y;

        /// <summary>
        /// Z
        /// </summary>
        public int Z;

        public Vector3IntLAB(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Vector3IntLAB to Vector3Int
        /// </summary>
        /// <param name="vector3IntLAB">Vector3IntLAB</param>
        /// <returns>Vector3Int</returns>
        public static Vector3Int ToVector3Int(Vector3IntLAB vector3IntLAB)
        {
            return new Vector3Int(vector3IntLAB.X, vector3IntLAB.Y, vector3IntLAB.Z);
        }

        /// <summary>
        /// Vector3Int to Vector3IntLAB
        /// </summary>
        /// <param name="vector3Int">Vector3Int</param>
        /// <returns>Vector3IntLAB</returns>
        public static Vector3IntLAB ToVector3IntLAB(Vector3Int vector3Int)
        {
            return new Vector3IntLAB(vector3Int.x, vector3Int.y, vector3Int.z);
        }
    }
}
