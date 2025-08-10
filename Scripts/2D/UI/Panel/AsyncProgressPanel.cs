namespace LAB2D
{
    /// <summary>
    /// 进度条面板
    /// </summary>
    public class AsyncProgressPanel : ABasePanel<AsyncProgressPanel>
    {
        public AsyncProgressPanel()
        {
            this.Name = "AsyncProgress";
            this.Init();
            this.Panel.transform.GetComponent<AsyncProgressUI>().Complete += () =>
            {
                this.Controller.Close();

                if (GlobalData.IsNew)
                {
                    // 新游戏, 创建玩家在随机位置, 否则在角色管理中创建
                    PlayerManager.Instance.Create();
                }
            };
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

            // 进入游戏主界面
            this.Controller.Show(ForegroundPanel.Instance);
        }
    }
}