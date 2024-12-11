using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class ItemFactory : Singleton<ItemFactory>
    {
        /// <summary>
        /// ͨ������ʵ����
        /// ����Ҫ������imageNameһ��
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
            Debug.LogError("��������Ϊ" + name + "��Item");
            return null;
        }
    }
}
