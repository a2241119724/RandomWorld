namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 资源管理.
    /// </summary>
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        private readonly Dictionary<string, GameObject> prefabDic; // <characterType,<name,prefab>>
        private readonly Dictionary<string, UnityEngine.Object> assetDic;
        private readonly Dictionary<string, Sprite> imageDic;
        private readonly Dictionary<string, string> pathDic; // key:filename(带后缀) value:path
        private readonly Dictionary<MapTileType, List<UnityEngine.Object>> tileDic;
        private readonly Dictionary<string, Shader> shaderDic;

        public ResourcesManager()
        {
            this.prefabDic = Tool.LoadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            this.assetDic = Tool.LoadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
            this.tileDic = new Dictionary<MapTileType, List<UnityEngine.Object>>();
            this.shaderDic = Tool.LoadResources<Shader>(ResourceConstant.SHADER_ROOT);
            foreach (KeyValuePair<string, UnityEngine.Object> asset in this.assetDic)
            {
                foreach (MapTileType tileType in Enum.GetValues(typeof(MapTileType)))
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

            this.imageDic = Tool.LoadResources<Sprite>(ResourceConstant.IMAGE_ROOT);
            this.pathDic = Tool.LoadPaths();
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
        /// 通过名称获得对应的预制体.
        /// </summary>
        /// <param name="name">预制体名称.</param>
        /// <returns>预制体.</returns>
        public GameObject GetPrefab(string name)
        {
            if (this.prefabDic.ContainsKey(name))
            {
                GameObject prefab = this.prefabDic[name];
                return prefab;
            }

            LogManager.Instance.Log(name + " prefab not found!!!", LogManager.LogLevel.Error);
            return null;
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
        public TileBase GetAssetByTileType(MapTileType tileType, string name = default)
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
        /// 获取Resource下文件的路径.
        /// </summary>
        /// <param name="name">需要加入后缀.</param>
        /// <returns>路径.</returns>
        public string GetPath(string name)
        {
            if (this.pathDic.ContainsKey(name))
            {
                string path = this.pathDic[name];
                return path;
            }

            LogManager.Instance.Log(name + " image not found!!!", LogManager.LogLevel.Error);
            return null;
        }
    }
}