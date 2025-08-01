namespace LAB2D
{
    /// <summary>
    /// 面板基接口
    /// </summary>
    public interface IBasePanel
    {
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