namespace LAB2D
{
    using UnityEngine.UI;

    /// <summary>
    /// 玩家信息面面板
    /// </summary>
    public class PlayerInfoPanel : BasePanel<PlayerInfoPanel>
    {
        public PlayerInfoPanel()
        {
            this.Name = "PlayerInfo";
            this.Open();
            Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_BackGame);
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
        public void OnClick_BackGame()
        {
            this.Controller.Close();
        }
    }
}