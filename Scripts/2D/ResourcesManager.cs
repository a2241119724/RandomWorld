using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace LAB2D
{
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        /// <summary>
        /// <characterType,<name,prefab>>
        /// </summary>
        private readonly Dictionary<string, GameObject> prefabsDic;
        private readonly Dictionary<string, UnityEngine.Object> assetsDic;
        private readonly Dictionary<string, Sprite> imagesDic;
        /// <summary>
        /// key:filename(带后缀) value:path
        /// </summary>
        private readonly Dictionary<string, string> pathsDic;
        private readonly Dictionary<TileType, List<UnityEngine.Object>> tileDic;

        public ResourcesManager() {
            prefabsDic = Tool.loadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            assetsDic = Tool.loadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
            tileDic = new Dictionary<TileType, List<UnityEngine.Object>>();
            foreach(KeyValuePair<string, UnityEngine.Object> asset in assetsDic)
            {
                foreach(TileType tileType in Enum.GetValues(typeof(TileType)))
                {
                    // 不包含Tile本身，仅包含其上的资源
                    if (!asset.Key.StartsWith(tileType.ToString()) || 
                        asset.Key.Equals(tileType.ToString())) continue;
                    if (!tileDic.ContainsKey(tileType))
                    {
                        tileDic.Add(tileType, new List<UnityEngine.Object>());
                    }
                    tileDic[tileType].Add(asset.Value);
                    break;
                }
            }
            imagesDic = Tool.loadResources<Sprite>(ResourceConstant.IMAGE_ROOT);
            pathsDic = Tool.loadPaths();
        }

        /// <summary>
        /// 通过类型与名称找到对应的预制体
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject getPrefab(string name)
        {
            if (prefabsDic.ContainsKey(name))
            {
                GameObject prefab = prefabsDic[name];
                return prefab;
            }
            LogManager.Instance.log(name + " prefab not found!!!", LogManager.LogLevel.Error);
            return null;
        }

        /// <summary>
        /// tilebase
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        //public UnityEngine.Object getAsset(string name)
        public TileBase getAsset(string name)
        {
            if (assetsDic.ContainsKey(name))
            {
                UnityEngine.Object asset = assetsDic[name];
                return (TileBase)asset;
            }
            LogManager.Instance.log(name + " asset not found!!!", LogManager.LogLevel.Error);
            return null;
        }

        /// <summary>
        /// 得到在Tile上的资源,默认随机获取
        /// </summary>
        /// <param name="tileType">在哪种Tile上</param>
        /// <param name="name">包含该名称的资源</param>
        /// <returns></returns>
        public TileBase getAssetByTileType(TileType tileType, string name=default) {
            if (!tileDic.ContainsKey(tileType)) return null;
            List<UnityEngine.Object> tiles = tileDic[tileType];
            if (tiles.Count == 0) return null;
            if (name == default)
            {
                return (TileBase)tiles[UnityEngine.Random.Range(0, tiles.Count)];
            }
            foreach(UnityEngine.Object tile in tiles)
            {
                if (tile.name.Contains(name))
                {
                    return (TileBase)tile;
                }
            }
            return null;
        }

        /// <summary>
        /// 在背包中展示图片
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Sprite getImage(string name)
        {
            if (imagesDic.ContainsKey(name))
            {
                Sprite sprite = imagesDic[name];
                return sprite;
            }
            LogManager.Instance.log(name + " image not found!!!", LogManager.LogLevel.Error);
            return null;
        }

        /// <summary>
        /// 获取Resource下文件的路径
        /// </summary>
        /// <param name="name">需要加入后缀</param>
        /// <returns></returns>
        public string getPath(string name) {
            if (pathsDic.ContainsKey(name))
            {
                string path = pathsDic[name];
                return path;
            }
            LogManager.Instance.log(name + " image not found!!!", LogManager.LogLevel.Error);
            return null;
        }
    }
}