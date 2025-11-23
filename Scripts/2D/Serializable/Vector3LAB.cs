namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 为了可以序列化
    /// </summary>
    [Serializable]
    public class Vector3LAB
    {
        /// <summary>
        /// X 坐标
        /// </summary>
        public float X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y;

        /// <summary>
        /// Z 坐标
        /// </summary>
        public float Z;

        public Vector3LAB(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// 将Vector3LAB转换为Vector3
        /// </summary>
        /// <param name="vector3LAB">Vector3LAB</param>
        /// <returns>Vector3</returns>
        public static Vector3 ToVector3(Vector3LAB vector3LAB)
        {
            return new Vector3(vector3LAB.X, vector3LAB.Y, vector3LAB.Z);
        }

        /// <summary>
        /// 将Vector3转换为Vector3LAB
        /// </summary>
        /// <param name="vector3">Vector3</param>
        /// <returns>Vector3LAB</returns>
        public static Vector3LAB ToVector3LAB(Vector3 vector3)
        {
            return new Vector3LAB(vector3.x, vector3.y, vector3.z);
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

        public override string ToString()
        {
            return $"({this.X},{this.Y},{this.Z})";
        }
    }

    /// <summary>
    /// Byte的Vector2
    /// </summary>
    [Serializable]
    public class Vector2SByteLAB
    {
        /// <summary>
        /// X 坐标
        /// </summary>
        public sbyte X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public sbyte Y;

        public Vector2SByteLAB(sbyte x, sbyte y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    /// <summary>
    /// Short的Vector2
    /// </summary>
    [Serializable]
    public class Vector2ShortLAB
    {
        /// <summary>
        /// X 坐标
        /// </summary>
        public short X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public short Y;

        public Vector2ShortLAB(short x, short y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}