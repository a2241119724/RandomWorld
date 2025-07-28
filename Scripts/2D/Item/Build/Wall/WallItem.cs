 namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 墙
    /// </summary>
    [Serializable]
    public abstract class WallItem
        : BuildItem
    {
        public WallItem()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset(this.GetType().Name);
        }

        /// <summary>
        /// 墙的方向
        /// </summary>
        public enum WallDirectionEnum
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
