namespace LAB2D
{
    using UnityEngine.UI;

    /// <summary>
    /// 新游戏或者继续游戏面板
    /// </summary>
    public class NewOrContinuePanel : ABasePanel<NewOrContinuePanel>
    {
        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Init();
            Tool.GetComponentInChildren<Button>(this.Panel, "NewGame").onClick.AddListener(this.OnClick_NewGame);
            Tool.GetComponentInChildren<Button>(this.Panel, "ContinueGame").onClick.AddListener(this.OnClick_ContinueGame);
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

        private void OnClick_NewGame()
        {
            this.Controller.Close();
            GlobalData.IsNew = true;
            this.Controller.Show(CreateDataPanel.Instance);
        }

        private void OnClick_ContinueGame()
        {
            if (!ArchiveManager.Instance.HasCurrentArchive())
            {
                GlobalInit.Instance.ShowTip("没有存档!!!");
                return;
            }

            this.Controller.Close();
            GlobalData.IsNew = false;
            this.Controller.Show(AsyncProgressPanel.Instance);
            ArchiveManager.Instance.LoadCurrentArchive();
        }
    }
}
