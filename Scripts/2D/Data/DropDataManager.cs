namespace LAB2D
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 掉落物管理
    /// </summary>
    public class DropDataManager : Singleton<DropDataManager>
    {
        private static readonly List<DropItem> Empty = new ();
        private readonly Dictionary<int, List<DropItem>> nameToDrop; // 资源, 与对应的掉落物, -1为默认掉落物

        public DropDataManager()
        {
            this.nameToDrop = new Dictionary<int, List<DropItem>>();
            DropItemDataSO dropItemDataSO = ResourceManager.Instance.GetDropSO("DropItemDataSO");

            dropItemDataSO.ResourceDropItems.ForEach(item =>
            {
                item.DropItems.ForEach(dropItem =>
                {
                    dropItem.Init();
                });

                if (item.Name.Equals("Default"))
                {
                    this.nameToDrop.Add(-1, item.DropItems);
                    return;
                }

                // 根据树的名称获取item信息
                this.nameToDrop.Add(ItemDataManager.Instance.GetByName(item.Name).Id, item.DropItems);
            });
        }

        /// <summary>
        /// 根据ID获取掉落物
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>掉落物</returns>
        public List<DropItem> GetDropItemsById(int id)
        {
            if (!this.nameToDrop.ContainsKey(id))
            {
                // 默认使用默认掉落物
                if (this.nameToDrop.ContainsKey(-1))
                {
                    return this.nameToDrop[-1];
                }

                return Empty;
            }

            return this.nameToDrop[id];
        }
    }

    /// <summary>
    /// 掉落物
    /// </summary>
    [Serializable]
    public class DropItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 掉落物信息
        /// </summary>
        public ResourceInfo ResourceInfo;

        public void Init()
        {
            this.ResourceInfo.Id = ItemDataManager.Instance.GetByName(this.Name).Id;
        }
    }
}
