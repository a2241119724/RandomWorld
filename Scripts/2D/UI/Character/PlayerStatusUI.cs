namespace LAB2D.UI.Character
{
    using LAB2D;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 玩家状态 UI
    /// </summary>
    public class PlayerStatusUI : MonoBehaviour
    {
        private Text hp; // 显示玩家血量,蓝量,等级,经验
        private Text mp;
        private Text level;
        private Text experience;
        private Slider barHp; // 玩家血量,蓝量,等级进度条
        private Slider barMp;
        private Slider barLevel;

        /// <summary>
        /// 单例
        /// </summary>
        public static PlayerStatusUI Instance { get; private set; }

        /// <summary>
        /// 更新玩家状态
        /// </summary>
        /// <param name="hp">血量</param>
        /// <param name="maxHp">最大血量</param>
        /// <param name="mp">蓝量</param>
        /// <param name="maxMp">最大蓝量</param>
        /// <param name="level">等级</param>
        /// <param name="currentExperience">当前经验</param>
        /// <param name="maxExperience">当前等级的最大经验</param>
        public void UpdatePlayerState(float hp, float maxHp, int mp, int maxMp, int level, int currentExperience, int maxExperience)
        {
            // 显示血量,蓝量,经验值
            this.hp.text = $"{hp}/{maxHp} ";
            this.mp.text = $"{mp}/{maxMp} ";
            this.level.text = $" Level:{level}";
            this.experience.text = $"{currentExperience}/{maxExperience} ";
            this.barHp.value = hp / (float)maxHp;
            this.barMp.value = mp / (float)maxMp;
            this.barLevel.value = currentExperience / (float)maxExperience;
        }

        public void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            this.hp = this.transform.Find("State/Hp/Value").GetComponent<Text>();
            if (this.hp == null)
            {
                LogManager.Instance.Log("Hp/Value Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.mp = this.transform.Find("State/Mp/Value").GetComponent<Text>();
            if (this.mp == null)
            {
                LogManager.Instance.Log("Mp/Value Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.level = this.transform.Find("State/Level/Value").GetComponent<Text>();
            if (this.level == null)
            {
                LogManager.Instance.Log("Level/Value Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.experience = this.transform.Find("State/Level/Experience").GetComponent<Text>();
            if (this.experience == null)
            {
                LogManager.Instance.Log("Experience Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.barHp = this.transform.Find("State/Hp/Bar").GetComponent<Slider>();
            if (this.barHp == null)
            {
                LogManager.Instance.Log("Hp/Bar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.barMp = this.transform.Find("State/Mp/Bar").GetComponent<Slider>();
            if (this.barMp == null)
            {
                LogManager.Instance.Log("Mp/Bar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.barLevel = this.transform.Find("State/Level/Bar").GetComponent<Slider>();
            if (this.barLevel == null)
            {
                LogManager.Instance.Log("Level/Bar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }
        }
    }
}
