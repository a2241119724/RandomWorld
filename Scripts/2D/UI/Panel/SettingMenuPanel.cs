namespace LAB2D
{
    using UnityEngine.UI;

    /// <summary>
    /// 设置菜单面板
    /// </summary>
    public class SettingMenuPanel : BasePanel<SettingMenuPanel>
    {
        public SettingMenuPanel()
        {
            this.Name = "SettingMenu";
            this.OpenPanel();

            // cameras = Object.FindObjectsOfType(typeof(Camera), true);
            Tool.GetComponentInChildren<Toggle>(this.Panel, "Toggle").onValueChanged.AddListener(this.OnClick_TogglePerspective);
            Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_BackGame);
            Tool.GetComponentInChildren<Slider>(this.Panel, "Slider").onValueChanged.AddListener(this.OnClick_GameSpeed);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <summary>
        /// 返回游戏
        /// </summary>
        private void OnClick_BackGame()
        {
            this.Controller.Close();
        }

        /// <summary>
        /// 切换视角(2.5D)
        /// </summary>
        private void OnClick_TogglePerspective(bool is_2D)
        {
            PlayerManager.Instance.Mine.TogglePerspective(is_2D);
        }

        private void OnClick_GameSpeed(float speed)
        {
            ForegroundPanel.Instance.TimeScale = speed;
        }
    }
}