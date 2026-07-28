namespace LAB2D.Editor
{
    using LAB2D.AI.Dialogue.LLM;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    public static class UIFontConfigMenu
    {
        private const string MenuRoot = "工具/界面/字体配置/";
        private const string CreateConfig = MenuRoot + "创建/选中 UI 字体配置";
        private const string ApplyToScene = MenuRoot + "应用全局字体到所有 UI";

        [MenuItem(CreateConfig, false, 52)]
        private static void CreateOrSelectConfig()
        {
            UIFontConfig config = Resources.Load<UIFontConfig>("SO/UIFontConfig");
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources/SO"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "SO");
                }

                config = ScriptableObject.CreateInstance<UIFontConfig>();
                config.font = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
                AssetDatabase.CreateAsset(config, "Assets/Resources/SO/UIFontConfig.asset");
                AssetDatabase.SaveAssets();
                Debug.Log("[UIFontConfig] 已创建 UI 字体配置: Resources/SO/UIFontConfig.asset");
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem(ApplyToScene, false, 53)]
        private static void ApplyFontToAllText()
        {
            UIFontConfig config = UIFontConfig.Instance;
            if (config == null || config.font == null)
            {
                EditorUtility.DisplayDialog("未配置", "请先运行 \"创建/选中 UI 字体配置\" 设置字体。", "确定");
                return;
            }

            Font font = config.font;
            int count = 0;
            foreach (Text text in Object.FindObjectsOfType<Text>(true))
            {
                Undo.RecordObject(text, "Apply Global Font");
                text.font = font;
                count++;
            }

            Debug.Log("[UIFontConfig] 已应用字体到 " + count + " 个 Text 组件");
            EditorUtility.DisplayDialog("完成", "已应用字体到 " + count + " 个 Text 组件。", "确定");
        }
    }
}
