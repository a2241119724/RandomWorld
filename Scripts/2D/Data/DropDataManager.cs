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
        private readonly Dictionary<string, List<DropItem>> nameToDrop; // 资源，与对应的掉落物

        public DropDataManager()
        {
            this.nameToDrop = new Dictionary<string, List<DropItem>>();
            DropItemDataSO dropItemDataSO = ResourceManager.Instance.GetDropSO("DropItemDataSO");

            dropItemDataSO.ResourceDropItems.ForEach(item =>
            {
                item.DropItems.ForEach(dropItem =>
                {
                    dropItem.Init();
                });
                this.nameToDrop.Add(item.Name, item.DropItems);
            });
        }

        /// <summary>
        /// 根据名称获取掉落物
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>掉落物</returns>
        public List<DropItem> GetDropItemsByName(string name)
        {
            if (!this.nameToDrop.ContainsKey(name))
            {
                // 默认使用默认掉落物
                if (this.nameToDrop.ContainsKey("Default"))
                {
                    return this.nameToDrop["Default"];
                }

                return Empty;
            }

            return this.nameToDrop[name];
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
