namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 掉落物，仓库，土地管理
    /// </summary>
    public class ItemMap : BaseTileMap, ISyncData
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static ItemMap Instance { get; private set; }

        /// <summary>
        /// 地图数据
        /// </summary>
        public ItemMapData ItemMapDataLAB { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.ItemMapDataLAB = new ItemMapData();
        }

        /// <summary>
        /// 删除图标
        /// </summary>
        /// <param name="posMap">位置</param>
        public void DeleteTile(Vector3Int posMap)
        {
            this.ItemMapDataLAB.Remove(posMap);
            this.tilemap.SetTile(posMap, null);
            this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, Tool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, true);
        }

        /// <summary>
        /// 从仓库捡起
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PickUpFromInventory(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            this.DeleteTile(posMap);
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
            DropManager.Instance.SubDropByAll(posMap, resourceInfo);
            this.DeleteTile(posMap);
        }

        /// <summary>
        /// 仅显示图片
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="tileBase">瓦片</param>
        public void AddTile(Vector3Int posMap, TileBase tileBase)
        {
            if (this.ItemMapDataLAB.ContainKey(posMap))
            {
                return;
            }

            this.ItemMapDataLAB.Add(posMap, tileBase.name);
            this.tilemap.SetTile(posMap, tileBase);
            this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, Tool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), tileBase.name);
        }

        /// <summary>
        /// 放到仓库
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="tileBase">瓦片</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PutDownToInventory(Vector3Int posMap, TileBase tileBase, ResourceInfo resourceInfo)
        {
            this.AddTile(posMap, tileBase);
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
            this.AddTile(posMap, tileBase);
            Item.ItemType itemType = ItemDataManager.Instance.GetTypeById(resourceInfo.Id);

            // 添加到掉落物管理中
            DropManager.Instance.AddDrop(itemType, posMap, resourceInfo);

            // 添加搬运任务
            WorkerTaskManager.Instance.AddTask(new WorkerCarryTask.CarryTaskBuilder()

                // .setEndTarget(InventoryManager.Instance.getCell(id))
                .SetResourceInfo(resourceInfo).SetStartTarget(posMap).Build());
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图道具数据");
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.ItemMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图道具数据");
            ItemMapData itemMapData = Tool.FromByteArray<ItemMapData>(data);
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = itemMapData.PosMaps.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator.Current.Key),
                    (TileBase)ResourceManager.Instance.GetAsset(enumerator.Current.Value));
            }
        }

        /// <summary>
        /// 同步地图道具数据
        /// </summary>
        /// <param name="vector3IntLAB">位置</param>
        /// <param name="tileBaseName">tile名称</param>
        /// <param name="isDelete">是否删除</param>
        [PunRPC]
        public void SyncDataResp(byte[] vector3IntLAB, string tileBaseName, bool isDelete = false)
        {
            LogManager.Instance.Log("Response: 同步地图道具数据");

            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(Tool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            this.tilemap.SetTile(vector3Int, (TileBase)ResourceManager.Instance.GetAsset(tileBaseName));
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
                BackpackController.Instance.AddItem(ItemFactory.Instance.GetBackpackItemByName(this.tilemap.GetTile(posMap).name));
                this.DeleteTile(posMap);
            }

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    tile = this.tilemap.GetTile(posMap);
                    if (tile != null)
                    {
                        BackpackController.Instance.AddItem(ItemFactory.Instance.GetBackpackItemByName(this.tilemap.GetTile(posMap).name));
                        this.DeleteTile(posMap);
                    }
                }
            }
        }

        /// <summary>
        /// 道具数据
        /// </summary>
        [Serializable]
        public class ItemMapData : ATileMapData
        {
        }
    }
}
