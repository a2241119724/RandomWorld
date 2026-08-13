namespace LAB2D.Editor
{
    using LAB2D.SO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 以 Name 作为列表元素折叠标题的 SO 自定义 Inspector 基类。
    /// 替换 Unity 默认的 "Element N" 标题，使列表更直观。
    /// </summary>
    public abstract class NameListSOEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 绘制一个以元素 Name 为折叠标题的列表。
        /// </summary>
        /// <param name="list">列表类型的 SerializedProperty。</param>
        protected void DrawNameList(SerializedProperty list)
        {
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, list.displayName, true);
            if (!list.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            // 列表长度
            EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = element.FindPropertyRelative("Name");
                string label = (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                    ? nameProp.stringValue
                    : $"元素 {i}";

                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, label, true);
                if (element.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawElementFields(element);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 绘制元素的所有可见字段（自动涵盖后续新增字段）。
        /// </summary>
        /// <param name="element">元素对应的 SerializedProperty。</param>
        private static void DrawElementFields(SerializedProperty element)
        {
            SerializedProperty child = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                EditorGUILayout.PropertyField(child, true);
                enterChildren = false;
            }
        }
    }

    /// <summary>
    /// ItemDataSO 自定义 Inspector：用 Name 显示列表元素标题。
    /// </summary>
    [CustomEditor(typeof(ItemDataSO))]
    public class ItemDataSOEditor : NameListSOEditor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("ItemType"));
            this.DrawNameList(this.serializedObject.FindProperty("ItemDatas"));

            this.serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// BuildItemDataSO 自定义 Inspector：用 Name 显示列表元素标题。
    /// </summary>
    [CustomEditor(typeof(BuildItemDataSO))]
    public class BuildItemDataSOEditor : NameListSOEditor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("ItemType"));
            this.DrawNameList(this.serializedObject.FindProperty("BuildItemDatas"));

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
