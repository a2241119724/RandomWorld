namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 掉落物管理
    /// </summary>
    public class DropManager : Singleton<DropManager>
    {
        /// <summary>
        /// 掉落物
        /// </summary>
        private static readonly Dictionary<Item.ItemType, Dictionary<Vector3Int, ResourceInfo>> Resources = new ();

        /// <summary>
        /// 添加掉落物
        /// </summary>
        /// <param name="itemType">掉落物类型</param>
        /// <param name="posMap">掉落物位置</param>
        /// <param name="resourceInfo">具体掉落物信息</param>
        public void AddDrop(Item.ItemType itemType, Vector3Int posMap, ResourceInfo resourceInfo)
        {
            Dictionary<Vector3Int, ResourceInfo> dict;
            if (Resources.ContainsKey(itemType))
            {
                dict = Resources[itemType];
            }
            else
            {
                dict = new Dictionary<Vector3Int, ResourceInfo>();
                Resources.Add(itemType, dict);
            }

            if (dict.ContainsKey(posMap))
            {
                dict[posMap].Count += resourceInfo.Count;
            }
            else
            {
                dict.Add(posMap, Tool.DeepCopyByBinary(resourceInfo));
            }
        }

        /// <summary>
        /// 删除掉落物
        /// </summary>
        /// <param name="itemType">掉落物类型</param>
        /// <param name="posMap">掉落物位置</param>
        /// <param name="resourceInfo">具体掉落物信息</param>
        public void SubDrop(Item.ItemType itemType, Vector3Int posMap, ResourceInfo resourceInfo)
        {
            Dictionary<Vector3Int, ResourceInfo> dict = Resources[itemType];
            dict[posMap].Count -= resourceInfo.Count;
            if (dict[posMap].Count <= 0)
            {
                dict.Remove(posMap);
            }
        }

        /// <summary>
        /// 删除掉落物，对于所有的掉落物
        /// </summary>
        /// <param name="posMap">掉落物位置</param>
        /// <param name="resourceInfo">具体掉落物信息</param>
        public void SubDropByAll(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            foreach (KeyValuePair<Item.ItemType, Dictionary<Vector3Int, ResourceInfo>> pair in Resources)
            {
                Dictionary<Vector3Int, ResourceInfo> dict = Resources[pair.Key];
                if (!dict.ContainsKey(posMap))
                {
                    continue;
                }

                dict[posMap].Count -= resourceInfo.Count;
                if (dict[posMap].Count <= 0)
                {
                    dict.Remove(posMap);
                }

                return;
            }
        }

        /// <summary>
        /// 获得掉落物
        /// </summary>
        /// <param name="itemType">掉落物类型</param>
        /// <param name="posMap">掉落物位置</param>
        /// <returns>掉落物信息</returns>
        public ResourceInfo GetDrop(Item.ItemType itemType, Vector3Int posMap)
        {
            if (!Resources[itemType].ContainsKey(posMap))
            {
                return null;
            }

            return Resources[itemType][posMap];
        }

        /// <summary>
        /// 获得掉落物，对于所有的掉落物
        /// </summary>
        /// <param name="posMap">掉落物位置</param>
        /// <returns>掉落物信息</returns>
        public ResourceInfo GetDropByAll(Vector3Int posMap)
        {
            foreach (KeyValuePair<Item.ItemType, Dictionary<Vector3Int, ResourceInfo>> pair in Resources)
            {
                if (pair.Value.ContainsKey(posMap))
                {
                    return pair.Value[posMap];
                }
            }

            return null;
        }

        /// <summary>
        /// 凋落物管理信息
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int posMap)
        {
            string text = string.Empty;
            ResourceInfo resourceInfo = this.GetDropByAll(posMap);
            if (resourceInfo != null)
            {
                text += $"id:{resourceInfo.Id}\n" +
                $"count:{resourceInfo.Count}\n";
            }

            return text;
        }
    }

    /// <summary>
    /// 资源信息
    /// </summary>
    [Serializable]
    public class ResourceInfo
    {
        /// <summary>
        /// Inventory,id=-1表示空
        /// </summary>
        public int Id;

        /// <summary>
        /// 数量
        /// </summary>
        public int Count;

        public ResourceInfo(int id, int count)
        {
            this.Id = id;
            this.Count = count;
        }
    }
}
