using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class PlayerInfoPanel : BasePanel<PlayerInfoPanel>
    {
        public PlayerInfoPanel()
        {
            Name = "PlayerInfo";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
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
    }
}