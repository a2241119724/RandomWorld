namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 背包道具
    /// </summary>
    [Serializable]
    public abstract class ABackpackItem : AItem
    {
        /// <summary>
        /// 瓦片
        /// </summary>
        [NonSerialized]
        public TileBase Tile;

        /// <summary>
        /// 品质
        /// </summary>
        public BackpackItemQualityEnum Quality;

        protected ABackpackItem()
        {
            this.Quality = BackpackItemQualityEnum.Gray;
        }

        /// <summary>
        /// 背包质量
        /// </summary>
        [Serializable]
        public enum BackpackItemQualityEnum
        {
            /// <summary>
            /// 灰色
            /// </summary>
            Gray,

            /// <summary>
            /// 白色
            /// </summary>
            White,

            /// <summary>
            /// 绿色
            /// </summary>
            Green,

            /// <summary>
            /// 蓝色
            /// </summary>
            Blue,

            /// <summary>
            /// 紫色
            /// </summary>
            Purple,

            /// <summary>
            /// 橙色
            /// </summary>
            Orange,

            /// <summary>
            /// 黄色
            /// </summary>
            Yellow,

            /// <summary>
            /// 红色
            /// </summary>
            Red,

            /// <summary>
            /// 黑色
            /// </summary>
            Black,
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return base.ToString() +
                $"品质: {this.Quality}\n";
        }
    }

    /// <summary>
    /// 背包对象
    /// </summary>
    public abstract class ABackpackItemObject : AItemObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();
        }
    }
}
