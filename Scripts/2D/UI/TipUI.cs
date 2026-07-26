namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 提示 UI
    /// </summary>
    public class TipUI : MonoBehaviour
    {
        private float colorAlpha = 1; // 透明度
        private RoundCorner roundCorner; // 警告信息背景
        private Text content; // 警告信息文本
        private float recordTime = 0.0f; // 记录时间

        /// <summary>
        /// 设置提示信息
        /// </summary>
        /// <param name="text">信息</param>
        public void SetText(string text)
        {
            this.content.text = text;
        }

        public void Awake()
        {
            this.roundCorner = this.GetComponent<RoundCorner>();
            if (this.roundCorner == null)
            {
                AWorkerTask.LogProvider("image Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.content = this.transform.Find("Content").GetComponent<Text>();
            if (this.content == null)
            {
                AWorkerTask.LogProvider("content Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Color32透明度0~1
        /// Color透明度0~255
        /// 由于可能受到Time.timeScale的影响
        /// 不能使用FixedUpdate,Time.deltaTime...
        /// </summary>
        public void Update()
        {
            this.recordTime += Time.deltaTime;

            // 两秒后淡出
            if (this.recordTime >= 5.0f)
            {
                this.FadeOut();
            }
            else
            {
                // 放大
                this.transform.localScale = Quaternion.Lerp(Quaternion.Euler(this.transform.localScale), Quaternion.Euler(1, 1, 1), 0.2f).eulerAngles;
            }
        }

        /// <summary>
        /// 淡出(透明度减小)
        /// </summary>
        private void FadeOut()
        {
            Color color = this.roundCorner.color;
            this.roundCorner.color = new Color(color.r, color.g, color.b, this.colorAlpha);
            if (this.roundCorner.color == null)
            {
                AWorkerTask.LogProvider("image.color assign resource Error!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            color = this.content.color;
            this.content.color = new Color(color.r, color.g, color.b, this.colorAlpha);
            if (this.content.color == null)
            {
                AWorkerTask.LogProvider("content.color assign resource Error!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.colorAlpha -= 0.02f;
            if (this.colorAlpha <= 0)
            {
                Destroy(this.gameObject);
            }
        }
    }
}