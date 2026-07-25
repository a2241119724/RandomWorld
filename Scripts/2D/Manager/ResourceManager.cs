namespace LAB2D.Manager
{
    using LAB2D;
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
        private readonly Dictionary<string, GameObject> prefabDic; // <characterType,<name,prefab>>
        private readonly Dictionary<string, UnityEngine.Object> assetDic;
        private readonly Dictionary<string, Sprite> imageDic;
        private readonly Dictionary<TileMap.MapTileTypeEnum, List<UnityEngine.Object>> tileDic;
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
            this.tileDic = new Dictionary<TileMap.MapTileTypeEnum, List<UnityEngine.Object>>();
            this.shaderDic = ResourceTool.LoadResources<Shader>(ResourceConstant.SHADER_ROOT);
            foreach (KeyValuePair<string, UnityEngine.Object> asset in this.assetDic)
            {
                foreach (TileMap.MapTileTypeEnum tileType in Enum.GetValues(typeof(TileMap.MapTileTypeEnum)))
                {
                    // 不包含Tile本身，仅包含其上的资源
                    if (!asset.Key.StartsWith(tileType.ToString()) ||
                        asset.Key.Equals(tileType.ToString()))
                    {
                        continue;
                    }

                    if (!this.tileDic.ContainsKey(tileType))
                    {
                        this.tileDic.Add(tileType, new List<UnityEngine.Object>());
                    }

                    this.tileDic[tileType].Add(asset.Value);
                    break;
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
                Debug.LogWarning(
                    "[ResourceManager] prefabDic is empty after initialization. " +
                    "Prefabs will NOT be available for Instantiate(). " +
                    "Ensure StreamingAssets/prefab AssetBundle exists and contains prefabs, " +
                    "or place prefabs under Resources/Prefabs/.");
            }
            else
            {
                Debug.Log($"[ResourceManager] Loaded {this.prefabDic.Count} prefabs successfully.");
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
        /// 获取背包道具SO
        /// </summary>
        /// <param name="name">道具数据名称</param>
        /// <returns>道具数据</returns>
        public ItemDataSO GetBackpackSO(string name)
        {
            if (!this.backpackDataDic.ContainsKey(name))
            {
                AWorkerTask.LogProvider(name + " scriptable not found!!!", LogManager.LogLevelEnum.Warning);
                return null;
            }

            return this.backpackDataDic[name];
        }

        /// <summary>
        /// 获取建造道具SO
        /// </summary>
        /// <param name="name">道具数据名称</param>
        /// <returns>道具数据</returns>
        public BuildItemDataSO GetBuildSO(string name)
        {
            if (!this.buildDataDic.ContainsKey(name))
            {
                AWorkerTask.LogProvider(name + " scriptable not found!!!", LogManager.LogLevelEnum.Error);
                return null;
            }

            return this.buildDataDic[name];
        }

        /// <summary>
        /// 获取掉落物道具SO
        /// </summary>
        /// <param name="name">道具数据名称</param>
        /// <returns>道具数据</returns>
        public DropItemDataSO GetDropSO(string name)
        {
            if (!this.dropDataDic.ContainsKey(name))
            {
                AWorkerTask.LogProvider(name + " scriptable not found!!!", LogManager.LogLevelEnum.Error);
                return null;
            }

            return this.dropDataDic[name];
        }

        /// <summary>
        /// 获取着色器
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>着色器</returns>
        public Shader GetShader(string name)
        {
            if (!this.shaderDic.ContainsKey(name))
            {
                AWorkerTask.LogProvider(name + " shader not found!!!", LogManager.LogLevelEnum.Error);
                return null;
            }

            return this.shaderDic[name];
        }

        /// <summary>
        /// 通过名称获得对应的tilebase.
        /// </summary>
        /// <param name="name">tilebase的名称.</param>
        /// <returns>tilebase.</returns>
        // public UnityEngine.Object getAsset(string name)
        public TileBase GetAsset(string name)
        {
            if (this.assetDic.ContainsKey(name))
            {
                UnityEngine.Object asset = this.assetDic[name];
                return (TileBase)asset;
            }

            AWorkerTask.LogProvider(name + " asset not found!!!", LogManager.LogLevelEnum.Error);
            return null;
        }

        /// <summary>
        /// 通过类型获得在Tile上的资源,默认随机获取.
        /// </summary>
        /// <param name="tileType">在哪种Tile上.</param>
        /// <param name="name">包含该名称的资源.</param>
        /// <returns>Tile.</returns>
        public TileBase GetAssetByTileType(TileMap.MapTileTypeEnum tileType, string name = default)
        {
            if (!this.tileDic.ContainsKey(tileType))
            {
                return null;
            }

            List<UnityEngine.Object> tiles = this.tileDic[tileType];
            if (tiles.Count == 0)
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
            if (this.imageDic.ContainsKey(name))
            {
                Sprite sprite = this.imageDic[name];
                return sprite;
            }

            AWorkerTask.LogProvider(name + " image not found!!!", LogManager.LogLevelEnum.Error);
            return null;
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
                        Debug.LogError(errorMsg);
                        AWorkerTask.LogProvider(errorMsg, LogManager.LogLevelEnum.Error);
                    }
                    else
                    {
                        Debug.LogWarning($"[ResourceManager] Prefab '{prefabName}' not found in dictionary ({this.prefabDic.Count} entries available)");
                        AWorkerTask.LogProvider(prefabName + " prefab not found!!!", LogManager.LogLevelEnum.Error);
                    }

                    return null;
                }

                GameObject prefab = this.prefabDic[prefabName];
                GameObject instance;
                if (!position.Equals(default) || !rotation.Equals(default))
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
                    Debug.LogError($"[ResourceManager] Failed to load AssetBundle from '{candidatePath}': {ex.Message}");
                }
            }

            if (assetBundle == null)
            {
                Debug.LogError(
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

                Debug.Log($"[ResourceManager] Loaded {prefabCount} prefabs from AssetBundle: {loadedPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] Error reading assets from AssetBundle: {ex.Message}");
            }
            finally
            {
                assetBundle.Unload(false);
            }
        }
    }
}
