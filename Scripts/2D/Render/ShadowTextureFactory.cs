namespace LAB2D.Render
{
    using UnityEngine;

    /// <summary>
    /// 角色椭圆软影纹理工厂 — 64x64 径向渐变（中心 alpha 0.4 → 边缘 0，平方衰减），
    /// 纹理与 Sprite 均静态缓存，全角色共享一份。椭圆形态由 SpriteRenderer 的
    /// localScale 压扁实现（x0.9/y0.45），纹理本身为正圆。
    /// </summary>
    public static class ShadowTextureFactory
    {
        private const int Size = 64;
        private const float MaxAlpha = 0.4f;

        private static Sprite cached;

        /// <summary>
        /// 获取共享的软影 Sprite（@64 PPU → 1 单位直径正圆，调用方压扁成椭圆）。
        /// </summary>
        public static Sprite GetOrCreate()
        {
            if (cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, // 软影边缘平滑（非像素风主体，无需 Point）
            };
            float c = (Size - 1) / 2f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, MaxAlpha * a * a));
                }
            }

            tex.Apply();
            cached = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            return cached;
        }
    }
}
