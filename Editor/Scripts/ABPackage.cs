using UnityEditor;
using UnityEngine;

namespace LAB2D
{
    public class ABPackage
    {
        /// <summary>
        /// 在预制体的Inspector最下面New AssetBundle,再点打包
        /// 打包到StreamingAssets文件夹下
        /// 使用方法
        /// AssetBundle assetBundleObj = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/snow");
        /// GameObject g = Instantiate(assetBundleObj.LoadAsset<GameObject>("snow"));
        /// g.name = "AB资源加载方式";
        /// </summary>
        [MenuItem("Tools/打AB包")]
        private static void BuildAB()
        {
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath,BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            AssetDatabase.Refresh();
            Debug.Log("打包完成");
        }
    }
}
