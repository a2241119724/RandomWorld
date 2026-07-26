namespace LAB2D.Core.Seek
{
    using UnityEngine;
    using LAB2D.Serializable;

    /// <summary>
    /// 寻路基
    /// </summary>
    public interface ISeek
    {
        /// <summary>
        /// 使用线程寻路
        /// </summary>
        /// <param name="targetMap">寻路目标</param>
        public void Seek(Vector3Int targetMap);
    }

    /// <summary>
    /// f = g + h
    /// </summary>
    public class Spend
    {
        /// <summary>
        /// 坐标
        /// </summary>
        // public Vector3Int PosMap;
        public Vector2ShortLAB PosMap;

        /// <summary>
        /// 预估总消耗
        /// </summary>
        public float F = 0;

        /// <summary>
        /// 已经的消耗
        /// </summary>
        public float G = 0;

        /// <summary>
        /// 后续预估的消耗
        /// </summary>
        public float H = 0;

        /// <summary>
        /// 指向路径的前一个位置
        /// </summary>
        public Spend Previous;

        public Spend(short x, short y)
        {
            this.PosMap = new Vector2ShortLAB(x, y);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            this.F = this.G = this.H = 0;
            this.Previous = null;
        }
    }
}