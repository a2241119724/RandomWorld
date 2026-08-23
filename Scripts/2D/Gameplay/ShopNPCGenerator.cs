namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Core.Seek;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 商店 NPC 生成器 — 地图就绪后在随机可到达位置生成商店。
    /// 参考 TaskBoardManager 的初始化模式，但分布到整个地图。
    /// </summary>
    public sealed class ShopNPCGenerator
    {
        /// <summary>默认生成商店数量</summary>
        public int ShopCount = 3;

        /// <summary>商店之间的最小间距（地图格子）</summary>
        private const int MinShopDistance = 40;

        /// <summary>位置搜索最大重试次数</summary>
        private const int MaxRetries = 500;

        /// <summary>已生成商店的位置列表</summary>
        private readonly List<Vector3Int> spawnedPositions = new List<Vector3Int>();

        /// <summary>已生成的 ShopNPC 实例</summary>
        public List<ShopNPC> SpawnedShops { get; } = new List<ShopNPC>();

        public ShopNPCGenerator()
        {
        }

        /// <summary>
        /// 订阅 MapInitCoordinator.OnMapReady，地图就绪后自动生成商店。
        /// 在 GlobalInit.Start() 中调用。
        /// </summary>
        public void SubscribeToMapReady()
        {
            Core.ServiceLocator.Get<MapInitCoordinator>().OnMapReady += this.OnMapReady;
        }

        private void OnMapReady()
        {
            this.SpawnAll();
        }

        /// <summary>
        /// 在地图上生成所有商店 NPC。
        /// </summary>
        public void SpawnAll()
        {
            TileMap tileMap = Core.ServiceLocator.Get<TileMap>();
            if (tileMap.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("[ShopNPCGenerator] TileMapDataLAB 未就绪，跳过商店生成", LogManager.LogLevelEnum.Error);
                return;
            }

            Transform tilemapParent = tileMap.gameObject.transform;

            // 将商店图标放到 All/Building 下
            Transform buildingParent = GameObject.Find("All/Building")?.transform;
            if (buildingParent == null)
            {
                buildingParent = new GameObject("Building").transform;
                GameObject all = GameObject.Find("All");
                if (all != null) buildingParent.SetParent(all.transform);
            }

            for (int i = 0; i < this.ShopCount; i++)
            {
                Vector3Int shopPos = this.FindValidShopPosition(tileMap);
                if (shopPos == default && i == 0)
                {
                    // 至少在地图中心放一个
                    shopPos = new Vector3Int(
                        tileMap.TileMapDataLAB.Height / 2,
                        tileMap.TileMapDataLAB.Width / 2,
                        0);
                }

                Vector3 worldPos = tileMap.MapPosToWorldPos(shopPos) + tilemapParent.position;
                ShopNPC shop = this.CreateShopAt(shopPos, worldPos, buildingParent, i);
                if (shop != null)
                {
                    this.spawnedPositions.Add(shopPos);
                    this.SpawnedShops.Add(shop);
                }
            }

            AWorkerTask.LogProvider(
                $"[ShopNPCGenerator] 已生成 {this.SpawnedShops.Count}/{this.ShopCount} 个商店",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 查找一个有效的商店位置：可到达、不与已有商店过近。
        /// </summary>
        private Vector3Int FindValidShopPosition(TileMap tileMap)
        {
            TerrainConfigDatabase terrainDb = Core.ServiceLocator.Get<TerrainConfigDatabase>();
            BuildMap buildMap = Core.ServiceLocator.Get<BuildMap>();
            ResourceMap resourceMap = Core.ServiceLocator.Get<ResourceMap>();

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                Vector3Int pos = tileMap.GenCanReachPos(default);
                if (pos == default) continue;

                // 地形必须可行走（排除山、水等）
                int terrainId = tileMap.TileMapDataLAB.MapTiles[pos.x, pos.y];
                if (!terrainDb.IsWalkable(terrainId)) continue;

                // 检查各层可达性
                if (!buildMap.IsCanReach(pos)) continue;
                if (!resourceMap.IsCanReach(pos)) continue;

                // 检查与已有商店的间距
                bool tooClose = false;
                foreach (Vector3Int existing in this.spawnedPositions)
                {
                    if (Vector3Int.Distance(pos, existing) < MinShopDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return pos;
            }

            return default;
        }

        /// <summary>Shop 预制体 / Tile 资源名称</summary>
        private const string ShopAssetName = PrefabConstant.SHOP;

        /// <summary>
        /// 创建商店 NPC：Tilemap 放图标 + 预制体挂组件。
        /// </summary>
        private ShopNPC CreateShopAt(Vector3Int shopPos, Vector3 worldPos, Transform parent, int index)
        {
            ShopPreset preset = GetRandomPreset();

            // DirectBuild：图标不可通行，Worker/Enemy 不会穿过商店
            TileBase tile = (TileBase)AWorkerTask.ResourceLoadProvider(ShopAssetName);
            if (tile != null)
            {
                BuildMap buildMap = Core.ServiceLocator.Get<BuildMap>();
                var posLAB = Vector3IntLAB.ToVector3IntLAB(shopPos);
                // 先登记 PosMap 再建视觉：DirectBuild 内部 visuals.CreateOrUpdate 会读 GetBuildLayerMode，
                // 若 PosMap 无 Shop 条目会回退 ItemLayerMode.Alpha，导致商店以"可淡化"模式注册进
                // OcclusionFader，与 BuildItemData.LayerMode=Normal（不淡化）相悖（生成顺序 bug）。
                if (!buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
                {
                    buildMap.BuildMapDataLAB.PosMap[posLAB] = new BuildMap.BuildTileData(ShopAssetName, true);
                }

                buildMap.DirectBuild(shopPos, tile, true);
                WalkabilityCache.UpdateCell(shopPos);
            }

            // 从预制体实例化（挂 ShopNPC 组件 + Collider2D）
            GameObject go = Core.ServiceLocator.Get<ResourceManager>().Instantiate(
                ShopAssetName, worldPos, Quaternion.identity, parent, false, true);
            if (go == null)
            {
                AWorkerTask.LogProvider($"[ShopNPCGenerator] 预制体 '{ShopAssetName}' 加载失败", LogManager.LogLevelEnum.Error);
                return null;
            }

            go.name = $"Shop_{preset.Name}";

            ShopNPC npc = go.GetComponent<ShopNPC>();
            if (npc == null)
            {
                AWorkerTask.LogProvider("[ShopNPCGenerator] Shop 预制体上未找到 ShopNPC 组件", LogManager.LogLevelEnum.Error);
                return null;
            }

            npc.ShopName = preset.Name;
            npc.InteractionRadius = 3f;
            npc.BuybackRate = Random.Range(0.5f, 0.8f);
            npc.ItemsForSale = this.GenerateShopItems(preset);

            return npc;
        }

        /// <summary>
        /// 为商店随机生成商品列表。
        /// </summary>
        private List<ShopNPC.ShopItem> GenerateShopItems(ShopPreset preset)
        {
            List<ShopNPC.ShopItem> items = new List<ShopNPC.ShopItem>();
            int itemCount = Random.Range(3, Mathf.Min(preset.PossibleItems.Length + 1, 7));

            // 打乱可能商品列表来随机抽取
            List<ItemTemplate> pool = new List<ItemTemplate>(preset.PossibleItems);
            Shuffle(pool);

            for (int i = 0; i < itemCount && i < pool.Count; i++)
            {
                ItemTemplate template = pool[i];
                int price = Mathf.Max(1, template.BasePrice + Random.Range(-template.BasePrice / 3, template.BasePrice / 3));
                items.Add(new ShopNPC.ShopItem
                {
                    ItemId = template.ItemId,
                    Price = price,
                    Stock = template.MaxStock < 0 ? -1 : Random.Range(1, template.MaxStock + 1),
                    CnName = template.CnName,
                });
            }

            return items;
        }

        private static ShopPreset GetRandomPreset()
        {
            ShopPreset[] presets = new[]
            {
                new ShopPreset
                {
                    Name = "食品店",
                    PossibleItems = new[]
                    {
                        new ItemTemplate { ItemId = 1, BasePrice = 5, MaxStock = 10, CnName = "苹果" },
                        new ItemTemplate { ItemId = 2, BasePrice = 10, MaxStock = 5, CnName = "肉" },
                        new ItemTemplate { ItemId = 3, BasePrice = 4, MaxStock = 15, CnName = "面包" },
                        new ItemTemplate { ItemId = 4, BasePrice = 3, MaxStock = 20, CnName = "浆果" },
                    },
                },
                new ShopPreset
                {
                    Name = "建材店",
                    PossibleItems = new[]
                    {
                        new ItemTemplate { ItemId = 10, BasePrice = 3, MaxStock = 30, CnName = "木材" },
                        new ItemTemplate { ItemId = 11, BasePrice = 3, MaxStock = 30, CnName = "石材" },
                        new ItemTemplate { ItemId = 12, BasePrice = 6, MaxStock = 15, CnName = "硬木" },
                        new ItemTemplate { ItemId = 13, BasePrice = 8, MaxStock = 10, CnName = "铁锭" },
                    },
                },
                new ShopPreset
                {
                    Name = "杂货铺",
                    PossibleItems = new[]
                    {
                        new ItemTemplate { ItemId = 1, BasePrice = 6, MaxStock = 10, CnName = "苹果" },
                        new ItemTemplate { ItemId = 10, BasePrice = 4, MaxStock = 20, CnName = "木材" },
                        new ItemTemplate { ItemId = 11, BasePrice = 4, MaxStock = 20, CnName = "石材" },
                        new ItemTemplate { ItemId = 20, BasePrice = 15, MaxStock = 3, CnName = "药水" },
                        new ItemTemplate { ItemId = 21, BasePrice = 8, MaxStock = 8, CnName = "绳子" },
                    },
                },
                new ShopPreset
                {
                    Name = "工具店",
                    PossibleItems = new[]
                    {
                        new ItemTemplate { ItemId = 30, BasePrice = 25, MaxStock = 3, CnName = "铁剑" },
                        new ItemTemplate { ItemId = 31, BasePrice = 20, MaxStock = 3, CnName = "铁斧" },
                        new ItemTemplate { ItemId = 32, BasePrice = 20, MaxStock = 3, CnName = "铁镐" },
                        new ItemTemplate { ItemId = 33, BasePrice = 12, MaxStock = 5, CnName = "火把" },
                    },
                },
            };

            return presets[Random.Range(0, presets.Length)];
        }

        /// <summary>Fisher–Yates 洗牌</summary>
        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>商店预设</summary>
        private struct ShopPreset
        {
            public string Name;
            public ItemTemplate[] PossibleItems;
        }

        /// <summary>商品模板</summary>
        private struct ItemTemplate
        {
            public int ItemId;
            public int BasePrice;
            public int MaxStock;    // -1 = 无限
            public string CnName;
        }
    }
}
