using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class ItemFactory : Singleton<ItemFactory>
    {
        /// <summary>
        /// ����
        /// </summary>
        private Dictionary<string, Item> singletonItems;

        public ItemFactory(){
            singletonItems = new Dictionary<string, Item>();
            readItems();
        }
        
        public Item getItemByName(string name) {
            return singletonItems[name];
        }

        public List<Item> getItems() {
            List<Item> items = new List<Item>();
            foreach (KeyValuePair<string, Item> item in singletonItems) {
                items.Add(item.Value);
            }
            return items;
        }

        /// <summary>
        /// ͨ������ʵ����
        /// ����Ҫ������imageNameһ��
        /// </summary>
        private void readItems()
        {
            List<Type> types = Tool.getChildByParent<Item>();
            foreach (Type type in types)
            {
                int id = ItemDataManager.Instance.getByName(type.Name).id;
                Item item = (Item)Activator.CreateInstance(type);
                item.id = id;
                singletonItems.Add(type.Name,item);
            }
        }
    }
}
