namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 道具实例化工厂
    /// EnName==对应类名==图片名
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
            item.Id = ItemDataManager.Instance.GetByName(name).Id;
            item.Quantity = 1;
            item.Uid = this.uid++;
            item.Tile = ResourceManager.Instance.GetAsset(name);
            return item;
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
        /// 通过反射实例化
        /// 仅需要类名与imageName一样
        /// </summary>
        /// <param name="itemDatas">所有装备数据(不包含武器)</param>
        public void InitItemInstances(List<ItemData> itemDatas)
        {
            // 装备(不包含武器)
            foreach (ItemData itemData in itemDatas)
            {
                if (itemData.Type == AItem.ItemTypeEnum.Equipment)
                {
                    this.backpackItemTypes.Add(itemData.EnName, typeof(CommonEquipment));
                }
            }

            // 非装备(包含武器)
            List<Type> types = Tool.GetChildByParent<ABackpackItem>();
            foreach (Type type in types)
            {
                if (this.backpackItemTypes.ContainsKey(type.Name))
                {
                    // 装备类覆盖CommonEquipment
                    this.backpackItemTypes[type.Name] = type;
                }
                else
                {
                    this.backpackItemTypes.Add(type.Name, type);
                }
            }

            // 移除CommonEquipment, 因为CommonEquipment是所有装备的基类
            this.backpackItemTypes.Remove(typeof(CommonEquipment).Name);

            // 建造
            types = Tool.GetChildByParent<ABuildItem>();
            foreach (Type type in types)
            {
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Length > 0 && interfaces.Contains(typeof(AItem.IDontShow)))
                {
                    continue;
                }

                int id = ItemDataManager.Instance.GetByName(type.Name).Id;
                ABuildItem item = (ABuildItem)Activator.CreateInstance(type);
                item.Id = id;
                this.buildItems.Add(type.Name, item);
            }
        }
    }
}
