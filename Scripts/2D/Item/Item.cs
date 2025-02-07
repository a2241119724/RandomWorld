using Photon.Pun;
using System;

namespace LAB2D
{
    /// <summary>
    /// 存入背包
    /// </summary>
    [Serializable]
    public abstract class Item
    {
        public int uid;
        public int id;
        public int quantity; // 数量

        public override string ToString()
        {
            ItemData itemData = ItemDataManager.Instance.getById(id);
            return $"uid: {uid}\n" +
                $"id: {id}\n" +
                $"quantity: {quantity}\n" +
                $"info: {itemData.info}\n" +
                $"isStackable: {itemData.isStackable}\n" +
                $"imageName: {itemData.imageName}\n" +
                $"itemName: {itemData.itemName}\n";
        }
    }

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