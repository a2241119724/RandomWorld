using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class ItemFactory : Singleton<ItemFactory>
    {
        /// <summary>
        /// 通过反射实例化
        /// 仅需要类名与imageName一样
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Item getByName(string name) {
            int id = ItemDataManager.Instance.getByName(name).id;
            List<Type> types = Tool.getChildByParen<Item>();
            foreach(Type type in types)
            {
                if (type.Name == name)
                {
                    Item item = (Item)Activator.CreateInstance(type);
                    item.id = id;
                    return item;
                }
            }
            Debug.LogError("不存在名为" + name + "的Item");
            return null;
        }
    }
}
