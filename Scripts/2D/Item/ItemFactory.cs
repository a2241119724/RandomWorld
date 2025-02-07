using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class ItemFactory : Singleton<ItemFactory>
    {
        /// <summary>
        /// 根据name实例化
        /// </summary>
        private Dictionary<string, Type> backpackItemTypes;
        /// <summary>
        /// 单例的
        /// </summary>
        private Dictionary<string, BuildItem> buildItems;
        private int uid = 0;

        public ItemFactory(){
            backpackItemTypes = new Dictionary<string, Type>();
            buildItems = new Dictionary<string, BuildItem>();
            readItems();
        }
        
        public BackpackItem getBackpackItemByName(string name) {
            int id = ItemDataManager.Instance.getByName(name).id;
            BackpackItem item = (BackpackItem)Activator.CreateInstance(backpackItemTypes[name]);
            item.id = id;
            item.quantity = 1;
            item.uid = uid++;
            return item;
        }

        public BuildItem getBuildItemByName(string name)
        {
            return buildItems[name];
        }

        public List<Item> genBackpackItems()
        {
            List<Item> items = new List<Item>();
            foreach (KeyValuePair<string, Type> item in backpackItemTypes)
            {
                items.Add(getBackpackItemByName(item.Key));
            }
            return items;
        }

        public List<Item> getBuildItems() {
            List<Item> items = new List<Item>();
            foreach (KeyValuePair<string, BuildItem> buildItem in buildItems) {
                items.Add(buildItem.Value);
            }
            return items;
        }

        /// <summary>
        /// 通过反射实例化
        /// 仅需要类名与imageName一样
        /// </summary>
        private void readItems()
        {
            List<Type> types = Tool.getChildByParent<BackpackItem>();
            foreach (Type type in types)
            {
                backpackItemTypes.Add(type.Name, type);
            }
            types = Tool.getChildByParent<BuildItem>();
            foreach (Type type in types)
            {
                int id = ItemDataManager.Instance.getByName(type.Name).id;
                BuildItem item = (BuildItem)Activator.CreateInstance(type);
                item.id = id;
                buildItems.Add(type.Name, item);
            }
        }
    }
}
