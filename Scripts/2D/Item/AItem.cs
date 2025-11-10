namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;

    /// <summary>
    /// 道具
    /// 通过ID关联ItemData
    /// </summary>
    [Serializable]
    public abstract class AItem
    {
        /// <summary>
        /// 道具的范围
        /// </summary>
        public static Dictionary<string, ItemTypeEnum[]> Ranges = new ()
        {
            { "Backpack", new ItemTypeEnum[] { ItemTypeEnum.Weapon, ItemTypeEnum.BackpackOther } },
            { "Build", new ItemTypeEnum[] { ItemTypeEnum.Room, ItemTypeEnum.BuildOther } },
        };

        /// <summary>
        /// 具体道具ID
        /// </summary>
        public int Uid;

        /// <summary>
        /// 道具ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity;

        /// <summary>
        /// 所有Backpack与Build均在该Enum中
        /// </summary>
        [Serializable]
        public enum ItemTypeEnum
        {
            // 背包

            /// <summary>
            /// 武器
            /// </summary>
            Weapon,

            /// <summary>
            /// 装备
            /// </summary>
            Equipment,

            /// <summary>
            /// 消耗品(道具)
            /// </summary>
            Consumable,

            /// <summary>
            /// 材料
            /// </summary>
            Material,

            /// <summary>
            /// 任务用品
            /// </summary>
            Task,

            /// <summary>
            /// 食物
            /// </summary>
            Food,

            /// <summary>
            /// 种子
            /// </summary>
            Seed,

            /// <summary>
            /// 背包其他
            /// </summary>
            BackpackOther,

            // 建造

            /// <summary>
            /// 房间
            /// </summary>
            Room,

            /// <summary>
            /// 墙
            /// </summary>
            Wall,

            /// <summary>
            /// 家具
            /// </summary>
            Furniture,

            /// <summary>
            /// 建造其他
            /// </summary>
            BuildOther,

            // 其他

            /// <summary>
            /// 空(用于仓库)
            /// </summary>
            Null,
        }

        /// <summary>
        /// 不可使用反射找到该类
        /// 不加入到BuildMenu中
        /// </summary>
        public interface IDontShow
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            ItemData itemData = ItemDataManager.Instance.GetById(this.Id);
            return $"uid: {this.Uid}\n" +
                $"id: {this.Id}\n" +
                $"quantity: {this.Quantity}\n" +
                $"info: {itemData.Info}\n" +
                $"isStackable: {itemData.IsStackable}\n" +
                $"imageName: {itemData.EnName}\n" +
                $"itemName: {itemData.CnName}\n";
        }
    }

    /// <summary>
    /// 道具对象
    /// </summary>
    public abstract class AItemObject : MonoBehaviourPun
    {
        /// <summary>
        /// 对应的Item数据
        /// </summary>
        public AItem Item { get; set; }

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
        }
    }
}