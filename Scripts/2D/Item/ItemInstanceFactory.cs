namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Item;
    using LAB2D.Item.Backpack;
    using LAB2D.Item.Build;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 道具实例化工厂
    /// Name==对应类名==图片名
    /// </summary>
    public class ItemInstanceFactory : Singleton<ItemInstanceFactory>
    {
        /// <summary>
        /// 根据name实例化
        /// </summary>
        private readonly Dictionary<string, Type> backpackItemTypes;

        /// <summary>
        /// 单例的
        /// </summary>
        private readonly Dictionary<string, ABuildItem> buildItems;
        private int uid = 0;

        public ItemInstanceFactory()
        {
            this.backpackItemTypes = new Dictionary<string, Type>();
            this.buildItems = new Dictionary<string, ABuildItem>();
        }

        /// <summary>
        /// 根据名称后去背包道具
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>背包道具</returns>
        public ABackpackItem GetBackpackItemByName(string name)
        {
            ABackpackItem item = (ABackpackItem)Activator.CreateInstance(this.backpackItemTypes[name]);
            item.Id = Core.ServiceLocator.Get<ItemDataManager>().GetByName(name).Id;
            item.Quantity = 1;
            item.Uid = this.uid++;
            // Tile 对背包物品可选：子类构造函数已设置 Tile（如 AddHp/CustomWood）则不覆盖；
            // 未设置时宽容获取（无对应 tile 资源不打日志，保持 null 供掉落路径自行处理）。
            if (item.Tile == null)
            {
                item.Tile = Core.ServiceLocator.Get<ResourceManager>().TryGetAsset(name);
            }

            if (item is AEquipment equip && Core.ServiceLocator.Get<ItemDataManager>().IdToType(item.Id) == AItem.ItemTypeEnum.Equipment)
            {
                equip.Type = Core.ServiceLocator.Get<ItemDataManager>().GetById(item.Id).EquipSlot;
            }

            return item;
        }

        /// <summary>
        /// 根据id获得背包道具
        /// </summary>
        /// <param name="id">id</param>
        /// <returns>背包道具</returns>
        public ABackpackItem GetBackpackItemById(int id)
        {
            ItemData itemData = Core.ServiceLocator.Get<ItemDataManager>().GetById(id);
            return this.GetBackpackItemByName(itemData.Name);
        }

        /// <summary>
        /// 根据名字获得建造道具
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>建造道具</returns>
        public ABuildItem GetBuildItemByName(string name)
        {
            return this.buildItems[name];
        }

        /// <summary>
        /// 得到所有的背包道具
        /// </summary>
        /// <returns>所有背包道具</returns>
        public List<AItem> GenBackpackItems()
        {
            List<AItem> items = new ();
            foreach (KeyValuePair<string, Type> item in this.backpackItemTypes)
            {
                items.Add(this.GetBackpackItemByName(item.Key));
            }

            return items;
        }

        /// <summary>
        /// 得到所有的建造道具
        /// </summary>
        /// <returns>所有建造道具</returns>
        public List<AItem> GetBuildItems()
        {
            return this.buildItems.Values.ToList<AItem>();
        }

        /// <summary>
        /// 初始化背包物品实例类型表。
        /// 简化规则：遍历所有背包 SO 物品——有对应 C# 类（ABackpackItem 子类，类名==Name）用自定义类，
        /// 没有则用默认类（装备→CommonEquipment，其余→ABackpackItem 默认实现），无需为每件物品建类。
        /// </summary>
        public void InitItemInstances(List<ItemData> itemDatas)
        {
            // 第一步：反射登记自定义类表（key=类名）。默认类（CommonEquipment）不参与覆盖。
            Dictionary<string, Type> customTypes = new Dictionary<string, Type>();
            foreach (Type type in LAB2D.Tool.Tool.GetChildByParent<ABackpackItem>())
            {
                if (type == typeof(CommonEquipment))
                {
                    continue;
                }

                customTypes[type.Name] = type;
            }

            // 第二步：以 SO 为准遍历所有背包物品——有类用自定义类，没类用默认类。
            this.backpackItemTypes.Clear();
            foreach (ItemData itemData in itemDatas)
            {
                if (itemData == null)
                {
                    continue;
                }

                Type resolvedType = itemData.Type == AItem.ItemTypeEnum.Equipment
                    ? typeof(CommonEquipment)
                    : typeof(ABackpackItem);
                if (customTypes.TryGetValue(itemData.Name, out Type customType))
                {
                    resolvedType = customType;
                }

                this.backpackItemTypes[itemData.Name] = resolvedType;
            }

            // 第三步：兜底——反射到但 SO 无对应条目的自定义类也进背包（保持原行为：有类即可入包）。
            foreach (KeyValuePair<string, Type> custom in customTypes)
            {
                if (!this.backpackItemTypes.ContainsKey(custom.Key))
                {
                    this.backpackItemTypes.Add(custom.Key, custom.Value);
                }
            }

            // ========== 建造物品 — 新逻辑 ==========
            // 第一步：反射扫描有特殊行为的 ABuildItem 子类
            List<Type> types = LAB2D.Tool.Tool.GetChildByParent<ABuildItem>();
            foreach (Type type in types)
            {
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Length > 0 && interfaces.Contains(typeof(AItem.IDontShow)))
                {
                    continue;
                }

                int id = ServiceLocator.Get<ItemDataManager>().GetByName(type.Name).Id;
                ABuildItem item = (ABuildItem)Activator.CreateInstance(type);
                item.Id = id;
                this.buildItems.Add(type.Name, item);
            }

            // 第二步：遍历 SO 中的 BuildItemData，为尚未有实例的物品创建普通 ABuildItem
            ItemDataManager itemDataManager = ServiceLocator.Get<ItemDataManager>();
            AItem.ItemTypeEnum[] buildTypes = AItem.Ranges["Build"];
            for (int typeIdx = (int)buildTypes[0]; typeIdx <= (int)buildTypes[1]; typeIdx++)
            {
                string soName = ((AItem.ItemTypeEnum)typeIdx).ToString() + "ItemData";
                BuildItemDataSO so = ServiceLocator.Get<ResourceManager>().GetBuildSO(soName);
                if (so == null)
                {
                    continue;
                }

                foreach (BuildItemData buildItemData in so.GetExpandedItems())
                {
                    string enName = buildItemData.Name;
                    if (this.buildItems.ContainsKey(enName))
                    {
                        continue; // 已有特殊子类实例，跳过
                    }

                    ABuildItem item = new ABuildItem(enName)
                    {
                        Id = buildItemData.Id,
                    };
                    this.buildItems.Add(enName, item);
                }
            }
        }
    }
}
