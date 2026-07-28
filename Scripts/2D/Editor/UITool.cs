namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI 编辑工具 — 修改 Slider 样式。
    /// </summary>
    public class UITool : MonoBehaviour
    {
        private const string Prefix = "工具/界面/";

        /// <summary>
        /// 修改Slider样式(填充/背景/手柄)
        /// </summary>
        [MenuItem(Prefix + "修改滑条")]
        public static void UpdateSlider()
        {
            var sliders = Resources.FindObjectsOfTypeAll(typeof(Slider));
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i] as Slider;
                if (slider == null)
                {
                    continue;
                }

                Undo.RecordObject(slider, slider.gameObject.name);

                RectTransform fill = slider.transform.Find("Fill Area/Fill").GetComponent<RectTransform>();
                fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
                fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
                fill.anchorMax = new Vector2(1, 1);
                fill.anchorMin = new Vector2(0, 0);
                fill.offsetMax = Vector2.zero;
                fill.offsetMin = Vector2.zero;

                RectTransform background = slider.transform.Find("Background").GetComponent<RectTransform>();
                background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
                background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
                background.anchorMax = new Vector2(1, 1);
                background.anchorMin = new Vector2(0, 0);
                background.offsetMax = new Vector2(0, -5);
                background.offsetMin = new Vector2(0, 5);

                RectTransform fillArea = slider.transform.Find("Fill Area").GetComponent<RectTransform>();
                fillArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
                fillArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
                fillArea.anchorMax = new Vector2(1, 1);
                fillArea.anchorMin = new Vector2(0, 0);
                fillArea.offsetMax = new Vector2(-1, -6);
                fillArea.offsetMin = new Vector2(1, 6);

                // 设置手柄为圆形
                Transform handle = slider.transform.Find("Handle Slide Area/Handle");
                if (handle == null)
                {
                    continue;
                }

                RectTransform handleTransform = handle.GetComponent<RectTransform>();
                handleTransform.sizeDelta = new Vector2(20, 0);

                EditorUtility.SetDirty(slider);
            }

            Debug.Log("Slider 修改完成");
        }
    }
}
