namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core.Seek;
    using LAB2D.Domain.Common;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 资源地图
    /// </summary>
    public class ResourceMap : BaseTileMap
    {
        private Tilemap resourceTileMapOne; // 仅占一格的资源，解决遮盖问题

        /// <summary>
        /// 树视觉拆分器（this.tilemap）：渲染到独立 SpriteRenderer（Character 层）。
        /// 恒底层资源（ItemData.LayerMode=Bottom）固定最底层，不参与 y 排序/遮挡淡化；
        /// 非恒底层资源参与 y 排序（LayerMode=Alpha 时玩家走到其后触发遮挡淡化）。
        /// Tilemap 保留碰撞/数据/存档/网络，TilemapRenderer 已禁用。
        /// </summary>
        private TileVisualSpawner visuals;

        /// <summary>
        /// 单格资源视觉拆分器（resourceTileMapOne，场景静态装饰）：同样拆到 Character 层参与 y 排序。
        /// </summary>
        private TileVisualSpawner visualsOne;

        /// <summary>
        /// 单例
        /// </summary>
        public static ResourceMap Instance { get; private set; }

        /// <summary>
        /// 资源地图数据
        /// </summary>
        public ResourceMapData ResourceMapDataLAB { get; private set; }

        /// <summary>
        /// 系统保留格（危险区资源等）：已由系统放置资源的格。
        /// GenResource 逐格扫描时跳过，防止概率命中后 Dictionary.Add 重复 key 异常；
        /// 不入档——只在撒放当帧与 GenResource 并发窗口内需要。
        /// </summary>
        private readonly HashSet<Vector3IntLAB> reservedPositions = new HashSet<Vector3IntLAB>();

        /// <summary>
        /// 登记系统保留格（系统放置资源前调用，防生成管线同格二次放置）。
        /// </summary>
        /// <param name="pos">位置</param>
        public void ReservePosition(Vector3Int pos)
        {
            this.reservedPositions.Add(Vector3IntLAB.ToVector3IntLAB(pos));
        }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.resourceTileMapOne = LAB2D.Tool.Tool.GetComponentInChildren<Tilemap>(this.transform.parent.gameObject, "ResourceMapOne");
            this.ResourceMapDataLAB = new ResourceMapData(0, 100);

            // 资源视觉混合模式：
            // - 恒底层资源（ItemData.LayerMode=Bottom）：由 TilemapRenderer 直接渲染在地图层，
            //   不创建 TreeVisual_*（两个 TilemapRenderer 保持启用）。
            // - 非恒底层资源：tile 保留数据/碰撞，颜色透明隐藏 TilemapRenderer 双重渲染，
            //   由独立 SpriteRenderer（Character 层 TreeVisual_*/TreeVisualOne_*）参与 y 排序。
            // 碰撞体由 TilemapCollider2D 从数据生成，与 Renderer 无关，IsCanReach（GetColliderType）不受影响。
            TilemapRenderer renderer = this.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }

            this.visuals = new TileVisualSpawner(this.tilemap, this.transform, "Character", "TreeVisual",
                this.GetResourceLayerMode, useTilemapColor: false, animationResolver: this.GetResourceAnimationPrefix,
                swayResolver: this.GetResourceSway);
            if (this.resourceTileMapOne != null)
            {
                TilemapRenderer rendererOne = this.resourceTileMapOne.GetComponent<TilemapRenderer>();
                if (rendererOne != null)
                {
                    rendererOne.enabled = true;
                }

                this.visualsOne = new TileVisualSpawner(this.resourceTileMapOne, this.transform, "Character", "TreeVisualOne",
                    this.GetResourceLayerMode, useTilemapColor: false, animationResolver: this.GetResourceAnimationPrefix,
                    swayResolver: this.GetResourceSway);
                this.visualsOne.RebuildAll(); // 静态场景装饰，一次性建视觉
            }
        }

        /// <inheritdoc/>
        public override void SetTile(Vector3Int pos, TileBase tileBase)
        {
            base.SetTile(pos, tileBase);
            WalkabilityCache.UpdateCell(pos);
            // 视觉同步（统一写路径入口）：有 tile 建/更，无 tile 删；刷新 8 邻域防树形态错乱
            this.visuals?.CreateOrUpdate(pos);
            this.visuals?.RefreshAround(pos);
        }

        /// <summary>
        /// 生成资源
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator GenResource()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete);
            if (!Core.ServiceLocator.TryGet(out TileMap tm) || tm.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMap data not available, cannot generate resources", LogManager.LogLevelEnum.Error);
                yield break;
            }

            // 确保进度总数已注册（幂等，重复调用安全）
            this.SetProgress();

            Core.GameServices.AsyncProgressSetTipProvider("生成资源...");
            int resourcesPlaced = 0;
            int assetMissCount = 0;
            for (int i = 0; i < tm.TileMapDataLAB.Height; i++)
            {
                for (int j = 0; j < tm.TileMapDataLAB.Width; j++)
                {
                    Core.GameServices.AsyncProgressAddOneProvider();
                    if (Core.ServiceLocator.Get<FrameControl>().IsNeedStop(1))
                    {
                        yield return null;
                    }

                    Vector3Int posMap = new (i, j, 0);
                    int terrainId = tm.TileMapDataLAB.MapTiles[i, j];

                    // 跳过水域格，避免无效检查
                    if (Core.ServiceLocator.Get<TerrainConfigDatabase>().IsWater(terrainId))
                    {
                        continue;
                    }

                    // 跳过系统保留格（危险区资源）：已放置，防 Dictionary.Add 重复 key 异常
                    if (this.reservedPositions.Contains(Vector3IntLAB.ToVector3IntLAB(posMap)))
                    {
                        continue;
                    }

                    if (tm.IsCanReach(posMap)
                        && Core.ServiceLocator.Get<TerrainConfigDatabase>().CanSpawnResources(terrainId)
                        && UnityEngine.Random.Range(0.0f, 1.0f) > 0.95f)
                    {
                        TileBase tileBase = Core.ServiceLocator.Get<ResourceManager>().GetAssetByTerrainId(terrainId);
                        if (tileBase == null)
                        {
                            assetMissCount++;
                            continue;
                        }

                        this.SetTile(posMap, tileBase);
                        this.ResourceMapDataLAB.Add(posMap, tileBase.name);
                        WalkabilityCache.UpdateCell(posMap);
                        resourcesPlaced++;
                    }
                }
            }

            if (resourcesPlaced == 0)
            {
                AWorkerTask.LogProvider($"GenResource: no resources placed (asset misses: {assetMissCount})", LogManager.LogLevelEnum.Warning);
            }

            // 重生上限 = 初始实际生成数量；当前数同步初始化（历史 bug：TreeCurCount 初始 0，
            // 砍初始树后变负 → while 恒真 → GenTree 无限补种）
            this.ResourceMapDataLAB.TreeTotalCount = resourcesPlaced;
            this.ResourceMapDataLAB.TreeCurCount = resourcesPlaced;

            // 确保进度条到达 100%（消除动态总量调整的微小缺口）
            Core.ServiceLocator.Get<LAB2D.UI.Panel.PanelUI.AsyncProgressUI>().ForceComplete();

            yield return this.StartCoroutine(this.GenTree());
        }

        /// <summary>
        /// 动态生成树
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator GenTree()
        {
            // 需要等待地图协程执行完后再执行
            yield return new WaitUntil(() => Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete);
            while (true)
            {
                if (this.ResourceMapDataLAB.TreeCurCount < this.ResourceMapDataLAB.TreeTotalCount)
                {
                    if (!Core.ServiceLocator.TryGet(out TileMap tm) || tm.TileMapDataLAB == null)
                    {
                        AWorkerTask.LogProvider("TileMap data not available, tree generation paused", LogManager.LogLevelEnum.Error);
                        yield return new WaitForSeconds(10f);
                        continue;
                    }

                    Vector3Int pos = Core.ServiceLocator.Get<IsAvailableMap>().GenAvailablePosMap();
                    int terrainId = tm.TileMapDataLAB.MapTiles[pos.x, pos.y];
                    if (!Core.ServiceLocator.Get<TerrainConfigDatabase>().CanGrowTrees(terrainId))
                    {
                        yield return null;
                        continue;
                    }

                    TileBase tileBase = Core.ServiceLocator.Get<ResourceManager>().GetAssetByTerrainId(terrainId);
                    if (tileBase == null)
                    {
                        yield return null;
                        continue;
                    }

                    this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(pos)), tileBase.name, false);

                    this.ResourceMapDataLAB.TreeCurCount++;
                    this.SetTile(pos, tileBase);
                    this.ResourceMapDataLAB.Add(pos, tileBase.name);
                    WalkabilityCache.UpdateCell(pos);
                    // 资源生成 → 该格变为不可通行（卡墙排查：新树阻挡原路径）。
                    // 注意：此处不得调用 tilemap.RefreshTile——它会从 tile 资产重置该格
                    // instance 颜色/flags，抹掉 SetColor(a=0) 的隐藏（历史 bug：半径内已有树
                    // 全部恢复不透明 + LockColor，与独立 SpriteRenderer 同位置双渲染）。
                    // RuleTile 邻域形态调和由 SetTile → TileVisualSpawner.RefreshAround 承担。
                    AWorkerTask.LogProvider(
                        $"[MapDiag] GenTree pos=({pos.x},{pos.y}) tile={tileBase.name}",
                        LogManager.LogLevelEnum.Trace);
                }

                yield return new WaitForSeconds(10f);
            }
        }

        /// <summary>
        /// 砍树
        /// </summary>
        /// <param name="posMap">位置</param>
        public void CutTree(Vector3Int posMap)
        {
            this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, false, true);

            this.ResourceMapDataLAB.Remove(posMap);
            this.SetTile(posMap, null);
            this.ResourceMapDataLAB.TreeCurCount--;
            WalkabilityCache.UpdateCell(posMap);
            // 资源移除 → 该格变为可通行（卡墙排查：砍树后路径重新打通）。
            AWorkerTask.LogProvider(
                $"[MapDiag] CutTree pos=({posMap.x},{posMap.y}) remain={this.ResourceMapDataLAB.TreeCurCount}",
                LogManager.LogLevelEnum.Trace);
        }

        /// <summary>
        /// 获取瓦片
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>瓦片</returns>
        public override TileBase GetTile(Vector3Int posMap)
        {
            TileBase tileBase = this.tilemap.GetTile(posMap);
            if (tileBase == null)
            {
                tileBase = this.resourceTileMapOne.GetTile(posMap);
            }

            return tileBase;
        }

        /// <summary>
        /// 该格资源的分层模式（ItemData.LayerMode 开关）。
        /// Bottom 恒底层资源不参与 y 排序，固定渲染在最底层（角色/建筑永远盖在其上），
        /// 玩家走到其后面也不触发遮挡淡化（不注册进 OcclusionFader）；
        /// 供 TileVisualSpawner 的 layerModeResolver 使用。tile 名查不到数据时按恒底层处理。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <returns>该格资源的分层模式。</returns>
        private ItemLayerMode GetResourceLayerMode(Vector3Int cell)
        {
            TileBase tile = this.GetTile(cell);
            if (tile == null)
            {
                return ItemLayerMode.Bottom;
            }

            ItemData item = Core.ServiceLocator.Get<ItemDataManager>().GetByName(tile.name);
            return item != null ? item.LayerMode : ItemLayerMode.Bottom;
        }

        /// <summary>
        /// 该格资源的帧动画前缀（ItemData.IsAnimation 开启且 LayerMode != Bottom 时启用，树资源通用）。
        /// 关闭/恒底层 = 返回 null，走静态 tile 图；开启且非恒底层 = 返回英文名（Name），
        /// TileVisualSpawner 按 Name_0/Name_1/... 序列图循环播放动画。
        /// 供 TileVisualSpawner 的 animationResolver 使用。tile 名查不到数据时返回 null（不动画）。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <returns>该格资源的动画帧前缀；无 tile、未开启 IsAnimation 或恒底层时返回 null。</returns>
        private string GetResourceAnimationPrefix(Vector3Int cell)
        {
            TileBase tile = this.GetTile(cell);
            if (tile == null)
            {
                return null;
            }

            ItemData item = Core.ServiceLocator.Get<ItemDataManager>().GetByName(tile.name);
            return item != null && item.IsAnimation && item.LayerMode != ItemLayerMode.Bottom ? item.Name : null;
        }

        /// <summary>
        /// 该格资源是否用 shader 摇摆材质（ItemData.IsSway 开启且 LayerMode != Bottom，树资源通用）。
        /// 开启时独立 sprite 视觉换 Custom/Sprite-Lit-Sway 材质，GPU 顶点位移做摆动
        /// （静态 tile 图、不挂动画组件——大地图数千棵树无逐实例 Animator 每帧求值开销）。
        /// 供 TileVisualSpawner 的 swayResolver 使用。tile 名查不到数据时返回 false（不摆）。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <returns>该格资源是否摇摆渲染；无 tile、未开启 IsSway 或恒底层时返回 false。</returns>
        private bool GetResourceSway(Vector3Int cell)
        {
            TileBase tile = this.GetTile(cell);
            if (tile == null)
            {
                return false;
            }

            ItemData item = Core.ServiceLocator.Get<ItemDataManager>().GetByName(tile.name);
            return item != null && item.IsSway && item.LayerMode != ItemLayerMode.Bottom;
        }

        /// <summary>
        /// 尝试获取可采集资源的信息。
        /// </summary>
        /// <param name="posMap">资源位置</param>
        /// <param name="resourceInfo">资源信息</param>
        /// <returns>该位置是否有可采集资源</returns>
        public bool TryGetGatherResourceInfo(Vector3Int posMap, out ResourceInfo resourceInfo)
        {
            resourceInfo = null;
            TileBase tileBase = this.GetTile(posMap);
            if (tileBase == null)
            {
                return false;
            }

            if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(tileBase.name, out ItemData itemData))
            {
                // 资源存在于 ResourceMap 但未在 ItemDataManager 注册（如 DesertGrass），
                // 仍允许采集，使用 Id=0 走默认掉落
                itemData = ItemData.Empty;
            }

            resourceInfo = new ResourceInfo(itemData.Id);
            return true;
        }

        private bool isProgressSet;

        /// <summary>
        /// 设置进度条（幂等，重复调用安全）
        /// </summary>
        public void SetProgress()
        {
            if (this.isProgressSet)
            {
                return;
            }

            if (!Core.ServiceLocator.TryGet(out TileMap tm) || tm.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMap data not initialized, cannot set resource generation progress", LogManager.LogLevelEnum.Error);
                return;
            }

            this.isProgressSet = true;
            // 总量已由 TileMap.SetProgress 统一分配，此处不再重复添加
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            Core.GameServices.AsyncProgressSetTipProvider("加载资源地图信息...");
            this.ResourceMapDataLAB = DataTool.LoadDataByBinary<ResourceMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            WalkabilityCache.Invalidate();
            if (this.ResourceMapDataLAB == null)
            {
                // 降级方案：存档无资源数据时，等待地图生成完毕后自动生成资源
                AWorkerTask.LogProvider("ResourceMap data not found in archive, will generate new resources", LogManager.LogLevelEnum.Warning);
                this.ResourceMapDataLAB = new ResourceMapData(0, 100);
                this.SetProgress();
                this.StartCoroutine(this.GenResource());
                return;
            }

            foreach (KeyValuePair<Vector3IntLAB, string> posMap in this.ResourceMapDataLAB.PosMap)
            {
                Vector3Int position = Vector3IntLAB.ToVector3Int(posMap.Key);
                this.SetTile(position, (TileBase)AWorkerTask.ResourceLoadProvider(posMap.Value));
                WalkabilityCache.UpdateCell(position);
            }

            // 读档完成：按恢复的 tile 状态重建树视觉（清理残留 cell）
            this.visuals?.RebuildAll();

            this.StartCoroutine(this.GenTree());
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.ResourceMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            AWorkerTask.LogProvider("Request: 同步地图资源数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.ResourceMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            AWorkerTask.LogProvider("Response: 同步地图资源数据", LogManager.LogLevelEnum.Trace);
            this.SetProgress();
            ResourceMapData resourceMapData = DataTool.FromByteArray<ResourceMapData>(data);
            this.ResourceMapDataLAB = resourceMapData;
            WalkabilityCache.Invalidate();
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = resourceMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator.Current.Key),
                    (TileBase)AWorkerTask.ResourceLoadProvider(enumerator.Current.Value));
                WalkabilityCache.UpdateCell(Vector3IntLAB.ToVector3Int(enumerator.Current.Key));
            }

            // 全量同步完成：按新数据重建树视觉（清理旧数据残留的 cell）
            this.visuals?.RebuildAll();
        }

        /// <summary>
        /// 同步地图资源数据
        /// </summary>
        /// <param name="vector3IntLAB">位置</param>
        /// <param name="tileBaseName">瓦片名称</param>
        /// <param name="isPass">是否可以通过</param>
        /// <param name="isDelete">是否删除</param>
        [PunRPC]
        public void SyncDataResp(byte[] vector3IntLAB, string tileBaseName, bool isPass = false, bool isDelete = false)
        {
            AWorkerTask.LogProvider("Response: 同步地图资源数据", LogManager.LogLevelEnum.Trace);
            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.SetTile(vector3Int, null);
                this.ResourceMapDataLAB.Remove(vector3Int);
                WalkabilityCache.UpdateCell(vector3Int);
                return;
            }

            if (!tileBaseName.Equals(string.Empty))
            {
                this.SetTile(vector3Int, (TileBase)AWorkerTask.ResourceLoadProvider(tileBaseName));
                this.ResourceMapDataLAB.PosMap[Vector3IntLAB.ToVector3IntLAB(vector3Int)] = tileBaseName;
            }

            if (isPass)
            {
                this.tilemap.RemoveTileFlags(vector3Int, TileFlags.LockColor);
            }

            WalkabilityCache.UpdateCell(vector3Int);
        }

        /// <summary>
        /// 资源数据
        /// </summary>
        [Serializable]
        public class ResourceMapData : ATileMapData
        {
            /// <summary>
            /// 当前树的数量
            /// </summary>
            public int TreeCurCount;

            /// <summary>
            /// 树的总数
            /// </summary>
            public int TreeTotalCount;

            public ResourceMapData(int treeCurCount, int treeTotalCount)
            {
                this.TreeCurCount = treeCurCount;
                this.TreeTotalCount = treeTotalCount;
            }
        }
    }
}
