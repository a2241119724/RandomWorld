namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEditor.U2D;
    using UnityEngine;

    /// <summary>
    /// 统一素材像素风：将 Resources/Images 下所有游戏图片的导入过滤模式设为 Point（无平滑），
    /// 图集 All.spriteatlas 同步设为 Point 并重新打包。
    /// 一次性工具，运行完成后可视情况删除。
    /// </summary>
    public class TexturePointFilterTool
    {
        private const string Prefix = "工具/像素风/";
        private const string ImageRoot = "Assets/Resources/Images";
        private const string AtlasPath = ImageRoot + "/All.spriteatlas";

        /// <summary>
        /// 全部图片 + 图集设为 Point 过滤
        /// </summary>
        [MenuItem(Prefix + "全部图片设为 Point 过滤", false, 900)]
        private static void SetAllImagesPoint()
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { ImageRoot });
            int changed = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || importer.filterMode == FilterMode.Point)
                    {
                        continue;
                    }

                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[像素风] 已将 {changed} 张图片设为 Point 过滤");

            var atlasImporter = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
            if (atlasImporter == null)
            {
                Debug.LogWarning("[像素风] 未找到 All.spriteatlas");
                return;
            }

            var settings = atlasImporter.textureSettings;
            settings.filterMode = FilterMode.Point;
            atlasImporter.textureSettings = settings;
            atlasImporter.SaveAndReimport();

            Debug.Log("[像素风] 图集 All.spriteatlas 已设为 Point 过滤并重新打包");
        }
    }
}
