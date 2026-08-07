namespace LAB2D.Item.Backpack
{
    using LAB2D;
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
            string qualityName = GetQualityDisplayName(this.Quality);
            Color c = EquipmentLootTool.GetQualityColor(this.Quality);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            return base.ToString() +
                $"品质: <color=#{hex}>{qualityName}</color>\n";
        }

        private static string GetQualityDisplayName(BackpackItemQualityEnum quality)
        {
            switch (quality)
            {
                case BackpackItemQualityEnum.Gray:   return "普通";
                case BackpackItemQualityEnum.White:  return "白色";
                case BackpackItemQualityEnum.Green:  return "不凡";
                case BackpackItemQualityEnum.Blue:   return "稀有";
                case BackpackItemQualityEnum.Purple: return "史诗";
                case BackpackItemQualityEnum.Orange: return "传说";
                case BackpackItemQualityEnum.Yellow: return "金色";
                case BackpackItemQualityEnum.Red:    return "神话";
                case BackpackItemQualityEnum.Black:  return "黑色";
                default:                             return quality.ToString();
            }
        }
    }

    /// <summary>
    /// 背包对象
    /// </summary>
    public abstract class ABackpackItemObject : AItemObject
    {
    }
}
