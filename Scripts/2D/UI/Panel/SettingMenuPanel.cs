namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 设置菜单面板
    /// </summary>
    public class SettingMenuPanel : ABasePanel<SettingMenuPanel>
    {
        public SettingMenuPanel()
        {
            this.Name = "SettingMenu";
            this.Init();

            // cameras = Object.FindObjectsOfType(typeof(Camera), true);
            Tool.GetComponentInChildren<Transform>(this.Panel, "Angle").Find("Toggle")
                .GetComponent<Toggle>().onValueChanged.AddListener(this.OnClick_TogglePerspective);
            Tool.GetComponentInChildren<Transform>(this.Panel, "Edge").Find("Toggle")
               .GetComponent<Toggle>().onValueChanged.AddListener(this.OnClick_ToggleEdgeMove);
            Tool.GetComponentInChildren<Transform>(this.Panel, "Speed").Find("Slider")
               .GetComponent<Slider>().onValueChanged.AddListener(this.OnClick_GameSpeed);
            Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_Back);
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
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        /// <summary>
        /// 切换视角(2.5D)
        /// </summary>
        private void OnClick_TogglePerspective(bool is2D)
        {
            PlayerManager.Instance.Mine.TogglePerspective(is2D);
        }

        /// <summary>
        /// 切换边缘移动
        /// </summary>
        private void OnClick_ToggleEdgeMove(bool isEdge)
        {
            CameraMove.IsEdgeMode = isEdge;
        }

        private void OnClick_GameSpeed(float speed)
        {
            ForegroundPanel.Instance.TimeScale = speed;
        }
    }
}