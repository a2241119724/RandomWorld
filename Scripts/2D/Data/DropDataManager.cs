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
        private readonly Dictionary<int, List<DropItem>> idToDrop; // 资源ID → 掉落物, -1为默认掉落物
        private readonly Dictionary<string, List<DropItem>> nameToDrop; // 资源名称 → 掉落物（支持地形名等非ItemData名称）

        /// <summary>
        /// 反向索引：材料 ID → 产出该材料的资源名称集合。
        /// 供 WorkerBrain 等决策层查询"哪些资源能产出我需要的材料"。
        /// 在构造函数中与正向索引同步构建。
        /// </summary>
        private readonly Dictionary<int, HashSet<string>> materialToSourceNames;

        public DropDataManager()
        {
            this.idToDrop = new Dictionary<int, List<DropItem>>();
            this.nameToDrop = new Dictionary<string, List<DropItem>>();
            this.materialToSourceNames = new Dictionary<int, HashSet<string>>();
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

                // 按名称索引（支持地形 tileResourceName 等非 ItemData 名称）
                this.nameToDrop[item.Name] = item.DropItems;

                // 同时按 ItemData ID 索引（兼容通过 ItemData.Id 查询的旧路径）
                if (Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(item.Name, out ItemData itemData))
                {
                    this.idToDrop.Add(itemData.Id, item.DropItems);
                }

                // 构建反向索引：材料 ID → 产出该材料的资源名称
                foreach (DropItem dropItem in item.DropItems)
                {
                    int materialId = dropItem.ResourceInfo.Id;
                    if (!this.materialToSourceNames.ContainsKey(materialId))
                    {
                        this.materialToSourceNames[materialId] = new HashSet<string>();
                    }

                    this.materialToSourceNames[materialId].Add(item.Name);
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
            if (!this.idToDrop.TryGetValue(id, out List<DropItem> drops))
            {
                return this.GetDefaultDrops();
            }

            return drops;
        }

        /// <summary>
        /// 根据资源名称获取掉落物（支持地形 tileResourceName 如 "Mountain"）。
        /// 优先按名称精确匹配，其次通过 ItemData 名称查找，最后回退默认掉落。
        /// </summary>
        /// <param name="resourceName">资源名称（对应 DropItemDataSO 中 ResourceDropItem.Name）</param>
        /// <returns>掉落物列表</returns>
        public List<DropItem> GetDropItemsByResourceName(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return this.GetDefaultDrops();
            }

            // 1. 按名称精确匹配（支持地形名如 "Mountain"）
            if (this.nameToDrop.TryGetValue(resourceName, out List<DropItem> drops))
            {
                return drops;
            }

            // 2. 尝试通过 ItemData 名称查找（兼容旧路径）
            if (Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resourceName, out ItemData itemData)
                && this.idToDrop.TryGetValue(itemData.Id, out drops))
            {
                return drops;
            }

            // 3. 回退默认掉落
            return this.GetDefaultDrops();
        }

        /// <summary>
        /// 获取默认掉落物。
        /// </summary>
        private List<DropItem> GetDefaultDrops()
        {
            if (this.idToDrop.TryGetValue(-1, out List<DropItem> defaultDrops))
            {
                return defaultDrops;
            }

            return Empty;
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

        /// <summary>
        /// 查询哪些资源名称能产出指定材料。
        /// 供决策层使用：Worker 需要建造材料 X 时，可优先采集产出 X 的资源。
        /// </summary>
        /// <param name="materialId">材料物品 ID</param>
        /// <param name="sourceNames">产出该材料的资源名称集合（输出）</param>
        /// <returns>是否找到对应资源</returns>
        public bool TryGetSourceNamesForMaterial(int materialId, out HashSet<string> sourceNames)
        {
            return this.materialToSourceNames.TryGetValue(materialId, out sourceNames);
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
