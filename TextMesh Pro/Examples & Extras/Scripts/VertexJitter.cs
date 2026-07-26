using System.Collections;
using UnityEngine;


namespace TMPro.Examples
{

    public class VertexJitter : MonoBehaviour
    {

        public float AngleMultiplier = 1.0f;
        public float SpeedMultiplier = 1.0f;
        public float CurveScale = 1.0f;

        private TMP_Text m_TextComponent;
        private bool hasTextChanged;

        /// <summary>
        /// 用于保存预计算动画数据的结构体。
        /// </summary>
        private struct VertexAnim
        {
            public float angleRange;
            public float angle;
            public float speed;
        }

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
            if (obj == m_TextComponent)
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

            int loopCount = 0;
            hasTextChanged = true;

            // 创建一个数组，其中包含预先计算的一批字符的角度范围和速度。
            VertexAnim[] vertexAnim = new VertexAnim[1024];
            for (int i = 0; i < 1024; i++)
            {
                vertexAnim[i].angleRange = Random.Range(10f, 25f);
                vertexAnim[i].speed = Random.Range(1f, 3f);
            }

            // 缓存文本对象的顶点数据，因为抖动特效应用于字符的原始位置。
            TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            while (true)
            {
                // 如果文本已更改，获取顶点数据的新副本。
                if (hasTextChanged)
                {
                    // 更新文本对象的顶点数据副本。
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                    hasTextChanged = false;
                }

                int characterCount = textInfo.characterCount;

                // 如果没有字符，则屈服并等待添加一些文本
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }


                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    // 跳过不可见的字符，因此没有可操作的几何体。
                    if (!charInfo.isVisible)
                        continue;

                    // 检索给定字符的预计算动画数据。
                    VertexAnim vertAnim = vertexAnim[i];

                    // 获取当前字符使用的材质索引。
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // 获取此文本元素使用的第一个顶点索引。
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // 获取此文本元素（字符或精灵）使用的网格的缓存顶点。
                    Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                    // 确定每个字符在基线上的中心点。
                    //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                    // 确定每个字符的中心点。
                    Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                    // 需要将每个四边形的 4 个顶点平移到与字符/基线中心对齐。
                    // 这是必需的，以便矩阵 TRS 作用于每个字符的原点。
                    Vector3 offset = charMidBasline;

                    Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                    destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                    destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                    destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                    vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(loopCount / 25f * vertAnim.speed, 1f));
                    Vector3 jitterOffset = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.25f, .25f), 0);

                    matrix = Matrix4x4.TRS(jitterOffset * CurveScale, Quaternion.Euler(0, 0, Random.Range(-5f, 5f) * AngleMultiplier), Vector3.one);

                    destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                    destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                    destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                    destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                    destinationVertices[vertexIndex + 0] += offset;
                    destinationVertices[vertexIndex + 1] += offset;
                    destinationVertices[vertexIndex + 2] += offset;
                    destinationVertices[vertexIndex + 3] += offset;

                    vertexAnim[i] = vertAnim;
                }

                // 将更改推送到网格中
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                loopCount += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}