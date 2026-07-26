namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Item;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 掉落物管理
    /// </summary>
    public class DropDataManager : Singleton<DropDataManager>
    {
        private static readonly List<DropItem> Empty = new ();
        private readonly Dictionary<int, List<DropItem>> idToDrop; // 资源, 与对应的掉落物, -1为默认掉落物

        public DropDataManager()
        {
            this.idToDrop = new Dictionary<int, List<DropItem>>();
            DropItemDataSO dropItemDataSO = Core.ServiceLocator.Get<ResourceManager>().GetDropSO("DropItemDataSO");

            dropItemDataSO.ResourceDropItems.ForEach(item =>
            {
                item.DropItems.ForEach(dropItem =>
                {
                    dropItem.Init();
                });

                if (item.Name.Equals("Default"))
                {
                    this.idToDrop.Add(-1, item.DropItems);
                    return;
                }

                // 根据资源名称获取item信息；缺少道具数据的资源不参与采集掉落。
                if (Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(item.Name, out ItemData itemData))
                {
                    this.idToDrop.Add(itemData.Id, item.DropItems);
                }
            });
        }

        /// <summary>
        /// 根据ID获取掉落物
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>掉落物</returns>
        public List<DropItem> GetDropItemsById(int id)
        {
            if (!this.idToDrop.ContainsKey(id))
            {
                // 默认使用默认掉落物
                if (this.idToDrop.ContainsKey(-1))
                {
                    return this.idToDrop[-1];
                }

                return Empty;
            }

            return this.idToDrop[id];
        }

        /// <summary>
        /// 是否配置了该资源的掉落信息。
        /// </summary>
        /// <param name="id">资源ID</param>
        /// <returns>是否配置</returns>
        public bool HasDropItemsById(int id)
        {
            return this.idToDrop.ContainsKey(id);
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
            this.ResourceInfo.Id = Core.ServiceLocator.Get<ItemDataManager>().GetByName(this.Name).Id;
        }
    }
}
