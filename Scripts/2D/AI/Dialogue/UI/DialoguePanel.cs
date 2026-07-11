namespace LAB2D.AI.Dialogue.UI
{
    using LAB2D;
    using LAB2D.UI.Panel;
    using UnityEngine.UI;

    /// <summary>
    /// NPC 对话面板（PanelController 栈管理）
    /// </summary>
    public class DialoguePanel : ABasePanel<DialoguePanel>
    {
        public DialoguePanel()
        {
            this.Name = "DialoguePanelUI";
            DialoguePanelUI.Ensure();
            this.Init();
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            // 面板由外部调用 DialoguePanelUI.Open 来激活
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.OnBackClicked();
        }

        private void OnBackClicked()
        {
            if (DialoguePanelUI.Instance != null)
            {
                DialoguePanelUI.Instance.Close();
            }

            this.Controller.Close();
        }
    }
}
