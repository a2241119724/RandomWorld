namespace LAB2D.Render
{
    using UnityEngine;

    /// <summary>
    /// 危险区毒雾圈 — 危险区的可见标识（M4 包 4 地图兴趣点）。
    /// 纹理运行时程序化生成（LingVeinGlow 同款管线）：128×128 暗紫雾场，
    /// 边缘环带最浓（圈界可见）+ 内部角向谐波絮状雾团 + 平缓中心淡雾；
    /// 极慢自转 + 轻微呼吸（雾在飘）。常显——危险必须可见，与洞府的揭示惊喜互补。
    /// 零 PNG 资产依赖。sorting 贴地装饰（-995），不参与 y 排序。
    /// </summary>
    public class DangerZoneGlow : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const float PixelsPerUnit = 64f;

        /// <summary>基准缩放系数：@64 PPU sprite 直径 2 单位，scale = 半径格数 → 圆面直径 2×半径格。</summary>
        private const float BaseScale = 1f;

        private const float RotationSpeed = 3f;    // °/s（雾团慢速漂移感）
        private const float BreathFrequency = 0.5f; // rad/s（周期约 12.6s 的缓呼吸）
        private const float BreathAmount = 0.04f;

        private static readonly Color PoisonColor = new Color(0.55f, 0.22f, 0.72f);

        private static Sprite cached;
        private SpriteRenderer spriteRenderer;
        private float elapsed;

        /// <summary>毒雾圈半径（格）——DangerZoneManager 撒点时按区半径设置。</summary>
        public float RadiusCells { get; set; } = 12f;

        /// <summary>共享毒雾 Sprite（调用方按半径缩放）。</summary>
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
                    float ang = Mathf.Atan2(dy, dx);

                    // 中心淡雾 → 中段渐浓的径向基线
                    float alpha = 0.10f + 0.14f * Mathf.SmoothStep(0.2f, 0.85f, d);

                    // 边缘环带（圈界可见的关键）：0.82~0.95 浓环后硬截止
                    alpha += Ring(d, 0.88f) * 0.20f;

                    // 角向低频谐波絮状雾团（k=3/5/7 叠加避免轴对称感），中段最强
                    alpha += FogLobe(d) * (0.05f
                        + 0.05f * Mathf.Sin(3f * ang + 0.7f)
                        + 0.04f * Mathf.Sin(5f * ang + 2.1f)
                        + 0.03f * Mathf.Sin(7f * ang + 4.4f));

                    tex.SetPixel(x, y, new Color(PoisonColor.r, PoisonColor.g, PoisonColor.b, Mathf.Clamp01(alpha)));
                }
            }

            tex.Apply();
            cached = Sprite.Create(
                tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return cached;
        }

        private static float Ring(float d, float radius)
        {
            const float width = 0.06f;
            float t = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / width);
            return t * t;
        }

        /// <summary>絮状雾团的径向包络：中段（0.3~0.8）最强，中心与边缘归零。</summary>
        private static float FogLobe(float d)
        {
            return Mathf.Clamp01(1f - Mathf.Abs(d - 0.55f) / 0.35f);
        }

        private void Awake()
        {
            this.spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = GetOrCreateSprite();
            this.spriteRenderer.sortingOrder = WorldYSortManager.BottomLayerOrder + 5; // -995，贴地装饰
            // 缩放不在 Awake 设：AddComponent 后才注入 RadiusCells，Update 首帧即按实际值落位
        }

        private void Update()
        {
            this.elapsed += Time.deltaTime;
            this.transform.rotation = Quaternion.Euler(0f, 0f, this.elapsed * RotationSpeed);
            float s = BaseScale * this.RadiusCells * (1f + BreathAmount * Mathf.Sin(this.elapsed * BreathFrequency));
            this.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
