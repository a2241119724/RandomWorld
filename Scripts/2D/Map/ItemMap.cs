namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 掉落物，仓库，土地管理
    /// </summary>
    public class ItemMap : BaseTileMap
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static ItemMap Instance { get; private set; }

        /// <summary>
        /// 地图数据
        /// </summary>
        public ItemMapData ItemMapDataLAB { get; set; }

        /// <summary>
        /// 隐藏图标
        /// </summary>
        /// <param name="posMap">位置</param>
        public void HindTile(Vector3Int posMap)
        {
            this.ItemMapDataLAB.Remove(posMap);
            this.tilemap.SetTile(posMap, null);
        }

        /// <summary>
        /// 从仓库捡起
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PickUpFromInventory(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            this.HindTile(posMap);
            InventoryManager.Instance.SubItem(posMap, resourceInfo);
        }

        /// <summary>
        /// 从掉落物捡起
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PickUpFromDrop(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            // 删除拿起来的东西
            DropResourceManager.Instance.SubDropByAll(posMap, resourceInfo);
            this.HindTile(posMap);
        }

        /// <summary>
        /// 仅显示图片
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="tileBase">瓦片</param>
        public void ShowTile(Vector3Int posMap, TileBase tileBase)
        {
            if (this.ItemMapDataLAB.ContainKey(posMap))
            {
                return;
            }

            this.ItemMapDataLAB.Add(posMap, tileBase.name);
            this.tilemap.SetTile(posMap, tileBase);
        }

        /// <summary>
        /// 放到仓库
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="tileBase">瓦片</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PutDownToInventory(Vector3Int posMap, TileBase tileBase, ResourceInfo resourceInfo)
        {
            this.ShowTile(posMap, tileBase);
            InventoryManager.Instance.AddItem(posMap, resourceInfo);
        }

        /// <summary>
        /// 放置掉落物
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="tileBase">瓦片</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PutDownToDrop(Vector3Int posMap, TileBase tileBase, ResourceInfo resourceInfo)
        {
            this.ShowTile(posMap, tileBase);
            ItemType itemType = ItemDataManager.Instance.GetTypeById(resourceInfo.Id);

            // 添加到掉落物管理中
            DropResourceManager.Instance.AddDrop(itemType, posMap, resourceInfo);

            // 添加搬运任务
            WorkerTaskManager.Instance.AddTask(new WorkerCarryTask.CarryTaskBuilder()

                // .setEndTarget(InventoryManager.Instance.getCell(id))
                .setResourceInfo(resourceInfo).setStartTarget(posMap).build());
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            this.ItemMapDataLAB = new ItemMapData();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.GetComponent<Player>() == null)
            {
                return;
            }

            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(collision.transform.position);
            TileBase tile = this.tilemap.GetTile(posMap);
            if (tile != null)
            {
                BackpackController.Instance.addItem(ItemFactory.Instance.GetBackpackItemByName(this.tilemap.GetTile(posMap).name));
                this.tilemap.SetTile(posMap, null);
            }

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    tile = this.tilemap.GetTile(posMap);
                    if (tile != null)
                    {
                        BackpackController.Instance.addItem(ItemFactory.Instance.GetBackpackItemByName(this.tilemap.GetTile(posMap).name));
                        this.tilemap.SetTile(posMap, null);
                    }
                }
            }
        }

        /// <summary>
        /// 地图数据
        /// </summary>
        [Serializable]
        public class ItemMapData
        {
            /// <summary>
            /// string:TileBase
            /// </summary>
            public Dictionary<Vector3IntLAB, string> PosMaps;

            public ItemMapData()
            {
                this.PosMaps = new Dictionary<Vector3IntLAB, string>();
            }

            /// <summary>
            /// 移除数据
            /// </summary>
            /// <param name="pos">位置</param>
            public void Remove(Vector3Int pos)
            {
                this.PosMaps.Remove(Vector3IntLAB.ToVector3IntLAB(pos));
            }

            /// <summary>
            /// 添加数据
            /// </summary>
            /// <param name="pos">位置</param>
            /// <param name="tileBase">瓦片</param>
            public void Add(Vector3Int pos, string tileBase)
            {
                this.PosMaps.Add(Vector3IntLAB.ToVector3IntLAB(pos), tileBase);
            }

            /// <summary>
            /// 是否包含Key
            /// </summary>
            /// <param name="pos">位置</param>
            /// <returns>是否</returns>
            public bool ContainKey(Vector3Int pos)
            {
                return this.PosMaps.ContainsKey(Vector3IntLAB.ToVector3IntLAB(pos));
            }
        }
    }
}
