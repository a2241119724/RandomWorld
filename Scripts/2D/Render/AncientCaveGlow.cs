namespace LAB2D.Render
{
    using LAB2D.Domain.Gameplay.AncientCave;
    using UnityEngine;

    /// <summary>
    /// 上古洞府洞口视觉 — 揭示后浮现的遗迹标识（M4 包 4 地图兴趣点）。
    /// 纹理运行时程序化生成（LingVeinGlow 同款管线）：中心暗洞（深棕黑径向衰减）
    /// + 外圈土金光环 + 环上微光尘；揭示时淡入（进度走 Domain RevealProgress 纯函数，
    /// 读档恢复的已揭示洞府跳过淡入直接全显）；极慢呼吸（静态遗迹不自转）。
    /// 零 PNG 资产依赖。sorting 贴地装饰（-995 之上 +6，略高于毒雾圈）。
    /// </summary>
    public class AncientCaveGlow : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const float PixelsPerUnit = 64f;

        /// <summary>基准直径 4 世界单位（洞口约 4 格可见范围）。</summary>
        private const float BaseScale = 2f;

        private const float BreathFrequency = 0.35f; // rad/s（周期约 18s 的极慢呼吸）
        private const float BreathAmount = 0.03f;

        private static readonly Color DarkMouthColor = new Color(0.16f, 0.10f, 0.06f);
        private static readonly Color GoldRimColor = new Color(0.85f, 0.66f, 0.28f);

        private static Sprite cached;
        private SpriteRenderer spriteRenderer;
        private float fadeElapsed = -1f;
        private bool fadeDone;
        private bool explored;

        /// <summary>共享洞口 Sprite（@64 PPU → 2 单位直径，调用方缩放）。</summary>
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

                    // 中心暗洞：径向平方衰减（洞口深处最黑）
                    float dark = Mathf.Pow(Mathf.Clamp01(1f - d / 0.42f), 1.6f);

                    // 外圈土金光环（两道，遗迹符环感）
                    float rim = Ring(d, 0.46f) * 0.85f + Ring(d, 0.58f) * 0.45f;

                    // 环上微光尘 ×3（呼吸时明暗可见）
                    rim += Spot(ang, d, 60f) * 0.30f;
                    rim += Spot(ang, d, 180f) * 0.30f;
                    rim += Spot(ang, d, 300f) * 0.30f;

                    // 暗洞用暗棕黑、光环用土金，按强度混合
                    float goldWeight = rim / Mathf.Max(rim + dark, 0.0001f);
                    var color = new Color(
                        Mathf.Lerp(DarkMouthColor.r, GoldRimColor.r, goldWeight),
                        Mathf.Lerp(DarkMouthColor.g, GoldRimColor.g, goldWeight),
                        Mathf.Lerp(DarkMouthColor.b, GoldRimColor.b, goldWeight),
                        Mathf.Min(dark * 0.9f + rim, 0.85f));
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            cached = Sprite.Create(
                tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return cached;
        }

        private static float Ring(float d, float radius)
        {
            const float width = 0.05f;
            float t = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / width);
            return t * t;
        }

        private static float Spot(float angleDeg, float d, float spotAngle)
        {
            float dAng = Mathf.Abs(Mathf.DeltaAngle(angleDeg * Mathf.Rad2Deg, spotAngle));
            float angular = Mathf.Clamp01(1f - dAng / 25f);
            float radial = Mathf.Clamp01(1f - Mathf.Abs(d - 0.52f) / 0.10f);
            return angular * angular * radial;
        }

        private void Awake()
        {
            this.spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = GetOrCreateSprite();
            this.spriteRenderer.sortingOrder = WorldYSortManager.BottomLayerOrder + 6; // -994，略高于毒雾圈
            this.transform.localScale = new Vector3(BaseScale, BaseScale, 1f);
            this.spriteRenderer.color = new Color(1f, 1f, 1f, 0f); // 创建即隐身，等淡入或直接显示
        }

        /// <summary>读档恢复的已揭示洞府：跳过淡入直接全显。</summary>
        public void ShowImmediately()
        {
            this.fadeDone = true;
            this.spriteRenderer.color = Color.white;
        }

        /// <summary>揭示淡入：进度走 Domain RevealProgress 纯函数。</summary>
        public void BeginFade()
        {
            if (this.fadeDone)
            {
                return;
            }

            this.fadeElapsed = 0f;
        }

        /// <summary>探索完毕枯竭：灰暗半透明 + 停呼吸（灵气采尽的死寂遗迹）。</summary>
        public void MarkExplored()
        {
            this.explored = true;
            this.fadeDone = true;
            this.spriteRenderer.color = new Color(0.55f, 0.55f, 0.55f, 0.45f);
        }

        private void Update()
        {
            if (this.explored)
            {
                return; // 枯竭后完全静止
            }

            if (this.fadeDone)
            {
                // 极慢呼吸（静态遗迹的神秘微光）
                float s = BaseScale * (1f + BreathAmount * Mathf.Sin(Time.time * BreathFrequency));
                this.transform.localScale = new Vector3(s, s, 1f);
                return;
            }

            if (this.fadeElapsed < 0f)
            {
                return; // 尚未开始淡入（保持隐身）
            }

            this.fadeElapsed += Time.deltaTime;
            float progress = AncientCaveRuleService.RevealProgress(this.fadeElapsed);
            var c = this.spriteRenderer.color;
            c.a = progress;
            this.spriteRenderer.color = c;

            if (progress >= 1f)
            {
                this.fadeDone = true;
            }
        }
    }
}
