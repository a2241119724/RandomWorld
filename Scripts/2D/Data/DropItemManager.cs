using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class DropItemManager : Singleton<DropItemManager>
    {
        /// <summary>
        /// 资源，与对应的掉落物
        /// </summary>
        private Dictionary<string, List<DropItem>> nameToDrop;
        private static readonly List<DropItem> empty = new List<DropItem>();

        public DropItemManager()
        {
            nameToDrop = new Dictionary<string, List<DropItem>>();
            string[] drops = Tool.getCSV(ResourceConstant.DATA_ROOT + "DropItem");
            for(int i = 0;i < drops.Length; i++)
            {
                string[] cols = drops[i].Split(',');
                for (int j = 1; j < cols.Length; j+=2)
                {
                    if (!nameToDrop.ContainsKey(cols[0]))
                    {
                        nameToDrop.Add(cols[0], new List<DropItem>());
                    }
                    nameToDrop[cols[0]].Add(new DropItem(cols[j], int.Parse(cols[j+1])));
                }
            }
        }

        public List<DropItem> getDropItemsByName(string name)
        {
            if (!nameToDrop.ContainsKey(name)) return empty;
            return nameToDrop[name];
        }
    }

    public class DropItem
    {
        public string Name { get; set; }
        public ResourceInfo ResourceInfo { get; private set; }

        public DropItem(string name, int count)
        {
            this.Name = name;
            ResourceInfo = new ResourceInfo(ItemDataManager.Instance.getByName(name).id, count);
        }
    }
}
