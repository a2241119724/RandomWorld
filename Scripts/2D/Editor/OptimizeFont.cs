namespace LAB2D
{
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 优化字体
    /// </summary>
    public class OptimizeFont : Editor
    {
        private static readonly string Exclude = @"^.*\[[^]]*E[^]]*\]$"; // 排除,不优化

        /// <summary>
        /// 优化Text清晰度
        /// </summary>
        [MenuItem("Tools/优化Text清晰度")]
        public static void UpdateText()
        {
            // 寻找所有的Text
            var tests = Resources.FindObjectsOfTypeAll(typeof(Text));
            for (int i = 0; i < tests.Length; i++)
            {
                Text t = tests[i] as Text;
                if (Regex.IsMatch(t.name, Exclude) || t.GetComponent<ExcludeEditor>() != null)
                {
                    Debug.Log("排除:" + t.name);
                    continue;
                }

                // 记录对象
                Undo.RecordObject(t, t.gameObject.name);
                t.fontSize = 35;

                RectTransform rectTransform = t.GetComponent<RectTransform>();
                RectTransform parent = rectTransform.parent.GetComponent<RectTransform>();
                if (parent == null)
                {
                    Debug.Log("父物体:" + t.name + "没有RectTransform");
                    continue;
                }

                rectTransform.sizeDelta = new Vector2(0, 0);
                rectTransform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                rectTransform.offsetMax = new Vector2(parent.rect.width / 2, parent.rect.height / 2);
                rectTransform.offsetMin = new Vector2(-parent.rect.width / 2, -parent.rect.height / 2);

                // 设置已改变
                EditorUtility.SetDirty(t);
            }

            Debug.Log("完成");
        }
    }
}
