using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class TipUI : MonoBehaviour
    {
        private float colorAlpha = 1; // ͸����
        private RoundCorner roundCorner; // ������Ϣ����
        private Text content; // ������Ϣ�ı�
        private float _recordTime = 0.0f; // ��¼ʱ��

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
        /// Color32͸����0~1
        /// Color͸����0~255
        /// ���ڿ����ܵ�Time.timeScale��Ӱ��
        /// ����ʹ��FixedUpdate,Time.deltaTime...
        /// </summary>
        void Update()
        {
            _recordTime += Time.deltaTime;
            if (_recordTime >= 2.0f) // ����󵭳�
            {
                fadeOut();
            }
            else {
                // �Ŵ�
                transform.localScale = Quaternion.Lerp(Quaternion.Euler(transform.localScale), Quaternion.Euler(1, 1, 1), 0.2f).eulerAngles;
            }
        }

        /// <summary>
        /// ����(͸���ȼ�С)
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