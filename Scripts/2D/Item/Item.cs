using Photon.Pun;
using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Item
    {
        public int uid; // 唯一id
        public int id; // 物品id
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
                $"itemName: {itemData.itemName}";
        }
    }

    public abstract class ItemObject : MonoBehaviourPun
    {
        public int Uid { get; set; } // 对应的Item数据

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