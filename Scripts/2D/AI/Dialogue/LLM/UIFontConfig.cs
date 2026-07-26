namespace LAB2D.AI.Dialogue.LLM
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "SO/AI/UIFontConfig", order = 1)]
    public class UIFontConfig : ScriptableObject
    {
        private static UIFontConfig cached;

        [Tooltip("全局 UI 字体")]
        public Font font;

        public static UIFontConfig Instance
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<UIFontConfig>("SO/UIFontConfig");
                }

                return cached;
            }
        }

        public static Font GetFont()
        {
            UIFontConfig cfg = Instance;
            if (cfg != null && cfg.font != null)
            {
                return cfg.font;
            }

            Font fallback = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
            if (fallback != null)
            {
                return fallback;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
