using System.Collections;
using UnityEngine;


namespace TMPro.Examples
{

    public class VertexShakeA : MonoBehaviour
    {

        public float AngleMultiplier = 1.0f;
        public float SpeedMultiplier = 1.0f;
        public float ScaleMultiplier = 1.0f;
        public float RotationMultiplier = 1.0f;

        private TMP_Text m_TextComponent;
        private bool hasTextChanged;


        void Awake()
        {
            m_TextComponent = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            // 订阅文本对象重新生成时触发的事件。
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
        }


        void Start()
        {
            StartCoroutine(AnimateVertexColors());
        }


        void ON_TEXT_CHANGED(Object obj)
        {
            if (obj = m_TextComponent)
                hasTextChanged = true;
        }

        /// <summary>
        /// 对 TMP Text 对象的顶点颜色进行动画处理的方法。
        /// </summary>
        /// <returns></returns>
        IEnumerator AnimateVertexColors()
        {

            // 我们强制更新文本对象，因为它只会在帧结束时更新。也就是说，在第一帧上执行此代码之前。
            // 或者，我们可以屈服并等待到帧结束时文本对象生成。
            m_TextComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_TextComponent.textInfo;

            Matrix4x4 matrix;
            Vector3[][] copyOfVertices = new Vector3[0][];

            hasTextChanged = true;

            while (true)
            {
                // 分配新的顶点 
                if (hasTextChanged)
                {
                    if (copyOfVertices.Length < textInfo.meshInfo.Length)
                        copyOfVertices = new Vector3[textInfo.meshInfo.Length][];

                    for (int i = 0; i < textInfo.meshInfo.Length; i++)
                    {
                        int length = textInfo.meshInfo[i].vertices.Length;
                        copyOfVertices[i] = new Vector3[length];
                    }

                    hasTextChanged = false;
                }

                int characterCount = textInfo.characterCount;

                // 如果没有字符，则屈服并等待添加一些文本
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                int lineCount = textInfo.lineCount;

                // 遍历文本的每一行。
                for (int i = 0; i < lineCount; i++)
                {

                    int first = textInfo.lineInfo[i].firstCharacterIndex;
                    int last = textInfo.lineInfo[i].lastCharacterIndex;

                    // 确定每行的中心
                    Vector3 centerOfLine = (textInfo.characterInfo[first].bottomLeft + textInfo.characterInfo[last].topRight) / 2;
                    Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(-0.25f, 0.25f) * RotationMultiplier);

                    // 遍历该行的每个字符。
                    for (int j = first; j <= last; j++)
                    {
                        // 跳过不可见的字符，因此没有可操作的几何体。
                        if (!textInfo.characterInfo[j].isVisible)
                            continue;

                        // 获取当前字符使用的材质索引。
                        int materialIndex = textInfo.characterInfo[j].materialReferenceIndex;

                        // 获取此文本元素使用的第一个顶点索引。
                        int vertexIndex = textInfo.characterInfo[j].vertexIndex;

                        // 获取此文本元素（字符或精灵）使用的网格顶点。
                        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                        // 需要将每个四边形的 4 个顶点平移到与字符中心对齐。
                        // 这是必需的，以便矩阵 TRS 作用于每个字符的原点。
                        copyOfVertices[materialIndex][vertexIndex + 0] = sourceVertices[vertexIndex + 0] - centerOfLine;
                        copyOfVertices[materialIndex][vertexIndex + 1] = sourceVertices[vertexIndex + 1] - centerOfLine;
                        copyOfVertices[materialIndex][vertexIndex + 2] = sourceVertices[vertexIndex + 2] - centerOfLine;
                        copyOfVertices[materialIndex][vertexIndex + 3] = sourceVertices[vertexIndex + 3] - centerOfLine;

                        // 确定每个字符的随机缩放变化。
                        float randomScale = Random.Range(0.995f - 0.001f * ScaleMultiplier, 1.005f + 0.001f * ScaleMultiplier);

                        // 设置矩阵旋转。
                        matrix = Matrix4x4.TRS(Vector3.one, rotation, Vector3.one * randomScale);

                        // 将矩阵 TRS 应用于相对于当前行中心的各个字符。
                        copyOfVertices[materialIndex][vertexIndex + 0] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 0]);
                        copyOfVertices[materialIndex][vertexIndex + 1] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 1]);
                        copyOfVertices[materialIndex][vertexIndex + 2] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 2]);
                        copyOfVertices[materialIndex][vertexIndex + 3] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 3]);

                        // 还原平移变化。
                        copyOfVertices[materialIndex][vertexIndex + 0] += centerOfLine;
                        copyOfVertices[materialIndex][vertexIndex + 1] += centerOfLine;
                        copyOfVertices[materialIndex][vertexIndex + 2] += centerOfLine;
                        copyOfVertices[materialIndex][vertexIndex + 3] += centerOfLine;
                    }
                }

                // 将更改推送到网格中
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = copyOfVertices[i];
                    m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}