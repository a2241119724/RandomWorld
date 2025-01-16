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

        public ResourcesManager() {
            prefabsDic = Tool.loadResources<GameObject>(ResourceConstant.PREFAB_ROOT);
            assetsDic = Tool.loadResources<UnityEngine.Object>(ResourceConstant.TILEMAP_ROOT);
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
            Debug.Log(name + " image not found!!!");
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
            Debug.Log(name + " image not found!!!");
            return null;
        }
    }
}