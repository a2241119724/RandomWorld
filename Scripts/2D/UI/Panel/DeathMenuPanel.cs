namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Death screen overlay created programmatically at runtime — no prefab required.
    /// Displays a full-screen dark overlay with "YOU DIED" title, respawn countdown,
    /// and death counter. Replaces the ABasePanel-based approach to avoid
    /// ResourceManager prefab-lookup errors.
    /// </summary>
    public class DeathMenuPanel : Singleton<DeathMenuPanel>
    {
        private GameObject panelRoot;
        private Text countdownText;
        private Text deathCountText;

        /// <summary>
        /// The root GameObject for this overlay. Exposed for compatibility with
        /// DeathPenaltyManager's null-check pattern.
        /// </summary>
        public GameObject Panel
        {
            get { return this.panelRoot; }
        }

        /// <summary>
        /// Show the death screen overlay.
        /// </summary>
        public void Show(int deathCount, int respawnSeconds)
        {
            if (this.panelRoot == null)
            {
                this.BuildOverlay();
            }

            if (this.panelRoot == null)
            {
                return;
            }

            this.panelRoot.SetActive(true);
            if (this.deathCountText != null)
            {
                this.deathCountText.text = $"Deaths: {deathCount}";
            }

            if (this.countdownText != null)
            {
                this.countdownText.text = $"Respawning in {respawnSeconds}s...";
            }
        }

        /// <summary>
        /// Update the countdown text each frame during respawn.
        /// </summary>
        public void UpdateCountdown(int secondsRemaining)
        {
            if (this.countdownText != null)
            {
                this.countdownText.text = $"Respawning in {secondsRemaining}s...";
            }
        }

        /// <summary>
        /// Hide the death screen overlay.
        /// </summary>
        public void Hide()
        {
            if (this.panelRoot != null)
            {
                this.panelRoot.SetActive(false);
            }
        }

        private void BuildOverlay()
        {
            GameObject uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG);
            if (uiRoot == null)
            {
                return;
            }

            // Root object
            this.panelRoot = new GameObject("DeathMenu");
            this.panelRoot.transform.SetParent(uiRoot.transform, false);

            RectTransform rootRt = this.panelRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Full-screen dark background
            Image bg = this.panelRoot.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.08f, 0.06f, 0.88f);

            // Center text group
            GameObject textGroup = new GameObject("TextGroup");
            textGroup.transform.SetParent(this.panelRoot.transform, false);
            RectTransform tgRt = textGroup.AddComponent<RectTransform>();
            tgRt.anchorMin = new Vector2(0.5f, 0.5f);
            tgRt.anchorMax = new Vector2(0.5f, 0.5f);
            tgRt.sizeDelta = new Vector2(500, 260);
            tgRt.anchoredPosition = Vector2.zero;

            Font font = this.GetDefaultFont();
            if (font == null)
            {
                return;
            }

            // "YOU DIED" title
            this.deathCountText = null; // placeholder, we'll create title first
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(textGroup.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "YOU DIED";
            titleText.font = font;
            titleText.fontSize = 52;
            titleText.color = PixelUITheme.DeathTitle;
            titleText.alignment = TextAnchor.MiddleCenter;
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0.68f);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // Respawn countdown
            GameObject cdGo = new GameObject("Countdown");
            cdGo.transform.SetParent(textGroup.transform, false);
            this.countdownText = cdGo.AddComponent<Text>();
            this.countdownText.font = font;
            this.countdownText.fontSize = 28;
            this.countdownText.color = PixelUITheme.DeathText;
            this.countdownText.alignment = TextAnchor.MiddleCenter;
            RectTransform cdRt = cdGo.GetComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(0, 0.36f);
            cdRt.anchorMax = new Vector2(1, 0.64f);
            cdRt.offsetMin = Vector2.zero;
            cdRt.offsetMax = Vector2.zero;

            // Death counter
            GameObject dcGo = new GameObject("DeathCount");
            dcGo.transform.SetParent(textGroup.transform, false);
            this.deathCountText = dcGo.AddComponent<Text>();
            this.deathCountText.font = font;
            this.deathCountText.fontSize = 22;
            this.deathCountText.color = PixelUITheme.DeathCount;
            this.deathCountText.alignment = TextAnchor.MiddleCenter;
            RectTransform dcRt = dcGo.GetComponent<RectTransform>();
            dcRt.anchorMin = new Vector2(0, 0.04f);
            dcRt.anchorMax = new Vector2(1, 0.32f);
            dcRt.offsetMin = Vector2.zero;
            dcRt.offsetMax = Vector2.zero;

            this.panelRoot.SetActive(false);
        }

        private Font GetDefaultFont()
        {
            Font font = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
