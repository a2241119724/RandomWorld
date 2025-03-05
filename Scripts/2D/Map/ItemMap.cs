using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// 掉落物，仓库，土地管理
    /// </summary>
    public class ItemMap : BaseTileMap
    {
        public static ItemMap Instance { private set; get; }
        public ItemMapData ItemMapDataLAB { get; set; }


        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            ItemMapDataLAB = new ItemMapData();
        }

        /// <summary>
        /// 隐藏图标
        /// </summary>
        /// <param name="posMap"></param>
        public void hindTile(Vector3Int posMap)
        {
            ItemMapDataLAB.remove(posMap);
            tilemap.SetTile(posMap, null);
        }

        public void pickUpFromInventory(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            hindTile(posMap);
            InventoryManager.Instance.subItem(posMap, resourceInfo);
        }

        public void pickUpFromDrop(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            // 删除拿起来的东西
            DropResourceManager.Instance.subDropByAll(posMap, resourceInfo);
            hindTile(posMap);
        }

        /// <summary>
        /// 仅显示图片
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="tileBase"></param>
        public void showTile(Vector3Int posMap, TileBase tileBase)
        {
            if (ItemMapDataLAB.containKey(posMap)) return;
            ItemMapDataLAB.add(posMap, tileBase.name);
            tilemap.SetTile(posMap, tileBase);
        }

        public void putDownToInventory(Vector3Int posMap, TileBase tileBase, ResourceInfo resourceInfo)
        {
            showTile(posMap, tileBase);
            InventoryManager.Instance.addItem(posMap, resourceInfo);
        }

        /// <summary>
        /// 放置掉落物
        /// </summary>
        public void putDownToDrop(Vector3Int posMap, TileBase tileBase, ResourceInfo resourceInfo)
        {
            showTile(posMap, tileBase);
            ItemType itemType = ItemDataManager.Instance.getTypeById(resourceInfo.id);
            // 添加到掉落物管理中
            DropResourceManager.Instance.addDrop(itemType, posMap, resourceInfo);
            // 添加搬运任务
            WorkerTaskManager.Instance.addTask(new WorkerCarryTask.CarryTaskBuilder()
                //.setEndTarget(InventoryManager.Instance.getCell(id))
                .setResourceInfo(resourceInfo).setStartTarget(posMap).build());
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.GetComponent<Player>() == null) return;
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(collision.transform.position);
            TileBase tile = tilemap.GetTile(posMap);
            if(tile != null)
            {
                BackpackController.Instance.addItem(ItemFactory.Instance.getBackpackItemByName(tilemap.GetTile(posMap).name));
                tilemap.SetTile(posMap, null);
            }
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    tile = tilemap.GetTile(posMap);
                    if (tile != null)
                    {
                        BackpackController.Instance.addItem(ItemFactory.Instance.getBackpackItemByName(tilemap.GetTile(posMap).name));
                        tilemap.SetTile(posMap, null);
                    }
                }
            }
        }

        [Serializable]
        public class ItemMapData
        {
            /// <summary>
            /// string:TileBase
            /// </summary>
            public Dictionary<Vector3IntLAB, string> posMaps;

            public ItemMapData()
            {
                posMaps = new Dictionary<Vector3IntLAB, string>();
            }

            public void remove(Vector3Int pos)
            {
                posMaps.Remove(Vector3IntLAB.toVector3IntLAB(pos));
            }

            public void add(Vector3Int pos, string tileBase)
            {
                posMaps.Add(Vector3IntLAB.toVector3IntLAB(pos), tileBase);
            }

            public bool containKey(Vector3Int pos)
            {
                return posMaps.ContainsKey(Vector3IntLAB.toVector3IntLAB(pos));
            }
        }
    }
}
