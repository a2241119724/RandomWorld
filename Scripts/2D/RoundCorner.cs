namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 圆角
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class RoundCorner : MaskableGraphic
    {
        /// <summary>
        /// 圆角半径
        /// </summary>
        [Range(0, 0.5f)]
        public float Radius = 0.5f;

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.material = this.GenerateMaterial(ResourceManager.Instance.GetShader("RoundCorner"));
            this.material.SetFloat("_Width", this.rectTransform.rect.width);
            this.material.SetFloat("_Height", this.rectTransform.rect.height);
            this.material.SetFloat("_RoundRadius", this.Radius);
        }

        /// <inheritdoc/>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            this.material.SetFloat("_Width", this.rectTransform.rect.width);
            this.material.SetFloat("_Height", this.rectTransform.rect.height);
        }

        /// <summary>
        /// 根据shader创建用于屏幕特效的材质
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>Material</returns>
        protected UnityEngine.Material GenerateMaterial(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            if (shader.isSupported == false)
            {
                return null;
            }

            UnityEngine.Material material = new (shader);
            material.hideFlags = HideFlags.DontSave;

            if (material)
            {
                return material;
            }

            return null;
        }
    }
}