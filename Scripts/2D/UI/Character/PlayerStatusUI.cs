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
                Debug.LogError("HpValue Not Found!!!");
                return;
            }
            MpValue = transform.Find("State/Mp/MpValue").GetComponent<Text>();
            if (MpValue == null)
            {
                Debug.LogError("MpValue Not Found!!!");
                return;
            }
            LevelValue = transform.Find("State/Level/LevelValue").GetComponent<Text>();
            if (LevelValue == null)
            {
                Debug.LogError("LevelValue Not Found!!!");
                return;
            }
            HpBar = transform.Find("State/Hp/HpBar").GetComponent<Slider>();
            if (HpBar == null)
            {
                Debug.LogError("HpBar Not Found!!!");
                return;
            }
            MpBar = transform.Find("State/Mp/MpBar").GetComponent<Slider>();
            if (MpBar == null)
            {
                Debug.LogError("MpBar Not Found!!!");
                return;
            }
            LevelBar = transform.Find("State/Level/LevelBar").GetComponent<Slider>();
            if (LevelBar == null)
            {
                Debug.LogError("LevelBar Not Found!!!");
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
