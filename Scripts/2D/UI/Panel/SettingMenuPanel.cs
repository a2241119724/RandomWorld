using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class SettingMenuPanel : BasePanel<SettingMenuPanel>
    {
        public SettingMenuPanel()
        {
            Name = "SettingMenu";
            setPanel();
            //cameras = Object.FindObjectsOfType(typeof(Camera), true);
            Tool.GetComponentInChildren<Toggle>(panel, "Toggle").onValueChanged.AddListener(OnClick_TogglePerspective);
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
            Tool.GetComponentInChildren<Slider>(panel, "Slider").onValueChanged.AddListener(OnClick_GameSpeed);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        /// <summary>
        /// ∑µªÿ”Œœ∑
        /// </summary>
        public void OnClick_BackGame()
        {
            controller.close();
        }

        /// <summary>
        /// «–ªª ”Ω«(2.5D)
        /// </summary>
        public void OnClick_TogglePerspective(bool is_2D)
        {
            PlayerManager.Instance.Mine.togglePerspective(is_2D);
        }

        public void OnClick_GameSpeed(float speed)
        {
            ForegroundPanel.Instance.TimeScale = speed;
        }
    }
}