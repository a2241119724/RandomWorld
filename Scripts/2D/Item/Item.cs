namespace LAB2D
{
    using System;
    using Photon.Pun;

    /// <summary>
    /// 道具
    /// </summary>
    [Serializable]
    public abstract class Item
    {
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

        /// <inheritdoc/>
        public override string ToString()
        {
            ItemData itemData = ItemDataManager.Instance.GetById(this.Id);
            return $"uid: {this.Uid}\n" +
                $"id: {this.Id}\n" +
                $"quantity: {this.Quantity}\n" +
                $"info: {itemData.Info}\n" +
                $"isStackable: {itemData.IsStackable}\n" +
                $"imageName: {itemData.ImageName}\n" +
                $"itemName: {itemData.ItemName}\n";
        }
    }

    /// <summary>
    /// 道具对象
    /// </summary>
    public abstract class ItemObject : MonoBehaviourPun
    {
        /// <summary>
        /// 对应的Item数据
        /// </summary>
        public Item Item { get; set; }

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