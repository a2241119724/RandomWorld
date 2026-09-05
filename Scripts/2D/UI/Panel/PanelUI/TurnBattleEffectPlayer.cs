namespace LAB2D.UI.Panel.PanelUI
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 回合制攻击特效序列帧播放器 — Image 逐帧换 sprite，挂在目标卡片 Root 下局部坐标，
    /// 卡片无 Mask 可溢出绘制（176px > 卡宽 180 内缩放后仍醒目）且随卡片前冲联动。
    /// timeScale=0 下用 unscaledDeltaTime 驱动（战斗面板冻结大世界但演出照常），
    /// 末 30% 渐隐、播完自毁，由 TurnBattleEffectLibrary 按需 Create。
    /// </summary>
    public class TurnBattleEffectPlayer : MonoBehaviour
    {
        private const float FadeStartRatio = 0.7f;

        private Sprite[] frames;
        private Image image;
        private float fps = 12f;
        private float elapsed;

        /// <summary>
        /// 在目标卡片下生成一段序列帧特效。
        /// frames 为空/null 时不生成（调用方已保证非空，此处防御）。
        /// </summary>
        public static TurnBattleEffectPlayer Create(
            Transform cardRoot, Vector3 localPos, Sprite[] frames,
            float fps = 12f, float size = 176f, bool flipX = false, Color? tint = null)
        {
            if (cardRoot == null || frames == null || frames.Length == 0)
            {
                return null;
            }

            GameObject go = new GameObject("BattleFx");
            go.transform.SetParent(cardRoot, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.localPosition = localPos;
            rt.sizeDelta = new Vector2(size, size);

            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.sprite = frames[0];
            if (tint != null)
            {
                img.color = tint.Value;
            }

            if (flipX)
            {
                rt.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            TurnBattleEffectPlayer player = go.AddComponent<TurnBattleEffectPlayer>();
            player.frames = frames;
            player.image = img;
            player.fps = fps > 0f ? fps : 12f;
            return player;
        }

        private void Update()
        {
            this.elapsed += Time.unscaledDeltaTime;

            int frameIndex = (int)(this.elapsed * this.fps);
            if (frameIndex >= this.frames.Length)
            {
                Destroy(this.gameObject);
                return;
            }

            this.image.sprite = this.frames[frameIndex];

            // 末 30% 线性渐隐（起帧即全亮，收尾消散）
            float totalDuration = this.frames.Length / this.fps;
            float fadeStart = totalDuration * FadeStartRatio;
            if (this.elapsed > fadeStart && totalDuration > fadeStart)
            {
                Color c = this.image.color;
                c.a = Mathf.Clamp01(1f - ((this.elapsed - fadeStart) / (totalDuration - fadeStart)));
                this.image.color = c;
            }
        }
    }
}
