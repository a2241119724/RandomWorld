namespace LAB2D
{
    using UnityEngine.UI;
    using static LAB2D.TileMap;

    /// <summary>
    /// 新游戏或者继续游戏面板
    /// </summary>
    public class NewOrContinuePanel : BasePanel<NewOrContinuePanel>
    {
        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Open();
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
            TileMapData data = Tool.LoadDataByBinary<TileMapData>(GlobalData.ConfigFile.GetPath("TileMap"));
            if (data == null)
            {
                GlobalInit.Instance.ShowTip("没有存档!!!");
                return;
            }

            this.Controller.Close();
            GlobalData.IsNew = false;
            this.Controller.Show(AsyncProgressPanel.Instance);
            AsyncProgressUI.Instance.AddTotal(ASaveData.Instances.Count + AMonoSaveData.Instances.Count);

            // 加载数据之前,线实例化
            PlayerManager.Instance.Init();
            foreach (ASaveData saveData in ASaveData.Instances)
            {
                if (saveData == null)
                {
                    continue;
                }

                AsyncProgressUI.Instance.SetTip(saveData.ToString());
                saveData.LoadData();
                AsyncProgressUI.Instance.AddOneProcess();
            }

            foreach (AMonoSaveData saveData in AMonoSaveData.Instances)
            {
                if (saveData == null)
                {
                    continue;
                }

                AsyncProgressUI.Instance.SetTip(saveData.ToString());
                saveData.LoadData();
                AsyncProgressUI.Instance.AddOneProcess();
            }
        }
    }
}
