namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 资源管理.
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>
    {
        private readonly Dictionary<string, GameObject> prefabDic; // <characterType,<name,prefab>>
        private readonly Dictionary<string, UnityEngine.Object> assetDic;
        private readonly Dictionary<string, Sprite> imageDic;
        private readonly Dictionary<TileMap.MapTileType, List<UnityEngine.Object>> tileDic;
        private readonly Dictionary<string, Shader> shaderDic;
        private readonly Dictionary<string, ItemDataSO> backpackDataDic;
        private readonly Dictionary<string, BuildItemDataSO> buildDataDic;
        private readonly Dictionary<string, DropItemDataSO> dropDataDic;

        public ResourceManager()
        {
            this.prefabDic = ResourceTool.LoadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            this.assetDic = ResourceTool.LoadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
            this.tileDic = new Dictionary<TileMap.MapTileType, List<UnityEngine.Object>>();
            this.shaderDic = ResourceTool.LoadResources<Shader>(ResourceConstant.SHADER_ROOT);
            foreach (KeyValuePair<string, UnityEngine.Object> asset in this.assetDic)
            {
                foreach (TileMap.MapTileType tileType in Enum.GetValues(typeof(TileMap.MapTileType)))
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
                LogManager.Instance.Log(name + " scriptable not found!!!", LogManager.LogLevel.Error);
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
                LogManager.Instance.Log(name + " scriptable not found!!!", LogManager.LogLevel.Error);
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
                LogManager.Instance.Log(name + " scriptable not found!!!", LogManager.LogLevel.Error);
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
                LogManager.Instance.Log(name + " shader not found!!!", LogManager.LogLevel.Error);
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

            LogManager.Instance.Log(name + " asset not found!!!", LogManager.LogLevel.Error);
            return null;
        }

        /// <summary>
        /// 通过类型获得在Tile上的资源,默认随机获取.
        /// </summary>
        /// <param name="tileType">在哪种Tile上.</param>
        /// <param name="name">包含该名称的资源.</param>
        /// <returns>Tile.</returns>
        public TileBase GetAssetByTileType(TileMap.MapTileType tileType, string name = default)
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
                return (TileBase)tiles[UnityEngine.Random.Range(0, tiles.Count)];
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

            LogManager.Instance.Log(name + " image not found!!!", LogManager.LogLevel.Error);
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
            if (NetworkConnect.Instance.IsOnline && !isLocal)
            {
                return PhotonNetwork.Instantiate(prefabName, position, rotation);
            }
            else
            {
                if (!this.prefabDic.ContainsKey(prefabName))
                {
                    LogManager.Instance.Log(prefabName + " prefab not found!!!", LogManager.LogLevel.Error);
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
                    LogManager.Instance.Log($"{prefabName} Instantiate Error!!!", LogManager.LogLevel.Error);
                    return null;
                }

                instance.name = prefabName;
                return instance;
            }
        }

        private void LoadPrefabs()
        {
            string prefabAB = Application.streamingAssetsPath + "/Prefab";
            AssetBundle assetBundle = AssetBundle.LoadFromFile(prefabAB);
            if (assetBundle == null)
            {
                LogManager.Instance.Log("AB包:" + prefabAB + "不存在");
                return;
            }

            string[] assetPaths = assetBundle.GetAllAssetNames();
            foreach (string path in assetPaths)
            {
                this.prefabDic[path.Split("/")[^1].Split(".")[0]] = assetBundle.LoadAsset<GameObject>(path);
            }

            assetBundle.Unload(false);
        }
    }
}