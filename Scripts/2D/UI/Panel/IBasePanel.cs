namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Constant;
    /// <summary>
    /// 面板基接口
    /// </summary>
    public interface IBasePanel : IPanelCommonButton
    {
        /// <summary>
        /// 初始化面板
        /// </summary>
        /// <param name="root">面板根</param>
        void Init(string root = TagConstant.UI_TAG);

        /// <summary>
        /// 进入面板
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 暂停面板
        /// </summary>
        void OnPause();

        /// <summary>
        /// 暂停后开启为运行
        /// </summary>
        void OnRun();

        /// <summary>
        /// 推出面板
        /// </summary>
        void OnExit();
    }
}