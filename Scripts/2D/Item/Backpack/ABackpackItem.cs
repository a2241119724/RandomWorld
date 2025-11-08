namespace LAB2D
{
    using System;
    using UnityEngine;
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
        public BackpackItemQuality Quality;

        protected ABackpackItem()
        {
            this.Quality = BackpackItemQuality.Gray;
        }

        /// <summary>
        /// 背包质量
        /// </summary>
        [Serializable]
        public enum BackpackItemQuality
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

        /// <summary>
        /// 即使挂在物体上的脚本没有开启,该方法也会执行
        /// 该道具碰到玩家,加到背包里面
        /// </summary>
        /// <param name="collision">碰撞体</param>
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                BackpackController.Instance.AddItem(ItemFactory.Instance.GetBackpackItemByName(this.name.Split("Object")[0]));
                Destroy(this.gameObject);

                // gameObject.SetActive(false); // 减小开销
            }
        }
    }
}
