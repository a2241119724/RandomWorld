namespace LAB2D.Tool
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 矢量工具类
    /// </summary>
    public class VectorTool
    {
        /// <summary>
        /// 向量加(x,y).
        /// </summary>
        /// <param name="vector">向量.</param>
        /// <param name="x">横坐标.</param>
        /// <param name="y">纵坐标.</param>
        /// <returns>结果.</returns>
        public static Vector3Int Add(Vector3Int vector, int x, int y)
        {
            return new Vector3Int(vector.x + x, vector.y + y, vector.z);
        }

        /// <summary>
        /// 向量转置.
        /// </summary>
        /// <param name="vector">向量.</param>
        /// <returns>转置结果.</returns>
        public static Vector3Int T(Vector3Int vector)
        {
            return new Vector3Int(vector.y, vector.x, vector.z);
        }
    }
}
