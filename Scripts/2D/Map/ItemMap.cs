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
            if (NetworkConnect.Instance.IsOnline)
            {
                this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, true);
            }
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
            if (NetworkConnect.Instance.IsOnline)
            {
                this.PhotonView.RPC("SyncDataResp", RpcTarget.Others, DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), tileBase.name);
            }
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
            EquipmentLootManager.Instance.TrySpawnBeamForInventory(posMap, resourceInfo.Id);
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
            AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(resourceInfo.Id);

            // 添加到掉落物管理中
            DropManager.Instance.AddDrop(itemType, posMap, resourceInfo);

            // 添加搬运任务
            WorkerTaskManager.Instance.AddTask(
                new WorkerCarryTask.CarryTaskBuilder()
                .SetResourceInfo(resourceInfo).SetStartTarget(posMap).Build(), Vector3IntLAB.ToVector3IntLAB(posMap));
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图道具数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.ItemMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图道具数据", LogManager.LogLevelEnum.Trace);
            ItemMapData itemMapData = DataTool.FromByteArray<ItemMapData>(data);
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = itemMapData.PosMap.GetEnumerator();
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
            LogManager.Instance.Log("Response: 同步地图道具数据", LogManager.LogLevelEnum.Trace);

            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            this.tilemap.SetTile(vector3Int, (TileBase)ResourceManager.Instance.GetAsset(tileBaseName));
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 自动拾取已由 NearbyItemPickupHUD 接管，不再在此处自动拾取
        }

        /// <summary>
        /// 拾取指定位置的地面道具（整合完整拾取流程）。
        /// </summary>
        /// <param name="posMap">道具所在 tilemap 坐标</param>
        public void PickUpItem(Vector3Int posMap)
        {
            TileBase tile = this.tilemap.GetTile(posMap);
            if (tile == null)
            {
                return;
            }

            AItem item = ItemInstanceFactory.Instance.GetBackpackItemByName(tile.name);
            BackpackController.Instance.AddItem(item);
            ItemCollectionTracker.Instance.RecordItemCollected(new ResourceInfo(item.Id, 1));
            EquipmentLootManager.Instance.RemoveDropByMapPosition(posMap);

            ResourceInfo resourceInfo = DropManager.Instance.GetDropByAll(posMap);
            if (resourceInfo != null)
            {
                DropManager.Instance.SubDropByAll(posMap, resourceInfo);
            }

            this.DeleteTile(posMap);
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
