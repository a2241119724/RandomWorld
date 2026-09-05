namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 圆角 — MaskableGraphic，程序化圆角矩形。
    /// 尺寸/半径经顶点 uv1/uv2 传给 shader（不占顶点色，保留 tint/TipUI 淡出链路），
    /// 全部实例共享一份静态材质 → UGUI 合批（原实现每实例 new Material，44+ 实例破坏合批）。
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class RoundCorner : MaskableGraphic
    {
        // 序列化迁移：场景/prefab 已存的 "Radius" 字段值迁到私有 backing field（漏迁移=全 UI 半径回落 0.5）
        [UnityEngine.Serialization.FormerlySerializedAs("Radius")]
        [SerializeField, Range(0, 0.5f)]
        private float radius = 0.5f;

        /// <summary>
        /// 圆角半径（0~0.5，相对高度的比例）
        /// </summary>
        public float Radius
        {
            get
            {
                return this.radius;
            }

            set
            {
                if (Mathf.Approximately(this.radius, value))
                {
                    return;
                }

                this.radius = value;
                if (this.IsActive())
                {
                    this.SetVerticesDirty(); // Editor 工具（UIOneClickNormalizer）直接赋值后立即重建
                }
            }
        }

        private static Material sharedRoundMaterial;

        /// <summary>
        /// 全部实例共享的圆角材质（懒加载；编辑器下 ResourceManager 不可用时 Shader.Find 兜底）
        /// </summary>
        private static Material SharedRoundMaterial
        {
            get
            {
                if (sharedRoundMaterial == null)
                {
                    // 直接 Shader.Find：Singleton.Instance 是懒创建语义（!= null 恒真），
                    // 编辑模式 OnEnable 反而会强拉 ResourceManager→LoadPrefabs 加载全部 prefab；
                    // shader 在 Resources/Shader/ 下全量打包，Shader.Find 编辑器/运行时均可用
                    Shader shader = Shader.Find("Custom/RoundCorner");
                    if (shader == null)
                    {
                        return null;
                    }

                    sharedRoundMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                }

                return sharedRoundMaterial;
            }
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            Material mat = SharedRoundMaterial;
            if (mat != null)
            {
                this.material = mat;
            }
        }

        /// <inheritdoc/>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (this.IsActive())
            {
                this.SetVerticesDirty(); // 尺寸变化 → w/h/radiusPx 重编码进 mesh
            }
        }

        /// <summary>
        /// 程序化圆角矩形 mesh：圆角参数打包进 uv0.zw（z=aspect 宽高比、w=radius 相对高度的比例），
        /// 顶点色承载 color（tint 淡出链路）。raycast 按 rectTransform.rect 判定，与 mesh 无关。
        /// 注意：uv0 是 Canvas 唯一无条件保留的顶点通道——曾用 uv1/uv2 传参，
        /// 但 Canvas.additionalShaderChannels 未启用 TexCoord1/2 时批处理 mesh 里这些通道不可用，
        /// 参数部分丢失会让 SDF 退化成全透明（一个像素都不渲染）。
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = this.GetPixelAdjustedRect();
            float w = r.width;
            float h = r.height;

            // aspect + 归一化半径（钳到短边一半，避免半径越过矩形中心）
            float aspect = h > 0.0f ? w / h : 0.0f;
            float radiusNorm = Mathf.Min(this.radius, 0.5f * Mathf.Min(1f, aspect));

            // UGUI 1.0.0 的 AddVert 无 (pos,color,uv0,uv1,uv2) 5 参重载（uv 通道是 Vector4），
            // 经 UIVertex 结构体一次填齐 position/color/uv0。
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = this.color;

            // Image 标准顶点序：LL, UL, UR, LR
            vertex.position = new Vector3(r.xMin, r.yMin);
            vertex.uv0 = new Vector4(0, 0, aspect, radiusNorm);
            vh.AddVert(vertex);
            vertex.position = new Vector3(r.xMin, r.yMax);
            vertex.uv0 = new Vector4(0, 1, aspect, radiusNorm);
            vh.AddVert(vertex);
            vertex.position = new Vector3(r.xMax, r.yMax);
            vertex.uv0 = new Vector4(1, 1, aspect, radiusNorm);
            vh.AddVert(vertex);
            vertex.position = new Vector3(r.xMax, r.yMin);
            vertex.uv0 = new Vector4(1, 0, aspect, radiusNorm);
            vh.AddVert(vertex);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 拖滑条即时刷新圆角
        /// </summary>
        private void OnValidate()
        {
            if (this.IsActive())
            {
                this.SetVerticesDirty();
            }
        }
#endif
    }
}
