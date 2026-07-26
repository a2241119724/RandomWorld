using System.Collections;
using UnityEngine;


namespace TMPro.Examples
{

    public class VertexColorCycler : MonoBehaviour
    {

        private TMP_Text m_TextComponent;

        void Awake()
        {
            m_TextComponent = GetComponent<TMP_Text>();
        }


        void Start()
        {
            StartCoroutine(AnimateVertexColors());
        }


        /// <summary>
        /// 对 TMP Text 对象的顶点颜色进行动画处理的方法。
        /// </summary>
        /// <returns></returns>
        IEnumerator AnimateVertexColors()
        {
            // 强制文本对象立即更新，以便从一开始就有可修改的几何体。
            m_TextComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            int currentCharacter = 0;

            Color32[] newVertexColors;
            Color32 c0 = m_TextComponent.color;

            while (true)
            {
                int characterCount = textInfo.characterCount;

                // 如果没有字符，则屈服并等待添加一些文本
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                // 获取当前字符使用的材质索引。
                int materialIndex = textInfo.characterInfo[currentCharacter].materialReferenceIndex;

                // 获取此文本元素（字符或精灵）使用的网格的顶点颜色。
                newVertexColors = textInfo.meshInfo[materialIndex].colors32;

                // 获取此文本元素使用的第一个顶点索引。
                int vertexIndex = textInfo.characterInfo[currentCharacter].vertexIndex;

                // 仅在文本元素可见时更改顶点颜色。
                if (textInfo.characterInfo[currentCharacter].isVisible)
                {
                    c0 = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);

                    newVertexColors[vertexIndex + 0] = c0;
                    newVertexColors[vertexIndex + 1] = c0;
                    newVertexColors[vertexIndex + 2] = c0;
                    newVertexColors[vertexIndex + 3] = c0;

                    // 新函数，当使用 Mesh Renderer 或 CanvasRenderer 时，将（所有）更新的顶点数据推送到相应的网格。
                    m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                    // 最后这一步可以只更新已更改的顶点数据，而不是所有顶点数据，但这需要额外的步骤并知道使用的是哪种渲染器。
                    // 这些额外步骤将是一种性能优化，但这种优化不太可能是必需的。
                }

                currentCharacter = (currentCharacter + 1) % characterCount;

                yield return new WaitForSeconds(0.05f);
            }
        }

    }
}
