namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Item;
    using LAB2D.Item.Backpack;
    using LAB2D.Serializable;
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

        /// <summary>
        /// 物品视觉拆分器：仅管理"非恒底层"物品（ItemData.IsBottomLayer=false）的独立
        /// SpriteRenderer（Character 层 ItemVisual_*，参与 y 排序）。
        /// 恒底层物品（默认开启）由 ItemMap 的 TilemapRenderer 直接渲染在 Map 上，不建 ItemVisual_*。
        /// </summary>
        private TileVisualSpawner visuals;

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.ItemMapDataLAB = new ItemMapData();

            // 物品视觉混合模式：
            // - 恒底层物品（ItemData.IsBottomLayer 默认开启）：由 TilemapRenderer 直接渲染在 Map 上，
            //   不创建 ItemVisual_*（TilemapRenderer 保持启用）。
            // - 非恒底层物品：tile 保留数据/碰撞，颜色透明隐藏 TilemapRenderer 双重渲染，
            //   由独立 SpriteRenderer（Character 层 ItemVisual_*）参与 y 排序。
            this.visuals = new TileVisualSpawner(this.tilemap, this.transform, "Character", "ItemVisual", this.IsBottomLayerItem, useTilemapColor: false);
        }

        /// <summary>
        /// 删除图标
        /// </summary>
        /// <param name="posMap">位置</param>
        public void DeleteTile(Vector3Int posMap)
        {
            this.ItemMapDataLAB.Remove(posMap);
            this.tilemap.SetTile(posMap, null);
            // 视觉同步：tilemap tile 已删（恒底层视觉随之消失）；删独立 SpriteRenderer（非恒底层）
            this.visuals?.Delete(posMap);
            this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, true);
        }

        /// <summary>
        /// 从仓库捡起
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PickUpFromInventory(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            this.DeleteTile(posMap);
            Core.ServiceLocator.Get<InventoryManager>().SubItem(posMap, resourceInfo);
        }

        /// <summary>
        /// 从掉落物捡起
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="resourceInfo">资源信息</param>
        public void PickUpFromDrop(Vector3Int posMap, ResourceInfo resourceInfo)
        {
            // 删除拿起来的东西
            Core.ServiceLocator.Get<DropManager>().SubDropByAll(posMap, resourceInfo);
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
            // 视觉分流：恒底层由 TilemapRenderer 渲染在 Map 上；非恒底层单独建 SpriteRenderer 参与 y 排序
            // （解除 LockColor 与 SetColor 统一在 ApplyTileVisual 内处理）
            this.ApplyTileVisual(posMap);
            this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), tileBase.name);
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
            Core.ServiceLocator.Get<InventoryManager>().AddItem(posMap, resourceInfo);
            Core.ServiceLocator.Get<EnemyLootManager>().TrySpawnBeamForInventory(posMap, resourceInfo.Id);
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
            AItem.ItemTypeEnum itemType = Core.ServiceLocator.Get<ItemDataManager>().IdToType(resourceInfo.Id);

            // 添加到掉落物管理中
            Core.ServiceLocator.Get<DropManager>().AddDrop(itemType, posMap, resourceInfo);

            // 添加搬运任务
            Core.ServiceLocator.Get<WorkerTaskManager>().AddTask(
                new WorkerCarryTask.CarryTaskBuilder()
                .SetResourceInfo(resourceInfo).SetStartTarget(posMap).Build(), new GameGridPosition(posMap.x, posMap.y, posMap.z));
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            AWorkerTask.LogProvider("Request: 同步地图道具数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.ItemMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            AWorkerTask.LogProvider("Response: 同步地图道具数据", LogManager.LogLevelEnum.Trace);
            ItemMapData itemMapData = DataTool.FromByteArray<ItemMapData>(data);
            // 全量同步：先清空 ItemVisual_*，视觉以新数据为准重建（恒底层由 TilemapRenderer 渲染）
            this.visuals?.ClearAll();
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = itemMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Vector3Int pos = Vector3IntLAB.ToVector3Int(enumerator.Current.Key);
                this.tilemap.SetTile(pos, (TileBase)AWorkerTask.ResourceLoadProvider(enumerator.Current.Value));
                this.ApplyTileVisual(pos);
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
            AWorkerTask.LogProvider("Response: 同步地图道具数据", LogManager.LogLevelEnum.Trace);

            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                this.visuals?.Delete(vector3Int);
                return;
            }

            this.tilemap.SetTile(vector3Int, (TileBase)AWorkerTask.ResourceLoadProvider(tileBaseName));
            this.ApplyTileVisual(vector3Int);
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

            // 拾取前获取装备稀有度信息，避免创建新道具后品质丢失
            EquipmentRarityType? rarity = Core.ServiceLocator.Get<EnemyLootManager>().TryGetRarityByMapPosition(posMap);

            // 安全创建物品实例（tile 名称可能不在 backpackItemTypes 中）
            AItem item;
            try
            {
                item = Core.ServiceLocator.Get<ItemInstanceFactory>().GetBackpackItemByName(tile.name);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                AWorkerTask.LogProvider(
                    $"PickUpItem: tile '{tile.name}' 不是有效的背包物品类型，无法拾取",
                    LogManager.LogLevelEnum.Error);
                return;
            }

            // 装备物品：交给 EnemyLootManager 触发对比弹窗，不直接入背包。
            // 稀有度、属性、地面清理均由 OnEquipmentPickup 内部处理。
            if (item is AEquipment equipment)
            {
                Core.ServiceLocator.Get<EnemyLootManager>().OnEquipmentPickup(equipment, equipment.Id, posMap);
                return;
            }

            // 将掉落时的品质应用到拾取的非装备道具上
            if (rarity.HasValue && item is ABackpackItem backpackItem)
            {
                backpackItem.Quality = EquipmentLootTool.MapRarityToQuality(rarity.Value);
            }

            // 先添加到背包，成功后再清理地面数据，避免物品丢失
            Core.ServiceLocator.Get<BackpackController>().AddItem(item);
            Core.ServiceLocator.Get<ItemCollectionTracker>().RecordItemCollected(new ResourceInfo(item.Id, 1));
            Core.ServiceLocator.Get<EnemyLootManager>().RemoveDropByMapPosition(posMap);

            ResourceInfo resourceInfo = Core.ServiceLocator.Get<DropManager>().GetDropByAll(posMap);
            if (resourceInfo != null)
            {
                Core.ServiceLocator.Get<DropManager>().SubDropByAll(posMap, resourceInfo);
            }

            this.DeleteTile(posMap);
        }

        /// <summary>
        /// 该格物品是否为"恒底层"物品（ItemData.IsBottomLayer 开关）。
        /// 恒底层物品不参与 y 排序，固定显示在地图上面（角色/其他建筑永远盖在其上）；
        /// 供 TileVisualSpawner 的 bottomLayerResolver 使用。tile 名查不到数据时按恒底层处理。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <returns>是否为恒底层物品。</returns>
        private bool IsBottomLayerItem(Vector3Int cell)
        {
            TileBase tile = this.tilemap.GetTile(cell);
            if (tile == null)
            {
                return false;
            }

            ItemData item = Core.ServiceLocator.Get<ItemDataManager>().GetByName(tile.name);
            return item != null && item.IsBottomLayer;
        }

        /// <summary>
        /// 按该格物品 IsBottomLayer 分流视觉：
        /// 恒底层 → 由 ItemMap TilemapRenderer 直接渲染（恢复白色，不建 ItemVisual_*）；
        /// 非恒底层 → tilemap 颜色透明隐藏（避免 TilemapRenderer 双重渲染，数据/碰撞保留），
        ///            单独建 SpriteRenderer（ItemVisual_*）参与 y 排序。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        private void ApplyTileVisual(Vector3Int cell)
        {
            if (!this.tilemap.HasTile(cell))
            {
                this.visuals?.Delete(cell);
                return;
            }

            // 先解除 LockColor：否则 SetColor 不生效，非恒底层 tile 的透明隐藏会失败
            // （BuildMap/ResourceMap 同款前置写法）
            this.tilemap.RemoveTileFlags(cell, TileFlags.LockColor);

            if (this.IsBottomLayerItem(cell))
            {
                // 恒底层：恢复颜色由 TilemapRenderer 渲染在 Map 上；确保不残留 ItemVisual_*
                this.tilemap.SetColor(cell, Color.white);
                this.visuals?.Delete(cell);
            }
            else
            {
                // 非恒底层：tilemap 透明隐藏（数据/碰撞保留），视觉由独立 SpriteRenderer 呈现
                this.tilemap.SetColor(cell, new Color(1f, 1f, 1f, 0f));
                this.visuals?.CreateOrUpdate(cell);
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
