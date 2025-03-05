using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class CharacterStatusUI : MonoBehaviour
    {
        private Slider slider; // Ѫ��������
        private Text text; // Ѫ����ʾ

        private void Awake()
        {
            slider = transform.Find("HpBar").GetComponent<Slider>();
            if (slider == null)
            {
                LogManager.Instance.log("slider Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            text = transform.Find("HpCount").GetComponent<Text>();
            if (text == null)
            {
                LogManager.Instance.log("text Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        void Update()
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        /// <summary>
        /// ���µ�������״��
        /// </summary>
        public void updateStatus(float Hp, float maxHp)
        {
            // ����Ѫ��
            slider.value = Hp / (float)maxHp;
            text.text = System.Math.Round(Hp, 1) + "/" + maxHp;
        }
    }
}
