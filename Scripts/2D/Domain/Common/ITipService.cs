namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 提示消息显示服务接口。
    /// 领域层通过此接口显示提示，不依赖 Unity UI 或 GlobalInit。
    /// </summary>
    public interface ITipService
    {
        /// <summary>
        /// 显示一条提示消息。
        /// </summary>
        /// <param name="text">提示文本。</param>
        void ShowTip(string text);
    }
}
