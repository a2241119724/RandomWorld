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
        private readonly Dictionary<string, GameObject> prefabsDic; // <characterType,<name,prefab>>
        private readonly Dictionary<string, UnityEngine.Object> assetsDic;
        private readonly Dictionary<string, Sprite> imagesDic;
        private readonly Dictionary<string, string> pathsDic; // key:filename(带后缀) value:path
        private readonly Dictionary<MapTileType, List<UnityEngine.Object>> tileDic;

        public ResourcesManager()
        {
            this.prefabsDic = Tool.LoadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            this.assetsDic = Tool.LoadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
            this.tileDic = new Dictionary<MapTileType, List<UnityEngine.Object>>();
            foreach (KeyValuePair<string, UnityEngine.Object> asset in this.assetsDic)
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

            this.imagesDic = Tool.LoadResources<Sprite>(ResourceConstant.IMAGE_ROOT);
            this.pathsDic = Tool.LoadPaths();
        }

        /// <summary>
        /// 通过名称获得对应的预制体.
        /// </summary>
        /// <param name="name">预制体名称.</param>
        /// <returns>预制体.</returns>
        public GameObject GetPrefab(string name)
        {
            if (this.prefabsDic.ContainsKey(name))
            {
                GameObject prefab = this.prefabsDic[name];
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
            if (this.assetsDic.ContainsKey(name))
            {
                UnityEngine.Object asset = this.assetsDic[name];
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
            if (this.imagesDic.ContainsKey(name))
            {
                Sprite sprite = this.imagesDic[name];
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
            if (this.pathsDic.ContainsKey(name))
            {
                string path = this.pathsDic[name];
                return path;
            }

            LogManager.Instance.Log(name + " image not found!!!", LogManager.LogLevel.Error);
            return null;
        }
    }
}