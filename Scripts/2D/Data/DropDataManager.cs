namespace LAB2D
{
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
            string[] drops = Tool.GetCSV(ResourceConstant.DATA_ROOT + "DropItem");

            // 跳过第一行
            for (int i = 1; i < drops.Length; i++)
            {
                string[] cols = drops[i].Split(',');
                for (int j = 1; j < cols.Length; j += 2)
                {
                    if (!this.nameToDrop.ContainsKey(cols[0]))
                    {
                        this.nameToDrop.Add(cols[0], new List<DropItem>());
                    }

                    this.nameToDrop[cols[0]].Add(new DropItem(cols[j], int.Parse(cols[j + 1])));
                }
            }
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
                return Empty;
            }

            return this.nameToDrop[name];
        }
    }

    /// <summary>
    /// 掉落物
    /// </summary>
    public class DropItem
    {
        public DropItem(string name, int count)
        {
            this.Name = name;
            this.ResourceInfo = new ResourceInfo(ItemDataManager.Instance.GetByName(name).Id, count);
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 掉落物信息
        /// </summary>
        public ResourceInfo ResourceInfo { get; private set; }
    }
}
