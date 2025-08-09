namespace LAB2D
{
    using UnityEngine.UI;

    /// <summary>
    /// 玩家信息面面板
    /// </summary>
    public class PlayerInfoPanel : ABasePanel<PlayerInfoPanel>
    {
        public PlayerInfoPanel()
        {
            this.Name = "PlayerInfo";
            this.Init();
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
    }
}