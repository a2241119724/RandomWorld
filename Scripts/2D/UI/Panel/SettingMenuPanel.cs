using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class SettingMenuPanel : BasePanel<SettingMenuPanel>
    {
        private Canvas rootCanvas; // UI root

        public SettingMenuPanel()
        {
            Name = "SettingMenu";
            setPanel();
            //cameras = Object.FindObjectsOfType(typeof(Camera), true);
            rootCanvas = GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG_ROOT).GetComponent<Canvas>();
            Toggle toggle = Tool.GetComponentInChildren<Toggle>(panel, "Toggle");
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
            toggle.onValueChanged.AddListener(OnClick_TogglePerspective);
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
    }
}