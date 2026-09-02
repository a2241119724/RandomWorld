namespace LAB2D.Item
{
    using LAB2D;
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
        private static readonly Dictionary<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> Resources = new ();

        /// <summary>
        /// 当前掉落物总数（按掉落类型聚合，O(类型数) 廉价读取，无分配）。
        /// 供决策层"全图无掉落则跳过逐格扫描"的提前退出判断（性能优化）。
        /// </summary>
        public int TotalDropCount
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> pair in Resources)
                {
                    total += pair.Value.Count;
                }

                return total;
            }
        }

        /// <summary>
        /// 添加掉落物（保持 resourceInfo 中的 OwnerId）。
        /// </summary>
        /// <param name="itemType">掉落物类型</param>
        /// <param name="posMap">掉落物位置</param>
        /// <param name="resourceInfo">具体掉落物信息（含所有权）</param>
        public void AddDrop(AItem.ItemTypeEnum itemType, Vector3Int posMap, ResourceInfo resourceInfo)
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
                // 合并时保持先到者的 OwnerId（同一位置同类型资源归同一拥有者）
            }
            else
            {
                dict.Add(posMap, DataTool.DeepCopyByBinary(resourceInfo));
            }
        }

        /// <summary>
        /// 检查指定拾取者是否可以拾取该位置的掉落物。
        /// </summary>
        /// <param name="posMap">掉落物位置</param>
        /// <param name="pickerOwnerId">拾取者 OwnerId（Player=0, Worker=instanceId）</param>
        /// <returns>可以拾取返回 true；位置无掉落物也返回 false</returns>
        public bool CanPickUp(Vector3Int posMap, int pickerOwnerId)
        {
            ResourceInfo resource = this.GetDropByAll(posMap);
            if (resource == null)
            {
                return false;
            }

            return Domain.Worker.ItemOwnershipService.CanPickUp(resource, pickerOwnerId);
        }

        /// <summary>
        /// 获取掉落物的拥有者 ID。
        /// </summary>
        /// <param name="posMap">掉落物位置</param>
        /// <returns>拥有者 ID，无掉落物返回 -1</returns>
        public int GetOwnerId(Vector3Int posMap)
        {
            ResourceInfo resource = this.GetDropByAll(posMap);
            return resource != null ? resource.OwnerId : -1;
        }

        /// <summary>
        /// 删除掉落物
        /// </summary>
        /// <param name="itemType">掉落物类型</param>
        /// <param name="posMap">掉落物位置</param>
        /// <param name="resourceInfo">具体掉落物信息</param>
        public void SubDrop(AItem.ItemTypeEnum itemType, Vector3Int posMap, ResourceInfo resourceInfo)
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
            foreach (KeyValuePair<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> pair in Resources)
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
        public ResourceInfo GetDrop(AItem.ItemTypeEnum itemType, Vector3Int posMap)
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
            foreach (KeyValuePair<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> pair in Resources)
            {
                if (pair.Value.ContainsKey(posMap))
                {
                    return pair.Value[posMap];
                }
            }

            return null;
        }

        /// <summary>
        /// 掉落物管理信息（含所有权）。
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int posMap)
        {
            string text = string.Empty;
            ResourceInfo resourceInfo = this.GetDropByAll(posMap);
            if (resourceInfo != null)
            {
                ItemData itemData = Core.ServiceLocator.Get<ItemDataManager>().GetById(resourceInfo.Id);
                if (itemData != null)
                {
                    text += $"ID:{resourceInfo.Id}\n" +
                        $"名称:{itemData.CnName}\n" +
                        $"英文名:{itemData.Name}\n" +
                        $"类型:{itemData.Type}\n" +
                        $"数量:{resourceInfo.Count}\n" +
                        $"拥有者:{Domain.Worker.ItemOwnershipService.GetOwnerLabel(resourceInfo)}\n" +
                        $"信息:{itemData.Info}\n" +
                        $"可堆叠:{itemData.IsStackable}\n";

                    if (itemData.Type == AItem.ItemTypeEnum.Weapon || itemData.Type == AItem.ItemTypeEnum.Equipment)
                    {
                        text += $"装备槽位:{itemData.EquipSlot}\n";
                    }
                }
                else
                {
                    text += $"ID:{resourceInfo.Id}\n" +
                        $"数量:{resourceInfo.Count}\n" +
                        $"拥有者:{Domain.Worker.ItemOwnershipService.GetOwnerLabel(resourceInfo)}\n";
                }
            }

            return text;
        }
    }
}
