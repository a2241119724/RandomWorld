namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using UnityEngine.UI;

    /// <summary>
    /// AI聊天面板
    /// </summary>
    public class AIChatPanel : ABasePanel<AIChatPanel>
    {
        public AIChatPanel()
        {
            this.Name = "AIChat";
            this.Init();
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Send").onClick.AddListener(this.OnClick_Send);
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        public void OnClick_Send()
        {
            ServiceLocator.Get<AIChatUI>().Send();
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }
    }
}
