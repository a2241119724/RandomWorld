namespace LAB2D.Render
{
    using UnityEngine;

    /// <summary>
    /// 灵脉光环 — 灵脉点的可见标识（M4 灵气环境）。
    /// 纹理运行时程序化生成（ShadowTextureFactory 同款）：128×128 径向平方衰减光晕 +
    /// 三道同心环 + 中环斜向亮斑（让自转可见），灵气青色；慢速自转 + 呼吸缩放。零 PNG 资产依赖。
    /// sorting：贴地装饰（-995），高于角色软影(-999)、低于角色(0+)，不参与 y 排序。
    /// </summary>
    public class LingVeinGlow : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const float PixelsPerUnit = 64f;

        /// <summary>基础直径 8 世界单位（半径 4 格的可见提示区，实际增幅半径 10 格）。</summary>
        private const float BaseScale = 4f;

        private const float RotationSpeed = 8f;     // °/s
        private const float BreathFrequency = 1.2f; // rad/s（周期约 5.2s 的慢呼吸）
        private const float BreathAmount = 0.06f;

        private static readonly Color QiColor = new Color(0.45f, 0.9f, 1.0f);

        private static Sprite cached;
        private SpriteRenderer spriteRenderer;
        private float elapsed;

        /// <summary>共享光环 Sprite（@64 PPU → 2 单位直径，调用方缩放）。</summary>
        public static Sprite GetOrCreateSprite()
        {
            if (cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            float c = (TextureSize - 1) / 2f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / c;
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                    // 径向平方衰减光晕
                    float alpha = 0.22f * Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);

                    // 三道同心环
                    alpha += Ring(d, 0.38f) * 0.50f;
                    alpha += Ring(d, 0.62f) * 0.42f;
                    alpha += Ring(d, 0.86f) * 0.34f;

                    // 中环斜向亮斑 ×4（自转可见的关键）
                    alpha += Spot(ang, d, 45f) * 0.35f;
                    alpha += Spot(ang, d, 135f) * 0.35f;
                    alpha += Spot(ang, d, 225f) * 0.35f;
                    alpha += Spot(ang, d, 315f) * 0.35f;

                    tex.SetPixel(x, y, new Color(QiColor.r, QiColor.g, QiColor.b, Mathf.Min(alpha, 0.8f)));
                }
            }

            tex.Apply();
            cached = Sprite.Create(
                tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return cached;
        }

        private static float Ring(float d, float radius)
        {
            const float width = 0.045f;
            float t = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / width);
            return t * t;
        }

        private static float Spot(float angleDeg, float d, float spotAngle)
        {
            float dAng = Mathf.Abs(Mathf.DeltaAngle(angleDeg, spotAngle));
            float angular = Mathf.Clamp01(1f - dAng / 30f);
            float radial = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) / 0.12f);
            return angular * angular * radial;
        }

        private void Awake()
        {
            this.spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = GetOrCreateSprite();
            this.spriteRenderer.sortingOrder = WorldYSortManager.BottomLayerOrder + 5; // -995，贴地装饰
            this.transform.localScale = new Vector3(BaseScale, BaseScale, 1f);
        }

        private void Update()
        {
            this.elapsed += Time.deltaTime;
            this.transform.rotation = Quaternion.Euler(0f, 0f, this.elapsed * RotationSpeed);
            float s = BaseScale * (1f + BreathAmount * Mathf.Sin(this.elapsed * BreathFrequency));
            this.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
