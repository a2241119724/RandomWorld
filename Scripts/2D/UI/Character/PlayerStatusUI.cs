using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class PlayerStatusUI : MonoBehaviour
    {
        public static PlayerStatusUI Instance { get; private set; }

        private Text HpValue, MpValue, LevelValue; // 显示玩家血量,蓝量,等级
        private Slider HpBar, MpBar, LevelBar; // 玩家血量,蓝量,等级进度条

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            HpValue = transform.Find("State/Hp/HpValue").GetComponent<Text>();
            if (HpValue == null)
            {
                LogManager.Instance.log("HpValue Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            MpValue = transform.Find("State/Mp/MpValue").GetComponent<Text>();
            if (MpValue == null)
            {
                LogManager.Instance.log("MpValue Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            LevelValue = transform.Find("State/Level/LevelValue").GetComponent<Text>();
            if (LevelValue == null)
            {
                LogManager.Instance.log("LevelValue Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            HpBar = transform.Find("State/Hp/HpBar").GetComponent<Slider>();
            if (HpBar == null)
            {
                LogManager.Instance.log("HpBar Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            MpBar = transform.Find("State/Mp/MpBar").GetComponent<Slider>();
            if (MpBar == null)
            {
                LogManager.Instance.log("MpBar Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            LevelBar = transform.Find("State/Level/LevelBar").GetComponent<Slider>();
            if (LevelBar == null)
            {
                LogManager.Instance.log("LevelBar Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        public void updatePlayerState(float Hp, float maxHp, int Mp, int maxMp, int Level, int currentExperience, int maxExperience)
        {
            // 显示血量,蓝量,经验值
            HpValue.text = " Hp               " + Hp + "/" + maxHp;
            MpValue.text = " Mp               " + Mp + "/" + maxMp;
            LevelValue.text = " Level:" + Level + "           " + currentExperience + "/" + maxExperience;
            HpBar.value = Hp / (float)maxHp;
            MpBar.value = Mp / (float)maxMp;
            LevelBar.value = currentExperience / (float)maxExperience;
        }
    }
}
