namespace LAB2D
{
    /// <summary>
    /// 进度条面板
    /// </summary>
    public class AsyncProgressPanel : BasePanel<AsyncProgressPanel>
    {
        public AsyncProgressPanel()
        {
            this.Name = "AsyncProgress";
            this.Open();
            this.Panel.transform.GetComponent<AsyncProgressUI>().Complete += this.Complate;
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

        private void Complate()
        {
            // 关闭该界面
            this.Controller.Close();
        }
    }
}