namespace LAB2D.UI.Character
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 玩家状态 UI — 通过 EventBus 订阅 PlayerStatusChangedEvent 实现解耦更新。
    /// </summary>
    public class PlayerStatusUI : MonoBehaviour
    {
        private Text hp;
        private Text mp;
        private Text level;
        private Text experience;
        private Slider barHp;
        private Slider barMp;
        private Slider barLevel;

        public static PlayerStatusUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            EventBus.Instance.Subscribe<PlayerStatusChangedEvent>(this.OnPlayerStatusChanged);
        }

        public void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<PlayerStatusChangedEvent>(this.OnPlayerStatusChanged);
        }

        private void OnPlayerStatusChanged(PlayerStatusChangedEvent e)
        {
            this.hp.text = $"{e.Hp}/{e.MaxHp} ";
            this.mp.text = $"{e.Mp}/{e.MaxMp} ";
            this.level.text = $" Level:{e.Level}";
            this.experience.text = $"{e.CurExperience}/{e.MaxExperience} ";
            this.barHp.value = e.Hp / e.MaxHp;
            this.barMp.value = e.Mp / (float)e.MaxMp;
            this.barLevel.value = e.CurExperience / (float)e.MaxExperience;
        }

        private void OnEnable()
        {
            this.hp = this.transform.Find("State/Hp/Value").GetComponent<Text>();
            if (this.hp == null)
            {
                AWorkerTask.LogProvider("Hp/Value Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.mp = this.transform.Find("State/Mp/Value").GetComponent<Text>();
            if (this.mp == null)
            {
                AWorkerTask.LogProvider("Mp/Value Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.level = this.transform.Find("State/Level/Value").GetComponent<Text>();
            if (this.level == null)
            {
                AWorkerTask.LogProvider("Level/Value Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.experience = this.transform.Find("State/Level/Experience").GetComponent<Text>();
            if (this.experience == null)
            {
                AWorkerTask.LogProvider("Experience Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.barHp = this.transform.Find("State/Hp/Bar").GetComponent<Slider>();
            if (this.barHp == null)
            {
                AWorkerTask.LogProvider("Hp/Bar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.barMp = this.transform.Find("State/Mp/Bar").GetComponent<Slider>();
            if (this.barMp == null)
            {
                AWorkerTask.LogProvider("Mp/Bar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.barLevel = this.transform.Find("State/Level/Bar").GetComponent<Slider>();
            if (this.barLevel == null)
            {
                AWorkerTask.LogProvider("Level/Bar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }
        }
    }
}
