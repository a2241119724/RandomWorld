using UnityEditor;
using UnityEngine;

namespace LAB2D
{
    public class ABPackage
    {
        /// <summary>
        /// ��Ԥ�����Inspector������New AssetBundle,�ٵ���
        /// �����StreamingAssets�ļ�����
        /// ʹ�÷���
        /// AssetBundle assetBundleObj = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/snow");
        /// GameObject g = Instantiate(assetBundleObj.LoadAsset<GameObject>("snow"));
        /// g.name = "AB��Դ���ط�ʽ";
        /// </summary>
        [MenuItem("Tools/��AB��")]
        private static void BuildAB()
        {
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath,BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            AssetDatabase.Refresh();
            Debug.Log("������");
        }
    }
}
