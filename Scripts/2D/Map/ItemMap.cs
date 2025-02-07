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
    public class ItemMap : MonoBehaviour
    {
        public static ItemMap Instance { private set; get; }
        public Tilemap ItemTileMap { get; set; }
        public ItemMapData ItemMapDataLAB { get; set; }

        private void Awake()
        {
            Instance = this;
            ItemMapDataLAB = new ItemMapData();
            ItemTileMap = GetComponent<Tilemap>();
        }

        /// <summary>
        /// 捡起掉落物
        /// </summary>
        /// <param name="posMap"></param>
        public void pickUp(Vector3Int posMap)
        {
            ItemMapDataLAB.remove(posMap);
            ItemTileMap.SetTile(posMap, null);
        }

        /// <summary>
        /// 放置掉落物或仓库
        /// 若放置掉落物，则添加搬运任务，并添加掉落物管理
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="tileBase"></param>
        public void putDown(Vector3Int posMap, TileBase tileBase, ResourceInfo resourceInfo = null, ItemType itemType = ItemType.Null)
        {
            if (ItemMapDataLAB.containKey(posMap)) return;
            ItemMapDataLAB.add(posMap, tileBase.name);
            ItemTileMap.SetTile(posMap, tileBase);
            if (resourceInfo == null) return;
            // 添加到掉落物管理中
            DropResourceManager.Instance.addDrop(itemType, posMap, resourceInfo);
            // 添加搬运任务
            WorkerTaskManager.Instance.addTask(new WorkerCarryTask.CarryTaskBuilder()
                //.setEndTarget(InventoryManager.Instance.getCell(id))
                .setResourceInfo(Tool.DeepCopyByBinary(resourceInfo)).setStartTarget(posMap).build());
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.GetComponent<Player>() == null) return;
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(collision.transform.position);
            TileBase tile = ItemTileMap.GetTile(posMap);
            if(tile != null)
            {
                BackpackController.Instance.addItem(ItemFactory.Instance.getBackpackItemByName(ItemTileMap.GetTile(posMap).name));
                ItemTileMap.SetTile(posMap, null);
            }
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    tile = ItemTileMap.GetTile(posMap);
                    if (tile != null)
                    {
                        BackpackController.Instance.addItem(ItemFactory.Instance.getBackpackItemByName(ItemTileMap.GetTile(posMap).name));
                        ItemTileMap.SetTile(posMap, null);
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
