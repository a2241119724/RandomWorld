namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Core;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 地形配置注册表 — 从 Resources/SO/TerrainConfigs/ 加载 TerrainTileConfig 资产。
    ///
    /// 不包含任何内置默认值：所有地形都必须以 .asset 文件形式提供。
    ///
    /// 用法：
    ///   var db = ServiceLocator.Get&lt;TerrainConfigDatabase&gt;();
    ///   var config = db.GetById(tileId);
    ///   if (db.IsWalkable(tileId)) { ... }
    /// </summary>
    public class TerrainConfigDatabase
    {
        private const string CONFIG_PATH = "SO/TerrainConfigs/";

        private readonly Dictionary<int, TerrainTileConfig> configById = new Dictionary<int, TerrainTileConfig>();

        private List<int> spawnableIds;
        private List<float> spawnWeights;
        private float totalSpawnWeight;

        public TerrainConfigDatabase()
        {
            this.Load();
            this.RebuildSpawnList();
        }

        /// <summary>
        /// 通过 terrainId 获取配置。未找到返回 null。
        /// </summary>
        public TerrainTileConfig GetById(int terrainId)
        {
            this.configById.TryGetValue(terrainId, out TerrainTileConfig config);
            return config;
        }

        /// <summary>
        /// 通过 terrainId 获取 Tile 资源名。未找到返回 null。
        /// </summary>
        public string GetTileResourceName(int terrainId)
        {
            TerrainTileConfig config = this.GetById(terrainId);
            if (config == null || string.IsNullOrEmpty(config.tileResourceName))
            {
                return null;
            }

            return config.tileResourceName;
        }

        /// <summary>
        /// 检查指定地形是否可行走。
        /// </summary>
        public bool IsWalkable(int terrainId)
        {
            return this.GetById(terrainId)?.isWalkable ?? false;
        }

        /// <summary>
        /// 检查指定地形是否可以建造。
        /// </summary>
        public bool CanBuild(int terrainId)
        {
            return this.GetById(terrainId)?.canBuild ?? false;
        }

        /// <summary>
        /// 检查指定地形是否可以生成树木。
        /// </summary>
        public bool CanGrowTrees(int terrainId)
        {
            return this.GetById(terrainId)?.canGrowTrees ?? false;
        }

        /// <summary>
        /// 检查指定地形是否可以生成资源。
        /// </summary>
        public bool CanSpawnResources(int terrainId)
        {
            return this.GetById(terrainId)?.canSpawnResources ?? false;
        }

        /// <summary>
        /// 获取一个随机地形 ID（按 spawnWeight 加权）。
        /// </summary>
        public int GetRandomWeighted()
        {
            if (this.totalSpawnWeight <= 0f || this.spawnableIds.Count == 0)
            {
                AWorkerTask.LogProvider(
                    "TerrainConfigDatabase: 没有可生成的地形配置，请检查 Resources/SO/TerrainConfigs/",
                    LogManager.LogLevelEnum.Error);
                return 0;
            }

            float roll = UnityEngine.Random.Range(0f, this.totalSpawnWeight);
            float cumulative = 0f;
            for (int i = 0; i < this.spawnableIds.Count; i++)
            {
                cumulative += this.spawnWeights[i];
                if (roll <= cumulative)
                {
                    return this.spawnableIds[i];
                }
            }

            return this.spawnableIds[this.spawnableIds.Count - 1];
        }

        /// <summary>
        /// 获取标记为 isBorder 的地形 ID（用于 CreateArroundTile）。
        /// 如果多个地形标记了 isBorder，返回第一个找到的。
        /// </summary>
        public int GetBorderTerrainId()
        {
            foreach (var kvp in this.configById)
            {
                if (kvp.Value.isBorder)
                {
                    return kvp.Key;
                }
            }

            AWorkerTask.LogProvider(
                "TerrainConfigDatabase: 没有地形标记 isBorder，地图边界将不会生成。",
                LogManager.LogLevelEnum.Error);
            return 0;
        }

        /// <summary>
        /// 获取指定地形 ID 的效果数据。未找到返回 null。
        /// </summary>
        public TerrainEffectData GetEffectData(int terrainId)
        {
            return this.GetById(terrainId)?.effectData;
        }

        /// <summary>
        /// 安全获取玩家移速倍率（默认 1.0）。
        /// </summary>
        public float GetPlayerMoveSpeedMultiplier(int terrainId)
        {
            return this.GetById(terrainId)?.effectData?.playerMoveSpeedMultiplier ?? 1.0f;
        }

        /// <summary>
        /// 安全获取工人移速倍率（默认 1.0）。
        /// </summary>
        public float GetWorkerMoveSpeedMultiplier(int terrainId)
        {
            return this.GetById(terrainId)?.effectData?.workerMoveSpeedMultiplier ?? 1.0f;
        }

        /// <summary>
        /// 安全获取敌人移速倍率（默认 1.0）。
        /// </summary>
        public float GetEnemyMoveSpeedMultiplier(int terrainId)
        {
            return this.GetById(terrainId)?.effectData?.enemyMoveSpeedMultiplier ?? 1.0f;
        }

        /// <summary>
        /// 安全获取工人疲劳衰减倍率（默认 1.0）。
        /// </summary>
        public float GetWorkerTiredDecayMultiplier(int terrainId)
        {
            return this.GetById(terrainId)?.effectData?.workerTiredDecayMultiplier ?? 1.0f;
        }

        /// <summary>
        /// 安全获取工人饥饿衰减倍率（默认 1.0）。
        /// </summary>
        public float GetWorkerHungryDecayMultiplier(int terrainId)
        {
            return this.GetById(terrainId)?.effectData?.workerHungryDecayMultiplier ?? 1.0f;
        }

        /// <summary>
        /// 获取水域地形 ID。遍历配置查找首个 isWater==true 的地形，未找到回退为 6。
        /// </summary>
        public int GetWaterTerrainId()
        {
            foreach (var kvp in this.configById)
            {
                if (kvp.Value.isWater)
                {
                    return kvp.Key;
                }
            }

            return 6; // fallback: 已知的 Water 地形 ID
        }

        /// <summary>
        /// 检查指定 terrainId 是否为水域地形。
        /// </summary>
        public bool IsWater(int terrainId)
        {
            TerrainTileConfig config = this.GetById(terrainId);
            return config != null && config.isWater;
        }

        /// <summary>
        /// 检查指定 terrainId 是否为可挖掘地形（如山）。
        /// </summary>
        public bool IsDiggable(int terrainId)
        {
            TerrainTileConfig config = this.GetById(terrainId);
            return config != null && config.isDiggable;
        }

        public IReadOnlyList<int> SpawnableIds => this.spawnableIds.AsReadOnly();
        public int Count => this.configById.Count;

        private void Load()
        {
            TerrainTileConfig[] configs = Resources.LoadAll<TerrainTileConfig>(CONFIG_PATH);
            if (configs == null || configs.Length == 0)
            {
                AWorkerTask.LogProvider(
                    $"TerrainConfigDatabase: 未在 Resources/{CONFIG_PATH} 找到任何地形配置（.asset），地图无法生成。",
                    LogManager.LogLevelEnum.Error);
                return;
            }

            foreach (TerrainTileConfig config in configs)
            {
                if (config == null || config.terrainId <= 0)
                {
                    AWorkerTask.LogProvider(
                        $"TerrainConfigDatabase: 跳过无效配置 '{config?.name}'（terrainId 必须 > 0）。",
                        LogManager.LogLevelEnum.Warning);
                    continue;
                }

                if (this.configById.ContainsKey(config.terrainId))
                {
                    AWorkerTask.LogProvider(
                        $"TerrainConfigDatabase: terrainId {config.terrainId} 重复，配置 '{config.name}' 覆盖了已有条目。",
                        LogManager.LogLevelEnum.Warning);
                }

                this.configById[config.terrainId] = config;
            }

            AWorkerTask.LogProvider(
                $"TerrainConfigDatabase: 加载了 {this.configById.Count} 个地形配置。",
                LogManager.LogLevelEnum.Trace);
        }

        private void RebuildSpawnList()
        {
            this.spawnableIds = new List<int>();
            this.spawnWeights = new List<float>();
            this.totalSpawnWeight = 0f;

            foreach (var kvp in this.configById)
            {
                if (kvp.Value.spawnWeight > 0f)
                {
                    this.spawnableIds.Add(kvp.Key);
                    this.spawnWeights.Add(kvp.Value.spawnWeight);
                    this.totalSpawnWeight += kvp.Value.spawnWeight;
                }
            }
        }
    }
}
