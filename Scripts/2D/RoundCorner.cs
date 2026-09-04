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
                if (this.IsActive)
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
                    Shader shader = ResourceManager.Instance != null
                        ? ResourceManager.Instance.GetShader("RoundCorner")
                        : null;
                    if (shader == null)
                    {
                        shader = Shader.Find("Custom/RoundCorner");
                    }

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
            if (this.IsActive)
            {
                this.SetVerticesDirty(); // 尺寸变化 → w/h/radiusPx 重编码进 mesh
            }
        }

        /// <summary>
        /// 程序化圆角矩形 mesh：uv0=标准 0-1、uv1=(w,h) 像素、uv2.x=radiusPx，
        /// 顶点色承载 color（tint 淡出链路）。raycast 按 rectTransform.rect 判定，与 mesh 无关。
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = this.GetPixelAdjustedRect();
            float w = r.width;
            float h = r.height;
            float radiusPx = Mathf.Min(this.radius * h, Mathf.Min(w, h) * 0.5f);
            Vector2 size = new(w, h);
            Vector2 rad = new(radiusPx, 0f);
            Color32 c32 = this.color;

            // Image 标准顶点序：LL, UL, UR, LR
            vh.AddVert(new Vector3(r.xMin, r.yMin), c32, new Vector2(0, 0), size, rad);
            vh.AddVert(new Vector3(r.xMin, r.yMax), c32, new Vector2(0, 1), size, rad);
            vh.AddVert(new Vector3(r.xMax, r.yMax), c32, new Vector2(1, 1), size, rad);
            vh.AddVert(new Vector3(r.xMax, r.yMin), c32, new Vector2(1, 0), size, rad);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 拖滑条即时刷新圆角
        /// </summary>
        private void OnValidate()
        {
            if (this.IsActive)
            {
                this.SetVerticesDirty();
            }
        }
#endif
    }
}
