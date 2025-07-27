namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 房间数据
    /// </summary>
    [Serializable]
    public abstract class RoomItem : BuildItem
    {
        /// <summary>
        /// 房间的所有墙
        /// </summary>
        public Dictionary<WallDirection, Wall> Walls;

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 获得X轴边界
        /// </summary>
        /// <param name="centerMap">中心为hi在</param>
        /// <returns>坐标</returns>
        public int[] GetXBoundary(Vector3Int centerMap)
        {
            return new int[] { centerMap.x - (this.Height / 2), centerMap.x + this.Height - 1 - (this.Height / 2) };
        }

        /// <summary>
        /// 获得Y边界
        /// </summary>
        /// <param name="centerMap">中心为hi在</param>
        /// <returns>坐标</returns>
        public int[] GetYBoundary(Vector3Int centerMap)
        {
            return new int[] { centerMap.y - (this.Width / 2), centerMap.y + this.Width - 1 - (this.Width / 2) };
        }

        /// <summary>
        /// 墙的方向
        /// </summary>
        public enum WallDirection
        {
            /// <summary>
            /// 上
            /// </summary>
            TOP,

            /// <summary>
            /// 下
            /// </summary>
            DOWN,

            /// <summary>
            /// 左
            /// </summary>
            LEFT,

            /// <summary>
            /// 右
            /// </summary>
            RIGHT,

            /// <summary>
            /// 右上
            /// </summary>
            RIGHT_TOP,

            /// <summary>
            /// 右下
            /// </summary>
            RIGHT_DOWN,

            /// <summary>
            /// 左上
            /// </summary>
            LEFT_TOP,

            /// <summary>
            /// 左下
            /// </summary>
            LEFT_DOWN,
        }
    }
}
