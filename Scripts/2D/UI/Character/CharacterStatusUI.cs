namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 角色状态 UI
    /// </summary>
    public class CharacterStatusUI : MonoBehaviour
    {
        private Slider slider; // 血条进度条
        private Text text; // 血量显示

        /// <summary>
        /// 更新敌人身体状况
        /// </summary>
        /// <param name="hp">当前血量</param>
        /// <param name="maxHp">最大血量</param>
        public void UpdateStatus(float hp, float maxHp)
        {
            // 敌人血条
            this.slider.value = hp / (float)maxHp;
            this.text.text = System.Math.Round(hp, 1) + "/" + maxHp;
        }

        private void Awake()
        {
            this.slider = this.transform.Find("HpBar").GetComponent<Slider>();
            if (this.slider == null)
            {
                LogManager.Instance.Log("slider Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.text = this.transform.Find("HpCount").GetComponent<Text>();
            if (this.text == null)
            {
                LogManager.Instance.Log("text Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        private void Update()
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}
