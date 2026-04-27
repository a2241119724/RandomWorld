namespace LAB2D
{
    using System;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 编辑工具
    /// </summary>
    public class UITool : MonoBehaviour
    {
        private const string Exclude = @"^$"; // 排除,不优化
        private const string Prefix = "Tools/UI/";

        /// <summary>
        /// 优化Text
        /// </summary>
        [MenuItem(Prefix + "修改Text")]
        public static void UpdateText()
        {
            UITool.UpdateCommon(
                (Text text) =>
            {
                // 字体
                text.font = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");

                // 清晰倍数
                float multiple = 2;

                // 清晰度
                text.fontSize = 20 * (int)multiple;
                RectTransform rectTransform = text.GetComponent<RectTransform>();
                RectTransform parent = rectTransform.parent.GetComponent<RectTransform>();
                if (parent == null)
                {
                    Debug.Log("父物体:" + text.name + "没有RectTransform");
                    return;
                }

                rectTransform.pivot = Vector2.zero;
                rectTransform.localScale = new Vector3(1 / multiple, 1 / multiple, 1);
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(multiple, multiple);
            }, @"^.*\[[^]]*E[^]]*\]$");
        }

        /// <summary>
        /// 修改RoundCorner半径
        /// </summary>
        [MenuItem(Prefix + "修改RoundCorner")]
        public static void UpdateRoundCorner()
        {
            // 寻找所有的RoundCorner
            var roundCorners = Resources.FindObjectsOfTypeAll(typeof(RoundCorner));
            for (int i = 0; i < roundCorners.Length; i++)
            {
                RoundCorner rc = roundCorners[i] as RoundCorner;
                if (Regex.IsMatch(rc.name, Exclude) || rc.GetComponent<ExcludeEditor>() != null)
                {
                    Debug.Log("排除:" + rc.name);
                    continue;
                }

                // 记录对象
                Undo.RecordObject(rc, rc.gameObject.name);

                if (rc.GetComponent<Transform>().name.Equals("Background"))
                {
                    rc.Radius = 0.01f;
                }
                else
                {
                    rc.Radius = 0.1f;
                }

                // 设置已改变
                EditorUtility.SetDirty(rc);
            }

            Debug.Log("完成");
        }

        /// <summary>
        /// 修改Button颜色
        /// </summary>
        [MenuItem(Prefix + "修改Button")]
        public static void UpdateButton()
        {
            var buttons = Resources.FindObjectsOfTypeAll(typeof(Button));
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i] as Button;
                if (Regex.IsMatch(btn.name, Exclude) || btn.GetComponent<ExcludeEditor>() != null)
                {
                    Debug.Log("排除:" + btn.name);
                    continue;
                }

                // 记录对象
                Undo.RecordObject(btn, btn.gameObject.name);

                ColorBlock colors = btn.colors;
                colors.normalColor = new Color32(242, 160, 175, 255);
                colors.highlightedColor = new Color32(252, 200, 213, 255);
                colors.pressedColor = new Color32(249, 213, 110, 255);
                colors.selectedColor = new Color32(126, 203, 154, 255);

                colors.disabledColor = new Color32(0, 0, 0, 0);
                btn.colors = colors;

                // 设置已改变
                EditorUtility.SetDirty(btn);

                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    // 记录对象
                    Undo.RecordObject(img, img.gameObject.name);

                    // 设置按钮所在组件上的图片颜色
                    img.color = Color.white;

                    // 设置已改变
                    EditorUtility.SetDirty(img);
                }
            }

            Debug.Log("完成");
        }

        /// <summary>
        /// 修改Slider
        /// </summary>
        [MenuItem(Prefix + "修改Slider")]
        public static void UpdateSlider()
        {
            UITool.UpdateCommon((Slider slider) =>
            {
                // RectTransform sliderTransform = slider.GetComponent<RectTransform>();
                // sliderTransform.sizeDelta = new Vector2(sliderTransform.sizeDelta.x, 20);
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
                background.anchorMax = new Vector2(1, 1);
                background.anchorMin = new Vector2(0, 0);
                fillArea.offsetMax = new Vector2(-1, -6);
                fillArea.offsetMin = new Vector2(1, 6);

                // 设置按钮为圆形
                Transform handle = slider.transform.Find("Handle Slide Area/Handle");
                if (handle == null)
                {
                    return;
                }

                RectTransform handleTransform = handle.GetComponent<RectTransform>();
                handleTransform.sizeDelta = new Vector2(20, 0);
            });
        }

        /// <summary>
        /// 公共的编辑UI组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="action">具体执行内容</param>
        /// <param name="exclude">通过名称正则排除</param>
        private static void UpdateCommon<T>(Action<T> action, string exclude = @"^$")
            where T : Component
        {
            var coponents = Resources.FindObjectsOfTypeAll(typeof(T));
            for (int i = 0; i < coponents.Length; i++)
            {
                T component = coponents[i] as T;
                if (Regex.IsMatch(component.name, exclude) || component.GetComponent<ExcludeEditor>() != null)
                {
                    Debug.Log("排除:" + component.name);
                    continue;
                }

                // 记录对象
                Undo.RecordObject(component, component.gameObject.name);

                // 修改
                action(component);

                // 设置已改变
                EditorUtility.SetDirty(component);
            }

            Debug.Log("完成");
        }
    }
}
