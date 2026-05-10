namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 敌人掉落物管理
    /// </summary>
    public class EnemyDropManager
    {
        private readonly Dictionary<int, TileBase> probToDropItem; // key:获取对应item的概率值
        private int dropTotal; // 生成Item的总概率值

        public EnemyDropManager()
        {
            this.probToDropItem = new Dictionary<int, TileBase>();
            List<AItem> items = ItemInstanceFactory.Instance.GenBackpackItems();
            this.AddDropItem(10, null);
            foreach (ABackpackItem item in items)
            {
                this.AddDropItem(10, item.Tile);
            }
        }

        /// <summary>
        /// 概率获取掉落道具
        /// </summary>
        /// <param name="dropCenter">中心位置</param>
        public void DropItem(Vector3 dropCenter)
        {
            int rand = Random.Range(0, this.dropTotal);

            // 转为数组下标
            Vector3Int pos = IsAvailableMap.Instance.GenAvailablePosMap(
                TileMap.Instance.WorldPosToMapPos(dropCenter), 3, true);
            if (pos == default)
            {
                return;
            }

            foreach (KeyValuePair<int, TileBase> dropItem in this.probToDropItem)
            {
                if (rand <= dropItem.Key)
                {
                    if (dropItem.Value == null)
                    {
                        break;
                    }

                    ItemData itemData = ItemDataManager.Instance.GetByName(dropItem.Value.name);
                    ResourceInfo resourceInfo = new (itemData.Id, 1);
                    ItemMap.Instance.PutDownToDrop(pos, dropItem.Value, resourceInfo);

                    // 为掉落道具生成光束特效
                    Vector3 beamWorldPos = TileMap.Instance.MapPosToWorldPos(pos);
                    EquipmentBeamManager.Instance.SpawnBeam(pos, beamWorldPos, EquipmentRarityType.Common);
                    break;
                }
            }
        }

        /// <summary>
        /// 添加可掉落物品到字典中.
        /// </summary>
        /// <param name="value">概率范围</param>
        /// <param name="tileBase">tile</param>
        protected void AddDropItem(int value, TileBase tileBase)
        {
            this.dropTotal += value;
            this.probToDropItem.Add(this.dropTotal, tileBase);
        }
    }
}