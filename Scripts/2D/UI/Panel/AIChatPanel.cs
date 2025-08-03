namespace LAB2D
{
    using UnityEngine.UI;

    /// <summary>
    /// AI聊天面板
    /// </summary>
    public class AIChatPanel : BasePanel<AIChatPanel>
    {
        public AIChatPanel()
        {
            this.Name = "AIChat";
            this.Init();
            Tool.GetComponentInChildren<Button>(this.Panel, "Send").onClick.AddListener(this.OnClick_Send);
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        public void OnClick_Send()
        {
            AIChatUI.Instance.Send();
        }
    }
}
