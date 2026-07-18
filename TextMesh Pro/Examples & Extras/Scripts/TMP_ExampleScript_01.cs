using UnityEngine;


namespace TMPro.Examples
{

    public class TMP_ExampleScript_01 : MonoBehaviour
    {
        public enum objectType { TextMeshPro = 0, TextMeshProUGUI = 1 };

        public objectType ObjectType;
        public bool isStatic;

        private TMP_Text m_text;

        //private TMP_InputField m_inputfield;


        private const string k_label = "The count is <#0080ff>{0}</color>";
        private int count;

        void Awake()
        {
            // 获取对 TMP 文本组件的引用，如果已存在则使用，否则添加一个。
            // 此示例展示了两种 TMP 组件都派生自 TMP_Text 的便利性。 
            if (ObjectType == 0)
                m_text = GetComponent<TextMeshPro>() ?? gameObject.AddComponent<TextMeshPro>();
            else
                m_text = GetComponent<TextMeshProUGUI>() ?? gameObject.AddComponent<TextMeshProUGUI>();

            // 加载新的字体资源并将其分配给文本对象。
            m_text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");

            // 加载通过上下文菜单复制创建的新材质预设。
            m_text.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/Anton SDF - Drop Shadow");

            // 设置字体大小。
            m_text.fontSize = 120;

            // 设置文本
            m_text.text = "A <#0080ff>simple</color> line of text.";

            // 根据提供的宽度和高度获取首选宽高，而非当前文本容器的实际大小。
            Vector2 size = m_text.GetPreferredValues(Mathf.Infinity, Mathf.Infinity);

            // 根据新计算的值设置 RectTransform 的大小。
            m_text.rectTransform.sizeDelta = new Vector2(size.x, size.y);
        }


        void Update()
        {
            if (!isStatic)
            {
                m_text.SetText(k_label, count % 1000);
                count += 1;
            }
        }

    }
}
