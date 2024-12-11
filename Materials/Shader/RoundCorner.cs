using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    [ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer), typeof(RectTransform)), DisallowMultipleComponent]
    [AddComponentMenu("LAB/RoundCorner (Unity UI Canvas)")]
    public class RoundCorner : MaskableGraphic
    {

        //Inspector面板上直接拖入  
        public Shader shader = null;
        [Range(0, 0.5f)] public float radius = 0.5f;

        protected override void Start()
        {
            base.Start();
            material = GenerateMaterial(shader);
            material.SetFloat("_Width", rectTransform.rect.width);
            material.SetFloat("_Height", rectTransform.rect.height);
        }


        private void Update()
        {
            material.SetFloat("_RoundRadius", radius);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            material.SetFloat("_Width", rectTransform.rect.width);
            material.SetFloat("_Height", rectTransform.rect.height);
        }

        //根据shader创建用于屏幕特效的材质
        protected UnityEngine.Material GenerateMaterial(Shader shader)
        {
            if (shader == null)
                return null;

            if (shader.isSupported == false)
                return null;
            UnityEngine.Material material = new UnityEngine.Material(shader);
            material.hideFlags = HideFlags.DontSave;

            if (material)
                return material;

            return null;
        }
    }
}