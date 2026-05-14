namespace LAB2D
{
    using System.Collections;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 流式文本视图，支持打字机效果
    /// </summary>
    public class StreamingTextView : MonoBehaviour
    {
        private Text textComponent;
        private LayoutElement bubbleLayoutElement;
        private LayoutElement textLayoutElement;
        private RectTransform textRectTransform;
        private RectTransform rebuildRoot;
        private readonly StringBuilder buffer = new StringBuilder();
        private float typewriterSpeed = 0;
        private float minBubbleWidth = 96f;
        private float maxBubbleWidth = 560f;
        private float horizontalPadding = 32f;
        private bool isAnimating;

        /// <summary>
        /// 打字速度（字符/秒），0 = 即时显示
        /// </summary>
        public float TypewriterSpeed
        {
            get { return this.typewriterSpeed; }
            set { this.typewriterSpeed = value; }
        }

        /// <summary>
        /// 获取当前完整文本
        /// </summary>
        public string FullText
        {
            get { return this.buffer.ToString(); }
        }

        public void Awake()
        {
            this.textComponent = this.GetComponentInChildren<Text>();
        }

        /// <summary>
        /// 设置聊天气泡布局刷新目标。
        /// </summary>
        public void ConfigureLayout(
            LayoutElement bubbleLayout,
            LayoutElement textLayout,
            RectTransform textRect,
            RectTransform root,
            float minWidth,
            float maxWidth,
            float padding)
        {
            this.bubbleLayoutElement = bubbleLayout;
            this.textLayoutElement = textLayout;
            this.textRectTransform = textRect;
            this.rebuildRoot = root;
            this.minBubbleWidth = minWidth;
            this.maxBubbleWidth = maxWidth;
            this.horizontalPadding = padding;
            this.RefreshLayout();
        }

        /// <summary>
        /// 追加 token
        /// </summary>
        public void AppendToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            this.buffer.Append(token);

            if (this.textComponent == null)
            {
                return;
            }

            if (this.typewriterSpeed <= 0)
            {
                this.textComponent.text = this.buffer.ToString();
                this.RefreshLayout();
            }
            else if (!this.isAnimating)
            {
                this.StartCoroutine(this.TypewriterEffect());
            }
        }

        /// <summary>
        /// 清空文本
        /// </summary>
        public void Clear()
        {
            this.buffer.Clear();
            if (this.textComponent != null)
            {
                this.textComponent.text = string.Empty;
            }

            this.RefreshLayout();
        }

        private IEnumerator TypewriterEffect()
        {
            this.isAnimating = true;
            int displayedLength = this.textComponent != null ? this.textComponent.text.Length : 0;

            while (displayedLength < this.buffer.Length)
            {
                if (this.textComponent == null)
                {
                    break;
                }

                displayedLength++;
                this.textComponent.text = this.buffer.ToString().Substring(0, displayedLength);
                this.RefreshLayout();
                yield return new WaitForSeconds(1f / this.typewriterSpeed);
            }

            this.isAnimating = false;
        }

        private void RefreshLayout()
        {
            if (this.textComponent == null)
            {
                this.textComponent = this.GetComponentInChildren<Text>();
                if (this.textComponent == null)
                {
                    return;
                }
            }

            float preferredTextWidth = string.IsNullOrEmpty(this.textComponent.text)
                ? 0f
                : this.textComponent.preferredWidth;
            float bubbleWidth = Mathf.Clamp(
                preferredTextWidth + this.horizontalPadding,
                this.minBubbleWidth,
                this.maxBubbleWidth);
            float textWidth = Mathf.Max(1f, bubbleWidth - this.horizontalPadding);

            if (this.bubbleLayoutElement != null)
            {
                this.bubbleLayoutElement.minWidth = this.minBubbleWidth;
                this.bubbleLayoutElement.preferredWidth = bubbleWidth;
            }

            if (this.textLayoutElement != null)
            {
                this.textLayoutElement.minWidth = textWidth;
                this.textLayoutElement.preferredWidth = textWidth;
            }

            if (this.textRectTransform != null)
            {
                this.textRectTransform.sizeDelta = new Vector2(textWidth, this.textRectTransform.sizeDelta.y);
            }

            if (this.rebuildRoot != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(this.rebuildRoot);
            }
        }
    }
}
