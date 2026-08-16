namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Item.Backpack.Equipment;
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// 不能将ItemData转换为json,因为需要[Serializable]修饰,而包装类没有被修饰
    /// 公共的数据，仅存储一份
    /// </summary>
    [Serializable]
    public class ItemData
    {
        /// <summary>
        /// 空物品
        /// </summary>
        public static readonly ItemData Empty = new ();

        /// <summary>
        /// 唯一标识符
        /// </summary>
        [Tooltip("唯一标识符")]
        public int Id;

        /// <summary>
        /// 中文名称
        /// </summary>
        [Tooltip("中文名称")]
        public string CnName;

        /// <summary>
        /// 英文名称(图片)
        /// </summary>
        [Tooltip("英文名称(图片)，与 Tile 资源/图片名一致")]
        public string Name;

        /// <summary>
        /// 物品信息
        /// </summary>
        [Tooltip("物品信息")]
        public string Info;

        /// <summary>
        /// 是否可堆叠
        /// </summary>
        [Tooltip("是否可堆叠")]
        public bool IsStackable;

        /// <summary>
        /// 恒底层物品开关（默认开启）。
        /// 开启后该物品的地面视觉不参与按 y 的全局排序，固定显示在地图上面、角色之下；
        /// 需要参与 y 排序（与角色/建筑/树交叉遮挡）的物品取消勾选。
        /// </summary>
        [Tooltip("恒底层物品开关（默认开启）：不参与 y 排序，固定显示在地图上面、角色之下；需要按视觉底端 y 与角色/建筑交叉遮挡的物品取消勾选。")]
        public bool IsBottomLayer = true;

        /// <summary>
        /// 物品类型
        /// </summary>
        [Tooltip("物品类型")]
        public AItem.ItemTypeEnum Type;

        /// <summary>
        /// 装备槽位（仅Equipment类型有效）
        /// </summary>
        [Tooltip("装备槽位（仅 Equipment 类型有效）")]
        public AEquipment.EquipTypeEnum EquipSlot;

        /// <summary>
        /// 任务时间
        /// </summary>
        [Tooltip("任务时间")]
        public TaskTime RelatedTaskTime;

        public ItemData()
        {
            this.RelatedTaskTime = new TaskTime();
        }

        public void EnsureTaskTime()
        {
            if (this.RelatedTaskTime == null)
            {
                this.RelatedTaskTime = new TaskTime();
            }
        }

        [Serializable]
        public class TaskTime
        {
            /// <summary>
            /// 搬运任务的拾取时间
            /// </summary>
            [Tooltip("搬运任务的拾取时间")]
            [FormerlySerializedAs("CarryTaskTakeTime")]
            public float TaskBaseTime = WorkerTaskTimeConfig.CarryTakeSeconds;

            /// <summary>
            /// 搬运任务的放置时间
            /// </summary>
            [Tooltip("搬运任务的放置时间")]
            public float CarryTaskPutDownTime = WorkerTaskTimeConfig.CarryPutDownSeconds;
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class ItemDataBuilder
        {
            private readonly ItemData itemData;

            public ItemDataBuilder()
            {
                this.itemData = new ItemData();
            }

            public ItemDataBuilder SetId(int id)
            {
                this.itemData.Id = id;
                return this;
            }

            public ItemDataBuilder SetItemName(string itemName)
            {
                this.itemData.CnName = itemName;
                return this;
            }

            public ItemDataBuilder SetImageName(string imageName)
            {
                this.itemData.Name = imageName;
                return this;
            }

            public ItemDataBuilder SetInfo(string info)
            {
                this.itemData.Info = info;
                return this;
            }

            public ItemDataBuilder SetIsStackable(bool isStackable)
            {
                this.itemData.IsStackable = isStackable;
                return this;
            }

            public ItemDataBuilder SetItemType(AItem.ItemTypeEnum type)
            {
                this.itemData.Type = type;
                return this;
            }

            public ItemData Build()
            {
                return this.itemData;
            }
        }
    }
}
