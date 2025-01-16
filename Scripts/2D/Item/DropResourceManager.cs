using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class DropResourceManager : Singleton<DropResourceManager>
    {
        /// <summary>
        /// µÙ¬‰ŒÔ
        /// </summary>
        private static Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>> resources = new Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>>();

        public void addDrop(ItemType itemType, Vector3Int posMap, ResourceInfo resourceInfo) {
            Dictionary<Vector3Int, ResourceInfo> dict;
            if (resources.ContainsKey(itemType))
            {
                dict = resources[itemType];
            }
            else
            {
                dict = new Dictionary<Vector3Int, ResourceInfo>();
                resources.Add(itemType, dict);
            }
            if (dict.ContainsKey(posMap))
            {
                Debug.Log(dict[posMap].id == resourceInfo.id);
                dict[posMap].count += resourceInfo.count;
            }
            else
            {
                dict.Add(posMap, resourceInfo);
            }
        }

        public void subDrop(ItemType itemType, Vector3Int posMap, ResourceInfo resourceInfo)
        {
            Dictionary<Vector3Int, ResourceInfo> dict = resources[itemType];
            dict[posMap].count -= resourceInfo.count;
            if(dict[posMap].count <= 0)
            {
                dict.Remove(posMap);
            }
        }

        public void subDropByAll(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            foreach (KeyValuePair<ItemType, Dictionary<Vector3Int, ResourceInfo>> pair in resources)
            {
                Dictionary<Vector3Int, ResourceInfo> dict = resources[pair.Key];
                if (!dict.ContainsKey(posMap)) continue;
                dict[posMap].count -= resourceInfo.count;
                if (dict[posMap].count <= 0)
                {
                    dict.Remove(posMap);
                }
                return;
            }
        }

        public ResourceInfo getDrop(ItemType itemType, Vector3Int posMap) {
            if (!resources[itemType].ContainsKey(posMap)) return null;
            return resources[itemType][posMap];
        }

        public ResourceInfo getDropByAll(Vector3Int posMap)
        {
            foreach (KeyValuePair<ItemType, Dictionary<Vector3Int, ResourceInfo>> pair in resources)
            {
                if (pair.Value.ContainsKey(posMap))
                {
                    return pair.Value[posMap];
                }
            }
            return null;
        }

        public string ToString(Vector3Int posMap)
        {
            string text = "";
            ResourceInfo resourceInfo = getDropByAll(posMap);
            if (resourceInfo != null)
            {
                text += $"id:{resourceInfo.id}\n" +
                $"count:{resourceInfo.count}\n";
            }
            return text;
        }
    }

    [Serializable]
    public class ResourceInfo {
        /// <summary>
        /// Inventory,id=-1±Ì æø’
        /// </summary>
        public int id;
        public int count;

        public ResourceInfo(int id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }
}
