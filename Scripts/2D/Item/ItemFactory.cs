namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 道具工厂
    /// </summary>
    public class ItemFactory : Singleton<ItemFactory>
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

        public ItemFactory()
        {
            this.backpackItemTypes = new Dictionary<string, Type>();
            this.buildItems = new Dictionary<string, ABuildItem>();
            this.ReadItems();
        }

        /// <summary>
        /// 根据名称后去背包道具
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>背包道具</returns>
        public ABackpackItem GetBackpackItemByName(string name)
        {
            int id = ItemDataManager.Instance.GetByName(name).Id;
            ABackpackItem item = (ABackpackItem)Activator.CreateInstance(this.backpackItemTypes[name]);
            item.Id = id;
            item.Quantity = 1;
            item.Uid = this.uid++;
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
        private void ReadItems()
        {
            List<Type> types = Tool.GetChildByParent<ABackpackItem>();
            foreach (Type type in types)
            {
                this.backpackItemTypes.Add(type.Name, type);
            }

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
