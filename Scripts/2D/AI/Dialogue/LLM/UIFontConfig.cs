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

        /// <summary>
        /// 运行时 UI 字体（薄委托 PixelUITheme.PixelFont——铁律③单一来源收口）。
        /// 此前三级链（SO→ark-pixel→内置）与 PixelFont 并存双源，且 SO 可在编辑器改，
        /// 违反「永久 ark-pixel」决策；SO 的 font 字段现仅剩 Editor 批量工具消费。
        /// </summary>
        public static Font GetFont()
        {
            return LAB2D.UI.PixelUITheme.PixelFont;
        }
    }
}
