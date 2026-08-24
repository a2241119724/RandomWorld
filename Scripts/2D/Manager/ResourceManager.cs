namespace LAB2D.Manager
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Core;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Map;
    using LAB2D.SO;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 资源管理.
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>
    {
        /// <summary>
        /// 整数随机数提供者（minInclusive, maxExclusive）。
        /// 默认实现封装 UnityEngine.Random.Range；可在测试中替换。
        /// </summary>
        internal static Func<int, int, int> RandomIntProvider { get; set; }
            = (minInclusive, maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive);
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());
        private readonly Dictionary<string, GameObject> prefabDic; // <characterType,<name,prefab>>
        private readonly Dictionary<string, UnityEngine.Object> assetDic;
        private readonly Dictionary<string, Sprite> imageDic;
        /// <summary>
        /// 地形 ID → 该地形上可生成的资源列表
        /// </summary>
        private readonly Dictionary<int, List<UnityEngine.Object>> tileDic;
        private readonly Dictionary<string, Shader> shaderDic;
        private readonly Dictionary<string, ItemDataSO> backpackDataDic;
        private readonly Dictionary<string, BuildItemDataSO> buildDataDic;
        private readonly Dictionary<string, DropItemDataSO> dropDataDic;

        /// <summary>
        /// 预制体字典是否已成功加载（非空即视为已加载）
        /// </summary>
        public bool IsPrefabLoaded => this.prefabDic.Count > 0;

        /// <summary>
        /// 资源字典是否已成功加载
        /// </summary>
        public bool IsAssetLoaded => this.assetDic.Count > 0;

        public ResourceManager()
        {
            this.prefabDic = ResourceTool.LoadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            this.assetDic = ResourceTool.LoadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
            this.tileDic = new Dictionary<int, List<UnityEngine.Object>>();
            this.shaderDic = ResourceTool.LoadResources<Shader>(ResourceConstant.SHADER_ROOT);

            // 通过 TerrainConfigDatabase 获取所有注册的地形配置，
            // 按 tileResourceName 前缀匹配 asset 名称来构建 tileDic。
            // 使用 TryGet 而非 Get：Editor 模式下 RestoreLastOpenedScenes 可能在
            // RegisterSafeServices 之前触发 ResourceManager 构造（如 RoundCorner 的
            // [ExecuteInEditMode] 回调），此时 TerrainConfigDatabase 尚未注册。
            if (ServiceLocator.TryGet<TerrainConfigDatabase>(out TerrainConfigDatabase terrainDb))
            {
                foreach (KeyValuePair<string, UnityEngine.Object> asset in this.assetDic)
                {
                    foreach (int terrainId in terrainDb.SpawnableIds)
                    {
                        TerrainTileConfig config = terrainDb.GetById(terrainId);
                        if (config == null || string.IsNullOrEmpty(config.tileResourceName))
                        {
                            continue;
                        }

                        // 不包含 Tile 本身，仅包含其上的资源（前缀匹配但不等同）
                        if (!asset.Key.StartsWith(config.tileResourceName) ||
                            asset.Key.Equals(config.tileResourceName))
                        {
                            continue;
                        }

                        if (!this.tileDic.ContainsKey(terrainId))
                        {
                            this.tileDic.Add(terrainId, new List<UnityEngine.Object>());
                        }

                        this.tileDic[terrainId].Add(asset.Value);
                        break;
                    }
                }
            }

            this.imageDic = ResourceTool.LoadResources<Sprite>(ResourceConstant.IMAGE_ROOT);
            this.backpackDataDic = ResourceTool.LoadResources<ItemDataSO>(ResourceConstant.SCRIPTABLE_ROOT);
            this.buildDataDic = ResourceTool.LoadResources<BuildItemDataSO>(ResourceConstant.SCRIPTABLE_ROOT);
            this.dropDataDic = ResourceTool.LoadResources<DropItemDataSO>(ResourceConstant.SCRIPTABLE_ROOT);
            this.LoadPrefabs();

            // 诊断日志：告知开发者资源加载状态
            if (this.prefabDic.Count == 0)
            {
                this.GameLogger.LogWarning(
                    "[ResourceManager] prefabDic is empty after initialization. " +
                    "Prefabs will NOT be available for Instantiate(). " +
                    "Ensure StreamingAssets/prefab AssetBundle exists and contains prefabs, " +
                    "or place prefabs under Resources/Prefabs/.");
            }
            else
            {
                this.GameLogger.Log($"[ResourceManager] Loaded {this.prefabDic.Count} prefabs successfully.");
            }
        }

        /// <summary>
        /// 获取 StreamingAssets 下资源的绝对路径.
        /// </summary>
        /// <param name="relativePath">相对 StreamingAssets 的路径.</param>
        /// <returns>绝对路径.</returns>
        public string GetStreamingAssetPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return Application.streamingAssetsPath;
            }

            string normalizedPath = relativePath.Replace('\\', '/');
            return Path.Combine(Application.streamingAssetsPath, normalizedPath);
        }

        /// <summary>
        /// 获取内置 LLM 模型的绝对路径.
        /// </summary>
        /// <returns>内置 LLM 模型路径.</returns>
        public string GetBuiltinLLMModelPath()
        {
            return this.GetStreamingAssetPath(ResourceConstant.BUILTIN_LLM_MODEL_RELATIVE_PATH);
        }

        /// <summary>
        /// 内置 LLM 模型文件是否存在.
        /// </summary>
        /// <returns>是否存在.</returns>
        public bool HasBuiltinLLMModel()
        {
            return File.Exists(this.GetBuiltinLLMModelPath());
        }

        /// <summary>
        /// 从字典中获取资源，未找到时记录日志并返回 default。
        /// </summary>
        private T TryGetResource<T>(Dictionary<string, T> dict, string name, string typeLabel, LogManager.LogLevelEnum logLevel = LogManager.LogLevelEnum.Error)
        {
            if (dict.TryGetValue(name, out T value))
            {
                return value;
            }

            AWorkerTask.LogProvider($"{name} {typeLabel} not found!!!", logLevel);
            return default;
        }

        /// <summary>
        /// 获取背包道具SO
        /// </summary>
        /// <param name="name">道具数据名称</param>
        /// <returns>道具数据</returns>
        public ItemDataSO GetBackpackSO(string name)
        {
            return this.TryGetResource(this.backpackDataDic, name, "scriptable", LogManager.LogLevelEnum.Warning);
        }

        /// <summary>
        /// 获取建造道具SO
        /// </summary>
        /// <param name="name">道具数据名称</param>
        /// <returns>道具数据</returns>
        public BuildItemDataSO GetBuildSO(string name)
        {
            return this.TryGetResource(this.buildDataDic, name, "scriptable");
        }

        /// <summary>
        /// 获取掉落物道具SO
        /// </summary>
        /// <param name="name">道具数据名称</param>
        /// <returns>道具数据</returns>
        public DropItemDataSO GetDropSO(string name)
        {
            return this.TryGetResource(this.dropDataDic, name, "scriptable");
        }

        /// <summary>
        /// 获取着色器
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>着色器</returns>
        public Shader GetShader(string name)
        {
            return this.TryGetResource(this.shaderDic, name, "shader");
        }

        /// <summary>
        /// 通过名称获得对应的tilebase.
        /// </summary>
        /// <param name="name">tilebase的名称.</param>
        /// <returns>tilebase.</returns>
        // public UnityEngine.Object getAsset(string name)
        public TileBase GetAsset(string name)
        {
            if (this.assetDic.TryGetValue(name, out UnityEngine.Object asset))
            {
                return (TileBase)asset;
            }

            AWorkerTask.LogProvider(name + " asset not found!!!", LogManager.LogLevelEnum.Error);
            return null;
        }

        /// <summary>
        /// 尝试获得对应的 tilebase，未找到时返回 null 且不打日志。
        /// 用于 Tile 对物品为可选的场景（如背包初始物品并非都要掉落/放置）。
        /// </summary>
        /// <param name="name">tilebase 名称。</param>
        /// <returns>tilebase；不存在时 null。</returns>
        public TileBase TryGetAsset(string name)
        {
            if (this.assetDic.TryGetValue(name, out UnityEngine.Object asset))
            {
                return (TileBase)asset;
            }

            return null;
        }

        /// <summary>
        /// 通过地形 ID 获得可放置的资源 Tile，默认随机获取。
        /// </summary>
        /// <param name="terrainId">地形 ID（对应 TerrainTileConfig.terrainId）。</param>
        /// <param name="name">包含该名称的资源（可选过滤）。</param>
        /// <returns>Tile，无匹配时返回 null。</returns>
        public TileBase GetAssetByTerrainId(int terrainId, string name = default)
        {
            if (!this.tileDic.TryGetValue(terrainId, out List<UnityEngine.Object> tiles) || tiles.Count == 0)
            {
                return null;
            }

            if (name == default)
            {
                return (TileBase)tiles[RandomIntProvider(0, tiles.Count)];
            }

            foreach (UnityEngine.Object tile in tiles)
            {
                if (tile.name.Contains(name))
                {
                    return (TileBase)tile;
                }
            }

            return null;
        }

        /// <summary>
        /// 通过名称获得Sprite
        /// 在背包中展示图片.
        /// </summary>
        /// <param name="name">名称.</param>
        /// <returns>Sprite.</returns>
        public Sprite GetImage(string name)
        {
            return this.TryGetResource(this.imageDic, name, "image");
        }

        /// <summary>
        /// 通过AB实例化对象
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <param name="isLocal">是否仅自己实例化.</param>
        /// <returns>对象.</returns>
        public GameObject Instantiate(string prefabName, bool isLocal = true)
        {
            return this.Instantiate(prefabName, default, default, null, false, isLocal);
        }

        /// <summary>
        /// 通过AB实例化对象
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <param name="parent">挂在某物体上</param>
        /// <param name="worldPositionStays">不跟随父物体旋转</param>
        /// <param name="isLocal">是否仅自己实例化.</param>
        /// <returns>对象.</returns>
        public GameObject Instantiate(string prefabName, Transform parent, bool worldPositionStays, bool isLocal = true)
        {
            return this.Instantiate(prefabName, default, default, parent, worldPositionStays, isLocal);
        }

        /// <summary>
        /// 通过AB实例化对象
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <param name="position">实例化位置.</param>
        /// <param name="rotation">实例化角度.</param>
        /// <param name="isLocal">是否仅自己实例化.</param>
        /// <returns>对象.</returns>
        public GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, bool isLocal = true)
        {
            return this.Instantiate(prefabName, position, rotation, null, false, isLocal);
        }

        /// <summary>
        /// 通过AB实例化对象
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <param name="position">实例化位置.</param>
        /// <param name="rotation">实例化角度.</param>
        /// <param name="parent">挂在某物体上</param>
        /// <param name="worldPositionStays">不跟随父物体旋转</param>
        /// <param name="isLocal">是否仅自己实例化.</param>
        /// <returns>对象.</returns>
        public GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, Transform parent, bool worldPositionStays, bool isLocal)
        {
            prefabName = prefabName.ToLower();
            if (ServiceLocator.Get<NetworkConnect>() != null && ServiceLocator.Get<NetworkConnect>().IsOnline && !isLocal)
            {
                return PhotonNetwork.Instantiate(prefabName, position, rotation);
            }
            else
            {
                if (!this.prefabDic.ContainsKey(prefabName))
                {
                    if (this.prefabDic.Count == 0)
                    {
                        string errorMsg =
                            $"[ResourceManager] prefabDic is empty! Cannot instantiate '{prefabName}'. " +
                            "The AssetBundle has not been loaded. " +
                            "Check the Console for earlier [ResourceManager] messages about AssetBundle loading.";
                        this.GameLogger.LogError(errorMsg);
                        AWorkerTask.LogProvider(errorMsg, LogManager.LogLevelEnum.Error);
                    }
                    else
                    {
                        this.GameLogger.LogWarning($"[ResourceManager] Prefab '{prefabName}' not found in dictionary ({this.prefabDic.Count} entries available)");
                        AWorkerTask.LogProvider(prefabName + " prefab not found!!!", LogManager.LogLevelEnum.Error);
                    }

                    return null;
                }

                GameObject prefab = this.prefabDic[prefabName];
                GameObject instance;
                if ((!position.Equals(default) || !rotation.Equals(default)) && parent != null)
                {
                    // 同时指定位置和父节点：使用带 parent 的位置重载，确保实例挂到正确的层级下
                    instance = GameObject.Instantiate(prefab, position, rotation, parent) as GameObject;
                }
                else if (!position.Equals(default) || !rotation.Equals(default))
                {
                    instance = GameObject.Instantiate(prefab, position, rotation) as GameObject;
                }
                else if (parent != null)
                {
                    instance = GameObject.Instantiate(prefab, parent, worldPositionStays);
                }
                else
                {
                    instance = GameObject.Instantiate(prefab) as GameObject;
                }

                if (instance == null)
                {
                    AWorkerTask.LogProvider($"{prefabName} Instantiate Error!!!", LogManager.LogLevelEnum.Error);
                    return null;
                }

                instance.name = prefabName;
                return instance;
            }
        }

        private void LoadPrefabs()
        {
            // 尝试多个可能的 AssetBundle 文件名（不同平台/构建可能使用不同命名）
            string[] candidatePaths = new[]
            {
                Path.Combine(Application.streamingAssetsPath, "prefab"),
                Path.Combine(Application.streamingAssetsPath, "Prefab"),
            };

            AssetBundle assetBundle = null;
            string loadedPath = null;

            foreach (string candidatePath in candidatePaths)
            {
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                try
                {
                    assetBundle = AssetBundle.LoadFromFile(candidatePath);
                    if (assetBundle != null)
                    {
                        loadedPath = candidatePath;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    this.GameLogger.LogError($"[ResourceManager] Failed to load AssetBundle from '{candidatePath}': {ex.Message}");
                }
            }

            if (assetBundle == null)
            {
                this.GameLogger.LogError(
                    $"[ResourceManager] AssetBundle not found! " +
                    $"Checked paths: {string.Join(", ", candidatePaths)}. " +
                    "Please build the AssetBundle first or ensure it is in StreamingAssets.");
                return;
            }

            try
            {
                string[] assetPaths = assetBundle.GetAllAssetNames();
                int prefabCount = 0;
                foreach (string path in assetPaths)
                {
                    string key = path.Split('/')[^1].Split('.')[0];
                    GameObject prefab = assetBundle.LoadAsset<GameObject>(path);
                    if (prefab != null)
                    {
                        this.prefabDic[key] = prefab;
                        prefabCount++;
                    }
                }

                this.GameLogger.Log($"[ResourceManager] Loaded {prefabCount} prefabs from AssetBundle: {loadedPath}");
            }
            catch (Exception ex)
            {
                this.GameLogger.LogError($"[ResourceManager] Error reading assets from AssetBundle: {ex.Message}");
            }
            finally
            {
                assetBundle.Unload(false);
            }
        }
    }
}
