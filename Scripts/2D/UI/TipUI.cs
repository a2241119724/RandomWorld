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
                // 放大（收敛后 snap 到 1 并停止写入——localScale setter 无值比较，
                // 原实现收敛后仍每帧写，持续脏化所在 Canvas 直至 5 秒淡出结束）
                Vector3 scale = this.transform.localScale;
                if (scale.x < 0.999f || scale.y < 0.999f || scale.z < 0.999f)
                {
                    this.transform.localScale = Quaternion.Lerp(Quaternion.Euler(scale), Quaternion.Euler(1, 1, 1), 0.2f).eulerAngles;
                }
                else if (scale != Vector3.one)
                {
                    this.transform.localScale = Vector3.one;
                }
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

            // 60fps 基准等速率淡出（原实现按帧递减，144fps 比 30fps 快 4.8 倍）
            this.colorAlpha -= 0.02f * 60f * Time.deltaTime;
            if (this.colorAlpha <= 0)
            {
                Destroy(this.gameObject);
            }
        }
    }
}