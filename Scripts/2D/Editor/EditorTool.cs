namespace LAB2D
{
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 编辑工具
    /// </summary>
    public class EditorTool : MonoBehaviour
    {
        private static readonly string Exclude = @"^$"; // 排除,不优化

        /// <summary>
        /// 修改RoundCorner半径
        /// </summary>
        [MenuItem("Tools/修改RoundCorner半径")]
        public static void UpdateText()
        {
            // 寻找所有的RoundCorner
            var roundCorners = Resources.FindObjectsOfTypeAll(typeof(RoundCorner));
            for (int i = 0; i < roundCorners.Length; i++)
            {
                RoundCorner rc = roundCorners[i] as RoundCorner;
                if (Regex.IsMatch(rc.name, Exclude))
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
    }
}
