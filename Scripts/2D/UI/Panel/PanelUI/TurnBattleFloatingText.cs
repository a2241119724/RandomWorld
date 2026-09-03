namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D.AI.Dialogue.LLM;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 回合制战斗面板内飘字 — 挂在单位卡片上方的局部坐标上浮渐隐。
    /// timeScale=0 下用 unscaledDeltaTime 驱动（战斗面板冻结大世界但演出照常），
    /// 动画播完自毁，由 TurnBattleUI 按需 Create。
    /// </summary>
    public class TurnBattleFloatingText : MonoBehaviour
    {
        private const float Duration = 0.9f;
        private const float RisePixels = 42f;

        private Text label;
        private Vector3 startPosition;
        private float elapsed;

        /// <summary>在目标卡片上方生成一条飘字。</summary>
        public static TurnBattleFloatingText Create(Transform cardRoot, string text, Color color, int fontSize = 28)
        {
            GameObject go = new GameObject("BattleFloatText");
            go.transform.SetParent(cardRoot, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.localPosition = new Vector3(0f, 86f, 0f);
            rt.sizeDelta = new Vector2(320f, 44f);

            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = UIFontConfig.GetFont();
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            TurnBattleFloatingText floating = go.AddComponent<TurnBattleFloatingText>();
            floating.label = txt;
            floating.startPosition = rt.localPosition;
            return floating;
        }

        private void Update()
        {
            this.elapsed += Time.unscaledDeltaTime;
            float t = this.elapsed / Duration;
            if (t >= 1f)
            {
                Destroy(this.gameObject);
                return;
            }

            this.transform.localPosition = this.startPosition + new Vector3(0f, RisePixels * t, 0f);
            Color c = this.label.color;
            c.a = 1f - (t * t);
            this.label.color = c;
        }
    }
}
