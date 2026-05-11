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
        private readonly StringBuilder buffer = new StringBuilder();
        private float typewriterSpeed = 0;
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
        }

        private IEnumerator TypewriterEffect()
        {
            this.isAnimating = true;
            string fullText = this.buffer.ToString();
            int displayedLength = this.textComponent != null ? this.textComponent.text.Length : 0;

            while (displayedLength < fullText.Length)
            {
                if (this.textComponent == null)
                {
                    break;
                }

                displayedLength++;
                this.textComponent.text = fullText.Substring(0, displayedLength);
                yield return new WaitForSeconds(1f / this.typewriterSpeed);
            }

            this.isAnimating = false;
        }
    }
}
