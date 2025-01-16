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

        public ResourcesManager() {
            prefabsDic = Tool.loadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            assetsDic = Tool.loadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
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
            Debug.Log(name + " prefab not found!!!");
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
            Debug.Log(name + " asset not found!!!");
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
            Debug.Log(name + " image not found!!!");
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
            Debug.Log(name + " image not found!!!");
            return null;
        }
    }
}