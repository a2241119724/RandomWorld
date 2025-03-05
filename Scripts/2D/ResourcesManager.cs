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
        /// key:filename(����׺) value:path
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
                    // ������Tile�������������ϵ���Դ
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
        /// ͨ�������������ҵ���Ӧ��Ԥ����
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
        /// �õ���Tile�ϵ���Դ,Ĭ�������ȡ
        /// </summary>
        /// <param name="tileType">������Tile��</param>
        /// <param name="name">���������Ƶ���Դ</param>
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
        /// �ڱ�����չʾͼƬ
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
        /// ��ȡResource���ļ���·��
        /// </summary>
        /// <param name="name">��Ҫ�����׺</param>
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