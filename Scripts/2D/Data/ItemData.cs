namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Item.Backpack.Equipment;
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// 物品地面视觉的分层模式，统一控制"是否参与 y 排序"与"角色在后面时是否淡化"。
    /// </summary>
    public enum ItemLayerMode
    {
        /// <summary>恒底层：不参与 y 排序，固定渲染在最底层，角色永远盖在其上。</summary>
        Bottom = 0,

        /// <summary>参与 y 排序，角色走到其后时淡化（透明）。</summary>
        Alpha = 1,

        /// <summary>参与 y 排序，角色走到其后不淡化（保持不透明）。</summary>
        Normal = 2,
    }

    /// <summary>
    /// 物品地面视觉的呈现方式：默认 tile 视觉（原有逻辑）或预制体视觉。
    /// </summary>
    public enum ItemVisualMode
    {
        /// <summary>默认 tile 视觉：建筑/掉落物由 tilemap + 动态 SpriteRenderer 呈现（原有逻辑）。</summary>
        Tile = 0,

        /// <summary>预制体视觉：以英文名（ItemData.Name）作为预制体名，经 ResourceManager 按名实例化完整预制体呈现。</summary>
        Prefab = 1,
    }

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
        /// 地面视觉分层模式：
        /// - Bottom：恒底层，不参与 y 排序，固定渲染在最底层（角色永远盖在其上）。
        /// - Alpha：参与 y 排序，角色走到其后时淡化（透明），保证角色可见。
        /// - Normal：参与 y 排序，角色走到其后不淡化（保持不透明）。
        /// </summary>
        [Tooltip("地面视觉分层模式：Bottom=恒底层不参与 y 排序；Alpha=参与 y 排序且角色在后时淡化；Normal=参与 y 排序但不淡化。")]
        public ItemLayerMode LayerMode = ItemLayerMode.Bottom;

        /// <summary>
        /// 地面视觉呈现方式：Tile = 默认 tile 视觉（tilemap + 动态 SpriteRenderer，原有逻辑）；
        /// Prefab = 以英文名（Name，与 tile/图片资源名一致）作为预制体名，经 ResourceManager 按名
        /// 实例化完整预制体呈现（可带多部件/动画/组件）；tile 仍保留数据/碰撞/存档/网络（视觉层替换）。
        /// 找不到/实例化失败时自动回退默认 tile 视觉。建筑方向变体（Clone）继承同一开关，预制体名取变体自身 Name。
        /// </summary>
        [Tooltip("地面视觉呈现方式：Tile=默认 tile 视觉（原有逻辑）；Prefab=以英文名(Name)作为预制体名实例化预制体呈现。")]
        public ItemVisualMode VisualMode = ItemVisualMode.Tile;

        /// <summary>
        /// 地面视觉帧动画开关：开启且 LayerMode != Bottom 时，以英文名（Name，与图片资源名一致）
        /// 为前缀，自动收集 Name_0、Name_1、Name_2… 序列 Sprite 循环播放（帧数收集至首个缺失，
        /// 无任何序列帧则回退静态 tile 图）。仅作用于独立 SpriteRenderer 视觉：
        /// VisualMode=Prefab 的预制体视觉由预制体自身呈现动画，不受此开关影响。
        /// </summary>
        [Tooltip("地面视觉帧动画开关：开启且 LayerMode != Bottom 时，按英文名(Name)加 _0/_1/_2... 序列图循环播放动画。")]
        public bool IsAnimation;

        /// <summary>
        /// 地面视觉 shader 摇摆开关：开启且 LayerMode != Bottom 时，独立 sprite 视觉换用
        /// Custom/Sprite-Lit-Sway 材质（GPU 顶点位移做摆动：树底固定、树顶摆，相位按世界
        /// 位置 hash 逐实例打散），sprite 走静态 tile 图，不挂任何动画组件——大地图数千棵
        /// 树的摆动不再产生逐实例 Animator 每帧求值开销。与 IsAnimation 相互独立；
        /// 序列帧动画物品（Torch/Campfire 等）勿开（帧动画本身已含摆动语义）。
        /// </summary>
        [Tooltip("shader 摇摆开关：开启且 LayerMode != Bottom 时独立视觉用 Sprite-Lit-Sway 材质做 GPU 顶点摆动（静态图，无动画组件）。序列帧动画物品勿开。")]
        public bool IsSway;

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
