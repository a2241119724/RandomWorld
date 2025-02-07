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
        public Dictionary<string, List<DropItem>> NameToDrop { get; private set; }

        public DropItemManager()
        {
            NameToDrop = new Dictionary<string, List<DropItem>>();
            string[] drops = Tool.getCSV(ResourceConstant.DATA_ROOT + "DropItem");
            for(int i = 0;i < drops.Length; i++)
            {
                string[] cols = drops[i].Split(',');
                if (cols[0].Equals("LAB")) continue;
                for (int j = 1; j < cols.Length; j+=2)
                {
                    if (!NameToDrop.ContainsKey(cols[0]))
                    {
                        NameToDrop.Add(cols[0], new List<DropItem>());
                    }
                    NameToDrop[cols[0]].Add(new DropItem(cols[j], (ItemType)int.Parse(cols[j+1])));
                }
            }
        }
    }

    public class DropItem
    {
        public string Name { get; set; }
        public ItemType ItemType { get; set; }

        public DropItem(string name, ItemType itemType)
        {
            this.Name = name;
            this.ItemType = itemType;
        }
    }
}
