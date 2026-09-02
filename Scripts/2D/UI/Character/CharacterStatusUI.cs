namespace LAB2D.UI.Character
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 角色状态 UI — 血条走 BarValueTransition 过渡播放队列，文字保持瞬时更新。
    /// </summary>
    public class CharacterStatusUI : MonoBehaviour
    {
        private Slider slider; // 血条进度条
        private Text text; // 血量显示
        private BarValueTransition transition; // 血条过渡播放队列

        /// <summary>
        /// 更新敌人身体状况
        /// </summary>
        /// <param name="hp">当前血量</param>
        /// <param name="maxHp">最大血量</param>
        public void UpdateStatus(float hp, float maxHp)
        {
            // 敌人血条：数字瞬时呈现权威值，条平滑过渡（首次自动吸附）
            this.text.text = System.Math.Round(hp, 1) + "/" + maxHp;
            this.transition.SetTarget(MathHelper.GetSafeRatio(hp, maxHp));
        }

        public void Awake()
        {
            this.slider = this.transform.Find("HpBar").GetComponent<Slider>();
            if (this.slider == null)
            {
                AWorkerTask.LogProvider("slider Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.transition = new BarValueTransition(
                () => this.slider.value,
                value => this.slider.value = value,
                duration: 0.3f);

            this.text = this.transform.Find("HpCount").GetComponent<Text>();
            if (this.text == null)
            {
                AWorkerTask.LogProvider("text Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }
        }

        public void Update()
        {
            // 仅在父级旋转（如敌人朝向转向）波及时归零抵消；父未转时跳过写，
            // 避免 rotation setter（无值比较）每帧脏化头顶 Canvas 的 transform
            if (this.transform.rotation != Quaternion.identity)
            {
                this.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            this.transition?.Tick(Time.deltaTime);
        }
    }
}
