using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class TipUI : MonoBehaviour
    {
        private float colorAlpha = 1; // 透明度
        private RoundCorner roundCorner; // 警告信息背景
        private Text content; // 警告信息文本
        private float _recordTime = 0.0f; // 记录时间

        void Awake()
        {
            roundCorner = GetComponent<RoundCorner>();
            if (roundCorner == null)
            {
                LogManager.Instance.log("image Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            content = transform.Find("Content").GetComponent<Text>();
            if (content == null)
            {
                LogManager.Instance.log("content Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Color32透明度0~1
        /// Color透明度0~255
        /// 由于可能受到Time.timeScale的影响
        /// 不能使用FixedUpdate,Time.deltaTime...
        /// </summary>
        void Update()
        {
            _recordTime += Time.deltaTime;
            if (_recordTime >= 2.0f) // 两秒后淡出
            {
                fadeOut();
            }
            else {
                // 放大
                transform.localScale = Quaternion.Lerp(Quaternion.Euler(transform.localScale), Quaternion.Euler(1, 1, 1), 0.2f).eulerAngles;
            }
        }

        /// <summary>
        /// 淡出(透明度减小)
        /// </summary>
        private void fadeOut() {
            Color color = roundCorner.color;
            roundCorner.color = new Color(color.r, color.g, color.b, colorAlpha);
            if (roundCorner.color == null)
            {
                LogManager.Instance.log("image.color assign resource Error!!!", LogManager.LogLevel.Error);
                return;
            }
            color = content.color;
            content.color = new Color(color.r, color.g, color.b, colorAlpha);
            if (content.color == null)
            {
                LogManager.Instance.log("content.color assign resource Error!!!", LogManager.LogLevel.Error);
                return;
            }
            colorAlpha -= 0.02f;
            if (colorAlpha <= 0)
            {
                Destroy(gameObject);
            }
        }


        public void setText(string text) {
            content.text = text;
        }
    }
}