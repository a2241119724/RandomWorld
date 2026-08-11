namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Data;
    using LAB2D.UI.Action;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 农作物管理（ASingletonSaveData）。
    /// </summary>
    public class FarmlandManager : ASingletonSaveData<FarmlandManager>
    {
        private readonly Dictionary<int, Dictionary<Vector3Int, PlantInfo>> id2Resource; // 同一个id对应的所有位置
        private readonly Dictionary<Vector3Int, PlantInfo> cells;
        private readonly Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>> preGatherResource; // 预采集资源
        private readonly Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>> prePlantResource; // 预种植资源
        private readonly Dictionary<Vector3Int, PlantGrowthBar> growthBars; // 生长进度条

        /// <summary>
        /// 植物生长总时长（秒）
        /// </summary>
        private const float PlantGrowthDuration = 30f;

        public FarmlandManager()
        {
            this.cells = new Dictionary<Vector3Int, PlantInfo>();
            this.id2Resource = new Dictionary<int, Dictionary<Vector3Int, PlantInfo>>();
            this.preGatherResource = new Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>>();
            this.prePlantResource = new Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>>();
            this.growthBars = new Dictionary<Vector3Int, PlantGrowthBar>();
        }

        /// <summary>
        /// 添加整个建筑物的所有墙
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="width">宽度</param>
        /// <param name="length">高度</param>
        public void AddCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            if (!this.id2Resource.ContainsKey(-1))
            {
                this.id2Resource.Add(-1, new Dictionary<Vector3Int, PlantInfo>());
            }

            Dictionary<Vector3Int, PlantInfo> resources = this.id2Resource[-1];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = VectorTool.Add(startPos, i, j);
                    this.cells.Add(pos, new PlantInfo(-1, 0, 0));
                    resources.Add(pos, new PlantInfo(-1, 0, 0));
                }
            }
        }

        /// <summary>
        /// 获取没有植物的土壤位置
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="resourceInfo">资源信息</param>
        /// <param name="isPre">是否需要预建造</param>
        /// <returns>位置</returns>
        public Vector3Int IsEnoughAndPrePlant(AWorker worker, ResourceInfo resourceInfo, bool isPre = false)
        {
            if (!this.id2Resource.ContainsKey(-1) || this.id2Resource[-1].Count == 0)
            {
                return default;
            }

            Dictionary<Vector3Int, PlantInfo>.KeyCollection.Enumerator enumerator = this.id2Resource[-1].Keys.GetEnumerator();
            enumerator.MoveNext();
            if (isPre)
            {
                if (!this.prePlantResource.ContainsKey(worker))
                {
                    this.prePlantResource.Add(worker, new Dictionary<Vector3Int, PlantInfo>());
                }

                this.prePlantResource[worker].Add(enumerator.Current, new PlantInfo(resourceInfo.Id, 1, 0));
            }

            return enumerator.Current;
        }

        /// <summary>
        /// 根据预种植进行种植
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        public void PlantByPrePlant(AWorker worker, Vector3Int posMap)
        {
            if (this.prePlantResource.ContainsKey(worker) && this.prePlantResource[worker].ContainsKey(posMap))
            {
                PlantInfo plantInfo = this.prePlantResource[worker][posMap];
                if (!this.id2Resource.ContainsKey(plantInfo.Id))
                {
                    this.id2Resource.Add(plantInfo.Id, new Dictionary<Vector3Int, PlantInfo>());
                }

                this.id2Resource[plantInfo.Id].Add(posMap, plantInfo);
                this.id2Resource[-1].Remove(posMap);
                this.cells[posMap] = plantInfo;
                this.prePlantResource[worker].Remove(posMap);
                Core.ServiceLocator.Get<ResourceMap>().SetTile(posMap, Core.ServiceLocator.Get<ResourceManager>().GetAsset(
                    Core.ServiceLocator.Get<ItemDataManager>().GetById(plantInfo.Id).EnName));

                // 创建生长进度条
                this.CreateGrowthBar(posMap, PlantGrowthDuration);
            }
        }

        /// <summary>
        /// 预采集
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="plantInfo">农作物信息</param>
        /// <param name="isPre">是否预采集</param>
        /// <returns>位置</returns>
        public Vector3Int PreGather(AWorker worker, PlantInfo plantInfo, bool isPre = false)
        {
            if (this.id2Resource[plantInfo.Id].Count == 0)
            {
                return default;
            }

            Dictionary<Vector3Int, PlantInfo>.KeyCollection.Enumerator enumerator = this.id2Resource[plantInfo.Id].Keys.GetEnumerator();
            enumerator.MoveNext();
            if (!this.preGatherResource.ContainsKey(worker))
            {
                this.preGatherResource.Add(worker, new Dictionary<Vector3Int, PlantInfo>());
            }

            this.preGatherResource[worker].Add(enumerator.Current, plantInfo);
            return enumerator.Current;
        }

        /// <summary>
        /// 根据预采集进行采集
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        public void GatherByPreGather(AWorker worker, Vector3Int posMap)
        {
            if (this.preGatherResource.ContainsKey(worker) && this.preGatherResource[worker].ContainsKey(posMap))
            {
                PlantInfo plantInfo = this.preGatherResource[worker][posMap];
                this.id2Resource[plantInfo.Id].Remove(posMap);
                this.id2Resource[-1].Add(posMap, plantInfo);
                this.cells[posMap] = plantInfo;
                this.preGatherResource[worker].Remove(posMap);

                // 销毁生长进度条
                this.DestroyGrowthBar(posMap);
            }
        }

        /// <summary>
        /// 判断指定位置是否为农田
        /// </summary>
        /// <param name="posMap">地图坐标</param>
        /// <returns>是否为农田</returns>
        public bool IsFarmland(Vector3Int posMap)
        {
            return this.cells.ContainsKey(posMap);
        }

        /// <summary>
        /// 判断指定位置是否为空闲农田（可种植）
        /// </summary>
        /// <param name="posMap">地图坐标</param>
        /// <returns>是否为空闲农田</returns>
        public bool IsEmptySoil(Vector3Int posMap)
        {
            return this.cells.TryGetValue(posMap, out PlantInfo info) && info.Id == -1;
        }

        /// <summary>
        /// 直接在指定位置种植（无需预种植步骤）
        /// </summary>
        /// <param name="posMap">地图坐标</param>
        /// <param name="plantId">种植物品ID</param>
        /// <param name="count">种植数量</param>
        public void Plant(Vector3Int posMap, int plantId, int count = 1)
        {
            if (!this.IsEmptySoil(posMap))
            {
                return;
            }

            PlantInfo plantInfo = new PlantInfo(plantId, count, 0);

            if (!this.id2Resource.ContainsKey(plantId))
            {
                this.id2Resource.Add(plantId, new Dictionary<Vector3Int, PlantInfo>());
            }

            this.id2Resource[-1].Remove(posMap);
            this.id2Resource[plantId].Add(posMap, plantInfo);
            this.cells[posMap] = plantInfo;

            Core.ServiceLocator.Get<ResourceMap>().SetTile(posMap,
                Core.ServiceLocator.Get<ResourceManager>().GetAsset(
                    Core.ServiceLocator.Get<ItemDataManager>().GetById(plantId).EnName));

            // 创建生长进度条
            this.CreateGrowthBar(posMap, PlantGrowthDuration);
        }

        /// <summary>
        /// 销毁指定位置的生长进度条。
        /// </summary>
        public void DestroyGrowthBar(Vector3Int posMap)
        {
            if (this.growthBars.TryGetValue(posMap, out PlantGrowthBar bar))
            {
                if (bar != null)
                {
                    UnityEngine.Object.Destroy(bar.gameObject);
                }

                this.growthBars.Remove(posMap);
            }
        }

        /// <summary>
        /// 创建生长进度条。
        /// </summary>
        private void CreateGrowthBar(Vector3Int posMap, float duration)
        {
            this.DestroyGrowthBar(posMap);

            GameObject barObj = Core.ServiceLocator.Get<ResourceManager>().Instantiate(
                PrefabConstant.PLANT_GROWTH_BAR);
            if (barObj == null)
            {
                return;
            }

            PlantGrowthBar growthBar = barObj.GetComponent<PlantGrowthBar>();
            if (growthBar != null)
            {
                Vector3 worldPos = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap)
                    + new Vector3(0.5f, 0.5f, -5f);
                barObj.transform.position = worldPos;

                GameObject actionUI = GameObject.FindGameObjectWithTag(TagConstant.ACTION_UI_TAG);
                if (actionUI != null)
                {
                    barObj.transform.SetParent(actionUI.transform);
                }

                growthBar.SetMapPos(posMap);
                growthBar.SetDuration(duration);
                this.growthBars[posMap] = growthBar;
            }
        }

        /// <summary>
        /// 农作物信息
        /// </summary>
        public class PlantInfo
        {
            /// <summary>
            /// ID
            /// </summary>
            public int Id;

            /// <summary>
            /// 数量
            /// </summary>
            public int Count;

            /// <summary>
            /// 生长时间
            /// </summary>
            public int Time;

            public PlantInfo(int id, int count, int time)
            {
                this.Id = id;
                this.Count = count;
                this.Time = time;
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            FarmlandManagerData data = new FarmlandManagerData();

            foreach (KeyValuePair<Vector3Int, PlantInfo> kv in this.cells)
            {
                PlantInfo info = kv.Value;
                // 只保存有种植物的格子（Id >= 0），空地由 AddCells 重建
                if (info.Id < 0)
                {
                    continue;
                }

                float growthElapsed = 0f;
                if (this.growthBars.TryGetValue(kv.Key, out PlantGrowthBar bar) && bar != null)
                {
                    growthElapsed = bar.Elapsed;
                }

                data.Cells.Add(new FarmlandCellEntry
                {
                    PosX = kv.Key.x,
                    PosY = kv.Key.y,
                    PosZ = kv.Key.z,
                    PlantId = info.Id,
                    Count = info.Count,
                    GrowthElapsed = growthElapsed,
                });
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            FarmlandManagerData data = DataTool.LoadDataByBinary<FarmlandManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null || data.Cells == null || data.Cells.Count == 0)
            {
                return;
            }

            foreach (FarmlandCellEntry entry in data.Cells)
            {
                Vector3Int pos = new Vector3Int(entry.PosX, entry.PosY, entry.PosZ);

                // 如果该位置尚未通过 AddCells 创建，则先创建
                if (!this.cells.ContainsKey(pos))
                {
                    if (!this.id2Resource.ContainsKey(-1))
                    {
                        this.id2Resource[-1] = new Dictionary<Vector3Int, PlantInfo>();
                    }

                    this.cells[pos] = new PlantInfo(-1, 0, 0);
                    this.id2Resource[-1][pos] = new PlantInfo(-1, 0, 0);
                }

                // 恢复植物数据
                PlantInfo plantInfo = new PlantInfo(entry.PlantId, entry.Count, 0);
                if (!this.id2Resource.ContainsKey(entry.PlantId))
                {
                    this.id2Resource[entry.PlantId] = new Dictionary<Vector3Int, PlantInfo>();
                }

                // 从空地索引移除
                if (this.id2Resource.ContainsKey(-1) && this.id2Resource[-1].ContainsKey(pos))
                {
                    this.id2Resource[-1].Remove(pos);
                }

                this.id2Resource[entry.PlantId][pos] = plantInfo;
                this.cells[pos] = plantInfo;

                // 恢复生长进度条
                if (entry.GrowthElapsed < PlantGrowthDuration)
                {
                    this.CreateGrowthBar(pos, PlantGrowthDuration);
                    if (this.growthBars.TryGetValue(pos, out PlantGrowthBar bar) && bar != null)
                    {
                        bar.SetElapsed(entry.GrowthElapsed);
                    }
                }
            }
        }

        [Serializable]
        public class FarmlandManagerData
        {
            public List<FarmlandCellEntry> Cells = new List<FarmlandCellEntry>();
        }

        [Serializable]
        public class FarmlandCellEntry
        {
            public int PosX;
            public int PosY;
            public int PosZ;
            public int PlantId;
            public int Count;
            public float GrowthElapsed;
        }
    }
}
