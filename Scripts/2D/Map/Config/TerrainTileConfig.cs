namespace LAB2D.Map
{
    using UnityEngine;

    /// <summary>
    /// 地形瓦片可配置属性 — 数据驱动的地形定义。
    /// 添加新地形：Create → SO → TerrainTileConfig，填写属性即可，无需修改任何 .cs 代码。
    ///
    /// 每个资产代表一种地形类型。terrainId 在存档/网络同步中使用；
    /// tileResourceName 对应通过 ResourceLoadProvider 加载的 Unity Tile 资源名称。
    /// </summary>
    [CreateAssetMenu(menuName = "SO/TerrainTileConfig")]
    public class TerrainTileConfig : ScriptableObject
    {
        /// <summary>
        /// 唯一地形 ID，用于存档/网络同步。自增即可。
        /// </summary>
        [Tooltip("唯一地形 ID（存档/网络同步用）。")]
        public int terrainId;

        /// <summary>
        /// Unity Tile 资源名，对应 ResourceLoadProvider / ResourceManager.GetAsset() 参数。
        /// </summary>
        [Tooltip("Unity Tile 资源名，与 Resources/Tilemap/ 下的 .asset 文件名一致。")]
        public string tileResourceName;

        /// <summary>
        /// 随机生成时的权重。值越大，该地形越容易出现。
        /// 设 0 表示该地形不参与随机生成（仅手动放置或通过生成器特定逻辑放置）。
        /// </summary>
        [Tooltip("随机生成权重（0=不参与随机散布）。")]
        [Range(0f, 10f)]
        public float spawnWeight = 1f;

        /// <summary>
        /// 角色/Worker 是否可以在此地形上行走（即 tile collider type 为 None）。
        /// </summary>
        [Tooltip("是否可以行走（无碰撞体）。")]
        public bool isWalkable = true;

        /// <summary>
        /// 是否可以在此地形上建造建筑。
        /// </summary>
        [Tooltip("是否可以在此地形上建造。")]
        public bool canBuild = true;

        /// <summary>
        /// 是否可以在此地形上生成树木。
        /// </summary>
        [Tooltip("是否可以生成树木。")]
        public bool canGrowTrees = true;

        /// <summary>
        /// 是否可以在此地形上生成资源（灌木、石头等）。
        /// </summary>
        [Tooltip("是否可以生成资源（灌木/石头等）。")]
        public bool canSpawnResources = true;

        /// <summary>
        /// 是否用作地图边界（CreateArroundTile 使用此地形包裹地图四周）。
        /// 每种地图配置中只能有一个地形的 isBorder 为 true。
        /// </summary>
        [Tooltip("是否用作地图边界（包围地图四周）。每张地图只有一个边界地形。")]
        public bool isBorder;

        /// <summary>
        /// 是否为水域地形（海洋/湖泊）。水域格不可行走、不可建造，在噪声岛屿生成中用作海洋。
        /// </summary>
        [Tooltip("是否为水域地形（海洋/湖泊）。")]
        public bool isWater;

        /// <summary>
        /// 是否可以被 Worker 挖掘（山脉、岩石等阻挡地形）。
        /// 挖掘后该格替换为周围可通行地形中最多的那种。
        /// </summary>
        [Tooltip("是否可以被 Worker 挖掘（山/岩石）。")]
        public bool isDiggable;

        /// <summary>
        /// 地形玩法效果配置（移速、疲劳消耗等）。
        /// </summary>
        [Tooltip("地形玩法效果（移速、疲劳消耗等）。")]
        public TerrainEffectData effectData = new TerrainEffectData();
    }

    /// <summary>
    /// 地形效果数据 — 各地形对角色移速和工人疲劳/饥饿消耗的影响。
    /// 所有倍率默认 1.0（无影响），在 Inspector 中按需调整。
    /// </summary>
    [System.Serializable]
    public class TerrainEffectData
    {
        /// <summary>
        /// 玩家在此地形上的移动速度倍率（1.0 = 无变化）。
        /// </summary>
        [Tooltip("玩家在此地形上的移动速度倍率。")]
        [UnityEngine.Range(0.1f, 3.0f)]
        public float playerMoveSpeedMultiplier = 1.0f;

        /// <summary>
        /// 工人在此地形的移动速度倍率。
        /// </summary>
        [Tooltip("工人在此地形的移动速度倍率。")]
        [UnityEngine.Range(0.1f, 3.0f)]
        public float workerMoveSpeedMultiplier = 1.0f;

        /// <summary>
        /// 敌人在此地形的移动速度倍率。
        /// </summary>
        [Tooltip("敌人在此地形的移动速度倍率。")]
        [UnityEngine.Range(0.1f, 3.0f)]
        public float enemyMoveSpeedMultiplier = 1.0f;

        /// <summary>
        /// 工人疲劳自然衰减速率倍率（1.0 = 默认 0.015/s），>1 表示更快疲劳。
        /// </summary>
        [Tooltip("工人疲劳自然衰减速率倍率（>1 = 更快疲劳）。")]
        [UnityEngine.Range(0.5f, 3.0f)]
        public float workerTiredDecayMultiplier = 1.0f;

        /// <summary>
        /// 工人饥饿自然衰减速率倍率（1.0 = 默认 0.025/s），>1 表示更快饥饿。
        /// </summary>
        [Tooltip("工人饥饿自然衰减速率倍率（>1 = 更快饥饿）。")]
        [UnityEngine.Range(0.5f, 3.0f)]
        public float workerHungryDecayMultiplier = 1.0f;
    }
}
